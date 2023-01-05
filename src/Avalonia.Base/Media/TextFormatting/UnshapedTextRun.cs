using System;
using Avalonia.Utilities;

namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// A group of characters that can be shaped.
    /// </summary>
    public sealed class UnshapedTextRun : SymbolTextRun
    {
        public UnshapedTextRun(CharacterBufferReference characterBufferReference, int length,
            TextRunProperties properties, sbyte biDiLevel) 
            : base(characterBufferReference, properties, length, biDiLevel)
        {
        }

        public bool CanShapeTogether(UnshapedTextRun unshapedTextRun)
        {
            if (!CharacterBufferReference.Equals(unshapedTextRun.CharacterBufferReference))
            {
                return false;
            }

            if (BidiLevel != unshapedTextRun.BidiLevel)
            {
                return false;
            }

            if (!MathUtilities.AreClose(Properties.FontRenderingEmSize,
                    unshapedTextRun.Properties.FontRenderingEmSize))
            {
                return false;
            }

            if (Properties.Typeface != unshapedTextRun.Properties.Typeface)
            {
                return false;
            }

            if (Properties.BaselineAlignment != unshapedTextRun.Properties.BaselineAlignment)
            {
                return false;
            }

            return true;
        }

        internal override void ReverseInternal()
        {
            //todo gillibald - implement this when you will add shaping skip logic.
            throw new NotSupportedException("This will not invoke until shaping skip logic will be created");
        }

        internal override SplitResult<SymbolTextRun> Split(int length)
        {
            //todo gillibald - implement this when you will add shaping skip logic.
            throw new NotSupportedException("This will not invoke until shaping skip logic will be created");
        }

        internal override bool TryMeasureCharacters(double availableWidth, out int length)
        {
            //todo gillibald - implement this when you will add shaping skip logic.
            throw new NotSupportedException("This will not invoke until shaping skip logic will be created");
        }

        internal override bool TryMeasureCharactersBackwards(double availableWidth, out int length, out double width)
        {
            //todo gillibald - implement this when you will add shaping skip logic.
            throw new NotSupportedException("This will not invoke until shaping skip logic will be created");
        }

        internal override GlyphRun CreateGlyphRun()
        {
            //todo gillibald - implement this when you will add shaping skip logic.
            throw new NotSupportedException("This will not invoke until shaping skip logic will be created");
        }
    }
}
