using System.Threading.Channels;

namespace Tmds.DBus.Protocol;

public partial class Connection
{
    public const string DBusObjectPath = "/org/freedesktop/DBus";
    public const string DBusServiceName = "org.freedesktop.DBus";
    public const string DBusInterface = "org.freedesktop.DBus";

    public Task<string[]> ListServicesAsync()
    {
        return CallMethodAsync(CreateMessage(), (Message m, object? s) => m.GetBodyReader().ReadArrayOfString());
        MessageBuffer CreateMessage()
        {
            using var writer = GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: DBusServiceName,
                path: DBusObjectPath,
                @interface: DBusInterface,
                member: "ListNames");
            return writer.CreateMessage();
        }
    }

    public Task<string[]> ListActivatableServicesAsync()
    {
        return CallMethodAsync(CreateMessage(), (Message m, object? s) => m.GetBodyReader().ReadArrayOfString());
        MessageBuffer CreateMessage()
        {
            using var writer = GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: DBusServiceName,
                path: DBusObjectPath,
                @interface: DBusInterface,
                member: "ListActivatableNames");
            return writer.CreateMessage();
        }
    }

    public async Task BecomeMonitorAsync(Action<Exception?, DisposableMessage> handler, IEnumerable<MatchRule>? rules = null)
    {
        if (_connectionOptions.IsShared)
        {
            throw new InvalidOperationException("Cannot become monitor on a shared connection.");
        }

        DBusConnection connection = await ConnectCoreAsync().ConfigureAwait(false);
        await connection.BecomeMonitorAsync(handler, rules).ConfigureAwait(false);
    }

    public static async IAsyncEnumerable<DisposableMessage> MonitorBusAsync(string address, IEnumerable<MatchRule>? rules = null, [EnumeratorCancellation]CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var channel = Channel.CreateUnbounded<DisposableMessage>(
            new UnboundedChannelOptions()
            {
                AllowSynchronousContinuations = true,
                SingleReader = true,
                SingleWriter = true,
            }
        );

        using var connection = new Connection(address);
        using CancellationTokenRegistration ctr =
#if NETCOREAPP3_1_OR_GREATER
                ct.UnsafeRegister(c => ((Connection)c!).Dispose(), connection);
#else
                ct.Register(c => ((Connection)c!).Dispose(), connection);
#endif
        try
        {
            await connection.ConnectAsync().ConfigureAwait(false);

            await connection.BecomeMonitorAsync(
                (Exception? ex, DisposableMessage message) =>
                {
                    if (ex is not null)
                    {
                        if (ct.IsCancellationRequested)
                        {
                            ex = new OperationCanceledException(ct);
                        }
                        channel.Writer.TryComplete(ex);
                        return;
                    }

                    if (!channel.Writer.TryWrite(message))
                    {
                        message.Dispose();
                    }
                },
                rules
            ).ConfigureAwait(false);
        }
        catch
        {
            ct.ThrowIfCancellationRequested();

            throw;
        }
     
        while (await channel.Reader.WaitToReadAsync().ConfigureAwait(false))
        {
            if (channel.Reader.TryRead(out DisposableMessage msg))
            {
                yield return msg;
            }
        }
    }
}