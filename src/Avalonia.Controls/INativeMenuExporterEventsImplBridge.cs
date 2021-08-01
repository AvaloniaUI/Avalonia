namespace Avalonia.Controls
{
    public interface INativeMenuExporterEventsImplBridge
    {
        void RaiseNeedsUpdate ();
        void RaiseOpening();
        void RaiseClosed();
    }
}
