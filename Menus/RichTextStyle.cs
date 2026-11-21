using System.Text.RegularExpressions;
using UnityEngine;

namespace Zaphkiel.VRCSDKPlus
{
    internal static class RichTextStyle
    {
        static readonly Regex ColorOpen = new Regex(@"<color=(#[0-9a-fA-F]{6,8})>", RegexOptions.Compiled);
        static readonly Regex AnyColorTag = new Regex(@"</?color(=[^>]*)?>", RegexOptions.Compiled);

        public static bool Has(string text, char tag) => text.Contains($"<{tag}>") && text.Contains($"</{tag}>");

        public static string Set(string text, char tag, bool enabled) =>
            enabled
                ? Has(text, tag) ? text : $"<{tag}>{text}</{tag}>"
                : text.Replace($"<{tag}>", string.Empty).Replace($"</{tag}>", string.Empty);

        public static Color GetColor(string text)
        {
            var match = ColorOpen.Match(text);
            return match.Success && text.Contains("</color>") && ColorUtility.TryParseHtmlString(match.Groups[1].Value, out var color)
                ? color
                : Color.white;
        }

        public static string SetColor(string text, Color color) =>
            $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{AnyColorTag.Replace(text, string.Empty)}</color>";
    }
}
