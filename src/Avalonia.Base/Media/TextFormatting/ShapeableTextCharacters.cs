using Avalonia.Utilities;

namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// A group of characters that can be shaped.
    /// </summary>
    public sealed class ShapeableTextCharacters : TextRun
    {
        public ShapeableTextCharacters(CharacterBufferReference characterBufferReference, int length,
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

        public bool CanShapeTogether(ShapeableTextCharacters shapeableTextCharacters)
        {
            if (!CharacterBufferReference.Equals(shapeableTextCharacters.CharacterBufferReference))
            {
                return false;
            }

            if (BidiLevel != shapeableTextCharacters.BidiLevel)
            {
                return false;
            }

            if (!MathUtilities.AreClose(Properties.FontRenderingEmSize,
                    shapeableTextCharacters.Properties.FontRenderingEmSize))
            {
                return false;
            }

            if (Properties.Typeface != shapeableTextCharacters.Properties.Typeface)
            {
                return false;
            }

            if (Properties.BaselineAlignment != shapeableTextCharacters.Properties.BaselineAlignment)
            {
                return false;
            }

            return true;
        }
    }
}
