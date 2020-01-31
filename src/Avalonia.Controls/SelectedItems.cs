// This source file is adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

using System;
using System.Collections;
using System.Collections.Generic;

#nullable enable

namespace Avalonia.Controls
{
    public interface ISelectedItemInfo
    {
        public IndexPath Path { get; }
    }

    internal class SelectedItems<TValue, Tinfo> : IReadOnlyList<TValue>
        where Tinfo : ISelectedItemInfo
    {
        private readonly List<Tinfo> _infos;
        private readonly Func<List<Tinfo>, int, TValue> _getAtImpl;

        public SelectedItems(
            List<Tinfo> infos,
            int count,
            Func<List<Tinfo>, int, TValue> getAtImpl)
        {
            _infos = infos;
            _getAtImpl = getAtImpl;
            Count = count;
        }

        public TValue this[int index] => _getAtImpl(_infos, index);

        public int Count { get; }

        public IEnumerator<TValue> GetEnumerator()
        {
            for (var i = 0; i < Count; ++i)
            {
                yield return this[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
