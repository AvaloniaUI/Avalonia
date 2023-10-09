using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Avalonia.Utilities;

internal static class AsyncEnumerableHelper
{
    public static IAsyncEnumerable<T> AsAsyncEnumerable<T>(this IEnumerable<T> enumerable)
        => new EnumerableAsyncWrapper<T>(enumerable);

    private sealed class EnumerableAsyncWrapper<T> : IAsyncEnumerable<T>
    {
        private readonly IEnumerable<T> _enumerable;

        public EnumerableAsyncWrapper(IEnumerable<T> enumerable)
            => _enumerable = enumerable;

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            => new EnumeratorAsyncWrapper<T>(_enumerable.GetEnumerator(), cancellationToken);
    }

    private sealed class EnumeratorAsyncWrapper<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _enumerator;
        private readonly CancellationToken _cancellationToken;

        public EnumeratorAsyncWrapper(IEnumerator<T> enumerator, CancellationToken cancellationToken)
        {
            _enumerator = enumerator;
            _cancellationToken = cancellationToken;
        }

        public T Current
            => _enumerator.Current;

        public ValueTask<bool> MoveNextAsync()
            => _cancellationToken.IsCancellationRequested ?
                new(Task.FromCanceled<bool>(_cancellationToken)) :
                new(_enumerator.MoveNext());

        public ValueTask DisposeAsync()
        {
            _enumerator.Dispose();
            return default;
        }
    }
}
