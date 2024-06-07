using System.Net;
using System.Net.Sockets;
using System.Reflection;

namespace Tmds.DBus.Protocol;

#if NETSTANDARD2_0 || NETSTANDARD2_1
static partial class NetstandardExtensions
{

    private static PropertyInfo s_safehandleProperty = typeof(Socket).GetTypeInfo().GetDeclaredProperty("SafeHandle");

    private const int MaxInputElementsPerIteration = 1 * 1024 * 1024;

    public static bool IsAssignableTo(this Type type, Type? targetType)
        => targetType?.IsAssignableFrom(type) ?? false;

    public static SafeHandle GetSafeHandle(this Socket socket)
    {
        if (s_safehandleProperty != null)
        {
            return (SafeHandle)s_safehandleProperty.GetValue(socket, null);
        }
        ThrowHelper.ThrowNotSupportedException();
        return null!;
    }

    public static async Task ConnectAsync(this Socket socket, EndPoint remoteEP, CancellationToken cancellationToken)
    {
        using var ctr = cancellationToken.Register(state => ((Socket)state!).Dispose(), socket, useSynchronizationContext: false);
        try
        {
            await Task.Factory.FromAsync(
                (targetEndPoint, callback, state) => ((Socket)state).BeginConnect(targetEndPoint, callback, state),
                asyncResult => ((Socket)asyncResult.AsyncState).EndConnect(asyncResult),
                remoteEP,
                state: socket).ConfigureAwait(false);
        }
        catch (ObjectDisposedException)
        {
            cancellationToken.ThrowIfCancellationRequested();

            throw;
        }
    }
}
#else
static partial class NetstandardExtensions
{
    public static SafeHandle GetSafeHandle(this Socket socket)
        => socket.SafeHandle;
}
#endif
