using System.Collections;
using System.Collections.Generic;
using Avalonia.Collections;

namespace Avalonia.Utilities;

/// <summary>
/// An <see cref="AvaloniaList{T}"/> which can be mutated while it's being enumerated.
/// </summary>
/// <remarks>
/// <para>
/// When the list is mutated while one or more enumerations are in progress, the inner list is first copied and the
/// mutation is applied to the copy: active enumerators continue iterating over the snapshot taken when they were created.
/// Mutations made while no enumerator is active are applied in place, at no extra cost.
/// </para>
/// <para>
/// Warning! Enumerating an instance statically typed as <see cref="AvaloniaList{T}"/> uses the base,
/// non-snapshotting enumerator: expose instances of this type as <see cref="SafeEnumerableAvaloniaList{T}"/> or
/// <see cref="IAvaloniaList{T}"/>.
/// </para>
/// </remarks>
internal sealed class SafeEnumerableAvaloniaList<T> : AvaloniaList<T>, IEnumerable<T>
{
    private int _generation;
    private int _enumCount;

    /// <summary>
    /// For unit tests only!
    /// </summary>
    internal List<T> InnerForTests => Inner;

    private protected override void OnMutating()
    {
        if (_enumCount > 0)
        {
            Inner = new List<T>(Inner);
            ++_generation;
            _enumCount = 0;
        }
    }

    public new Enumerator GetEnumerator() => new(this);

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public new struct Enumerator : IEnumerator<T>
    {
        private readonly SafeEnumerableAvaloniaList<T> _owner;
        private readonly int _generation;
        private List<T>.Enumerator _innerEnumerator;

        internal Enumerator(SafeEnumerableAvaloniaList<T> owner)
        {
            _owner = owner;
            _generation = owner._generation;
            ++owner._enumCount;
            _innerEnumerator = owner.Inner.GetEnumerator();
        }

        public bool MoveNext()
            => _innerEnumerator.MoveNext();

        public T Current
            => _innerEnumerator.Current;

        object? IEnumerator.Current
            => Current;

        void IEnumerator.Reset()
            => ((IEnumerator)_innerEnumerator).Reset();

        public void Dispose()
        {
            if (_generation == _owner._generation)
                --_owner._enumCount;
        }
    }
}
