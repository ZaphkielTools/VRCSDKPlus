using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Zaphkiel.VRCSDKPlus
{
    internal static class TextUtility
    {
        static readonly Regex TrailingNumber = new Regex(@"(\d+)$", RegexOptions.Compiled);
        static readonly Regex RichTextTag = new Regex(@"<[^>]*>", RegexOptions.Compiled);
        static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();

        public static string MakeUnique(string name, Func<string, bool> isAvailable, bool addNumberIfMissing = true)
        {
            name ??= string.Empty;
            if (isAvailable(name)) return name;

            var match = TrailingNumber.Match(name);
            if (!match.Success && !addNumberIfMissing) return name;

            string digits = match.Success ? match.Groups[1].Value : "1";
            string stem = match.Success ? name.Substring(0, match.Index) : name.EndsWith(" ") ? name : name + " ";
            if (!long.TryParse(digits, out long number)) number = 1;

            string candidate;
            do
            {
                candidate = stem + number.ToString(new string('0', digits.Length));
                number++;
            } while (!isAvailable(candidate));

            return candidate;
        }

        public static bool ContainsIgnoreCase(string text, string search) =>
            !string.IsNullOrEmpty(text) && text.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;

        public static string StripRichText(string text) => string.IsNullOrEmpty(text) ? text : RichTextTag.Replace(text, string.Empty);

        public static string ToFileName(string text)
        {
            var builder = new StringBuilder(text?.Trim() ?? string.Empty);
            for (int i = 0; i < builder.Length; i++)
                if (Array.IndexOf(InvalidFileNameChars, builder[i]) >= 0)
                    builder[i] = '-';
            return builder.Length == 0 ? "Unnamed" : builder.ToString();
        }
    }
}
