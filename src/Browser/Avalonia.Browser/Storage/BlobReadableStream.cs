using System;
using System.IO;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Browser.Interop;

namespace Avalonia.Browser.Storage;

internal class BlobReadableStream : Stream
{
    private JSObject? _jSReference;
    private long _position;
    private readonly long _length;

    public BlobReadableStream(JSObject jsStreamReference)
    {
        _jSReference = jsStreamReference;
        _position = 0;
        _length = StreamHelper.ByteLength(JSReference);
    }

    private JSObject JSReference => _jSReference ?? throw new ObjectDisposedException(nameof(WriteableStream));

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => _length;

    public override long Position
    {
        get => _position;
        set => throw new NotSupportedException();
    }

    public override void Flush() { }

    public override long Seek(long offset, SeekOrigin origin)
    {
        return _position = origin switch
        {
            SeekOrigin.Current => _position + offset,
            SeekOrigin.End => _length + offset,
            _ => offset
        };
    }

    public override void SetLength(long value)
        => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count)
        => throw new NotSupportedException();

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new InvalidOperationException("Browser supports only ReadAsync");
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => await ReadAsync(buffer.AsMemory(offset, count), cancellationToken);

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var numBytesToRead = (int)Math.Min(buffer.Length, Length - _position);
        var bytesRead = await StreamHelper.SliceAsync(JSReference, _position, numBytesToRead);
        if (bytesRead.Length != numBytesToRead)
        {
            throw new EndOfStreamException("Failed to read the requested number of bytes from the stream.");
        }

        _position += bytesRead.Length;
        bytesRead.CopyTo(buffer);

        return bytesRead.Length;
    }

    protected override void Dispose(bool disposing)
    {
        if (_jSReference is { } jsReference)
        {
            _jSReference = null;
            jsReference.Dispose();
        }
    }
}
