using System.Text.RegularExpressions;

namespace StS2Toys.Services;

static class DescriptionFormatter
{
    static readonly Regex TagRegex =
        new(@"\[/?[a-zA-Z]+\]", RegexOptions.Compiled);

    static readonly Regex TemplateRegex =
        new(@"\{[^}]+\}", RegexOptions.Compiled);

    public static string Clean(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return raw;
        var s = TagRegex.Replace(raw, "");
        s = TemplateRegex.Replace(s, "?");
        return s.Trim();
    }
}
