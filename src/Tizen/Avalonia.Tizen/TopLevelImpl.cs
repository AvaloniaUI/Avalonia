using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Input.TextInput;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;

namespace Avalonia.Tizen;

internal class TopLevelImpl : ITopLevelImpl
{
    private readonly ITizenView _view;

    public TopLevelImpl(ITizenView view)
    {
        _view = view;

        //_nativeControlHost = new NativeControlHostImpl(view);
        //_storageProvider = new TizenStorageProvider(view);
        //_insetsManager = new InsetsManager(view);
        //_insetsManager.DisplayEdgeToEdgeChanged += (sender, b) =>
        //{
        //    view._topLevel.Padding = b ? default : _insetsManager.SafeAreaPadding;
        //};
        //_clipboard = new ClipboardImpl();
    }

    public Size ClientSize => _view.ClientSize;

    public Size? FrameSize => null;

    public double RenderScaling => 1;

    public IEnumerable<object> Surfaces { get; set; }

    public Action<RawInputEventArgs>? Input { get; set; }
    public Action<Rect>? Paint { get; set; }
    public Action<Size, WindowResizeReason>? Resized { get; set; }
    public Action<double>? ScalingChanged { get; set; }
    public Action<WindowTransparencyLevel>? TransparencyLevelChanged { get; set; }

    public Compositor Compositor => TizenPlatform.Compositor;

    public Action? Closed { get; set; }
    public Action? LostFocus { get; set; }

    public WindowTransparencyLevel TransparencyLevel { get; set; }

    public AcrylicPlatformCompensationLevels AcrylicCompensationLevels { get; } = new AcrylicPlatformCompensationLevels();
    public IPopupImpl? CreatePopup()
    {
        return null;
    }

    public void Dispose()
    {
        //
    }

    public Point PointToClient(PixelPoint point) => new Point(point.X, point.Y);

    public PixelPoint PointToScreen(Point point) => new PixelPoint((int)point.X, (int)point.Y);

    public void SetCursor(ICursorImpl? cursor)
    {
        //
    }

    public void SetFrameThemeVariant(PlatformThemeVariant themeVariant)
    {
        //
    }

    public void SetInputRoot(IInputRoot inputRoot)
    {
        _view.InputRoot = inputRoot;
    }

    public void SetTransparencyLevelHint(IReadOnlyList<WindowTransparencyLevel> transparencyLevels)
    {
        //
    }

    public object? TryGetFeature(Type featureType)
    {
        //if (featureType == typeof(IStorageProvider))
        //{
        //    return _storageProvider;
        //}

        if (featureType == typeof(ITextInputMethodImpl))
        {
            return _view;
        }

        //if (featureType == typeof(INativeControlHostImpl))
        //{
        //    return _nativeControlHost;
        //}

        //if (featureType == typeof(IInsetsManager))
        //{
        //    return _insetsManager;
        //}

        //if (featureType == typeof(IClipboard))
        //{
        //    return _clipboard;
        //}

        return null;
    }
}
