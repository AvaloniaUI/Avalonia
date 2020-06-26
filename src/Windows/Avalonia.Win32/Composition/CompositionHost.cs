using System;
using System.Runtime.InteropServices;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Angle;
using Windows.UI.Composition;
using Windows.UI.Composition.Interop;
using WinRT;

namespace Avalonia.Win32
{
    public class CompositionEglGlPlatformSurface : EglGlPlatformSurfaceBase
    {
        private readonly EglDisplay _display;
        private readonly EglContext _context;
        private readonly IEglWindowGlPlatformSurfaceInfo _info;
        private ICompositionDrawingSurfaceInterop _surfaceInterop;

        public CompositionEglGlPlatformSurface(EglContext context, IEglWindowGlPlatformSurfaceInfo info) : base()
        {
            _display = context.Display;
            _context = context;
            _info = info;
        }

        public void AttachToCompositionTree(IntPtr hwnd)
        {
            _surfaceInterop = CompositionHost.Instance.InitialiseWindowCompositionTree(hwnd);
        }

        public override IGlPlatformSurfaceRenderTarget CreateGlRenderTarget()
        {
            return new CompositionRenderTarget(_display, _context, _surfaceInterop, _info);
        }

        class CompositionRenderTarget : EglPlatformSurfaceRenderTargetBase
        {
            private readonly EglDisplay _display;
            private readonly EglContext _context;
            private readonly IEglWindowGlPlatformSurfaceInfo _info;
            private PixelSize _initialSize;
            private readonly ICompositionDrawingSurfaceInterop _surfaceInterop;

            public CompositionRenderTarget(EglDisplay display, EglContext context, ICompositionDrawingSurfaceInterop interopSurface, IEglWindowGlPlatformSurfaceInfo info) : base(display, context)
            {
                _display = display;
                _context = context;
                _surfaceInterop = interopSurface;
                _info = info;
                _initialSize = info.Size;
                lastSize = new POINT { X = _info.Size.Width, Y = _info.Size.Height };
                _surfaceInterop.Resize(lastSize);
            }

            public override bool IsCorrupted => _initialSize != _info.Size;

            bool _firstRun = true;
            POINT lastSize;
            public override IGlPlatformSurfaceRenderingSession BeginDraw()
            {
                if (_firstRun)
                {
                    _firstRun = false;
                    var windowSurface = new EglGlPlatformSurface(Win32GlManager.EglFeature.DeferredContext, _info);

                    using (var target = windowSurface.CreateGlRenderTarget())
                    {
                        using (var session = target.BeginDraw())
                        {
                            using (session.Context.MakeCurrent())
                            {
                                var gl = _context.GlInterface;
                                gl.Viewport(0, 0, _info.Size.Width, _info.Size.Height);
                                gl.ClearStencil(0);
                                gl.ClearColor(0, 0, 0, 0);
                                gl.Clear(GlConsts.GL_COLOR_BUFFER_BIT | GlConsts.GL_DEPTH_BUFFER_BIT);
                                gl.Flush();                                
                            }
                        }
                    }
                }


                var iid = Guid.Parse("6f15aaf2-d208-4e89-9ab4-489535d34f9c");
                var updateRect = new RECT { right = _info.Size.Width, bottom = _info.Size.Height };
                var offset = new POINT();

                if (lastSize.X != _info.Size.Width || lastSize.Y != _info.Size.Height)
                {
                    lastSize = new POINT { X = _info.Size.Width, Y = _info.Size.Height };
                   // _surfaceInterop.Resize(lastSize);
                }                
                _surfaceInterop.BeginDraw(
                    ref updateRect,
                    ref iid,
                    out IntPtr texture, ref offset);

                var surface = (_display as AngleWin32EglDisplay).WrapDirect3D11Texture(texture);

                return base.BeginDraw(surface, _info, () => { _surfaceInterop.EndDraw(); Marshal.Release(texture); surface.Dispose();  }, true);
            }
        }
    }



    class CompositionHost
    {
        internal enum DISPATCHERQUEUE_THREAD_APARTMENTTYPE
        {
            DQTAT_COM_NONE = 0,
            DQTAT_COM_ASTA = 1,
            DQTAT_COM_STA = 2
        };

        internal enum DISPATCHERQUEUE_THREAD_TYPE
        {
            DQTYPE_THREAD_DEDICATED = 1,
            DQTYPE_THREAD_CURRENT = 2,
        };

        [StructLayout(LayoutKind.Sequential)]
        internal struct DispatcherQueueOptions
        {
            public int dwSize;

            [MarshalAs(UnmanagedType.I4)]
            public DISPATCHERQUEUE_THREAD_TYPE threadType;

            [MarshalAs(UnmanagedType.I4)]
            public DISPATCHERQUEUE_THREAD_APARTMENTTYPE apartmentType;
        };

