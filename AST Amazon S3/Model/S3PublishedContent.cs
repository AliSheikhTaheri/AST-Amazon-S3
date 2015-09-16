namespace AST.S3.Model
{
    using Umbraco.Core.Models;
    using Umbraco.Web.Models;

    public class S3PublishedContent : DynamicPublishedContent
    {
        public S3PublishedContent(IPublishedContent content) : base(content)
        {
            this.Url = Url;
        }

        public new string Url { get; set; }
    }
}
