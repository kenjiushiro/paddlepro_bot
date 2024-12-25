namespace paddlepro.API.Helpers;

public static class StringHelper
{
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

    public static (string, string, string, string) SplitBy4(this string value, string separator)
    {
        var splitted = value.Split(separator);
        if (splitted.Length != 4)
        {
            throw new Exception("Split must have exactly 4 values");
        }
        return (splitted[0], splitted[1], splitted[2], splitted[3]);
    }
}
