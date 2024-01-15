using System.IO;

using Foundation;

using UIKit;

#nullable enable

namespace Avalonia.iOS.Storage;

internal sealed class IOSSecurityScopedStream : Stream
{
    private readonly UIDocument _document;
    private readonly FileStream _stream;
    private readonly NSUrl _url;
    private readonly NSUrl _securityScopedAncestorUrl;

    internal IOSSecurityScopedStream(NSUrl url, NSUrl securityScopedAncestorUrl, FileAccess access)
    {
        _document = new UIDocument(url);
        var path = _document.FileUrl.Path!;
        _url = url;
        _securityScopedAncestorUrl = securityScopedAncestorUrl;
        _securityScopedAncestorUrl.StartAccessingSecurityScopedResource();
        _stream = File.Open(path, FileMode.Open, access);
    }

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

    public override int Read(byte[] buffer, int offset, int count) =>
        _stream.Read(buffer, offset, count);

    public override long Seek(long offset, SeekOrigin origin) =>
        _stream.Seek(offset, origin);

    public override void SetLength(long value) =>
        _stream.SetLength(value);

    public override void Write(byte[] buffer, int offset, int count) =>
        _stream.Write(buffer, offset, count);

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _stream.Dispose();
            _document.Dispose();
            _securityScopedAncestorUrl.StopAccessingSecurityScopedResource();
        }
    }
}
