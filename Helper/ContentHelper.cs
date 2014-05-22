namespace AST.S3.Helper
{
    using System.Text.RegularExpressions;

    public static class ContentHelper
    {
        public static string ParseContent(string input)
        {
            if (string.IsNullOrEmpty(GlobalHelper.GetCdnDomain()))
            {
                return input;
            }

            var output = ParseMediaLinks(input);
            return output;
        }

        private static string ParseMediaLinks(string input)
        {
            var output = input;

            var matchesImages = Regex.Matches(input, @"<img(?<attr1>.*?)src=""(?<url>/media/.*?)""(?<attr2>.*?)/>");
            foreach (Match match in matchesImages)
            {
                string url = match.Groups["url"].Value;

                var outputLink = string.Format(@"<img{0}src=""{1}{2}""{3} />", match.Groups["attr1"].Value, GlobalHelper.GetCdnDomain(), url, match.Groups["attr2"].Value);
                output = output.Replace(match.Groups[0].Value, outputLink);
            }

            var matchesLinks = Regex.Matches(input, @"<a(?<attr1>.*?)href=""(?<url>/media/.*?)""(?<attr2>.*?)>(?<content>.*?)</a>");
            foreach (Match match in matchesLinks)
            {
                string url = match.Groups["url"].Value, content = match.Groups["content"].Value;

                if (IsPlainText(content))
                {
                    var outputLink = string.Format(@"<a{0}href=""{1}{2}""{3}>{4}</a>", match.Groups["attr1"].Value, GlobalHelper.GetCdnDomain(), url, match.Groups["attr2"].Value, content);
                    output = output.Replace(match.Groups[0].Value, outputLink);
                }
            }

            return output;
        }

        private static bool IsPlainText(string input)
        {
            return !Regex.IsMatch(input, "<[a-z].*>", RegexOptions.IgnoreCase);
        }
    }
}