        [DllImport("coremessaging.dll", EntryPoint = "CreateDispatcherQueueController", CharSet = CharSet.Unicode)]
        internal static extern IntPtr CreateDispatcherQueueController(DispatcherQueueOptions options, out IntPtr dispatcherQueueController);

        public static CompositionHost Instance { get; } = new CompositionHost();

        private Compositor _compositor;
        private Windows.System.DispatcherQueueController _dispatcherQueueController;
        private CompositionGraphicsDevice _graphicsDevice;

        private CompositionHost()
        {
            Initialize();
        }

        public void AddElement(CompositionTarget target, float size, float x, float y)
        {
            if (target.Root != null)
            {
                var visuals = target.Root.As<ContainerVisual>().Children;

                var visual = _compositor.CreateSpriteVisual();

                var element = _compositor.CreateSpriteVisual();
                var rand = new Random();

                element.Brush = _compositor.CreateColorBrush(new Windows.UI.Color { A = 255, R = (byte)(rand.NextDouble() * 255), G = (byte)(rand.NextDouble() * 255), B = (byte)(rand.NextDouble() * 255) });
                element.Size = new System.Numerics.Vector2(size, size);
                element.Offset = new System.Numerics.Vector3(x, y, 0.0f);

                var animation = _compositor.CreateVector3KeyFrameAnimation();
                var bottom = (float)600 - element.Size.Y;
                animation.InsertKeyFrame(1, new System.Numerics.Vector3(element.Offset.X, bottom, 0));

                animation.Duration = TimeSpan.FromSeconds(2);
                animation.DelayTime = TimeSpan.FromSeconds(3);
                element.StartAnimation("Offset", animation);
                visuals.InsertAtTop(element);

                visuals.InsertAtTop(visual);
            }
        }

        private void Initialize()
        {
            EnsureDispatcherQueue();
            if (_dispatcherQueueController != null)
                _compositor = new Windows.UI.Composition.Compositor();

            var interop = _compositor.As<Windows.UI.Composition.Interop.ICompositorInterop>();

            var display = Win32GlManager.EglFeature.Display as AngleWin32EglDisplay;

            _graphicsDevice = interop.CreateGraphicsDevice(display.GetDirect3DDevice());
        }

        public ICompositionDrawingSurfaceInterop InitialiseWindowCompositionTree(IntPtr hwnd)
        {
            var target = CreateDesktopWindowTarget(hwnd);            

            var surface = _graphicsDevice.CreateDrawingSurface(new Windows.Foundation.Size(0, 0),
                Windows.Graphics.DirectX.DirectXPixelFormat.B8G8R8A8UIntNormalized,
                Windows.Graphics.DirectX.DirectXAlphaMode.Premultiplied);

            var surfaceInterop = surface.As<ICompositionDrawingSurfaceInterop>();

            var brush = _compositor.CreateSurfaceBrush(surface);

            var visual = _compositor.CreateSpriteVisual();

            visual.RelativeSizeAdjustment = new System.Numerics.Vector2(1.0f, 1.0f);
            visual.Brush = brush;
            //_target.Root = visual;

            target.Root = CreateBlur();

            var visuals = target.Root.As<ContainerVisual>().Children;

            visuals.InsertAtTop(visual);

            return surfaceInterop;
        }

        public SpriteVisual CreateBlur()
        {
            var effect = new GaussianBlurEffect();
            var effectFactory = _compositor.CreateEffectFactory(effect);
            var blurBrush = effectFactory.CreateBrush();

            var backDropBrush = _compositor.CreateBackdropBrush();

            blurBrush.SetSourceParameter("backdrop", backDropBrush);

            var visual = _compositor.CreateSpriteVisual();

            visual.RelativeSizeAdjustment = new System.Numerics.Vector2(1.0f, 1.0f);
            visual.Brush = blurBrush;

            return visual;
        }

        CompositionTarget CreateDesktopWindowTarget(IntPtr window)
        {
            var interop = _compositor.As<global::Windows.UI.Composition.Desktop.ICompositorDesktopInterop>();

            interop.CreateDesktopWindowTarget(window, false, out var windowTarget);
            return Windows.UI.Composition.Desktop.DesktopWindowTarget.FromAbi(windowTarget);
        }

        void EnsureDispatcherQueue()
        {
            if (_dispatcherQueueController == null)
            {
                DispatcherQueueOptions options = new DispatcherQueueOptions();
                options.apartmentType = DISPATCHERQUEUE_THREAD_APARTMENTTYPE.DQTAT_COM_STA;
                options.threadType = DISPATCHERQUEUE_THREAD_TYPE.DQTYPE_THREAD_CURRENT;
                options.dwSize = Marshal.SizeOf(typeof(DispatcherQueueOptions));

                CreateDispatcherQueueController(options, out var queue);
                _dispatcherQueueController = Windows.System.DispatcherQueueController.FromAbi(queue);
            }
        }
    }
}

