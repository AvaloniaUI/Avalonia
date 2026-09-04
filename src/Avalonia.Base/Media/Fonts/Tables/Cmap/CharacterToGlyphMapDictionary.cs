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
        private int _cachedCount = -1;

        /// <summary>
        /// Initializes a new instance of the <see cref="CharacterToGlyphMapDictionary"/> class that wraps the specified <see cref="CharacterToGlyphMap"/>.
        /// </summary>
        /// <param name="map"></param>
        public CharacterToGlyphMapDictionary(CharacterToGlyphMap map)
        {
            _map = map;
        }

        /// <summary>
        /// Gets the glyph ID corresponding to the specified code point. If the code point does not have a corresponding glyph ID in the map, a <see cref="KeyNotFoundException"/> is thrown.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        /// <exception cref="KeyNotFoundException"></exception>
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

        /// <summary>
        /// Yields the code points that have corresponding glyph IDs in the map. The order of the code points is not guaranteed to match the order of the glyph IDs returned by <see cref="Values"/>.
        /// </summary>
        public IEnumerable<int> Keys
        {
            get
            {
                foreach (var range in GetRanges())
                {
                    for (int codePoint = range.Start; codePoint <= range.End; codePoint++)
                    {
                        // Membership must match TryGetValue (TryGetGlyph), not ContainsGlyph:
                        // the two predicates disagree for mappings that resolve to glyph 0, and
                        // Keys yielding a key the indexer rejects breaks the dictionary contract.
                        if (_map.TryGetGlyph(codePoint, out _))
                        {
                            yield return codePoint;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Yields the glyph IDs corresponding to the code points in the map. The order of the glyph IDs is not guaranteed to match the order of the code points returned by <see cref="Keys"/>.
        /// </summary>
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

        /// <summary>
        /// Returns the number of code point to glyph ID mappings in the map. This is computed by iterating over all code points in the mapped ranges and counting those that have a corresponding glyph ID. 
        /// The result is cached after the first computation for efficiency.
        /// </summary>
        public int Count
        {
            get
            {
                int cachedCount = _cachedCount;
                if (cachedCount >= 0)
                {
                    return cachedCount;
                }

                int count = 0;
                foreach (var range in GetRanges())
                {
                    for (int codePoint = range.Start; codePoint <= range.End; codePoint++)
                    {
                        if (_map.TryGetGlyph(codePoint, out _))
                        {
                            count++;
                        }
                    }
                }

                _cachedCount = count;
                return count;
            }
        }

        /// <summary>
        /// Determines whether the map contains a mapping for the specified code point. This is implemented by calling <see cref="CharacterToGlyphMap.TryGetGlyph"/> and returning true if it returns true, regardless of the glyph ID returned.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ContainsKey(int key)
        {
            return _map.TryGetGlyph(key, out _);
        }

        /// <summary>
        /// Attempts to get the glyph ID corresponding to the specified code point. This is implemented by calling <see cref="CharacterToGlyphMap.TryGetGlyph"/> and returning its result directly, passing through the glyph ID if found.
        /// </summary>
        /// <param name="key">The code point for which to get the glyph ID.</param>
        /// <param name="value">When this method returns, contains the glyph ID associated with the specified code point, if the code point is found; otherwise, the default value for the type of the value parameter. This parameter is passed uninitialized.</param>
        /// <returns>true if the map contains a mapping for the specified code point; otherwise, false.</returns>
        public bool TryGetValue(int key, [MaybeNullWhen(false)] out ushort value)
        {
            return _map.TryGetGlyph(key, out value);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the code point to glyph ID mappings in the map. The enumerator yields <see cref="KeyValuePair{TKey, TValue}"/> instances where the key is a code point and the value is the corresponding glyph ID. The order of the mappings is not guaranteed to match the order of the code points or glyph IDs returned by <see cref="Keys"/> or <see cref="Values"/>.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the code point to glyph ID mappings.</returns>
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

        /// <summary>
        /// Gets the list of code point ranges that have mappings in the map. This is implemented by calling <see cref="CharacterToGlyphMap.GetMappedRanges"/> and caching the result for efficiency, since the ranges are immutable and expensive to compute.
        /// </summary>
        /// <returns></returns>
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
