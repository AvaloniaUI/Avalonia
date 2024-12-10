using System;
using System.Threading;
using Avalonia.Controls.Embedding;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.X11.Dispatching;

namespace Avalonia.X11;

public class XEmbedPlug : IDisposable
{
    private EmbeddableControlRoot _root;
    private Color _backgroundColor;
    private readonly X11Info _x11;
    private readonly X11Window.XEmbedClientWindowMode _mode;

    private XEmbedPlug(IntPtr? parentXid)
    {
        var platform = AvaloniaLocator.Current.GetService<AvaloniaX11Platform>();
        _mode = new X11Window.XEmbedClientWindowMode();
        _root = new EmbeddableControlRoot(new X11Window(platform, null, _mode));
        _root.Prepare();
        _x11 = platform.Info;
        if (parentXid.HasValue)
            XLib.XReparentWindow(platform.Display, Handle, parentXid.Value, 0, 0);
        
        // Make sure that the newly created XID is visible for other clients
        XLib.XSync(platform.Display, false);
    }

    public IntPtr Handle =>
        _root?.PlatformImpl!.Handle!.Handle ?? throw new ObjectDisposedException(nameof(XEmbedPlug));
    
    public object Content
    {
        get => _root.Content;
        set => _root.Content = value;
    }

    public Color BackgroundColor
    {
        get => _backgroundColor;
        set
        {
            _backgroundColor = value;
            XLib.XSetWindowBackground(_x11.Display, Handle, new IntPtr(
                (int)(value.ToUInt32() | 0xff000000)));
            XLib.XFlush(_x11.Display);
        }
    }

    public double ScaleFactor
    {
        get => _mode.Scaling;
        set => _mode.Scaling = value;
    }

    public void ProcessInteractiveResize(PixelSize size)
    {
        
        var events = (IX11PlatformDispatcher)AvaloniaLocator.Current.GetService<IDispatcherImpl>();
        events.EventDispatcher.DispatchX11Events(CancellationToken.None);
        _mode.ProcessInteractiveResize(size);
        Dispatcher.UIThread.RunJobs(DispatcherPriority.UiThreadRender);
    }

    public void Dispose()
    {
        if (_root != null)
        {
            _root.StopRendering();
            _root.Dispose();
            _root = null;
        }
    }

    public static XEmbedPlug Create() => new(null);

    public static XEmbedPlug Create(IntPtr embedderXid) =>
        embedderXid == IntPtr.Zero ? throw new ArgumentException() : new XEmbedPlug(embedderXid);
}