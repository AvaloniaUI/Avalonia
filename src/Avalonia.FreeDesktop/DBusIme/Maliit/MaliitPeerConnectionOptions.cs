using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Buffers.Binary;
using System.Net.Sockets;
using Tmds.DBus.Protocol;

namespace Avalonia.FreeDesktop.DBusIme.Maliit;

// Maliit sends initialization callbacks before replying to the client's Hello probe.
// Tmds 0.94.1 cannot reply to them while ConnectAsync is pending. Its public stream
// extension lets us defer those frames until the callback handler is installed.
internal sealed class MaliitPeerConnectionOptions(string address) : DBusConnectionOptions(address)
{
    private MaliitHandshakeStream? _stream;
    public void ReleaseCallbacks() => _stream!.ReleaseCallbacks();

    protected override async ValueTask<SetupResult> SetupAsync(CancellationToken cancellationToken)
    {
        var result = await base.SetupAsync(cancellationToken).ConfigureAwait(false);
        var entries = result.ConnectionAddress[5..].Split(',').Select(p => p.Split('=', 2)).ToArray();
        var endpoint = entries.SingleOrDefault(p => p.Length == 2 && p[0] is "path" or "abstract")
            ?? throw new InvalidOperationException("Maliit Unix socket path missing.");
        var path = Uri.UnescapeDataString(endpoint[1]);
        if (path.Length == 0 || path.Contains('\0')) throw new InvalidOperationException("Invalid Maliit socket path.");
        if (endpoint[0] == "abstract") path = "\0" + path;
        else if (!path.StartsWith('/')) throw new InvalidOperationException("Maliit socket path must be absolute.");
        var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        try
        {
            await socket.ConnectAsync(new UnixDomainSocketEndPoint(path), cancellationToken).ConfigureAwait(false);
            _stream = new MaliitHandshakeStream(new NetworkStream(socket, ownsSocket: true));
            result.ConnectionStream = _stream;
            return result;
        }
        catch { socket.Dispose(); throw; }
    }
}

/// <summary>Bounded, temporary frame ordering during peer setup; payloads are neither decoded nor logged.</summary>
internal sealed class MaliitHandshakeStream(Stream inner) : Stream
{
    private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Queue<byte[]> _deferred = new();
    private readonly CancellationTokenSource _closed = new();
    private ReadOnlyMemory<byte> _pending;
    private bool _binary;
    private bool _helloReplied;
    private int _buffered;
    private int _disposed;
    public void ReleaseCallbacks() => _ready.TrySetResult();

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (buffer.Length == 0) return 0;
        if (!_binary) return await inner.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _closed.Token);
        var token = linked.Token;
        if (_pending.IsEmpty && _helloReplied)
        {
            await _ready.Task.WaitAsync(token).ConfigureAwait(false);
            if (_deferred.TryDequeue(out var deferred)) _pending = deferred;
            else return await inner.ReadAsync(buffer, token).ConfigureAwait(false);
        }
        while (_pending.IsEmpty)
        {
            var header = new byte[16];
            await inner.ReadExactlyAsync(header, token).ConfigureAwait(false);
            if (header[0] is not ((byte)'l') and not ((byte)'B') || header[3] != 1)
                throw new InvalidDataException("Invalid D-Bus peer frame.");
            var little = header[0] == 'l';
            var bodyLength = little ? BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(4)) : BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(4));
            var fieldsLength = little ? BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(12)) : BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(12));
            var total = 16L + ((fieldsLength + 7L) & ~7L) + bodyLength;
            if (total > 65536 || _buffered + total > 65536)
                throw new InvalidDataException("Maliit initialization exceeded its buffer limit.");
            var frame = new byte[(int)total];
            header.CopyTo(frame, 0);
            await inner.ReadExactlyAsync(frame.AsMemory(16), token).ConfigureAwait(false);
            if (header[1] is 2 or 3) // Hello return/error: allow ConnectAsync to complete first.
            {
                _helloReplied = true;
                _pending = frame;
            }
            else if (header[1] is 1 or 4)
            {
                if (_deferred.Count >= 32)
                    throw new InvalidDataException("Maliit initialization exceeded its frame limit.");
                _deferred.Enqueue(frame);
                _buffered += frame.Length;
            }
            else throw new InvalidDataException("Invalid D-Bus message type.");
        }
        var count = Math.Min(buffer.Length, _pending.Length);
        _pending.Span[..count].CopyTo(buffer.Span);
        _pending = _pending[count..];
        return count;
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (!_binary && buffer.Span.SequenceEqual("BEGIN\r\n"u8)) _binary = true;
        return inner.WriteAsync(buffer, cancellationToken);
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing && Interlocked.Exchange(ref _disposed, 1) == 0)
        { _closed.Cancel(); inner.Dispose(); }
        base.Dispose(disposing);
    }
    public override bool CanRead => true;
    public override bool CanWrite => true;
    public override bool CanSeek => false;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public override void Flush() => inner.Flush();
    public override int Read(byte[] buffer, int offset, int count) => ReadAsync(buffer.AsMemory(offset, count)).AsTask().GetAwaiter().GetResult();
    public override void Write(byte[] buffer, int offset, int count) => WriteAsync(buffer.AsMemory(offset, count)).AsTask().GetAwaiter().GetResult();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
}
