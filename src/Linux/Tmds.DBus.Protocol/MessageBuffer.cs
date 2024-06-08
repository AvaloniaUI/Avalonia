namespace Tmds.DBus.Protocol;

public sealed class MessageBuffer : IDisposable
{
    private readonly MessageBufferPool _messagePool;

    private readonly Sequence<byte> _data;

    internal uint Serial { get; private set; }

    internal MessageFlags MessageFlags { get; private set; }

    internal UnixFdCollection? Handles { get; private set; }

    internal MessageBuffer(MessageBufferPool messagePool, Sequence<byte> sequence)
    {
        _messagePool = messagePool;
        _data = sequence;
    }

    internal void Init(uint serial, MessageFlags flags, UnixFdCollection? handles)
    {
        Serial = serial;
        MessageFlags = flags;
        Handles = handles;
    }

    public void Dispose() => ReturnToPool();

    internal void ReturnToPool()
    {
        _data.Reset();
        Handles?.DisposeHandles();
        Handles = null;
        _messagePool.Return(this);
    }

    // For writing data.
    internal Sequence<byte> Sequence => _data;

    // For reading data.
    internal ReadOnlySequence<byte> AsReadOnlySequence() => _data.AsReadOnlySequence;
}