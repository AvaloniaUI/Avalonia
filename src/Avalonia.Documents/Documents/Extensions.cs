using Avalonia.Media;

namespace Avalonia.Documents
{
    internal static class Extensions
    {
        public static bool ValueEquals(this TextDecorationCollection self, TextDecorationCollection textDecorations)
        {
            if (textDecorations == null)
                return false;   // o is either null or not TextDecorations object

            if (self == textDecorations)
                return true;    // Reference equality.

            if (self.Count != textDecorations.Count)
                return false;   // Two counts are different.

            // To be considered equal, TextDecorations should be same in the exact order.
            // Order matters because they imply the Z-order of the text decorations on screen.
            // Same set of text decorations drawn with different orders may have different result.
            for (int i = 0; i < self.Count; i++)
            {
                if (!self[i].ValueEquals(textDecorations[i]))
                    return false;
            }
            return true;
        }

        public static bool ValueEquals(this TextDecoration self, TextDecoration textDecoration)
        {
            if (textDecoration == null)
                return false; // o is either null or not a TextDecoration object.

            if (self == textDecoration)
                return true; // reference equality.

            return (
                self.Location == textDecoration.Location
                && self.StrokeOffset == textDecoration.StrokeOffset
                && self.StrokeOffsetUnit == textDecoration.StrokeOffsetUnit
                && self.StrokeThicknessUnit == textDecoration.StrokeThicknessUnit
                && (self.Stroke?.Equals(textDecoration.Stroke) ?? textDecoration.Stroke == null)
            );
        }
    }
}
