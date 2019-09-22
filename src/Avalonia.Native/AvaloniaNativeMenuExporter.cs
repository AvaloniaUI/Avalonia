using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Controls;
using Avalonia.Controls.Platform;

namespace Avalonia.Native
{
    class AvaloniaNativeMenuExporter : ITopLevelNativeMenuExporter
    {
        public bool IsNativeMenuExported => throw new NotImplementedException();

        public event EventHandler OnIsNativeMenuExportedChanged;

        public void SetNativeMenu(NativeMenu menu)
        {
            throw new NotImplementedException();
        }

        public void SetPrependApplicationMenu(bool prepend)
        {
            throw new NotImplementedException();
        }
    }
}
