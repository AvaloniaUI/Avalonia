namespace Avalonia.Rendering.Composition.Server
{
    internal class ReadbackIndices
    {
        private readonly object _lock = new object();
        public int ReadIndex { get; private set; } = 0;
        public int WriteIndex { get; private set; } = -1;
        public ulong ReadRevision { get; private set; }
        public ulong WriteRevision { get; private set; }
        private ulong[] _revisions = new ulong[3];


        public void NextRead()
        {
            lock (_lock)
            {
                for (var c = 0; c < 3; c++)
                {
                    if (c != WriteIndex && c != ReadIndex && _revisions[c] > ReadRevision)
                    {
                        ReadIndex = c;
                        ReadRevision = _revisions[c];
                        return;
                    }
                }
            }
        }

        public void NextWrite(ulong revision)
        {
            lock (_lock)
            {
                for (var c = 0; c < 3; c++)
                {
                    if (c != WriteIndex && c != ReadIndex)
                    {
                        WriteIndex = c;
                        WriteRevision = revision;
                        _revisions[c] = revision;
                        return;
                    }
                }
            }
        }
    }
}