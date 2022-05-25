namespace Avalonia.Rendering.Composition.Server
{
    internal class ReadbackIndices
    {
        private readonly object _lock = new object();
        public int ReadIndex { get; private set; } = 0;
        public int WriteIndex { get; private set; } = 1;
        public int WrittenIndex { get; private set; } = 0;
        public ulong ReadRevision { get; private set; }
        public ulong LastWrittenRevision { get; private set; }
        
        public void NextRead()
        {
            lock (_lock)
            {
                if (ReadRevision < LastWrittenRevision)
                {
                    ReadIndex = WrittenIndex;
                    ReadRevision = LastWrittenRevision;
                }
            }
        }

        public void CompleteWrite(ulong writtenRevision)
        {
            lock (_lock)
            {
                for (var c = 0; c < 3; c++)
                {
                    if (c != WriteIndex && c != ReadIndex)
                    {
                        WrittenIndex = WriteIndex;
                        LastWrittenRevision = writtenRevision;
                        WriteIndex = c;
                        return;
                    }
                }
            }
        }
    }
}