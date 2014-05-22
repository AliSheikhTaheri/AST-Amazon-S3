namespace AST.S3.Helper
{
    using System;
    using System.Configuration;

    public class GlobalHelper
    {
        /// <summary>
        /// Read CDN Domain from web.config
        /// </summary>
        /// <returns>returns CDN url</returns>
        public static string GetCdnDomain()
        {
            var output = string.Empty;
            if (!string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["useCDN"]) && string.Equals(ConfigurationManager.AppSettings["useCDN"], "true", StringComparison.CurrentCulture))
            {
                if (!string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["cdnDomain"]))
                {
                    output = ConfigurationManager.AppSettings["cdnDomain"];
                }
            }

            return output;
        }
    }
}
