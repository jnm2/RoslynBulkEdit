namespace RoslynBulkEdit;

internal static class Extensions
{
    public static int IndexOfMax<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector, IComparer<TKey>? comparer = null)
    {
        using var enumerator = source.GetEnumerator();

        if (!enumerator.MoveNext())
            return -1;

        var firstValue = enumerator.Current;

        if (!enumerator.MoveNext())
            return 0;

        var maxKey = selector(firstValue);
        var maxIndex = 0;
        var currentIndex = 0;
        comparer ??= Comparer<TKey>.Default;

        do
        {
            currentIndex++;
            var currentKey = selector(enumerator.Current);

            if (comparer.Compare(currentKey, maxKey) > 0)
            {
                maxKey = currentKey;
                maxIndex = currentIndex;
            }
        }
        while (enumerator.MoveNext());

        return maxIndex;
    }
}
