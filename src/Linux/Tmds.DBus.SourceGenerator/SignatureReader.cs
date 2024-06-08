using System;


namespace Tmds.DBus.SourceGenerator
{
    internal ref struct SignatureReader
    {
        private ReadOnlySpan<byte> _signature;

        public readonly ReadOnlySpan<byte> Signature => _signature;

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
                innerSignature = type switch
                {
                    DBusType.Array => _signature.Slice(1, length - 1),
                    DBusType.Struct or DBusType.DictEntry => _signature.Slice(1, length - 2),
                    _ => innerSignature
                };
            }

            _signature = _signature.Slice(length);

            return true;
        }

        private static DBusType ReadSingleType(ReadOnlySpan<byte> signature, out int length)
        {
            length = 0;

            if (signature.IsEmpty)
                return DBusType.Invalid;

            DBusType type = (DBusType)signature[0];

            if (IsBasicType(type))
                length = 1;
            else
            {
                switch (type)
                {
                    case DBusType.Variant:
                        length = 1;
                        break;
                    case DBusType.Array when ReadSingleType(signature.Slice(1), out int elementLength) != DBusType.Invalid:
                        type = DBusType.Array;
                        length = elementLength + 1;
                        break;
                    case DBusType.Array:
                        type = DBusType.Invalid;
                        break;
                    case DBusType.Struct:
                        length = DetermineLength(signature.Slice(1), (byte)'(', (byte)')');
                        if (length == 0)
                            type = DBusType.Invalid;
                        break;
                    case DBusType.DictEntry:
                        length = DetermineLength(signature.Slice(1), (byte)'{', (byte)'}');
                        if (length < 4 ||
                            !IsBasicType((DBusType)signature[1]) ||
                            ReadSingleType(signature.Slice(2), out int valueTypeLength) == DBusType.Invalid ||
                            length != valueTypeLength + 3)
                            type = DBusType.Invalid;
                        break;
                    default:
                        type = DBusType.Invalid;
                        break;
                }
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
                    return 0;

                if (span[offset] == startChar)
                    count++;
                else
                    count--;

                length += offset + 1;
                span = span.Slice(offset + 1);
            } while (count > 0);

            return length;
        }

        private static bool IsBasicType(DBusType type) => type is DBusType.Byte or DBusType.Bool or DBusType.Int16 or DBusType.UInt16 or DBusType.Int32 or DBusType.UInt32 or DBusType.Int64 or DBusType.UInt64 or DBusType.Double or DBusType.String or DBusType.ObjectPath or DBusType.Signature or DBusType.UnixFd;

        private static ReadOnlySpan<byte> ReadSingleType(ref ReadOnlySpan<byte> signature)
        {
            if (signature.Length == 0)
                return default;

            int length;
            DBusType type = (DBusType)signature[0];
            switch (type)
            {
                case DBusType.Struct:
                    length = DetermineLength(signature.Slice(1), (byte)'(', (byte)')');
                    break;
                case DBusType.DictEntry:
                    length = DetermineLength(signature.Slice(1), (byte)'{', (byte)'}');
                    break;
                case DBusType.Array:
                    ReadOnlySpan<byte> remainder = signature.Slice(1);
                    length = 1 + ReadSingleType(ref remainder).Length;
                    break;
                default:
                    length = 1;
                    break;
            }

            ReadOnlySpan<byte> rv = signature.Slice(0, length);
            signature = signature.Slice(length);
            return rv;
        }

        public static T Transform<T>(ReadOnlySpan<byte> signature, Func<DBusType, T[], T> map)
        {
            DBusType dbusType = signature.Length == 0 ? DBusType.Invalid : (DBusType)signature[0];

            switch (dbusType)
            {
                case DBusType.Array when (DBusType)signature[1] == DBusType.DictEntry:
                    signature = signature.Slice(2);
                    ReadOnlySpan<byte> keySignature = ReadSingleType(ref signature);
                    ReadOnlySpan<byte> valueSignature = ReadSingleType(ref signature);
                    signature = signature.Slice(1);
                    T keyType = Transform(keySignature, map);
                    T valueType = Transform(valueSignature, map);
                    return map(DBusType.DictEntry, [keyType, valueType]);
                case DBusType.Array:
                    signature = signature.Slice(1);
                    T elementType = Transform(signature, map);
                    //signature = signature.Slice(1);
                    return map(DBusType.Array, [elementType]);
                case DBusType.Struct:
                    signature = signature.Slice(1, signature.Length - 2);
                    int typeCount = CountTypes(signature);
                    T[] innerTypes = new T[typeCount];
                    for (int i = 0; i < innerTypes.Length; i++)
                    {
                        ReadOnlySpan<byte> innerTypeSignature = ReadSingleType(ref signature);
                        innerTypes[i] = Transform(innerTypeSignature, map);
                    }

                    return map(DBusType.Struct, innerTypes);
                default:
                    return map(dbusType, []);
            }
        }

        // Counts the number of single types in a signature.
        private static int CountTypes(ReadOnlySpan<byte> signature)
        {
            if (signature.Length is 0 or 1)
                return signature.Length;

            DBusType type = (DBusType)signature[0];
            signature = signature.Slice(1);

            if (type == DBusType.Struct)
                ReadToEnd(ref signature, (byte)'(', (byte)')');
            else if (type == DBusType.DictEntry)
                ReadToEnd(ref signature, (byte)'{', (byte)'}');

            return (type == DBusType.Array ? 0 : 1) + CountTypes(signature);

            static void ReadToEnd(ref ReadOnlySpan<byte> span, byte startChar, byte endChar)
            {
                int count = 1;
                do
                {
                    int offset = span.IndexOfAny(startChar, endChar);
                    if (span[offset] == startChar)
                        count++;
                    else
                        count--;
                    span = span.Slice(offset + 1);
                } while (count > 0);
            }
        }
    }
}
