namespace Tmds.DBus.Protocol;

public sealed class Message
{
    private const int HeaderFieldsLengthOffset = 12;

    private readonly MessagePool _pool;
    private readonly Sequence<byte> _data;

    private UnixFdCollection? _handles;
    private ReadOnlySequence<byte> _body;

    public bool IsBigEndian { get; private set; }
    public uint Serial { get; private set; }
    public MessageFlags MessageFlags { get; private set; }
    public MessageType MessageType { get; private set; }

    public uint? ReplySerial { get; private set; }
    public int UnixFdCount { get; private set; }

    private HeaderBuffer _path;
    private HeaderBuffer _interface;
    private HeaderBuffer _member;
    private HeaderBuffer _errorName;
    private HeaderBuffer _destination;
    private HeaderBuffer _sender;
    private HeaderBuffer _signature;

    public string? PathAsString => _path.ToString();
    public string? InterfaceAsString => _interface.ToString();
    public string? MemberAsString => _member.ToString();
    public string? ErrorNameAsString => _errorName.ToString();
    public string? DestinationAsString => _destination.ToString();
    public string? SenderAsString => _sender.ToString();
    public string? SignatureAsString => _signature.ToString();

    public ReadOnlySpan<byte> Path => _path.Span;
    public ReadOnlySpan<byte> Interface => _interface.Span;
    public ReadOnlySpan<byte> Member => _member.Span;
    public ReadOnlySpan<byte> ErrorName => _errorName.Span;
    public ReadOnlySpan<byte> Destination => _destination.Span;
    public ReadOnlySpan<byte> Sender => _sender.Span;
    public ReadOnlySpan<byte> Signature => _signature.Span;

    public bool PathIsSet => _path.IsSet;
    public bool InterfaceIsSet => _interface.IsSet;
    public bool MemberIsSet => _member.IsSet;
    public bool ErrorNameIsSet => _errorName.IsSet;
    public bool DestinationIsSet => _destination.IsSet;
    public bool SenderIsSet => _sender.IsSet;
    public bool SignatureIsSet => _signature.IsSet;

    struct HeaderBuffer
    {
        private byte[] _buffer;
        private int _length;
        private string? _string;

        public Span<byte> Span => new Span<byte>(_buffer, 0, Math.Max(_length, 0));

        public void Set(ReadOnlySpan<byte> data)
        {
            _string = null;
            if (_buffer is null || data.Length > _buffer.Length)
            {
                _buffer = new byte[data.Length];
            }
            data.CopyTo(_buffer);
            _length = data.Length;
        }

        public void Clear()
        {
            _length = -1;
            _string = null;
        }

        public override string? ToString()
        {
            return _length == -1 ? null : _string ??= Encoding.UTF8.GetString(Span);
        }

        public bool IsSet => _length != -1;
    }

    public Reader GetBodyReader() => new Reader(IsBigEndian, _body, _handles, UnixFdCount);

    internal Message(MessagePool messagePool, Sequence<byte> sequence)
    {
        _pool = messagePool;
        _data = sequence;
        ClearHeaders();
    }

    internal void ReturnToPool()
    {
        _data.Reset();
        ClearHeaders();
        _handles?.DisposeHandles();
        _pool.Return(this);
    }

    private void ClearHeaders()
    {
        ReplySerial = null;
        UnixFdCount = 0;

        _path.Clear();
        _interface.Clear();
        _member.Clear();
        _errorName.Clear();
        _destination.Clear();
        _sender.Clear();
        _signature.Clear();
    }

