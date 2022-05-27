using Avalonia.Metadata;

namespace Avalonia.Controls
{
    [Unstable]
    public interface INativeMenuExporterEventsImplBridge
    {
        void RaiseNeedsUpdate ();
        void RaiseOpening();
        void RaiseClosed();
    }
}
