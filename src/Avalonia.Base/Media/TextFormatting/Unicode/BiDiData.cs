// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.
// Ported from: https://github.com/SixLabors/Fonts/

using System;
using Avalonia.Utilities;

namespace Avalonia.Media.TextFormatting.Unicode
{
    /// <summary>
    /// Represents a unicode string and all associated attributes
    /// for each character required for the bidirectional Unicode algorithm
    /// </summary>
    internal struct BidiData : IDisposable
    {
        private ArrayBuilder<BidiClass> _classes;
        private ArrayBuilder<BidiPairedBracketType> _pairedBracketTypes;
        private ArrayBuilder<int> _pairedBracketValues;
        private ArrayBuilder<BidiClass> _savedClasses;
        private ArrayBuilder<BidiPairedBracketType> _savedPairedBracketTypes;
        private ArrayBuilder<sbyte> _tempLevelBuffer;

        public BidiData(sbyte paragraphEmbeddingLevel)
        {
            ParagraphEmbeddingLevel = paragraphEmbeddingLevel;
        }

        public sbyte ParagraphEmbeddingLevel { get; private set; }

        public bool HasBrackets { get; private set; }

        public bool HasEmbeddings { get; private set; }

        public bool HasIsolates { get; private set; }

        /// <summary>
        /// Gets the length of the data held by the BidiData
        /// </summary>
        public int Length{get; private set; }

        /// <summary>
        /// Gets the bidi character type of each code point
        /// </summary>
        public ArraySlice<BidiClass> Classes { get; private set; }

        /// <summary>
        /// Gets the paired bracket type for each code point
        /// </summary>
        public ArraySlice<BidiPairedBracketType> PairedBracketTypes { get; private set; }

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
        public ArraySlice<int> PairedBracketValues { get; private set; }

        /// <summary>
        /// Appends text to the bidi data.
        /// </summary>
        /// <param name="text">The text to process.</param>
        public void Append(ReadOnlySpan<char> text)
        {
            _classes.Add(text.Length);
            _pairedBracketTypes.Add(text.Length);
            _pairedBracketValues.Add(text.Length);

            // Resolve the BidiCharacterType, paired bracket type and paired
            // bracket values for all code points

            int i = Length;
             
            var codePointEnumerator = new CodepointEnumerator(text);
            
            while (codePointEnumerator.MoveNext())
            {
                var codepoint = codePointEnumerator.Current;

                // Look up BiDiClass
                var dir = codepoint.BiDiClass;
                
                _classes[i] = dir;

                switch (dir)
                {
                    case BidiClass.LeftToRightEmbedding:
                    case BidiClass.LeftToRightOverride:
                    case BidiClass.RightToLeftEmbedding:
                    case BidiClass.RightToLeftOverride:
                    case BidiClass.PopDirectionalFormat:
                    {
                        HasEmbeddings = true;
                        break;
                    }

                    case BidiClass.LeftToRightIsolate:
                    case BidiClass.RightToLeftIsolate:
                    case BidiClass.FirstStrongIsolate:
                    case BidiClass.PopDirectionalIsolate:
                    {
                        HasIsolates = true;
                        break;
                    }
                }

                // Lookup paired bracket types
                var pbt = codepoint.PairedBracketType;
                
                _pairedBracketTypes[i] = pbt;

                if (pbt == BidiPairedBracketType.Open)
                {
                    // Opening bracket types can never have a null pairing.
                    codepoint.TryGetPairedBracket(out var paired);
                    
                    _pairedBracketValues[i] = (int)Codepoint.GetCanonicalType(paired).Value;

                    HasBrackets = true;
                }
                else if (pbt == BidiPairedBracketType.Close)
                {
                    _pairedBracketValues[i] = (int)Codepoint.GetCanonicalType(codepoint).Value;
                    
                    HasBrackets = true;
                }

                i++;
            }

            Length = i;
            
            Classes = _classes.AsSlice(0, Length);
            PairedBracketTypes = _pairedBracketTypes.AsSlice(0, Length);
            PairedBracketValues = _pairedBracketValues.AsSlice(0, Length);
        }

        /// <summary>
        /// Save the Types and PairedBracketTypes of this BiDiData
        /// </summary>
        /// <remarks>
        /// This is used when processing embedded style runs with
        /// BiDiClass overrides. Text layout process saves the data,
        /// overrides the style runs to neutral, processes the bidi
        /// data for the entire paragraph and then restores this data
        /// before processing the embedded runs.
        /// </remarks>
        public void SaveTypes()
        {
            // Capture the types data
            _savedClasses.Clear();
            _savedClasses.Add(_classes.AsSlice());
            _savedPairedBracketTypes.Clear();
            _savedPairedBracketTypes.Add(_pairedBracketTypes.AsSlice());
        }

        /// <summary>
        /// Restore the data saved by SaveTypes
        /// </summary>
        public void RestoreTypes()
        {
            _classes.Clear();
            _classes.Add(_savedClasses.AsSlice());
            _pairedBracketTypes.Clear();
            _pairedBracketTypes.Add(_savedPairedBracketTypes.AsSlice());
        }

        /// <summary>
        /// Gets a temporary level buffer. Used by the text layout process when
        /// resolving style runs with different BiDiClass.
        /// </summary>
        /// <param name="length">Length of the required ExpandableBuffer</param>
        /// <returns>An uninitialized level ExpandableBuffer</returns>
        public ArraySlice<sbyte> GetTempLevelBuffer(int length)
        {
            _tempLevelBuffer.Clear();
            
            return _tempLevelBuffer.Add(length, false);
        }

        public void Dispose()
        {
            _classes.Dispose();
            _pairedBracketTypes.Dispose();
            _pairedBracketValues.Dispose();
            _savedClasses.Dispose();
            _savedPairedBracketTypes.Dispose();
            _tempLevelBuffer.Dispose();
        }
    }
}
