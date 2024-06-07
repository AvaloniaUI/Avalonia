namespace Tmds.DBus.Protocol;

public ref partial struct MessageWriter
{
    public void WriteMethodCallHeader(
        string? destination = null,
        string? path = null,
        string? @interface = null,
        string? member = null,
        string? signature = null,
        MessageFlags flags = MessageFlags.None)
    {
        ArrayStart start = WriteHeaderStart(MessageType.MethodCall, flags);

        // Path.
        if (path is not null)
        {
            WriteStructureStart();
            WriteByte((byte)MessageHeader.Path);
            WriteVariantObjectPath(path);
        }

        // Interface.
        if (@interface is not null)
        {
            WriteStructureStart();
            WriteByte((byte)MessageHeader.Interface);
            WriteVariantString(@interface);
        }

        // Member.
        if (member is not null)
        {
            WriteStructureStart();
            WriteByte((byte)MessageHeader.Member);
            WriteVariantString(member);
        }

        // Destination.
        if (destination is not null)
        {
            WriteStructureStart();
            WriteByte((byte)MessageHeader.Destination);
            WriteVariantString(destination);
        }

        // Signature.
        if (signature is not null)
        {
            WriteStructureStart();
            WriteByte((byte)MessageHeader.Signature);
            WriteVariantSignature(signature);
        }

        WriteHeaderEnd(start);
    }

    public void WriteMethodReturnHeader(
        uint replySerial,
        Utf8Span destination = default,
        string? signature = null)
    {
        ArrayStart start = WriteHeaderStart(MessageType.MethodReturn, MessageFlags.None);

        // ReplySerial
        WriteStructureStart();
        WriteByte((byte)MessageHeader.ReplySerial);
        WriteVariantUInt32(replySerial);

        // Destination.
        if (!destination.IsEmpty)
        {
            WriteStructureStart();
            WriteByte((byte)MessageHeader.Destination);
            WriteVariantString(destination);
        }

        // Signature.
        if (signature is not null)
        {
            WriteStructureStart();
            WriteByte((byte)MessageHeader.Signature);
            WriteVariantSignature(signature);
        }

        WriteHeaderEnd(start);
    }

    public void WriteError(
        uint replySerial,
        ReadOnlySpan<byte> destination = default,
        string? errorName = null,
        string? errorMsg = null)
    {
        ArrayStart start = WriteHeaderStart(MessageType.Error, MessageFlags.None);

        // ReplySerial
        WriteStructureStart();
        WriteByte((byte)MessageHeader.ReplySerial);
        WriteVariantUInt32(replySerial);

        // Destination.
        if (!destination.IsEmpty)
        {
            WriteStructureStart();
            WriteByte((byte)MessageHeader.Destination);
            WriteVariantString(destination);
        }

        // Error.
        if (errorName is not null)
        {
            WriteStructureStart();
            WriteByte((byte)MessageHeader.ErrorName);
            WriteVariantString(errorName);
        }

        // Signature.
        if (errorMsg is not null)
        {
            WriteStructureStart();
            WriteByte((byte)MessageHeader.Signature);
            WriteVariantSignature(ProtocolConstants.StringSignature);
        }

        WriteHeaderEnd(start);

        if (errorMsg is not null)
        {
            WriteString(errorMsg);
        }
    }

    public void WriteSignalHeader(
        string? destination = null,
        string? path = null,
        string? @interface = null,
        string? member = null,
        string? signature = null)
    {
        ArrayStart start = WriteHeaderStart(MessageType.Signal, MessageFlags.None);

        // Path.
        if (path is not null)
        {
            WriteStructureStart();
            WriteByte((byte)MessageHeader.Path);
            WriteVariantObjectPath(path);
        }

        // Interface.
        if (@interface is not null)
        {
            WriteStructureStart();
            WriteByte((byte)MessageHeader.Interface);
            WriteVariantString(@interface);
        }

        // Member.
        if (member is not null)
        {
            WriteStructureStart();
            WriteByte((byte)MessageHeader.Member);
            WriteVariantString(member);
        }

        // Destination.
        if (destination is not null)
        {
            WriteStructureStart();
            WriteByte((byte)MessageHeader.Destination);
            WriteVariantString(destination);
        }

        // Signature.
        if (signature is not null)
        {
            WriteStructureStart();
            WriteByte((byte)MessageHeader.Signature);
            WriteVariantSignature(signature);
        }

        WriteHeaderEnd(start);
    }

    private void WriteHeaderEnd(ArrayStart start)
    {
        WriteArrayEnd(start);
        WritePadding(DBusType.Struct);
    }

    private ArrayStart WriteHeaderStart(MessageType type, MessageFlags flags)
    {
        _flags = flags;

        WriteByte(BitConverter.IsLittleEndian ? (byte)'l' : (byte)'B'); // endianness
        WriteByte((byte)type);
        WriteByte((byte)flags);
        WriteByte((byte)1); // version
        WriteUInt32((uint)0); // length placeholder
        Debug.Assert(_offset == LengthOffset + 4);
        WriteUInt32(_serial);
        Debug.Assert(_offset == SerialOffset + 4);

        // headers
        ArrayStart start = WriteArrayStart(DBusType.Struct);

        // UnixFds
        WriteStructureStart();
        WriteByte((byte)MessageHeader.UnixFds);
        WriteVariantUInt32(0); // unix fd length placeholder
        Debug.Assert(_offset == UnixFdLengthOffset + 4);
        return start;
    }
}
