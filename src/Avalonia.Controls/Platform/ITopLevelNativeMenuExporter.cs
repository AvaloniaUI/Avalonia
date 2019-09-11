using System;
using System.Collections.Generic;
using Avalonia.Platform;

namespace Avalonia.Controls.Platform
{
    public interface ITopLevelNativeMenuExporter
    {
        bool IsNativeMenuExported { get; }
        event EventHandler OnIsNativeMenuExportedChanged;
        void SetNativeMenu(NativeMenu menu);
        void SetPrependApplicationMenu(bool prepend);
    }
    
    public interface ITopLevelImplWithNativeMenuExporter : ITopLevelImpl
    {
        ITopLevelNativeMenuExporter NativeMenuExporter { get; }
    }
}
