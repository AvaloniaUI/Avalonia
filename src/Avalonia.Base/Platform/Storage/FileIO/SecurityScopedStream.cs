using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Avalonia.Platform.Storage.FileIO;

/// <summary>
/// Stream wrapper currently used by Apple platforms,
/// where in sandboxed scenario it's advised to call [NSUri startAccessingSecurityScopedResource].
/// </summary>
internal sealed class SecurityScopedStream(FileStream _stream, IDisposable _securityScope) : Stream
{
    public override bool CanRead => _stream.CanRead;

    public override bool CanSeek => _stream.CanSeek;

    public override bool CanWrite => _stream.CanWrite;

    public override long Length => _stream.Length;

    public override long Position
    {
        get => _stream.Position;
        set => _stream.Position = value;
    }

    public override void Flush() =>
        _stream.Flush();

    public override Task FlushAsync(CancellationToken cancellationToken) =>
        _stream.FlushAsync(cancellationToken);

    public override int ReadByte() =>
        _stream.ReadByte();

    public override int Read(byte[] buffer, int offset, int count) =>
        _stream.Read(buffer, offset, count);

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        _stream.ReadAsync(buffer, offset, count, cancellationToken);

#if NET6_0_OR_GREATER
    public override int Read(Span<byte> buffer) => _stream.Read(buffer);

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
        _stream.ReadAsync(buffer, cancellationToken);
#endif

    public override void Write(byte[] buffer, int offset, int count) =>
        _stream.Write(buffer, offset, count);

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        _stream.WriteAsync(buffer, offset, count, cancellationToken);

#if NET6_0_OR_GREATER
    public override void Write(ReadOnlySpan<byte> buffer) => _stream.Write(buffer);

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) =>
        _stream.WriteAsync(buffer, cancellationToken);
#endif

    public override void WriteByte(byte value) => _stream.WriteByte(value);

    public override long Seek(long offset, SeekOrigin origin) =>
        _stream.Seek(offset, origin);

    public override void SetLength(long value) =>
        _stream.SetLength(value);

#if NET6_0_OR_GREATER
    public override void CopyTo(Stream destination, int bufferSize) => _stream.CopyTo(destination, bufferSize);
#endif

    public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) =>
        _stream.CopyToAsync(destination, bufferSize, cancellationToken);

    public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) =>
        _stream.BeginRead(buffer, offset, count, callback, state);

    public override int EndRead(IAsyncResult asyncResult) => _stream.EndRead(asyncResult);

    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) =>
        _stream.BeginWrite(buffer, offset, count, callback, state);

    public override void EndWrite(IAsyncResult asyncResult) => _stream.EndWrite(asyncResult);

    protected override void Dispose(bool disposing)
    {
        try
        {
            if (disposing)
            {
                _stream.Dispose();
            }
        }
        finally
        {
            _securityScope.Dispose();
        }
    }

#if NET6_0_OR_GREATER
    public override async ValueTask DisposeAsync()
    {
        try
        {
            await _stream.DisposeAsync();
        }
        finally
        {
            _securityScope.Dispose();
        }
    }
#endif
}
