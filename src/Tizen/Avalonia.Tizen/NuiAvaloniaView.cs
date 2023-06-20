﻿using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Controls.Embedding;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Platform;
using Avalonia.Tizen.Platform;
using Avalonia.Tizen.Platform.Interop;
using Tizen.NUI;
using Tizen.NUI.BaseComponents;
using Window = Tizen.NUI.Window;

namespace Avalonia.Tizen;

public class NuiAvaloniaView : ImageView, ITizenView, IFramebufferPlatformSurface
{
    private IntPtr _buffer;

    private Renderer _renderer;
    private Geometry _geometry;
    private Shader _shader;
    private Texture _texture;
    private TextureSet _textureSet;
    private NativeImageQueue _nativeImageSource;

    private TopLevelImpl _topLevelImpl;
    private EmbeddableControlRoot _topLevel;
    private TouchDevice _device = new TouchDevice();

    public IInputRoot InputRoot { get; set; }
    public ITopLevelImpl TopLevelImpl => _topLevelImpl;
    public double Scaling => ScalingInfo.ScalingFactor;
    public Size ClientSize => new Size(Size.Width, Size.Height);

    public Control? Content
    {
        get => _topLevel.Content as Control;
        set => _topLevel.Content = value;
    }

    #region Setup

    public NuiAvaloniaView()
    {
        Layout = new CustomLayout
        {
            SizeUpdated = OnResized
        };

        TouchEvent += OnTouchEvent;
    }

    internal void Initialise()
    {
        _topLevelImpl = new TopLevelImpl(this)
        {
            Surfaces = new[] { this }
        };

        _topLevel = new EmbeddableControlRoot(_topLevelImpl);
        _topLevel.Prepare();
        _topLevel.StartRendering();

        _geometry = CreateQuadGeometry();
        _shader = new Shader(Consts.VertexShader, Consts.FragmentShader);
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

        UpdateSurface();
        UpdateTexture();

        _topLevelImpl.Resized?.Invoke(_topLevelImpl.ClientSize, WindowResizeReason.Layout);
        _topLevelImpl.Paint?.Invoke(new Rect(_topLevelImpl.ClientSize));
    }

    #endregion

    #region Surface and rendering

    void UpdateSurface()
    {
        _nativeImageSource?.Dispose();
        _nativeImageSource = new NativeImageQueue((uint)Size.Width, (uint)Size.Height, NativeImageQueue.ColorFormat.BGRA8888);
    }

    void UpdateTexture()
    {
        _texture?.Dispose();
        _textureSet?.Dispose();
        _texture = new Texture(_nativeImageSource);
        _textureSet = new TextureSet();
        _textureSet.SetTexture(0, _texture);

        if (_renderer == null)
        {
            _renderer = new Renderer(_geometry, _shader);
            AddRenderer(_renderer);
        }
        _renderer.SetTextures(_textureSet);
    }

    static Geometry CreateQuadGeometry()
    {
        var vertexData = CreateVertextBuffer();

        var vertex1 = new TexturedQuadVertex();
        var vertex2 = new TexturedQuadVertex();
        var vertex3 = new TexturedQuadVertex();
        var vertex4 = new TexturedQuadVertex();
        vertex1.position = new Vec2(-0.5f, -0.5f);
        vertex2.position = new Vec2(-0.5f, 0.5f);
        vertex3.position = new Vec2(0.5f, -0.5f);
        vertex4.position = new Vec2(0.5f, 0.5f);

        var texturedQuadVertexData = new TexturedQuadVertex[] { vertex1, vertex2, vertex3, vertex4 };

        int lenght = Marshal.SizeOf(vertex1);
        var pA = Marshal.AllocHGlobal(lenght * 4);

        for (int i = 0; i < 4; i++)
        {
            Marshal.StructureToPtr(texturedQuadVertexData[i], pA + i * lenght, true);
        }
        vertexData.SetData(pA, 4);

        var geometry = new Geometry();
        geometry.AddVertexBuffer(vertexData);
        geometry.SetType(Geometry.Type.TRIANGLE_STRIP);
        return geometry;
    }

    static PropertyBuffer CreateVertextBuffer()
    {
        PropertyMap vertexFormat = new PropertyMap();
        vertexFormat.Add("aPosition", new PropertyValue((int)PropertyType.Vector2));
        return new PropertyBuffer(vertexFormat);
    }

    #endregion

    #region Framebuffer

    public ILockedFramebuffer Lock()
    {
        var lockBuffer = new LockedFramebuffer(this);
        lockBuffer.Dequeue();
        return lockBuffer;
    }

    private class LockedFramebuffer : ILockedFramebuffer
    {
        private readonly NuiAvaloniaView _view;
        private int _bufferWidth;
        private int _bufferHeight;
        private int _bufferStride;

        public LockedFramebuffer(NuiAvaloniaView view)
        {
            _view = view;
        }

        public IntPtr Address => _view._buffer;

        public PixelSize Size => new PixelSize(_bufferWidth, _bufferHeight);

        public int RowBytes => _bufferStride;

        public Vector Dpi => Consts.Dpi;
        //public Vector Dpi => new Vector(Window.Instance.Dpi.X, Window.Instance.Dpi.Y);

        public Avalonia.Platform.PixelFormat Format => Avalonia.Platform.PixelFormat.Bgra8888;

        public void Dequeue()
        {
            if (_view._buffer != IntPtr.Zero)
                return;

            if (!_view._nativeImageSource.CanDequeueBuffer())
            {
                Window.Instance.RenderOnce();
            }

            _view._buffer = _view._nativeImageSource!.DequeueBuffer(ref _bufferWidth, ref _bufferHeight, ref _bufferStride);
        }

        public void Dispose()
        {
            _view._nativeImageSource.EnqueueBuffer(_view._buffer);
            _view._buffer = IntPtr.Zero;
        }
    }

    #endregion

    #region Event handlers

    private bool OnTouchEvent(object source, TouchEventArgs e)
    {
        var count = e.Touch.GetPointCount();
        for (var i = 0u; i < count; i++)
        {
            var point = e.Touch.GetLocalPosition(i);
            var state = e.Touch.GetState(i);
            var timestamp = e.Touch.GetTime();
            var mouseEvent = new RawTouchEventArgs(
                _device,
                timestamp,
                InputRoot,
                state switch
                {
                    PointStateType.Down => RawPointerEventType.TouchBegin,
                    PointStateType.Up => RawPointerEventType.TouchEnd,
                    PointStateType.Motion => RawPointerEventType.Move,
                    PointStateType.Interrupted => RawPointerEventType.TouchCancel,
                    _ => RawPointerEventType.LeaveWindow
                },
                new Point(point.X, point.Y),
                RawInputModifiers.None,
                i);
            _topLevelImpl?.Input?.Invoke(mouseEvent);
        }
        return true;
    }

    #endregion

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _topLevel.StopRendering();
            _topLevel.Dispose();
            _topLevelImpl.Dispose();
            _device.Dispose();

            _nativeImageSource?.Dispose();
            _nativeImageSource = null;
        }
        base.Dispose(disposing);
    }
}

