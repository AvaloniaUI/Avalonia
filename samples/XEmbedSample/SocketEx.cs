using Avalonia;
using Avalonia.X11;
using Gdk;
using Color = Cairo.Color;

namespace XEmbedSample;

public class AvaloniaXEmbedGtkSocket : Gtk.Socket
{
    private readonly RGBA _backgroundColor;
    private XEmbedPlug? _avaloniaPlug;
    public AvaloniaXEmbedGtkSocket(RGBA backgroundColor)
    {
        _backgroundColor = backgroundColor;
    }
    
    private object _content;
    public object Content
    {
        get => _content;
        set
        {
            _content = value;
            if (_avaloniaPlug != null)
                _avaloniaPlug.Content = _content;
        }
    }

    protected override void OnRealized()
    {
        base.OnRealized();
        _avaloniaPlug ??= XEmbedPlug.Create();
        _avaloniaPlug.ScaleFactor = ScaleFactor;
        _avaloniaPlug.BackgroundColor = Avalonia.Media.Color.FromRgb((byte)(_backgroundColor.Red * 255),
            (byte)(_backgroundColor.Green * 255),
            (byte)(_backgroundColor.Blue * 255)
        );
        _avaloniaPlug.Content = _content;
        ApplyInteractiveResize();
        AddId((ulong)_avaloniaPlug.Handle);
    }

    void ApplyInteractiveResize()
    {
        // This is _NOT_ a part of XEmbed, but allows us to have smooth resize
        GetAllocatedSize(out var rect, out _);
        var scale = ScaleFactor;
        _avaloniaPlug?.ProcessInteractiveResize(new PixelSize(rect.Width * scale, rect.Height * scale));
    }

    protected override void OnSizeAllocated(Rectangle allocation)
    {
        base.OnSizeAllocated(allocation);
        Display.Default.Sync();
        ApplyInteractiveResize();
    }

    protected override void OnDestroyed()
    {
        _avaloniaPlug?.Dispose();
        _avaloniaPlug = null;
        base.OnDestroyed();
    }
}