    internal static Message? TryReadMessage(MessagePool messagePool, ref ReadOnlySequence<byte> sequence, UnixFdCollection? handles = null, bool isMonitor = false)
    {
        SequenceReader<byte> seqReader = new(sequence);
        if (!seqReader.TryRead(out byte endianness) ||
            !seqReader.TryRead(out byte msgType) ||
            !seqReader.TryRead(out byte flags) ||
            !seqReader.TryRead(out byte version))
        {
            return null;
        }

        if (version != 1)
        {
            throw new NotSupportedException();
        }

        bool isBigEndian = endianness == 'B';

        if (!TryReadUInt32(ref seqReader, isBigEndian, out uint bodyLength) ||
            !TryReadUInt32(ref seqReader, isBigEndian, out uint serial) ||
            !TryReadUInt32(ref seqReader, isBigEndian, out uint headerFieldLength))
        {
            return null;
        }

        headerFieldLength = (uint)ProtocolConstants.Align((int)headerFieldLength, DBusType.Struct);

        long totalLength = seqReader.Consumed + headerFieldLength + bodyLength;

        if (sequence.Length < totalLength)
        {
            return null;
        }

        // Copy data so it has a lifetime independent of the source sequence.
        var message = messagePool.Rent();
        Sequence<byte> dst = message._data;
        do
        {
            ReadOnlySpan<byte> srcSpan = sequence.First.Span;
            int length = (int)Math.Min(totalLength, srcSpan.Length);
            Span<byte> dstSpan = dst.GetSpan(0);
            length = Math.Min(length, dstSpan.Length);
            srcSpan.Slice(0, length).CopyTo(dstSpan);
            dst.Advance(length);
            sequence = sequence.Slice(length);
            totalLength -= length;
        } while (totalLength > 0);

        message.IsBigEndian = isBigEndian;
        message.Serial = serial;
        message.MessageType = (MessageType)msgType;
        message.MessageFlags = (MessageFlags)flags;
        message.ParseHeader(handles, isMonitor);

        return message;

        static bool TryReadUInt32(ref SequenceReader<byte> seqReader, bool isBigEndian, out uint value)
        {
            int v;
            bool rv = (isBigEndian && seqReader.TryReadBigEndian(out v) || seqReader.TryReadLittleEndian(out v));
            value = (uint)v;
            return rv;
        }
    }

    private void ParseHeader(UnixFdCollection? handles, bool isMonitor)
    {
        var reader = new Reader(IsBigEndian, _data.AsReadOnlySequence);
        reader.Advance(HeaderFieldsLengthOffset);

        ArrayEnd headersEnd = reader.ReadArrayStart(DBusType.Struct);
        while (reader.HasNext(headersEnd))
        {
            MessageHeader hdrType = (MessageHeader)reader.ReadByte();
            ReadOnlySpan<byte> sig = reader.ReadSignature();
            switch (hdrType)
            {
                case MessageHeader.Path:
                    _path.Set(reader.ReadObjectPathAsSpan());
                    break;
                case MessageHeader.Interface:
                    _interface.Set(reader.ReadStringAsSpan());
                    break;
                case MessageHeader.Member:
                    _member.Set(reader.ReadStringAsSpan());
                    break;
                case MessageHeader.ErrorName:
                    _errorName.Set(reader.ReadStringAsSpan());
                    break;
                case MessageHeader.ReplySerial:
                    ReplySerial = reader.ReadUInt32();
                    break;
                case MessageHeader.Destination:
                    _destination.Set(reader.ReadStringAsSpan());
                    break;
                case MessageHeader.Sender:
                    _sender.Set(reader.ReadStringAsSpan());
                    break;
                case MessageHeader.Signature:
                    _signature.Set(reader.ReadSignature());
                    break;
                case MessageHeader.UnixFds:
                    UnixFdCount = (int)reader.ReadUInt32();
                    if (UnixFdCount > 0 && !isMonitor)
                    {
                        if (handles is null || UnixFdCount > handles.Count)
                        {
                            throw new ProtocolException("Received less handles than UNIX_FDS.");
                        }
                        if (_handles is null)
                        {
                            _handles = new(handles.IsRawHandleCollection);
                        }
                        handles.MoveTo(_handles, UnixFdCount);
                    }
                    break;
                default:
                    throw new NotSupportedException();
            }
        }
        reader.AlignStruct();

        _body = reader.UnreadSequence;
    }
}