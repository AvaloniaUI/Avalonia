// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.
// Ported from: https://github.com/SixLabors/Fonts/

using Avalonia.Utilities;

namespace Avalonia.Media.TextFormatting.Unicode
{
    /// <summary>
    /// Represents a unicode string and all associated attributes
    /// for each character required for the Bidi algorithm
    /// </summary>
    internal class BiDiData
    {
        private ArrayBuilder<BiDiClass> _types;
        private ArrayBuilder<BiDiPairedBracketType> _pairedBracketTypes;
        private ArrayBuilder<int> _pairedBracketValues;
        private ArrayBuilder<BiDiClass> _savedTypes;
        private ArrayBuilder<BiDiPairedBracketType> _savedPairedBracketTypes;
        private ArrayBuilder<sbyte> _tempLevelBuffer;

        public sbyte ParagraphEmbeddingLevel { get; private set; }

        public bool HasBrackets { get; private set; }

        public bool HasEmbeddings { get; private set; }

        public bool HasIsolates { get; private set; }

        /// <summary>
        /// Gets the length of the data held by the BidiData
        /// </summary>
        public int Length => _types.Length;

        /// <summary>
        /// Gets the bidi character type of each code point
        /// </summary>
        public Slice<BiDiClass> Types { get; private set; }

        /// <summary>
        /// Gets the paired bracket type for each code point
        /// </summary>
        public Slice<BiDiPairedBracketType> PairedBracketTypes { get; private set; }

        /// <summary>
        /// Gets the paired bracket value for code point
        /// </summary>
        /// <remarks>
        /// The paired bracket values are the code points
        /// of each character where the opening code point
        /// is replaced with the closing code point for easier
        /// matching.  Also, bracket code points are mapped
        /// to their canonical equivalents
        /// </remarks>
        public Slice<int> PairedBracketValues { get; private set; }

        /// <summary>
        /// Initialize with a text value.
        /// </summary>
        /// <param name="text">The text to process.</param>
        /// <param name="paragraphEmbeddingLevel">The paragraph embedding level</param>
        public void Init(ReadOnlySlice<char> text, sbyte paragraphEmbeddingLevel)
        {
            // Set working buffer sizes
            _types.Length = text.Length;
            _pairedBracketTypes.Length = text.Length;
            _pairedBracketValues.Length = text.Length;
            
            ParagraphEmbeddingLevel = paragraphEmbeddingLevel;

            // Resolve the BiDiClass, paired bracket type and paired
            // bracket values for all code points
            HasBrackets = false;
            HasEmbeddings = false;
            HasIsolates = false;

            var i = 0;
            var codePointEnumerator = new CodepointEnumerator(text);
            
            while (codePointEnumerator.MoveNext())
            {
                var codepoint = codePointEnumerator.Current;

                // Look up BiDiClass
                var dir = codepoint.BiDiClass;
                _types[i] = dir;

                switch (dir)
                {
                    case BiDiClass.LeftToRightEmbedding:
                    case BiDiClass.LeftToRightOverride:
                    case BiDiClass.RightToLeftEmbedding:
                    case BiDiClass.RightToLeftOverride:
                    case BiDiClass.PopDirectionalFormat:
                    {
                        HasEmbeddings = true;
                        break;
                    }

                    case BiDiClass.LeftToRightIsolate:
                    case BiDiClass.RightToLeftIsolate:
                    case BiDiClass.FirstStrongIsolate:
                    case BiDiClass.PopDirectionalIsolate:
                    {
                        HasIsolates = true;
                        break;
                    }
                }

                // Lookup paired bracket types
                var pbt = codepoint.PairedBracketType;
                _pairedBracketTypes[i] = pbt;

                if (pbt == BiDiPairedBracketType.Open)
                {
                    // Opening bracket types can never have a null pairing.
                    codepoint.TryGetPairedBracket(out var paired);
                    
                    _pairedBracketValues[i] = Codepoint.GetCanonicalType(paired).Value;

                    HasBrackets = true;
                }
                else if (pbt == BiDiPairedBracketType.Close)
                {
                    _pairedBracketValues[i] = Codepoint.GetCanonicalType(codepoint).Value;
                    
                    HasBrackets = true;
                }

                i++;
            }

            // Create slices on work buffers
            Types = _types.AsSlice().Take(i);
            PairedBracketTypes = _pairedBracketTypes.AsSlice().Take(i);
            PairedBracketValues = _pairedBracketValues.AsSlice().Take(i);
        }

        /// <summary>
        /// Save the Types and PairedBracketTypes of this bididata
        /// </summary>
        /// <remarks>
        /// This is used when processing embedded style runs with
        /// BiDiClass overrides.  TextLayout saves the data,
        /// overrides the style runs to neutral, processes the bidi
        /// data for the entire paragraph and then restores this data
        /// before processing the embedded runs.
        /// </remarks>
        public void SaveTypes()
        {
            // Capture the types data
            _savedTypes.Clear();
            _savedTypes.Add(_types.AsSlice());
            _savedPairedBracketTypes.Clear();
            _savedPairedBracketTypes.Add(_pairedBracketTypes.AsSlice());
        }

        /// <summary>
        /// Restore the data saved by SaveTypes
        /// </summary>
        public void RestoreTypes()
        {
            _types.Clear();
            _types.Add(_savedTypes.AsSlice());
            _pairedBracketTypes.Clear();
            _pairedBracketTypes.Add(_savedPairedBracketTypes.AsSlice());
        }

        /// <summary>
        /// Gets a temporary level buffer. Used by TextLayout when
        /// resolving style runs with different BiDiClass.
        /// </summary>
        /// <param name="length">Length of the required ExpandableBuffer</param>
        /// <returns>An uninitialized level ExpandableBuffer</returns>
        public Slice<sbyte> GetTempLevelBuffer(int length)
        {
            _tempLevelBuffer.Clear();
            
            return _tempLevelBuffer.Add(length, false);
        }
    }
}
