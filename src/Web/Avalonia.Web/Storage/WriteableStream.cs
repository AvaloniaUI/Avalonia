using System;
using System.IO;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using System.Threading.Tasks;

namespace Avalonia.Web.Storage;

[System.Runtime.Versioning.SupportedOSPlatform("browser")] // gets rid of callsite warnings
// Loose wrapper implementaion of a stream on top of FileAPI FileSystemWritableFileStream
internal sealed class JSWriteableStream : Stream
{
    private JSObject? _jSReference;

    // Unfortunatelly we can't read current length/position, so we need to keep it C#-side only.
    private long _length, _position;

    internal JSWriteableStream(JSObject jSReference, long initialLength)
    {
        _jSReference = jSReference;
        _length = initialLength;
    }

    private JSObject JSReference => _jSReference ?? throw new ObjectDisposedException(nameof(JSWriteableStream));

    public override bool CanRead => false;

    public override bool CanSeek => true;

    public override bool CanWrite => true;

    public override long Length => _length;

    public override long Position
    {
        get => _position;
        set => Seek(_position, SeekOrigin.Begin);
    }

    public override void Flush()
    {
        // no-op
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        var position = origin switch
        {
            SeekOrigin.Current => _position + offset,
            SeekOrigin.End => _length + offset,
            _ => offset
        };
        StreamHelper.Seek(JSReference, position);
        return position;
    }

    public override void SetLength(long value)
    {
        _length = value;

        // See https://docs.w3cub.com/dom/filesystemwritablefilestream/truncate
        // If the offset is smaller than the size, it remains unchanged. If the offset is larger than size, the offset is set to that size
        if (_position > _length)
        {
            _position = _length;
        }

        StreamHelper.Truncate(JSReference, value);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        StreamHelper.Write(JSReference, buffer.AsSpan(offset, count));
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        if (offset != 0 || count != buffer.Length)
        {
            // TODO, we need to pass prepared buffer to the JS
            // Can't use ArrayPool as it can return bigger array than requested
            // Can't use Span/Memory, as it's not supported by JS interop yet.
            // Alternatively we can pass original buffer and offset+count, so it can be trimmed on the JS side (but is it more efficient tho?)
            buffer = buffer.AsMemory(offset, count).ToArray();
        }
        return WriteAsyncInternal(buffer, cancellationToken);
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        return new ValueTask(WriteAsyncInternal(buffer.ToArray(), cancellationToken));
    }

    private Task WriteAsyncInternal(byte[] buffer, CancellationToken _)
    {
        _position += buffer.Length;

        return StreamHelper.WriteAsync(JSReference, buffer);
    }

    protected override void Dispose(bool disposing)
    {
        if (_jSReference is { } jsReference)
        {
            _jSReference = null;
            try
            {
                StreamHelper.Close(JSReference);
            }
            finally
            {
                jsReference.Dispose();
            }
        }
    }

    public override async ValueTask DisposeAsync()
    {
        if (_jSReference is { } jsReference)
        {
            _jSReference = null;
            try
            {
                await StreamHelper.CloseAsync(JSReference);
            }
            finally
            {
                jsReference.Dispose();
            }
        }
    }
}
