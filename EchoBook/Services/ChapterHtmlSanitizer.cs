using HtmlAgilityPack;

namespace EchoBook.Services
{
    public class ChapterHtmlSanitizer
    {
        public static string ExtractAndSanitize(string rawHtml, Func<string, string>? resolveAssetUrl)
        {
            if (string.IsNullOrWhiteSpace(rawHtml))
                return string.Empty;

            var doc = new HtmlDocument();
            doc.LoadHtml(rawHtml);

            // Bỏ các thẻ script, style, iframe, meta, link
            var unsafeNodes = doc.DocumentNode.SelectNodes("//script|//style|//iframe|//meta|//link");
            if (unsafeNodes != null)
            {
                foreach (var node in unsafeNodes)
                {
                    node.Remove();
                }
            }

            // Cập nhật lại đường dẫn ảnh nếu có
            var imgNodes = doc.DocumentNode.SelectNodes("//img");
            if (imgNodes != null && resolveAssetUrl != null)
            {
                foreach (var img in imgNodes)
                {
                    var src = img.GetAttributeValue("src", string.Empty);
                    if (!string.IsNullOrWhiteSpace(src))
                    {
                        var newSrc = resolveAssetUrl(src);
                        img.SetAttributeValue("src", newSrc);
                    }
                }
            }

            var bodyNode = doc.DocumentNode.SelectSingleNode("//body") ?? doc.DocumentNode;
            return bodyNode.InnerHtml;
        }

        public string Sanitize(string rawHtml)
        {
            return ExtractAndSanitize(rawHtml, null);
        }
    }
}