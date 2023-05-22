using Avalonia.Metadata;

namespace Avalonia.Controls
{
    [PrivateApi]
    public interface INativeMenuExporterEventsImplBridge
    {
        void RaiseNeedsUpdate ();
        void RaiseOpening();
        void RaiseClosed();
    }
}
