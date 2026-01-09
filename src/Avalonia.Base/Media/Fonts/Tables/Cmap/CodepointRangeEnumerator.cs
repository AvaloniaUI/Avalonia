using System.Runtime.CompilerServices;

namespace Avalonia.Media.Fonts.Tables.Cmap
{
    /// <summary>
    /// Enumerates contiguous ranges of Unicode code points present in a character map (cmap) table.
    /// </summary>
    /// <remarks>This enumerator is typically used to iterate over all code point ranges defined by a cmap
    /// table in an OpenType or TrueType font. It supports both Format 4 and Format 12 cmap subtables. The enumerator is
    /// a ref struct and must be used within the stack context; it cannot be stored on the heap or used across await or
    /// yield boundaries.</remarks>
    public ref struct CodepointRangeEnumerator
    {
        private readonly CmapFormat _format;
        private readonly CmapFormat4Table? _f4;
        private readonly CmapFormat12Table? _f12;
        private int _index;

        internal CodepointRangeEnumerator(CmapFormat format, CmapFormat4Table? f4, CmapFormat12Table? f12)
        {
            _format = format;
            _f4 = f4;
            _f12 = f12;
            _index = -1;
        }

        /// <summary>
        /// Gets the current code point range in the enumeration sequence.
        /// </summary>
        public CodepointRange Current { get; private set; }

        /// <summary>
        /// Advances the enumerator to the next character mapping range in the collection.
        /// </summary>
        /// <remarks>After calling MoveNext, check the Current property to access the current character
        /// mapping range. If the end of the collection is reached, MoveNext returns false and Current is set to its
        /// default value.</remarks>
        /// <returns>true if the enumerator was successfully advanced to the next range; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            _index++;

            switch (_format)
            {
                case CmapFormat.Format4:
                    {
                        var result = _f4!.TryGetRange(_index, out var range);

                        Current = range;

                        return result;
                    }
                case CmapFormat.Format12:
                    {
                        var result = _f12!.TryGetRange(_index, out var range);

                        Current = range;

                        return result;
                    }
                default:
                    {
                        Current = default;

                        return false;
                    }
            }
        }
    }
}
