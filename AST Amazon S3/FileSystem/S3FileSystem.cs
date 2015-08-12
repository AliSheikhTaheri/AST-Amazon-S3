namespace AST.S3.FileSystem
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;

    using Umbraco.Core;
    using Umbraco.Core.IO;
    using Umbraco.Core.Logging;

    public class S3FileSystem : IFileSystem
    {
        private readonly string _rootUrl;

        private AmazonS3FileSystem amazonS3FileSystem;

        #region constructors

        public S3FileSystem(string virtualRoot, string awsAccessKey, string awsSecretKey, string awsBucketName, string awsSaveMediaToS3)
        {
            if (virtualRoot == null)
            {
                throw new ArgumentNullException("virtualRoot");
            }

            if (!virtualRoot.StartsWith("~/"))
            {
                throw new ArgumentException("The virtualRoot argument must be a virtual path and start with '~/'");
            }

            RootPath = IOHelper.MapPath(virtualRoot);
            _rootUrl = IOHelper.ResolveUrl(virtualRoot);

            AwsAccessKey = awsAccessKey;
            AwsSecretKey = awsSecretKey;
            AwsBucketName = awsBucketName;
            AwsSaveMediaToS3 = bool.Parse(awsSaveMediaToS3);

            if (AwsIsValid)
            {
                amazonS3FileSystem = new AmazonS3FileSystem(AwsAccessKey, AwsSecretKey, AwsBucketName);
            }
        }

        public S3FileSystem(string rootPath, string rootUrl)
        {
            if (string.IsNullOrEmpty(rootPath))
            {
                throw new ArgumentException("The argument 'rootPath' cannot be null or empty.");
            }

            if (string.IsNullOrEmpty(rootUrl))
            {
                throw new ArgumentException("The argument 'rootUrl' cannot be null or empty.");
            }

            if (rootPath.StartsWith("~/"))
            {
                throw new ArgumentException("The rootPath argument cannot be a virtual path and cannot start with '~/'");
            }

            RootPath = rootPath;
            _rootUrl = rootUrl;
        }

        #endregion

        internal string RootPath { get; private set; }

        private string AwsAccessKey { get; set; }

        private string AwsSecretKey { get; set; }

        private string AwsBucketName { get; set; }

        private bool AwsSaveMediaToS3 { get; set; }

        private bool AwsIsValid
        {
            get
            {
                return !string.IsNullOrWhiteSpace(AwsAccessKey) && !string.IsNullOrWhiteSpace(AwsBucketName)
                       && !string.IsNullOrWhiteSpace(AwsBucketName);
            }
        }

        #region Methods

        public void AddFile(string path, Stream stream)
        {
            AddFile(path, stream, true);
        }

        public void AddFile(string path, Stream stream, bool overrideIfExists)
        {
            if (FileExists(path) && !overrideIfExists)
            {
                throw new InvalidOperationException(string.Format("A file at path '{0}' already exists", path));
            }

            EnsureDirectory(Path.GetDirectoryName(path));

            if (stream.CanSeek)
            {
                stream.Seek(0, 0);
            }

            using (var destination = (Stream)File.Create(GetFullPath(path)))
            {
                stream.CopyTo(destination);
            }

            if (AwsIsValid && AwsSaveMediaToS3)
            {
                amazonS3FileSystem.SaveFile(GetFullPath(path), GetFolderStructureForAmazon(path), GetFileNameFromPath(path));
            }
        }

        public void DeleteFile(string path)
        {
            if (!FileExists(path))
            {
                return;
            }

            try
            {
                File.Delete(GetFullPath(path));

                // delete an empty folder bug introduced in v6
                var curDirectory = Path.GetDirectoryName(GetFullPath(path));
                if (curDirectory != null && Directory.Exists(curDirectory))
                {
                    if (!Directory.GetFileSystemEntries(curDirectory).Any())
                    {
                        DeleteDirectory(Path.GetDirectoryName(GetFullPath(path)));
                    }
                }

                if (AwsIsValid && AwsSaveMediaToS3)
                {
                    amazonS3FileSystem.DeleteFile(GetFolderStructureForAmazon(path), GetFileNameFromPath(path));
                }
            }
            catch (FileNotFoundException ex)
            {
                LogHelper.Info<S3FileSystem>(string.Format("DeleteFile failed with FileNotFoundException: {0}", ex.InnerException));
            }
        }

        public void DeleteDirectory(string path)
        {
            DeleteDirectory(path, false);
        }

        public void DeleteDirectory(string path, bool recursive)
        {
            if (!DirectoryExists(path))
            {
                return;
            }

            try
            {
                Directory.Delete(GetFullPath(path), recursive);

                if (AwsIsValid && AwsSaveMediaToS3)
                {
                    amazonS3FileSystem.DeleteFolder(GetFolderStructureForAmazon(path));
                }
            }
            catch (DirectoryNotFoundException ex)
            {
                LogHelper.Error<S3FileSystem>("Directory not found", ex);
            }
        }

        public IEnumerable<string> GetDirectories(string path)
        {
            path = EnsureTrailingSeparator(GetFullPath(path));

            try
            {
                if (Directory.Exists(path))
                {
                    return Directory.EnumerateDirectories(path).Select(GetRelativePath);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                LogHelper.Error<S3FileSystem>("Not authorized to get directories", ex);
            }
            catch (DirectoryNotFoundException ex)
            {
                LogHelper.Error<S3FileSystem>("Directory not found", ex);
            }

            return Enumerable.Empty<string>();
        }

        public bool DirectoryExists(string path)
        {
            return Directory.Exists(GetFullPath(path));
        }

        public IEnumerable<string> GetFiles(string path)
        {
            return GetFiles(path, "*.*");
        }

        public IEnumerable<string> GetFiles(string path, string filter)
        {
            path = EnsureTrailingSeparator(GetFullPath(path));

            try
            {
                if (Directory.Exists(path))
                {
                    return Directory.EnumerateFiles(path, filter).Select(GetRelativePath);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                LogHelper.Error<S3FileSystem>("Not authorized to get directories", ex);
            }
            catch (DirectoryNotFoundException ex)
            {
                LogHelper.Error<S3FileSystem>("Directory not found", ex);
            }

            return Enumerable.Empty<string>();
        }

        public Stream OpenFile(string path)
        {
            var fullPath = GetFullPath(path);
            return File.OpenRead(fullPath);
        }

        public bool FileExists(string path)
        {
            return File.Exists(GetFullPath(path));
        }

        public string GetRelativePath(string fullPathOrUrl)
        {
            var relativePath = fullPathOrUrl
                .TrimStart(_rootUrl)
                .Replace('/', Path.DirectorySeparatorChar)
                .TrimStart(RootPath)
                .TrimStart(Path.DirectorySeparatorChar);

            return relativePath;
        }

        public string GetFullPath(string path)
        {
            return !path.StartsWith(RootPath)
                ? Path.Combine(RootPath, path)
                : path;
        }

        public string GetUrl(string path)
        {
            return _rootUrl.TrimEnd("/") + "/" + path
                .TrimStart(Path.DirectorySeparatorChar)
                .Replace(Path.DirectorySeparatorChar, '/')
                .TrimEnd("/");
        }

        public DateTimeOffset GetLastModified(string path)
        {
            return DirectoryExists(path)
                ? new DirectoryInfo(GetFullPath(path)).LastWriteTimeUtc
                : new FileInfo(GetFullPath(path)).LastWriteTimeUtc;
        }

        public DateTimeOffset GetCreated(string path)
        {
            return DirectoryExists(path)
                ? Directory.GetCreationTimeUtc(GetFullPath(path))
                : File.GetCreationTimeUtc(GetFullPath(path));
        }

        #endregion

        #region Helper Methods

        protected virtual void EnsureDirectory(string path)
        {
            path = GetFullPath(path);
            Directory.CreateDirectory(path);
        }

        protected string EnsureTrailingSeparator(string path)
        {
            if (!path.EndsWith(Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal))
            {
                path = path + Path.DirectorySeparatorChar;
            }

            return path;
        }

        protected string GetFileNameFromPath(string path)
        {
            return Path.GetFileName(path);
        }

        protected string GetFolderStructureForAmazon(string path)
        {
            var output = Path.Combine(_rootUrl, path.Split('\\').Count() > 1 ? Path.GetDirectoryName(path) : path);

            if (output.StartsWith("/"))
            {
                output = output.Substring(1);
            }

            if (!output.EndsWith("/"))
            {
                output += "/";
            }

            return output;
        }

        #endregion
    }
}
