using Avalonia.Utility;

namespace Avalonia.Media.TextFormatting
{
    /// <summary>
    /// A text run that holds text characters.
    /// </summary>
    public class TextCharacters : TextRun
    {
        protected TextCharacters()
        {
            
        }

        public TextCharacters(ReadOnlySlice<char> text, TextStyle style)
        {
            Text = text;
            Style = style;
        }
    }
}
