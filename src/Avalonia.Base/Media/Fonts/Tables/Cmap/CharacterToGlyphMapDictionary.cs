using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Avalonia.Media.Fonts.Tables.Cmap
{
    /// <summary>
    /// Provides a read-only dictionary view over a <see cref="CharacterToGlyphMap"/>.
    /// </summary>
    internal sealed class CharacterToGlyphMapDictionary : IReadOnlyDictionary<int, ushort>
    {
        private readonly CharacterToGlyphMap _map;
        private List<CodepointRange>? _cachedRanges;

        public CharacterToGlyphMapDictionary(CharacterToGlyphMap map)
        {
            _map = map;
        }

        public ushort this[int key]
        {
            get
            {
                if (!_map.TryGetGlyph(key, out var glyphId))
                {
                    throw new KeyNotFoundException($"The code point {key} was not found in the character map.");
                }

                return glyphId;
            }
        }

        public IEnumerable<int> Keys
        {
            get
            {
                foreach (var range in GetRanges())
                {
                    for (int codePoint = range.Start; codePoint <= range.End; codePoint++)
                    {
                        if (_map.ContainsGlyph(codePoint))
                        {
                            yield return codePoint;
                        }
                    }
                }
            }
        }

        public IEnumerable<ushort> Values
        {
            get
            {
                foreach (var range in GetRanges())
                {
                    for (int codePoint = range.Start; codePoint <= range.End; codePoint++)
                    {
                        if (_map.TryGetGlyph(codePoint, out var glyphId))
                        {
                            yield return glyphId;
                        }
                    }
                }
            }
        }

        public int Count
        {
            get
            {
                int count = 0;
                foreach (var range in GetRanges())
                {
                    for (int codePoint = range.Start; codePoint <= range.End; codePoint++)
                    {
                        if (_map.ContainsGlyph(codePoint))
                        {
                            count++;
                        }
                    }
                }
                return count;
            }
        }

        public bool ContainsKey(int key)
        {
            return _map.ContainsGlyph(key);
        }

        public bool TryGetValue(int key, [MaybeNullWhen(false)] out ushort value)
        {
            return _map.TryGetGlyph(key, out value);
        }

        public IEnumerator<KeyValuePair<int, ushort>> GetEnumerator()
        {
            foreach (var range in GetRanges())
            {
                for (int codePoint = range.Start; codePoint <= range.End; codePoint++)
                {
                    if (_map.TryGetGlyph(codePoint, out var glyphId))
                    {
                        yield return new KeyValuePair<int, ushort>(codePoint, glyphId);
                    }
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private List<CodepointRange> GetRanges()
        {
            var cachedRanges = _cachedRanges;
            if (cachedRanges != null)
            {
                return cachedRanges;
            }

            var ranges = new List<CodepointRange>();
            var enumerator = _map.GetMappedRanges();
            while (enumerator.MoveNext())
            {
                ranges.Add(enumerator.Current);
            }

            _cachedRanges = ranges;
            return ranges;
        }
    }
}
