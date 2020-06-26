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
            _surfaceInterop = CompositionHost.Instance.Initialize(hwnd);
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
            }

            public override bool IsCorrupted => _initialSize != _info.Size;

            public override IGlPlatformSurfaceRenderingSession BeginDraw()
            {
                var l = _context.Lock();

                var iid = Guid.Parse("6f15aaf2-d208-4e89-9ab4-489535d34f9c");
                var updateRect = new RECT { right = _info.Size.Width, bottom = _info.Size.Height };
                var offset = new POINT();

                _surfaceInterop.Resize(new POINT { X = _info.Size.Width, Y = _info.Size.Height });
                _surfaceInterop.BeginDraw(
                    ref updateRect,
                    ref iid,
                    out IntPtr texture, ref offset);

                var surface = (_display as AngleWin32EglDisplay).WrapDirect3D11Texture(texture);

                var restoreContext = _context.MakeCurrent(surface);

                return base.BeginDraw(surface, _info, () => { _surfaceInterop.EndDraw(); }, true);
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
        private Windows.UI.Composition.Desktop.DesktopWindowTarget _target;

        private CompositionHost()
        {
        }

        public void AddElement(float size, float x, float y)
        {
            if (_target.Root != null)
            {
                var visuals = _target.Root.As<ContainerVisual>().Children;

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

        public ICompositionDrawingSurfaceInterop Initialize(IntPtr hwnd)
        {
            EnsureDispatcherQueue();
            if (_dispatcherQueueController != null)
                _compositor = new Windows.UI.Composition.Compositor();

            CreateDesktopWindowTarget(hwnd);
            CreateCompositionRoot();

            var interop = _compositor.As<Windows.UI.Composition.Interop.ICompositorInterop>();

            var display = Win32GlManager.EglFeature.Display as AngleWin32EglDisplay;

            var gDevice = interop.CreateGraphicsDevice(display.GetDirect3DDevice());

            var surface = gDevice.CreateDrawingSurface(new Windows.Foundation.Size(100, 100), 
                Windows.Graphics.DirectX.DirectXPixelFormat.B8G8R8A8UIntNormalized, 
                Windows.Graphics.DirectX.DirectXAlphaMode.Premultiplied);

            var surfaceInterop = surface.As<ICompositionDrawingSurfaceInterop>();            

            var brush = _compositor.CreateSurfaceBrush(surface);

            var visual = _compositor.CreateSpriteVisual();

            visual.RelativeSizeAdjustment = new System.Numerics.Vector2(1.0f, 1.0f);
            visual.Brush = brush;

            _target.Root = visual;

            AddElement(100, 200, 200);

            return surfaceInterop;
        }

        public void CreateBlur()
        {
            var effect = new GaussianBlurEffect();
            var effectFactory = _compositor.CreateEffectFactory(effect);
            var blurBrush = effectFactory.CreateBrush();

            var backDropBrush = _compositor.CreateBackdropBrush();

            blurBrush.SetSourceParameter("backdrop", backDropBrush);

            var visual = _compositor.CreateSpriteVisual();

            visual.RelativeSizeAdjustment = new System.Numerics.Vector2(1.0f, 1.0f);
            visual.Brush = blurBrush;

            _target.Root = visual;
        }

        void CreateCompositionRoot()
        {
            var root = _compositor.CreateContainerVisual();
            root.RelativeSizeAdjustment = new System.Numerics.Vector2(1.0f, 1.0f);
            //root.Offset = new System.Numerics.Vector3(0, 0, 0);
            _target.Root = root;
        }

        void CreateDesktopWindowTarget(IntPtr window)
        {
            var interop = _compositor.As<global::Windows.UI.Composition.Desktop.ICompositorDesktopInterop>();

            interop.CreateDesktopWindowTarget(window, false, out var windowTarget);
            _target = Windows.UI.Composition.Desktop.DesktopWindowTarget.FromAbi(windowTarget);
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

