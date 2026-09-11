using System.Runtime.InteropServices.JavaScript;

namespace Avalonia.Browser.Text
{
    internal class EditContext(JSObject handle)
    {
        public string? Text => handle.GetPropertyAsString("text");
        public int SelectionStart => handle.GetPropertyAsInt32("selectionStart");
        public int SelectionEnd => handle.GetPropertyAsInt32("selectionEnd");
        public int CharacterBoundsRangeStart => handle.GetPropertyAsInt32("characterBoundsRangeStart");
    }
}
