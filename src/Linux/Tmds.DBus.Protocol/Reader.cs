[assembly: InternalsVisibleTo("Tmds.DBus.Protocol.Tests, PublicKey=002400000480000094000000060200000024000052534131000400000100010071a8770f460cce31df0feb6f94b328aebd55bffeb5c69504593df097fdd9b29586dbd155419031834411c8919516cc565dee6b813c033676218496edcbe7939c0dd1f919f3d1a228ebe83b05a3bbdbae53ce11bcf4c04a42d8df1a83c2d06cb4ebb0b447e3963f48a1ca968996f3f0db8ab0e840a89d0a5d5a237e2f09189ed3")]

namespace Tmds.DBus.Protocol;

public ref partial struct Reader
{
    private delegate object ValueReader(ref Reader reader);

    private readonly bool _isBigEndian;
    private readonly UnixFdCollection? _handles;
    private readonly int _handleCount;
    private SequenceReader<byte> _reader;

    internal ReadOnlySequence<byte> UnreadSequence => _reader.Sequence.Slice(_reader.Position);

    internal void Advance(long count) => _reader.Advance(count);

    internal Reader(bool isBigEndian, ReadOnlySequence<byte> sequence) : this(isBigEndian, sequence, handles: null, 0) { }

    internal Reader(bool isBigEndian, ReadOnlySequence<byte> sequence, UnixFdCollection? handles, int handleCount)
    {
        _reader = new(sequence);

        _isBigEndian = isBigEndian;
        _handles = handles;
        _handleCount = handleCount;
    }

    public void AlignStruct() => AlignReader(DBusType.Struct);

    private void AlignReader(DBusType type)
    {
        long pad = ProtocolConstants.GetPadding((int)_reader.Consumed, type);
        if (pad != 0)
        {
            _reader.Advance(pad);
        }
    }

    private void AlignReader(int alignment)
    {
        long pad = ProtocolConstants.GetPadding((int)_reader.Consumed, alignment);
        if (pad != 0)
        {
            _reader.Advance(pad);
        }
    }

    public ArrayEnd ReadArrayStart(DBusType elementType)
    {
        uint arrayLength = ReadUInt32();
        AlignReader(elementType);
        int endOfArray = (int)(_reader.Consumed + arrayLength);
        return new ArrayEnd(elementType, endOfArray);
    }

    public bool HasNext(ArrayEnd iterator)
    {
        int consumed = (int)_reader.Consumed;
        int nextElement = ProtocolConstants.Align(consumed, iterator.Type);
        if (nextElement >= iterator.EndOfArray)
        {
            return false;
        }
        int advance = nextElement - consumed;
        if (advance != 0)
        {
            _reader.Advance(advance);
        }
        return true;
    }

    public void SkipTo(ArrayEnd end)
    {
        int advance = end.EndOfArray - (int)_reader.Consumed;
        _reader.Advance(advance);
    }
}

public ref struct ArrayEnd
{
    internal readonly DBusType Type;
    internal readonly int EndOfArray;

    internal ArrayEnd(DBusType type, int endOfArray)
    {
        Type = type;
        EndOfArray = endOfArray;
    }
}