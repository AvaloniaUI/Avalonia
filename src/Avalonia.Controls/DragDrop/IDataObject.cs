using System.Collections.Generic;

namespace Avalonia.Controls.DragDrop
{
    public interface IDataObject
    {
        IEnumerable<string> GetDataFormats();

        bool Contains(string dataFormat);

        string GetText();

        IEnumerable<string> GetFileNames();
        
        object Get(string dataFormat);
    }
}