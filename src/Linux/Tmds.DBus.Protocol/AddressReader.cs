namespace Tmds.DBus.Protocol;

static class AddressParser
{
    public struct AddressEntry
    {
        internal string String { get; }
        internal int Offset { get; }
        internal int Count { get; }

        internal AddressEntry(string s, int offset, int count) =>
            (String, Offset, Count) = (s, offset, count);

        internal ReadOnlySpan<char> AsSpan() => String.AsSpan(Offset, Count);

        public override string ToString() => AsSpan().AsString();
    }

    public static bool TryGetNextEntry(string addresses, ref AddressEntry address)
    {
        int offset = address.String is null ? 0 : address.Offset + address.Count + 1;
        if (offset >= addresses.Length - 1)
        {
            return false;
        }
        ReadOnlySpan<char> span = addresses.AsSpan().Slice(offset);
        int length = span.IndexOf(';');
        if (length == -1)
        {
            length = span.Length;
        }
        address = new AddressEntry(addresses, offset, length);
        return true;
    }

    public static bool IsType(AddressEntry address, string type)
    {
        ReadOnlySpan<char> span = address.AsSpan();
        return span.Length > type.Length && span[type.Length] == ':' && span.StartsWith(type.AsSpan());
    }

    public static void ParseTcpProperties(AddressEntry address, out string host, out int? port, out Guid guid)
    {
        host = null!;
        port = null;
        guid = default;
        ReadOnlySpan<char> properties = GetProperties(address);
        while (TryParseProperty(ref properties, out ReadOnlySpan<char> key, out ReadOnlySpan<char> value))
        {
            if (key.SequenceEqual("host".AsSpan()))
            {
                host = Unescape(value);
            }
            else if (key.SequenceEqual("port".AsSpan()))
            {
                port = int.Parse(Unescape(value));
            }
            else if (key.SequenceEqual("guid".AsSpan()))
            {
                guid = Guid.ParseExact(Unescape(value), "N");
            }
        }
        if (host is null)
        {
            host = "localhost";
        }
    }

    public static void ParseUnixProperties(AddressEntry address, out string path, out Guid guid)
    {
        path = null!;
        bool isAbstract = false;
        guid = default;
        ReadOnlySpan<char> properties = GetProperties(address);
        while (TryParseProperty(ref properties, out ReadOnlySpan<char> key, out ReadOnlySpan<char> value))
        {
            if (key.SequenceEqual("path".AsSpan()))
            {
                path = Unescape(value);
            }
            else if (key.SequenceEqual("abstract".AsSpan()))
            {
                isAbstract = true;
                path = Unescape(value);
            }
            else if (key.SequenceEqual("guid".AsSpan()))
            {
                guid = Guid.ParseExact(Unescape(value), "N");
            }
        }
        if (string.IsNullOrEmpty(path))
        {
            throw new ArgumentException("path");
        }
        if (isAbstract)
        {
            path = (char)'\0' + path;
        }
    }

    private static ReadOnlySpan<char> GetProperties(AddressEntry address)
    {
        ReadOnlySpan<char> span = address.AsSpan();
        int colonPos = span.IndexOf(':');
        if (colonPos == -1)
        {
            throw new FormatException("No colon found.");
        }
        return span.Slice(colonPos + 1);
    }

    public static bool TryParseProperty(ref ReadOnlySpan<char> properties, out ReadOnlySpan<char> key, out ReadOnlySpan<char> value)
    {
        if (properties.Length == 0)
        {
            key = default;
            value = default;
            return false;
        }
        int end = properties.IndexOf(',');
        ReadOnlySpan<char> property;
        if (end == -1)
        {
            property = properties;
            properties = default;
        }
        else
        {
            property = properties.Slice(0, end);
            properties = properties.Slice(end + 1);
        }
        int equalPos = property.IndexOf('=');
        if (equalPos == -1)
        {
            throw new FormatException("No equals sign found.");
        }
        key = property.Slice(0, equalPos);
        value = property.Slice(equalPos + 1);
        return true;
    }

    private static string Unescape(ReadOnlySpan<char> value)
    {
        if (!value.Contains("%".AsSpan(), StringComparison.Ordinal))
        {
            return value.AsString();
        }
        Span<char> unescaped = stackalloc char[Constants.StackAllocCharThreshold];
        int pos = 0;
        for (int i = 0; i < value.Length;)
        {
            char c = value[i++];
            if (c != '%')
            {
                unescaped[pos++] = c;
            }
            else if (i + 2 < value.Length)
            {
                int a = FromHexChar(value[i++]);
                int b = FromHexChar(value[i++]);
                if (a == -1 || b == -1)
                {
                    throw new FormatException("Invalid hex char.");
                }
                unescaped[pos++] = (char)((a << 4) + b);
            }
            else
            {
                throw new FormatException("Escape sequence is too short.");
            }
        }
        return unescaped.Slice(0, pos).AsString();

        static int FromHexChar(char c)
        {
            if (c >= '0' && c <= '9')
            {
                return c - '0';
            }
            if (c >= 'A' && c <= 'F')
            {
                return c - 'A' + 10;
            }
            if (c >= 'a' && c <= 'f')
            {
                return c - 'a' + 10;
            }
            return -1;
        }
    }
}