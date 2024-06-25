using Ganss.Xss;

namespace KVHAI.CustomClass
{
    public class InputSanitize
    {
        private readonly HtmlSanitizer _sanitizer;
        public InputSanitize()
        {
            _sanitizer = new HtmlSanitizer();
        }
        public string HTMLSanitizer(string inputHTML)
        {
            if (string.IsNullOrEmpty(inputHTML))
            {
                return string.Empty; // Return empty string for empty input
            }

            var sanitizedHTML =  _sanitizer.Sanitize(inputHTML);

            return sanitizedHTML;
        }

        public Task<string> HTMLSanitizerAsync(string inputHTML)
        {
            return Task.Run(() => HTMLSanitizer(inputHTML));
        }
    }
}
