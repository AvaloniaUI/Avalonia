[assembly: InternalsVisibleTo("dotnet-dbus, PublicKey=002400000480000094000000060200000024000052534131000400000100010071a8770f460cce31df0feb6f94b328aebd55bffeb5c69504593df097fdd9b29586dbd155419031834411c8919516cc565dee6b813c033676218496edcbe7939c0dd1f919f3d1a228ebe83b05a3bbdbae53ce11bcf4c04a42d8df1a83c2d06cb4ebb0b447e3963f48a1ca968996f3f0db8ab0e840a89d0a5d5a237e2f09189ed3")]

namespace Tmds.DBus.Protocol;

public ref struct SignatureReader
{
    private ReadOnlySpan<byte> _signature;

    public ReadOnlySpan<byte> Signature => _signature;

    public SignatureReader(ReadOnlySpan<byte> signature)
    {
        _signature = signature;
    }

    public bool TryRead(out DBusType type, out ReadOnlySpan<byte> innerSignature)
    {
        innerSignature = default;

        if (_signature.IsEmpty)
        {
            type = DBusType.Invalid;
            return false;
        }

        type = ReadSingleType(_signature, out int length);

        if (length > 1)
        {
            switch (type)
            {
                case DBusType.Array:
                    innerSignature = _signature.Slice(1, length - 1);
                    break;
                case DBusType.Struct:
                case DBusType.DictEntry:
                    innerSignature = _signature.Slice(1, length - 2);
                    break;
            }
        }

        _signature = _signature.Slice(length);

        return true;
    }

    private static DBusType ReadSingleType(ReadOnlySpan<byte> signature, out int length)
    {
        length = 0;

        if (signature.IsEmpty)
        {
            return DBusType.Invalid;
        }

        DBusType type = (DBusType)signature[0];

        if (IsBasicType(type))
        {
            length = 1;
        }
        else if (type == DBusType.Variant)
        {
            length = 1;
        }
        else if (type == DBusType.Array)
        {
            if (ReadSingleType(signature.Slice(1), out int elementLength) != DBusType.Invalid)
            {
                type = DBusType.Array;
                length = elementLength + 1;
            }
            else
            {
                type = DBusType.Invalid;
            }
        }
        else if (type == DBusType.Struct)
        {
            length = DetermineLength(signature.Slice(1), (byte)'(', (byte)')');
            if (length == 0)
            {
                type = DBusType.Invalid;
            }
        }
        else if (type == DBusType.DictEntry)
        {
            length = DetermineLength(signature.Slice(1), (byte)'{', (byte)'}');
            if (length < 4 ||
                !IsBasicType((DBusType)signature[1]) ||
                ReadSingleType(signature.Slice(2), out int valueTypeLength) == DBusType.Invalid ||
                length != valueTypeLength + 3)
            {
                type = DBusType.Invalid;
            }
        }
        else
        {
            type = DBusType.Invalid;
        }

        return type;
    }

    private static int DetermineLength(ReadOnlySpan<byte> span, byte startChar, byte endChar)
    {
        int length = 1;
        int count = 1;
        do
        {
            int offset = span.IndexOfAny(startChar, endChar);
            if (offset == -1)
            {
                return 0;
            }

            if (span[offset] == startChar)
            {
                count++;
            }
            else
            {
                count--;
            }

            length += offset + 1;
            span = span.Slice(offset + 1);

        } while (count > 0);

        return length;
    }

    private static bool IsBasicType(DBusType type)
    {
        return BasicTypes.IndexOf((byte)type) != -1;
    }

    private static ReadOnlySpan<byte> BasicTypes => new byte[] {
        (byte)DBusType.Byte,
        (byte)DBusType.Bool,
        (byte)DBusType.Int16,
        (byte)DBusType.UInt16,
        (byte)DBusType.Int32,
        (byte)DBusType.UInt32,
        (byte)DBusType.Int64,
        (byte)DBusType.UInt64,
        (byte)DBusType.Double,
        (byte)DBusType.String,
        (byte)DBusType.ObjectPath,
        (byte)DBusType.Signature,
        (byte)DBusType.UnixFd };

    private static ReadOnlySpan<byte> ReadSingleType(ref ReadOnlySpan<byte> signature)
    {
        if (signature.Length == 0)
        {
            return default;
        }

        int length;
        DBusType type = (DBusType)signature[0];
        if (type == DBusType.Struct)
        {
            length = DetermineLength(signature.Slice(1), (byte)'(', (byte)')');
        }
        else if (type == DBusType.DictEntry)
        {
            length = DetermineLength(signature.Slice(1), (byte)'{', (byte)'}');
        }
        else if (type == DBusType.Array)
        {
            ReadOnlySpan<byte> remainder = signature.Slice(1);
            length = 1 + ReadSingleType(ref remainder).Length;
        }
        else
        {
            length = 1;
        }

        ReadOnlySpan<byte> rv = signature.Slice(0, length);
        signature = signature.Slice(length);
        return rv;
    }

    internal static T Transform<T>(ReadOnlySpan<byte> signature, Func<DBusType, T[], T> map)
    {
        DBusType dbusType = signature.Length == 0 ? DBusType.Invalid : (DBusType)signature[0];

        if (dbusType == DBusType.Array)
        {
            if ((DBusType)signature[1] == DBusType.DictEntry)
            {
                signature = signature.Slice(2);
                ReadOnlySpan<byte> keySignature = ReadSingleType(ref signature);
                ReadOnlySpan<byte> valueSignature = ReadSingleType(ref signature);
                signature = signature.Slice(1);
                T keyType = Transform(keySignature, map);
                T valueType = Transform(valueSignature, map);
                return map(DBusType.DictEntry, new[] { keyType, valueType });
            }
            else
            {
                signature = signature.Slice(1);
                T elementType = Transform(signature, map);
                signature = signature.Slice(1);
                return map(DBusType.Array, new[] { elementType });
            }
        }
        else if (dbusType == DBusType.Struct)
        {
            signature = signature.Slice(1, signature.Length - 2);
            int typeCount = CountTypes(signature);
            T[] innerTypes = new T[typeCount];
            for (int i = 0; i < innerTypes.Length; i++)
            {
                ReadOnlySpan<byte> innerTypeSignature = ReadSingleType(ref signature);
                innerTypes[i] = Transform(innerTypeSignature, map);
            }
            return map(DBusType.Struct, innerTypes);
        }

        return map(dbusType, Array.Empty<T>());
    }

    // Counts the number of single types in a signature.
    private static int CountTypes(ReadOnlySpan<byte> signature)
    {
        if (signature.Length == 0)
        {
            return 0;
        }

        if (signature.Length == 1)
        {
            return 1;
        }

        DBusType type = (DBusType)signature[0];
        signature = signature.Slice(1);

        if (type == DBusType.Struct)
        {
            ReadToEnd(ref signature, (byte)'(', (byte)')');
        }
        else if (type == DBusType.DictEntry)
        {
            ReadToEnd(ref signature, (byte)'{', (byte)'}');
        }

        return (type == DBusType.Array ? 0 : 1) + CountTypes(signature);

        static void ReadToEnd(ref ReadOnlySpan<byte> span, byte startChar, byte endChar)
        {
            int count = 1;
            do
            {
                int offset = span.IndexOfAny(startChar, endChar);
                if (span[offset] == startChar)
                {
                    count++;
                }
                else
                {
                    count--;
                }
                span = span.Slice(offset + 1);
            } while (count > 0);
        }
    }
}
