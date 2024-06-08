namespace Tmds.DBus.Protocol;

static class StringBuilderExtensions
{
    public static void AppendUTF8(this StringBuilder sb, ReadOnlySpan<byte> value)
    {
        char[]? valueArray = null;

        int length = Encoding.UTF8.GetCharCount(value);

        Span<char> charBuffer = length <= Constants.StackAllocCharThreshold ?
            stackalloc char[length] :
            (valueArray = ArrayPool<char>.Shared.Rent(length));

        int charsWritten = Encoding.UTF8.GetChars(value, charBuffer);

        sb.Append(charBuffer.Slice(0, charsWritten));

        if (valueArray is not null)
        {
            ArrayPool<char>.Shared.Return(valueArray);
        }
    }
}