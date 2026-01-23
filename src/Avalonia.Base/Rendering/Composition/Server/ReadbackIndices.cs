using System.Threading;

namespace Avalonia.Rendering.Composition.Server
{
    /// <summary>
    /// A helper class used to manage the current slots for writing data from the render thread
    /// and reading it from the UI thread.
    /// Used mostly by hit-testing which needs to know the last transform of the visual
    /// </summary>
    internal class ReadbackIndices
    {
        public readonly object _lock = new object();

        public ulong ReadRevision { get; private set; }
        private ulong _nextWriteRevision = 1;
        public ulong WriteRevision { get; private set; }
        public ulong LastCompletedWrite { get; private set; }

        public void NextRead()
        {
            lock (_lock)
                ReadRevision = LastCompletedWrite;
        }
        
        public void BeginWrite()
        {
            Monitor.Enter(_lock);
            WriteRevision = _nextWriteRevision++;
        }

        public void EndWrite()
        {
            LastCompletedWrite = WriteRevision;
            Monitor.Exit(_lock);
        }
    }
}
