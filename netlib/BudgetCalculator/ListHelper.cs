public static class ListHelper
{
    public static int AddIndex<T>(this IList<T> values, T item)
    {
        int index = values.Count;
        values.Add(item);
        return index;
    }
}
