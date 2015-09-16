using System.Reflection;
using System.Runtime.Remoting;
using Amazon;

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

        internal string RootPath { get; private set; }

        private string AwsAccessKey { get; set; }

        private string AwsSecretKey { get; set; }

        private string AwsBucketName { get; set; }

        private bool AwsSaveMediaToS3 { get; set; }

        private RegionEndpoint AwsRegion { get; set; }

        private readonly AmazonS3FileSystem _amazonS3FileSystem;

        #region constructors

        public S3FileSystem(string virtualRoot, string awsAccessKey, string awsSecretKey, string awsBucketName, string awsSaveMediaToS3, string awsRegion)
        {
            if (virtualRoot == null)
            {
                throw new ArgumentNullException(nameof(virtualRoot));
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

            // [ML] - This isnt ideal, but is the best way i an think of making this paramaterized in the IFileSystemProviderManager

            FieldInfo field = null;

            if (!string.IsNullOrWhiteSpace(awsRegion))
            {
                field = typeof(RegionEndpoint).GetField(awsRegion, BindingFlags.Static | BindingFlags.Public);

                if (field == null)
                {
                    throw new ArgumentException($"No Field found on '{typeof(RegionEndpoint).Name}' with the name '{awsRegion}'");
                }
            }

            AwsRegion = field?.GetValue(null) as RegionEndpoint ?? RegionEndpoint.USWest1;

            if (AwsIsValid)
            {
                _amazonS3FileSystem = new AmazonS3FileSystem(AwsAccessKey, AwsSecretKey, AwsBucketName, AwsRegion);
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

        private bool AwsIsValid => !string.IsNullOrWhiteSpace(AwsAccessKey) && !string.IsNullOrWhiteSpace(AwsBucketName) && !string.IsNullOrWhiteSpace(AwsBucketName)  && AwsRegion != null;

        #region Methods

        public void AddFile(string path, Stream stream) => AddFile(path, stream, true);

        public void AddFile(string path, Stream stream, bool overrideIfExists)
        {
            if (FileExists(path) && !overrideIfExists)
            {
                throw new InvalidOperationException($"A file at path '{path}' already exists");
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
                _amazonS3FileSystem.SaveFile(GetFullPath(path), GetFolderStructureForAmazon(path), GetFileNameFromPath(path));
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
                    _amazonS3FileSystem.DeleteFile(GetFolderStructureForAmazon(path), GetFileNameFromPath(path));
                }
            }
            catch (FileNotFoundException ex)
            {
                LogHelper.Info<S3FileSystem>($"DeleteFile failed with FileNotFoundException: {ex.InnerException}");
            }
        }

        public void DeleteDirectory(string path) => DeleteDirectory(path, false);

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
                    _amazonS3FileSystem.DeleteFolder(GetFolderStructureForAmazon(path));
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

        public bool DirectoryExists(string path) =>  Directory.Exists(GetFullPath(path));

        public IEnumerable<string> GetFiles(string path) => GetFiles(path, "*.*");

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

        public Stream OpenFile(string path) => File.OpenRead(GetFullPath(path));

        public bool FileExists(string path) => File.Exists(GetFullPath(path));

        public string GetRelativePath(string fullPathOrUrl) => fullPathOrUrl.TrimStart(_rootUrl).Replace('/', Path.DirectorySeparatorChar).TrimStart(RootPath).TrimStart(Path.DirectorySeparatorChar);

        public string GetFullPath(string path) => !path.StartsWith(RootPath) ? Path.Combine(RootPath, path) : path;
        
        public string GetUrl(string path) => _rootUrl.TrimEnd("/") + "/" + path.TrimStart(Path.DirectorySeparatorChar).Replace(Path.DirectorySeparatorChar, '/').TrimEnd("/");

        public DateTimeOffset GetLastModified(string path) => DirectoryExists(path) ? new DirectoryInfo(GetFullPath(path)).LastWriteTimeUtc : new FileInfo(GetFullPath(path)).LastWriteTimeUtc;

        public DateTimeOffset GetCreated(string path) => DirectoryExists(path) ? Directory.GetCreationTimeUtc(GetFullPath(path)) : File.GetCreationTimeUtc(GetFullPath(path));

        #endregion

        #region Helper Methods

        protected virtual void EnsureDirectory(string path) => Directory.CreateDirectory(GetFullPath(path));
        
        protected string EnsureTrailingSeparator(string path)
        {
            if (!path.EndsWith(Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal))
            {
                path = path + Path.DirectorySeparatorChar;
            }

            return path;
        }

        protected string GetFileNameFromPath(string path) => Path.GetFileName(path);

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
