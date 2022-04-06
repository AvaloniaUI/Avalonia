using Avalonia.Utilities;

namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// A group of characters that can be shaped.
    /// </summary>
    public sealed class ShapeableTextCharacters : TextRun
    {
        public ShapeableTextCharacters(ReadOnlySlice<char> text, TextRunProperties properties, sbyte biDiLevel)
        {
            TextSourceLength = text.Length;
            Text = text;
            Properties = properties;
            BidiLevel = biDiLevel;
        }

        public override int TextSourceLength { get; }

        public override ReadOnlySlice<char> Text { get; }

        public override TextRunProperties Properties { get; }
        
        public sbyte BidiLevel { get; }

        public bool CanShapeTogether(ShapeableTextCharacters shapeableTextCharacters)
        {
            if (!Text.Buffer.Equals(shapeableTextCharacters.Text.Buffer))
            {
                return false;
            }

            if (Text.Start + Text.Length != shapeableTextCharacters.Text.Start)
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
