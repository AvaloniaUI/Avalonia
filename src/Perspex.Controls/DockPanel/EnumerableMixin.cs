namespace Perspex.Controls
{
    using System.Collections.Generic;

    public static class EnumerableMixin
    {
        public static IEnumerable<T> Shrink<T>(this IEnumerable<T> source, int left, int right)
        {
            int i = 0;
            var buffer = new Queue<T>(right + 1);

            foreach (T x in source)
            {
                if (i >= left) // Read past left many elements at the start
                {
                    buffer.Enqueue(x);
                    if (buffer.Count > right) // Build a buffer to drop right many elements at the end
                        yield return buffer.Dequeue();
                }
                else i++;
            }
        }
        public static IEnumerable<T> WithoutLast<T>(this IEnumerable<T> source, int n = 1)
        {
            return source.Shrink(0, n);
        }
        public static IEnumerable<T> WithoutFirst<T>(this IEnumerable<T> source, int n = 1)
        {
            return source.Shrink(n, 0);
        }
    }
}