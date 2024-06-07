namespace Tmds.DBus.Protocol;

public ref partial struct Reader
{
    public VariantValue ReadVariantValue()
    {
        Utf8Span signature = ReadSignature();
        SignatureReader sigReader = new(signature);
        if (!sigReader.TryRead(out DBusType type, out ReadOnlySpan<byte> innerSignature))
        {
            ThrowInvalidSignature($"Invalid variant signature: {signature.ToString()}");
        }
        return ReadTypeAsVariantValue(type, innerSignature);
    }

    private VariantValue ReadTypeAsVariantValue(DBusType type, ReadOnlySpan<byte> innerSignature)
    {
        SignatureReader sigReader;
        switch (type)
        {
            case DBusType.Byte:
                return new VariantValue(ReadByte());
            case DBusType.Bool:
                return new VariantValue(ReadBool());
            case DBusType.Int16:
                return new VariantValue(ReadInt16());
            case DBusType.UInt16:
                return new VariantValue(ReadUInt16());
            case DBusType.Int32:
                return new VariantValue(ReadInt32());
            case DBusType.UInt32:
                return new VariantValue(ReadUInt32());
            case DBusType.Int64:
                return new VariantValue(ReadInt64());
            case DBusType.UInt64:
                return new VariantValue(ReadUInt64());
            case DBusType.Double:
                return new VariantValue(ReadDouble());
            case DBusType.String:
                return new VariantValue(ReadString());
            case DBusType.ObjectPath:
                return new VariantValue(ReadObjectPath());
            case DBusType.Signature:
                return new VariantValue(ReadSignatureAsSignature());
            case DBusType.UnixFd:
                int idx = (int)ReadUInt32();
                return new VariantValue(_handles, idx);
            case DBusType.Variant:
                return ReadVariantValue();
            case DBusType.Array:
                sigReader = new(innerSignature);
                if (!sigReader.TryRead(out type, out innerSignature))
                {
                    ThrowInvalidSignature("Signature is missing array item type.");
                }
                bool isDictionary = type == DBusType.DictEntry;
                if (isDictionary)
                {
                    sigReader = new(innerSignature);
                    DBusType valueType = default;
                    ReadOnlySpan<byte> valueInnerSignature = default;
                    if (!sigReader.TryRead(out DBusType keyType, out ReadOnlySpan<byte> keyInnerSignature) ||
                        !sigReader.TryRead(out valueType, out valueInnerSignature))
                    {
                        ThrowInvalidSignature("Signature is missing dict entry types.");
                    }
                    List<KeyValuePair<VariantValue, VariantValue>> items = new();
                    ArrayEnd arrayEnd = ReadArrayStart(type);
                    while (HasNext(arrayEnd))
                    {
                        AlignStruct();
                        VariantValue key = ReadTypeAsVariantValue(keyType, keyInnerSignature);
                        VariantValue value = ReadTypeAsVariantValue(valueType, valueInnerSignature);
                        items.Add(new KeyValuePair<VariantValue, VariantValue>(key, value));
                    }
                    return new VariantValue(ToVariantValueType(keyType), ToVariantValueType(valueType), items.ToArray());
                }
                else
                {
                    if (type == DBusType.Byte)
                    {
                        return new VariantValue(ReadArrayOfByte());
                    }
                    else if (type == DBusType.Int16)
                    {
                        return new VariantValue(ReadArrayOfInt16());
                    }
                    else if (type == DBusType.UInt16)
                    {
                        return new VariantValue(ReadArrayOfUInt16());
                    }
                    else if (type == DBusType.Int32)
                    {
                        return new VariantValue(ReadArrayOfInt32());
                    }
                    else if (type == DBusType.UInt32)
                    {
                        return new VariantValue(ReadArrayOfUInt32());
                    }
                    else if (type == DBusType.Int64)
                    {
                        return new VariantValue(ReadArrayOfInt64());
                    }
                    else if (type == DBusType.UInt64)
                    {
                        return new VariantValue(ReadArrayOfUInt64());
                    }
                    else if (type == DBusType.Double)
                    {
                        return new VariantValue(ReadArrayOfDouble());
                    }
                    else if (type == DBusType.String ||
                             type == DBusType.ObjectPath)
                    {
                        return new VariantValue(ToVariantValueType(type), ReadArrayOfString());
                    }
                    else
                    {
                        List<VariantValue> items = new();
                        ArrayEnd arrayEnd = ReadArrayStart(type);
                        while (HasNext(arrayEnd))
                        {
                            VariantValue value = ReadTypeAsVariantValue(type, innerSignature);
                            items.Add(value);
                        }
                        return new VariantValue(ToVariantValueType(type), items.ToArray());
                    }
                }
            case DBusType.Struct:
                {
                    AlignStruct();
                    sigReader = new(innerSignature);
                    List<VariantValue> items = new();
                    while (sigReader.TryRead(out type, out innerSignature))
                    {
                        VariantValue value = ReadTypeAsVariantValue(type, innerSignature);
                        items.Add(value);
                    }
                    return new VariantValue(items.ToArray());
                }
            case DBusType.DictEntry: // Already handled under DBusType.Array.
            default:
                // note: the SignatureReader maps all unknown types to DBusType.Invalid
                //       so we won't see the actual character that caused it to fail.
                ThrowInvalidSignature($"Unexpected type in signature: {type}.");
                return default;
        }
    }

    private void ThrowInvalidSignature(string message)
    {
        throw new ProtocolException(message);
    }

    private static VariantValueType ToVariantValueType(DBusType type)
        => type switch
        {
            DBusType.Variant => VariantValueType.VariantValue,
            _ => (VariantValueType)type
        };
}
