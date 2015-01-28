namespace AST.S3.Helper
{
    using AST.S3.Model;

    using Umbraco.Web;

    public static class MediaHelper
    {
        public static S3PublishedContent ParseMedia(string mediaId, UmbracoHelper umbracoHelper)
        {
            var media = umbracoHelper.TypedMedia(mediaId);
            if (media != null)
            {
                var output = new S3PublishedContent(media);
                output.Url = string.Format("{0}{1}", GlobalHelper.GetCdnDomain(), output.Url());
                return output;
            }

            return null;
        }
    }
}
