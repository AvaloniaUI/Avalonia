namespace Avalonia.Native
{
    internal interface INativeMenuExporterResetHandler
    {
        void QueueReset();
        void UpdateIfNeeded();
    }
}
