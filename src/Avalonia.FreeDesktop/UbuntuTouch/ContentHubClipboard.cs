using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Tmds.DBus.Protocol;

namespace Avalonia.FreeDesktop.UbuntuTouch;

internal sealed class ContentHubClipboard : IClipboardImpl
{
    private const string Service = "com.lomiri.content.dbus.Service";
    private readonly string _address;
    private readonly string _appId;
    private readonly TimeSpan _timeout;

    public ContentHubClipboard() : this(DBusAddress.Session ?? "",
        Environment.GetEnvironmentVariable("APP_ID") ?? "", TimeSpan.FromSeconds(2)) { }

    internal ContentHubClipboard(string address, string appId, TimeSpan timeout)
    {
        _address = address;
        _appId = appId;
        _timeout = timeout;
    }

    public async Task SetDataAsync(IAsyncDataTransfer dataTransfer)
    {
        using (dataTransfer)
        {
            var text = await dataTransfer.TryGetTextAsync();
            if (text is null)
                throw new NotSupportedException("Content Hub clipboard currently supports plain text only.");
            await WriteTextAsync(text);
        }
    }

    public Task ClearAsync() => WriteTextAsync("");

    private Task WriteTextAsync(string text)
    {
        var data = ContentHubTextCodec.Encode(text);
        return ExecuteAsync(async connection =>
        {
            MessageBuffer message;
            using (var writer = connection.GetMessageWriter())
            {
                writer.WriteMethodCallHeader(destination: Service, path: "/", @interface: Service,
                    member: "CreatePaste", signature: "ssayas");
                writer.WriteString(_appId);
                // Empty surface ID uses Content Hub's normal caller-PID focus check.
                writer.WriteString("");
                writer.WriteArray(data);
                writer.WriteArray(new[] { "text/plain" });
                message = writer.CreateMessage();
            }
            var accepted = await connection.CallMethodAsync(message,
                static (reply, _) => reply.GetBodyReader().ReadBool()).ConfigureAwait(false);
            if (!accepted)
                throw new UnauthorizedAccessException("Content Hub rejected the clipboard write.");
            return true;
        });
    }

    public async Task<IAsyncDataTransfer?> TryGetDataAsync()
    {
        var bytes = await ExecuteAsync(connection =>
        {
            using var writer = connection.GetMessageWriter();
            writer.WriteMethodCallHeader(destination: Service, path: "/", @interface: Service,
                member: "GetLatestPasteData", signature: "s");
            writer.WriteString("");
            return connection.CallMethodAsync(writer.CreateMessage(),
                static (reply, _) => reply.GetBodyReader().ReadArrayOfByte());
        }).ConfigureAwait(false);
        var text = ContentHubTextCodec.Decode(bytes);
        if (text is null)
            return null;
        var transfer = new DataTransfer();
        transfer.Add(DataTransferItem.CreateText(text));
        return transfer;
    }

    private async Task<T> ExecuteAsync<T>(Func<DBusConnection, Task<T>> action)
    {
        if (!_address.StartsWith("unix:", StringComparison.Ordinal) || _address.Contains(';'))
            throw new UnauthorizedAccessException("Content Hub requires a local session bus.");
        using var timeout = new CancellationTokenSource(_timeout);
        using var connection = new DBusConnection(_address);
        try
        {
            await ObserveAsync(connection.ConnectAsync().AsTask(), timeout.Token).ConfigureAwait(false);
            var pending = action(connection);
            await ObserveAsync(pending, timeout.Token).ConfigureAwait(false);
            return await pending.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException("Content Hub clipboard operation timed out.");
        }
        catch (DBusExceptionBase)
        {
            // Surface a failure to Cut rather than claiming ownership and losing source text.
            throw new UnauthorizedAccessException("Content Hub clipboard operation was rejected.");
        }
    }

    private static async Task ObserveAsync(Task pending, CancellationToken token)
    {
        _ = pending.ContinueWith(t => { _ = t.Exception; }, CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        await pending.WaitAsync(token).ConfigureAwait(false);
    }
}
