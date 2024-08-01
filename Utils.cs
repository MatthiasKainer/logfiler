namespace logfiler;

public static class Utils
{
    public static IEnumerable<T> NotNull<T>(this IEnumerable<T?> source) where T : class
    {
        return source.Where(item => item != null)!;
    }
}