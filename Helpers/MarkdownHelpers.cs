namespace paddlepro.API.Helpers;

public static class MarkdownHelpers
{
    public static string MdEscapeChars(this string body)
    {
        return body.Replace("-", "\\-").Replace(".", "\\.").Replace("(", "\\(").Replace(")", "\\)").Replace("+", "\\+");
    }

    public static string MdItalic(this string text)
    {
        return $"_{text}_";
    }

    public static string MdStrikethrough(this string text)
    {
        return $"~{text}~";
    }

    public static string MdSpoiler(this string text)
    {
        return $"||{text}||";
    }

    public static string MdUrl(this string text, string url)
    {
        return $"[{text}]({url})";
    }

    public static string MdUnderline(this string text)
    {
        return $"__{text}__";
    }

    public static string MdBold(this string text)
    {
        return $"*{text}*";
    }

    public static string MdQuote(this string text)
    {
        return text.Split("\n").Select(t => $">{t}").Join("\n");
    }

    public static string MdExpandable(this string text)
    {
        return @$"**>{text.MdQuote()}||";
    }
}
