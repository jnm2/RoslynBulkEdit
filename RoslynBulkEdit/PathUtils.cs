namespace RoslynBulkEdit;

internal static class PathUtils
{
    public static ReadOnlySpan<char> GetCommonPath(IEnumerable<string> paths)
    {
        using var enumerator = paths.GetEnumerator();

        if (!enumerator.MoveNext())
            return default;

        var commonPath = enumerator.Current.AsSpan();

        while (commonPath.Length > 0 && enumerator.MoveNext())
            commonPath = GetCommonPath(commonPath, enumerator.Current);

        return commonPath;
    }

    public static ReadOnlySpan<char> GetCommonPath(ReadOnlySpan<char> first, ReadOnlySpan<char> second)
    {
        var commonPrefix = first[..first.CommonPrefixLength(second)];
        if (commonPrefix.Length == first.Length || commonPrefix.Length == second.Length)
            return commonPrefix;

        var lastSeparatorIndex = commonPrefix.LastIndexOfAny(new[] { '/', '\\' });
        return lastSeparatorIndex == -1 ? default : commonPrefix[..(lastSeparatorIndex + 1)];
    }
}
