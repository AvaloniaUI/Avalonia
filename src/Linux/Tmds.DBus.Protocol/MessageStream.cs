using System.IO.Pipelines;
using System.Net.Sockets;
using System.Threading.Channels;

namespace Tmds.DBus.Protocol;

#pragma warning disable VSTHRD100 // Avoid "async void" methods

class MessageStream : IMessageStream
{
    private static readonly ReadOnlyMemory<byte> OneByteArray = new[] { (byte)0 };
    private readonly Socket _socket;
    private UnixFdCollection? _fdCollection;
    private bool _supportsFdPassing;
    private readonly MessagePool _messagePool;

    // Messages going out.
    private readonly ChannelReader<MessageBuffer> _messageReader;
    private readonly ChannelWriter<MessageBuffer> _messageWriter;

    // Bytes coming in.
    private readonly PipeWriter _pipeWriter;
    private readonly PipeReader _pipeReader;

    private Exception? _completionException;
    private bool _isMonitor;

    public MessageStream(Socket socket)
    {
        _socket = socket;
        Channel<MessageBuffer> channel = Channel.CreateUnbounded<MessageBuffer>(new UnboundedChannelOptions
        {
            AllowSynchronousContinuations = true,
            SingleReader = true,
            SingleWriter = false
        });
        _messageReader = channel.Reader;
        _messageWriter = channel.Writer;
        var pipe = new Pipe(new PipeOptions(useSynchronizationContext: false));
        _pipeReader = pipe.Reader;
        _pipeWriter = pipe.Writer;
        _messagePool = new();
    }

    public void BecomeMonitor()
    {
        _isMonitor = true;
    }

