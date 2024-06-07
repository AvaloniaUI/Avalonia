namespace Tmds.DBus.Protocol;

public ref partial struct MessageWriter
{
    private const int LengthOffset = 4;
    private const int SerialOffset = 8;
    private const int HeaderFieldsLengthOffset = 12;
    private const int UnixFdLengthOffset = 20;

    private MessageBuffer _message;
    private Sequence<byte> _data;
    private UnixFdCollection? _handles;
    private readonly uint _serial;
    private MessageFlags _flags;
    private Span<byte> _firstSpan;
    private Span<byte> _span;
    private int _offset;
    private int _buffered;

    public MessageBuffer CreateMessage()
    {
        Flush();

        Span<byte> span = _firstSpan;

        // Length
        uint headerFieldsLength = Unsafe.ReadUnaligned<uint>(ref MemoryMarshal.GetReference(span.Slice(HeaderFieldsLengthOffset)));
        uint pad = headerFieldsLength % 8;
        if (pad != 0)
        {
            headerFieldsLength += (8 - pad);
        }
        uint length = (uint)_data.Length                 // Total length
                      - headerFieldsLength               // Header fields
                      - 4                                // Header fields length
                      - (uint)HeaderFieldsLengthOffset;  // Preceeding header fields
        Unsafe.WriteUnaligned<uint>(ref MemoryMarshal.GetReference(span.Slice(LengthOffset)), length);

        // UnixFdLength
        Unsafe.WriteUnaligned<uint>(ref MemoryMarshal.GetReference(span.Slice(UnixFdLengthOffset)), (uint)HandleCount);

        uint serial = _serial;
        MessageFlags flags = _flags;
        ReadOnlySequence<byte> data = _data;
        UnixFdCollection? handles = _handles;
        var message = _message;

        _message = null!;
        _handles = null;
        _data = null!;

        message.Init(serial, flags, handles);

        return message;
    }

    internal MessageWriter(MessageBufferPool messagePool, uint serial)
    {
        _message = messagePool.Rent();
        _data = _message.Sequence;
        _handles = null;
        _flags = default;
        _offset = 0;
        _buffered = 0;
        _serial = serial;
        _firstSpan = _span = _data.GetSpan(sizeHint: 0);
    }

    public ArrayStart WriteArrayStart(DBusType elementType)
    {
        // Array length.
        WritePadding(DBusType.UInt32);
        Span<byte> lengthSpan = GetSpan(4);
        Advance(4);

        WritePadding(elementType);

        return new ArrayStart(lengthSpan, _offset);
    }

    public void WriteArrayEnd(ArrayStart start)
    {
        start.WriteLength(_offset);
    }

    public void WriteStructureStart()
    {
        WritePadding(DBusType.Struct);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Advance(int count)
    {
        _buffered += count;
        _offset += count;
        _span = _span.Slice(count);
    }

    private void WritePadding(DBusType type)
    {
        int pad = ProtocolConstants.GetPadding(_offset, type);
        if (pad != 0)
        {
            GetSpan(pad).Slice(0, pad).Fill(0);
            Advance(pad);
        }
    }

    private void WritePadding(int alignment)
    {
        int pad = ProtocolConstants.GetPadding(_offset, alignment);
        if (pad != 0)
        {
            GetSpan(pad).Slice(0, pad).Fill(0);
            Advance(pad);
        }
    }

    private Span<byte> GetSpan(int sizeHint)
    {
        Ensure(sizeHint);
        return _span;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Ensure(int count = 1)
    {
        if (_span.Length < count)
        {
            EnsureMore(count);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void EnsureMore(int count = 0)
    {
        if (_buffered > 0)
        {
            Flush();
        }

        _span = _data.GetSpan(count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Flush()
    {
        var buffered = _buffered;
        if (buffered > 0)
        {
            _buffered = 0;
            _data.Advance(buffered);
            _span = default;
        }
    }

    public void Dispose()
    {
        _message?.ReturnToPool();
        _handles?.Dispose();

        _message = null!;
        _data = null!;
        _handles = null!;
    }

    // For Tests.
    internal ReadOnlySequence<byte> AsReadOnlySequence()
    {
        Flush();
        return _data.AsReadOnlySequence;
    }
    // For Tests.
    internal UnixFdCollection? Handles => _handles;
}

public ref struct ArrayStart
{
    private Span<byte> _span;
    private int _offset;

    internal ArrayStart(Span<byte> lengthSpan, int offset)
    {
        _span = lengthSpan;
        _offset = offset;
    }

    internal void WriteLength(int offset)
    {
        uint length = (uint)(offset - _offset);
        Unsafe.WriteUnaligned<uint>(ref MemoryMarshal.GetReference(_span), length);
    }
}
