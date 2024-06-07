namespace Tmds.DBus.Protocol;

class MessageBufferPool
{
    private const int MinimumSpanLength = 512;

    public static readonly MessageBufferPool Shared = new MessageBufferPool(Environment.ProcessorCount * 2);

    private readonly int _maxSize;
    private readonly Stack<MessageBuffer> _pool = new Stack<MessageBuffer>();

    internal MessageBufferPool(int maxSize)
    {
        _maxSize = maxSize;
    }

    public MessageBuffer Rent()
    {
        lock (_pool)
        {
            if (_pool.Count > 0)
            {
                return _pool.Pop();
            }
        }

        var sequence = new Sequence<byte>(ArrayPool<byte>.Shared) { MinimumSpanLength = MinimumSpanLength };

        return new MessageBuffer(this, sequence);
    }

    internal void Return(MessageBuffer value)
    {
        lock (_pool)
        {
            if (_pool.Count < _maxSize)
            {
                _pool.Push(value);
            }
        }
    }
}
