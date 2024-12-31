namespace paddlepro.API.Helpers;

public static class StringHelper
{
    private static string CALLBACK_DELIMITER = ";";
    public static (string, string) SplitBy(this string value, string separator)
    {
        var splitted = value.Split(separator);
        if (splitted.Length != 2)
        {
            throw new Exception("Split must have exactly 2 values");
        }
        return (splitted[0], splitted[1]);
    }

    public static (string, string, string) SplitBy3(this string value, string separator)
    {
        var splitted = value.Split(separator);
        if (splitted.Length != 3)
        {
            throw new Exception("Split must have exactly 3 values");
        }
        return (splitted[0], splitted[1], splitted[2]);
    }

    public static string EncodeCallback(this (string, string) data)
    {
        return $"{data.Item1}{CALLBACK_DELIMITER}{data.Item2}";
    }

    public static (string, string) DecodeCallback(this string callback)
    {
        return callback.SplitBy(CALLBACK_DELIMITER);
    }

    public static (string, string, string, string) SplitBy4(this string value, string separator)
    {
        var splitted = value.Split(separator);
        if (splitted.Length != 4)
        {
            throw new Exception("Split must have exactly 4 values");
        }
        return (splitted[0], splitted[1], splitted[2], splitted[3]);
    }

    public static string Join(this IEnumerable<string> list, string separator)
    {
        return string.Join(separator, list);
    }

    public static string ToQueryParams(this Dictionary<string, string> queryParams)
    {
        return "?" + queryParams.Select(kv => $"{kv.Key}={kv.Value}").Join("&");
    }

}
