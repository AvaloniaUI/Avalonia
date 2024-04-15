namespace DataGridAsyncDemoMVVM;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

public static class Extensions
{
    internal static Task<T?> GetRowAsync<T>(this IQueryable<T> table, Expression<Func<T, bool>> predicate)
        where T : class
    {
        return Task.Run(() => table.FirstOrDefault(predicate))!;
    }

    internal static Task<int> GetRowCountAsync<T>(this IQueryable<T> table, Func<IQueryable<T>, IQueryable<T>>? query = null)
        where T : class
    {
        IQueryable<T> rows = table;

        if (query != null)
        {
            rows = query(rows);
        }

        return Task.Run(() => rows.Count());
    }

    internal static Task<IEnumerable<T>> GetRowsAsync<T>(this IQueryable<T> table, int offset, int count, Func<IQueryable<T>, IQueryable<T>>? query = null)
        where T : class
    {
        IQueryable<T> rows = table;

        if (query != null)
        {
            rows = query(rows);
        }

        return Task.Run(() => rows.Skip(offset).Take(count).AsEnumerable());
    }
}