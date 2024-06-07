namespace Tmds.DBus.Protocol;

static class ThrowHelper
{
    public static void ThrowIfDisposed(bool condition, object instance)
    {
        if (condition)
        {
            ThrowObjectDisposedException(instance);
        }
    }

    private static void ThrowObjectDisposedException(object instance)
    {
        throw new ObjectDisposedException(instance?.GetType().FullName);
    }

    public static void ThrowIndexOutOfRange()
    {
        throw new IndexOutOfRangeException();
    }

    public static void ThrowNotSupportedException()
    {
        throw new NotSupportedException();
    }

    internal static void ThrowUnexpectedSignature(ReadOnlySpan<byte> signature, string expected)
    {
        throw new ProtocolException($"Expected signature '{expected}' does not match actual signature '{Encoding.UTF8.GetString(signature)}'.");
    }
}