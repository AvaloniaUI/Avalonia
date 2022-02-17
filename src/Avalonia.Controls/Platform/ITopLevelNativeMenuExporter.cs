using System;
using Avalonia.Platform;

namespace Avalonia.Controls.Platform
{
    public interface INativeMenuExporter
    {
        void SetNativeMenu(NativeMenu? menu);
    }

    public interface ITopLevelNativeMenuExporter : INativeMenuExporter
    {
        bool IsNativeMenuExported { get; }

        event EventHandler OnIsNativeMenuExportedChanged;
    }

    public interface INativeMenuExporterProvider
    {
        INativeMenuExporter? NativeMenuExporter { get; }
    }
    
    public interface ITopLevelImplWithNativeMenuExporter : ITopLevelImpl
    {
        ITopLevelNativeMenuExporter? NativeMenuExporter { get; }
    }
}
