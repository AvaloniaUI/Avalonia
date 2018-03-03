using System.Collections.Generic;

namespace Avalonia.Controls.DragDrop
{
    public interface IDragData
    {
        IEnumerable<string> GetDataFormats();

        bool Contains(string dataFormat);

        string GetText();

        IEnumerable<string> GetFileNames();
    }
}