    private async void ReadFromSocketIntoPipe()
    {
        var writer = _pipeWriter;
        Exception? exception = null;
        try
        {
            while (true)
            {
                Memory<byte> memory = writer.GetMemory(1024);
                int bytesRead = await _socket.ReceiveAsync(memory, _fdCollection).ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    throw new IOException("Connection closed by peer");
                }
                writer.Advance(bytesRead);

                await writer.FlushAsync().ConfigureAwait(false);
            }
        }
        catch (Exception e)
        {
            exception = e;
        }
        writer.Complete(exception);
    }

    private async void ReadMessagesIntoSocket()
    {
        while (true)
        {
            if (!await _messageReader.WaitToReadAsync().ConfigureAwait(false))
            {
                // No more messages will be coming.
                return;
            }
            var message = await _messageReader.ReadAsync().ConfigureAwait(false);
            try
            {
                IReadOnlyList<SafeHandle>? handles = _supportsFdPassing ? message.Handles : null;
                var buffer = message.AsReadOnlySequence();
                if (buffer.IsSingleSegment)
                {
                    await _socket.SendAsync(buffer.First, handles).ConfigureAwait(false);
                }
                else
                {
                    SequencePosition position = buffer.Start;
                    while (buffer.TryGet(ref position, out ReadOnlyMemory<byte> memory))
                    {
                        await _socket.SendAsync(memory, handles).ConfigureAwait(false);
                        handles = null;
                    }
                }
            }
            catch (Exception exception)
            {
                Close(exception);
                return;
            }
            finally
            {
                message.ReturnToPool();
            }
        }
    }

    public async void ReceiveMessages<T>(IMessageStream.MessageReceivedHandler<T> handler, T state)
    {
        var reader = _pipeReader;
        try
        {
            while (true)
            {
                ReadResult result = await reader.ReadAsync().ConfigureAwait(false);
                ReadOnlySequence<byte> buffer = result.Buffer;

                ReadMessages(ref buffer, handler, state);

                reader.AdvanceTo(buffer.Start, buffer.End);
            }
        }
        catch (Exception exception)
        {
            exception = CloseCore(exception);
            OnException(exception, handler, state);
        }
        finally
        {
            _fdCollection?.Dispose();
        }

        void ReadMessages<TState>(ref ReadOnlySequence<byte> buffer, IMessageStream.MessageReceivedHandler<TState> handler, TState state)
        {
            Message? message;
            while ((message = Message.TryReadMessage(_messagePool, ref buffer, _fdCollection, _isMonitor)) != null)
            {
                handler(closeReason: null, message, state);
            }
        }

        static void OnException(Exception exception, IMessageStream.MessageReceivedHandler<T> handler, T state)
        {
            handler(exception, message: null!, state);
        }
    }

    private struct AuthenticationResult
    {
        public bool IsAuthenticated;
        public bool SupportsFdPassing;
        public Guid Guid;
    }

    public async ValueTask DoClientAuthAsync(Guid guid, string? userId, bool supportsFdPassing)
    {
        ReadFromSocketIntoPipe();

        // send 1 byte
        await _socket.SendAsync(OneByteArray, SocketFlags.None).ConfigureAwait(false);
        // auth
        var authenticationResult = await SendAuthCommandsAsync(userId, supportsFdPassing).ConfigureAwait(false);
        _supportsFdPassing = authenticationResult.SupportsFdPassing;
        if (_supportsFdPassing)
        {
            _fdCollection = new();
        }
        if (guid != Guid.Empty)
        {
            if (guid != authenticationResult.Guid)
            {
                throw new ConnectException("Authentication failure: Unexpected GUID");
            }
        }

        ReadMessagesIntoSocket();
    }

    private async ValueTask<AuthenticationResult> SendAuthCommandsAsync(string? userId, bool supportsFdPassing)
    {
        AuthenticationResult result;
        if (userId is not null)
        {
            string command = CreateAuthExternalCommand(userId);

            result = await SendAuthCommandAsync(command, supportsFdPassing).ConfigureAwait(false);

            if (result.IsAuthenticated)
            {
                return result;
            }
        }

        result = await SendAuthCommandAsync("AUTH ANONYMOUS\r\n", supportsFdPassing).ConfigureAwait(false);
        if (result.IsAuthenticated)
        {
            return result;
        }

        throw new ConnectException("Authentication failure");
    }

    private static string CreateAuthExternalCommand(string userId)
    {
        const string AuthExternal = "AUTH EXTERNAL ";
        const string hexchars = "0123456789abcdef";
#if NETSTANDARD2_0
        StringBuilder sb = new();
        sb.Append(AuthExternal);
        for (int i = 0; i < userId.Length; i++)
        {
            byte b = (byte)userId[i];
            sb.Append(hexchars[(int)(b >> 4)]);
            sb.Append(hexchars[(int)(b & 0xF)]);
        }
        sb.Append("\r\n");
        return sb.ToString();
#else
        return string.Create<string>(
            length: AuthExternal.Length + userId.Length * 2 + 2, userId,
            static (Span<char> span, string userId) =>
            {
                AuthExternal.AsSpan().CopyTo(span);
                span = span.Slice(AuthExternal.Length);

                for (int i = 0; i < userId.Length; i++)
                {
                    byte b = (byte)userId[i];
                    span[i * 2] = hexchars[(int)(b >> 4)];
                    span[i * 2 + 1] = hexchars[(int)(b & 0xF)];
                }
                span = span.Slice(userId.Length * 2);

                span[0] = '\r';
                span[1] = '\n';
            });
#endif
    }

    private async ValueTask<AuthenticationResult> SendAuthCommandAsync(string command, bool supportsFdPassing)
    {
        byte[] lineBuffer = ArrayPool<byte>.Shared.Rent(512);
        try
        {
            AuthenticationResult result = default(AuthenticationResult);
            await WriteAsync(command, lineBuffer).ConfigureAwait(false);
            int lineLength = await ReadLineAsync(lineBuffer).ConfigureAwait(false);

            if (StartsWithAscii(lineBuffer, lineLength, "OK"))
            {
                result.IsAuthenticated = true;
                result.Guid = ParseGuid(lineBuffer, lineLength);

                if (supportsFdPassing)
                {
                    await WriteAsync("NEGOTIATE_UNIX_FD\r\n", lineBuffer).ConfigureAwait(false);

                    lineLength = await ReadLineAsync(lineBuffer).ConfigureAwait(false);

                    result.SupportsFdPassing = StartsWithAscii(lineBuffer, lineLength, "AGREE_UNIX_FD");
                }

                await WriteAsync("BEGIN\r\n", lineBuffer).ConfigureAwait(false);
                return result;
            }
            else if (StartsWithAscii(lineBuffer, lineLength, "REJECTED"))
            {
                return result;
            }
            else
            {
                await WriteAsync("ERROR\r\n", lineBuffer).ConfigureAwait(false);
                return result;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(lineBuffer);
        }

        static bool StartsWithAscii(byte[] line, int length, string expected)
        {
            if (length < expected.Length)
            {
                return false;
            }
            for (int i = 0; i < expected.Length; i++)
            {
                if (line[i] != expected[i])
                {
                    return false;
                }
            }
            return true;
        }

        static Guid ParseGuid(byte[] line, int length)
        {
            Span<byte> span = new Span<byte>(line, 0, length);
            int spaceIndex = span.IndexOf((byte)' ');
            if (spaceIndex == -1)
            {
                return Guid.Empty;
            }
            span = span.Slice(spaceIndex + 1);
            spaceIndex = span.IndexOf((byte)' ');
            if (spaceIndex != -1)
            {
                span = span.Slice(0, spaceIndex);
            }
            Span<char> charBuffer = stackalloc char[span.Length]; // TODO (low prio): check length
            for (int i = 0; i < span.Length; i++)
            {
                // TODO (low prio): validate char
                charBuffer[i] = (char)span[i];
            }
#if NETSTANDARD2_0
            return Guid.ParseExact(charBuffer.AsString(), "N");
#else
            return Guid.ParseExact(charBuffer, "N");
#endif
        }
    }

    private async ValueTask WriteAsync(string message, Memory<byte> lineBuffer)
    {
        int length = Encoding.ASCII.GetBytes(message.AsSpan(), lineBuffer.Span);
        lineBuffer = lineBuffer.Slice(0, length);
        await _socket.SendAsync(lineBuffer, SocketFlags.None).ConfigureAwait(false);
    }

    private async ValueTask<int> ReadLineAsync(Memory<byte> lineBuffer)
    {
        var reader = _pipeReader;
        while (true)
        {
            ReadResult result = await reader.ReadAsync().ConfigureAwait(false);
            ReadOnlySequence<byte> buffer = result.Buffer;

            // TODO (low prio): check length.

            SequencePosition? position = buffer.PositionOf((byte)'\n');

            if (!position.HasValue)
            {
                reader.AdvanceTo(buffer.Start, buffer.End);
                continue;
            }

            int length = CopyBuffer(buffer.Slice(0, position.Value), lineBuffer);
            reader.AdvanceTo(buffer.GetPosition(1, position.Value));
            return length;
        }

        int CopyBuffer(ReadOnlySequence<byte> src, Memory<byte> dst)
        {
            Span<byte> span = dst.Span;
            src.CopyTo(span);
            span = span.Slice(0, (int)src.Length);
            if (!span.EndsWith((ReadOnlySpan<byte>)new byte[] { (byte)'\r' }))
            {
                throw new ProtocolException("Authentication messages from server must end with '\\r\\n'.");
            }
            if (span.Length == 1)
            {
                throw new ProtocolException("Received empty authentication message from server.");
            }
            return span.Length - 1;
        }
    }

    public async ValueTask<bool> TrySendMessageAsync(MessageBuffer message)
    {
        while (await _messageWriter.WaitToWriteAsync().ConfigureAwait(false))
        {
            if (_messageWriter.TryWrite(message))
                return true;
        }

        return false;
    }

    public void Close(Exception closeReason) => CloseCore(closeReason);

    private Exception CloseCore(Exception closeReason)
    {
        Exception? previous = Interlocked.CompareExchange(ref _completionException, closeReason, null);
        if (previous is null)
        {
            _socket?.Dispose();
            _messageWriter.Complete();
        }
        return previous ?? closeReason;
    }
}
