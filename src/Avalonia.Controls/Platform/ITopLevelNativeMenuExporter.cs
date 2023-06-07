using System;
using Avalonia.Metadata;
using Avalonia.Platform;

namespace Avalonia.Controls.Platform
{
    [Unstable]
    public interface INativeMenuExporter
    {
        void SetNativeMenu(NativeMenu? menu);
    }

    [Unstable]
    public interface ITopLevelNativeMenuExporter : INativeMenuExporter
    {
        bool IsNativeMenuExported { get; }

        event EventHandler OnIsNativeMenuExportedChanged;
    }

    [Unstable]
    public interface INativeMenuExporterProvider
    {
        INativeMenuExporter? NativeMenuExporter { get; }
    }
}
