using Avalonia.Utilities;

namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// A group of characters that can be shaped.
    /// </summary>
    public sealed class UnshapedTextRun : TextRun
    {
        public UnshapedTextRun(CharacterBufferReference characterBufferReference, int length,
            TextRunProperties properties, sbyte biDiLevel)
        {
            CharacterBufferReference = characterBufferReference;
            Length = length;
            Properties = properties;
            BidiLevel = biDiLevel;
        }

        public override int Length { get; }

        public override CharacterBufferReference CharacterBufferReference { get; }

        public override TextRunProperties Properties { get; }

        public sbyte BidiLevel { get; }

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
    }
}
