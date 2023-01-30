using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Media.TextFormatting;

namespace Avalonia.Utilities
{
    /// <summary>
    /// A simple bi-directional dictionary.
    /// </summary>
    /// <typeparam name="T1">Key type</typeparam>
    /// <typeparam name="T2">Value type</typeparam>
    internal sealed class BidiDictionary<T1, T2> where T1 : notnull where T2 : notnull
    {
        private Dictionary<T1, T2> _forward = new();
        private Dictionary<T2, T1> _reverse = new();

        public void ClearThenResetIfTooLarge()
        {
            FormattingBufferHelper.ClearThenResetIfTooLarge(ref _forward);
            FormattingBufferHelper.ClearThenResetIfTooLarge(ref _reverse);
        }

        public void Add(T1 key, T2 value)
        {
            _forward.Add(key, value);
            _reverse.Add(value, key);
        }

        public bool TryGetValue(T1 key, [MaybeNullWhen(false)] out T2 value) => _forward.TryGetValue(key, out value);

        public bool TryGetKey(T2 value, [MaybeNullWhen(false)] out T1 key) => _reverse.TryGetValue(value, out key);

        public bool ContainsKey(T1 key) => _forward.ContainsKey(key);

        public bool ContainsValue(T2 value) => _reverse.ContainsKey(value);
    }
}
