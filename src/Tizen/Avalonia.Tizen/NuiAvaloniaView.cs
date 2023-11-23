using Avalonia.Controls;
using Avalonia.Controls.Embedding;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Input.TextInput;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Server;
using Tizen.NUI;
using Tizen.NUI.BaseComponents;

namespace Avalonia.Tizen;

/// <summary>
/// Avalonia View for Tizen NUI controls
/// </summary>
public class NuiAvaloniaView : GLView, ITizenView, ITextInputMethodImpl
{
    private readonly NuiKeyboardHandler _keyboardHandler;
    private readonly NuiTouchHandler _touchHandler;
    private readonly NuiAvaloniaViewTextEditable _textEditor;
    private TizenRenderTimer? _renderTimer;
    private TopLevelImpl? _topLevelImpl;
    private EmbeddableControlRoot? _topLevel;
    private readonly TouchDevice _device = new();
    private ServerCompositionTarget? _compositionTargetServer;
    private IInputRoot? _inputRoot;

    public INativeControlHostImpl NativeControlHost { get; }
    public double Scaling => 1;
    public Size ClientSize => new(Size.Width, Size.Height);

    public IInputRoot InputRoot
    {
        get => _inputRoot ?? throw new InvalidOperationException($"{nameof(InputRoot)} hasn't been set");
        set => _inputRoot = value;
    }

    private TopLevel TopLevel
        => _topLevel ?? throw new InvalidOperationException($"{nameof(NuiAvaloniaView)} hasn't been initialized");

    internal TopLevelImpl TopLevelImpl
        => _topLevelImpl ?? throw new InvalidOperationException($"{nameof(NuiAvaloniaView)} hasn't been initialized");

    public Control? Content
    {
        get => TopLevel.Content as Control;
        set => TopLevel.Content = value;
    }

    internal NuiAvaloniaViewTextEditable TextEditor => _textEditor;
    internal NuiKeyboardHandler KeyboardHandler => _keyboardHandler;

    #region Setup

    public event Action? OnSurfaceInit;

    public NuiAvaloniaView() : base(ColorFormat.RGBA8888)
    {
        RenderingMode = GLRenderingMode.OnDemand;
        SetGraphicsConfig(true, true, 0, GLESVersion.Version30);
        RegisterGLCallbacks(GlInit, GlRenderFrame, GlTerminate);

        _textEditor = new NuiAvaloniaViewTextEditable(this);
        _keyboardHandler = new NuiKeyboardHandler(this);
        _touchHandler = new NuiTouchHandler(this);
        NativeControlHost = new NuiNativeControlHostImpl(this);

        Layout = new CustomLayout
        {
            SizeUpdated = OnResized
        };

        TouchEvent += OnTouchEvent;
        WheelEvent += OnWheelEvent;
    }

    private void GlInit()
    {
        OnSurfaceInit?.Invoke();
    }

    private int GlRenderFrame()
    {
        if (_renderTimer == null || _compositionTargetServer == null)
            return 0;

        var rev = _compositionTargetServer.Revision;
        _renderTimer.ManualTick();
        return rev == _compositionTargetServer.Revision ? 0 : 1;
    }

    private void GlTerminate()
    {
    }

    internal void Initialise()
    {
        _topLevelImpl = new TopLevelImpl(this, new[] { new NuiGlLayerSurface(this) });
        _topLevelImpl.Compositor.AfterCommit += RenderOnce;
        TizenPlatform.ThreadingInterface.TickExecuted += RenderOnce;

        _topLevel = new(_topLevelImpl);
        _topLevel.Prepare();
        _topLevel.StartRendering();

        _compositionTargetServer = ((CompositingRenderer)((IRenderRoot)_topLevel).Renderer).CompositionTarget.Server;

        _renderTimer = (TizenRenderTimer)AvaloniaLocator.Current.GetRequiredService<IRenderTimer>();
        _renderTimer.RenderTick += RenderOnce;

        OnResized();
    }

    #endregion

    #region Resize and layout

    private class CustomLayout : AbsoluteLayout
    {
        float _width;
        float _height;

        public Action? SizeUpdated { get; set; }

        protected override void OnLayout(bool changed, LayoutLength left, LayoutLength top, LayoutLength right, LayoutLength bottom)
        {
            var sizeChanged = _width != Owner.SizeWidth || _height != Owner.SizeHeight;
            _width = Owner.SizeWidth;
            _height = Owner.SizeHeight;
            if (sizeChanged)
            {
                SizeUpdated?.Invoke();
            }
            base.OnLayout(changed, left, top, right, bottom);
        }
    }

    protected void OnResized()
    {
        if (Size.Width == 0 || Size.Height == 0)
            return;

        _topLevelImpl?.Resized?.Invoke(_topLevelImpl.ClientSize, WindowResizeReason.Layout);
    }

    #endregion

    #region Event handlers

    private bool OnTouchEvent(object source, TouchEventArgs e)
    {
        _touchHandler.Handle(e);
        return true;
    }

    private bool OnWheelEvent(object source, WheelEventArgs e)
    {
        _touchHandler.Handle(e);
        return true;
    }

    public void SetClient(TextInputMethodClient? client)
    {
        _textEditor.SetClient(client);
    }

    public void SetCursorRect(Rect rect)
    {
    }

    public void SetOptions(TextInputOptions options) =>
        _textEditor.SetOptions(options);

    #endregion

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _topLevel?.StopRendering();
            _topLevel?.Dispose();
            _topLevelImpl?.Dispose();
            _device.Dispose();
        }
        base.Dispose(disposing);
    }
}
