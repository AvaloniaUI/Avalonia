using System;
using System.Buffers;
using System.IO;
using System.Runtime.InteropServices;
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

    public override bool CanSeek => true;

    public override bool CanWrite => false;

    public override long Length => _length;

    public override long Position
    {
        get => _position;
        set => Seek(value, SeekOrigin.Begin);
    }

    public override void Flush() { }

    public override long Seek(long offset, SeekOrigin origin)
    {
        var position = origin switch
        {
            SeekOrigin.Current => _position + offset,
            SeekOrigin.End => _length + offset,
            _ => offset
        };
        if (position < 0)
        {
            throw new IOException("An attempt was made to move the position before the beginning of the stream.");
        }

        return _position = position;
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
        var numBytesToRead = (int)Math.Clamp(Length - _position, 0, buffer.Length);
        if (numBytesToRead == 0)
        {
            return 0;
        }

        // Blob data is copied by JS directly into the managed buffer. Non-array memory goes through a pooled array.
        int bytesRead;
        if (MemoryMarshal.TryGetArray<byte>(buffer, out var segment))
        {
            bytesRead = await StreamHelper.ReadIntoAsync(JSReference, _position, segment.Slice(0, numBytesToRead));
        }
        else
        {
            var rented = ArrayPool<byte>.Shared.Rent(numBytesToRead);
            try
            {
                bytesRead = await StreamHelper.ReadIntoAsync(JSReference, _position, new ArraySegment<byte>(rented, 0, numBytesToRead));
                rented.AsSpan(0, bytesRead).CopyTo(buffer.Span);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }

        if (bytesRead != numBytesToRead)
        {
            throw new EndOfStreamException("Failed to read the requested number of bytes from the stream.");
        }

        _position += bytesRead;

        return bytesRead;
    }

    public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
    {
        var task = ReadAsync(buffer, offset, count, default);
        return TaskToAsyncResult.Begin(task, callback, state);
    }

    public override int EndRead(IAsyncResult asyncResult)
    {
        return TaskToAsyncResult.End<int>(asyncResult);
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
