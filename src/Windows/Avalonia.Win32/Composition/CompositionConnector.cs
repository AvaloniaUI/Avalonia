using System;
using System.Runtime.InteropServices;
using Avalonia.Logging;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Angle;
using Avalonia.OpenGL.Egl;
using Windows.UI.Composition;
using Windows.UI.Composition.Interop;
using WinRT;

namespace Avalonia.Win32
{
    internal class CompositionConnector
    {
        private Compositor _compositor;
        private Windows.System.DispatcherQueueController _dispatcherQueueController;
        private CompositionGraphicsDevice _graphicsDevice;

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

        public static CompositionConnector TryCreate(EglPlatformOpenGlInterface egl)
        {
            const int majorRequired = 10;
            const int buildRequired = 16299;

            var majorInstalled = Win32Platform.WindowsVersion.Major;
            var buildInstalled = Win32Platform.WindowsVersion.Build;
            
            if (majorInstalled >= majorRequired &&
                buildInstalled >= buildRequired)
            {
                try
                {
                    return new CompositionConnector(egl);
                }
                catch (Exception e)
                {
                    Logger.TryGet(LogEventLevel.Error, "WinUIComposition")?.Log(null, "Unable to initialize WinUI compositor: {0}", e);

                    return null;
                }
            }

            var osVersionNotice = $"Windows {majorRequired} Build {buildRequired} is required. Your machine has Windows {majorInstalled} Build {buildInstalled} installed.";

            Logger.TryGet(LogEventLevel.Warning, "WinUIComposition")?.Log(null,
                $"Unable to initialize WinUI compositor: {osVersionNotice}");

            return null;
        }
        
        public CompositionConnector(EglPlatformOpenGlInterface egl)
        {
            EnsureDispatcherQueue();

            if (_dispatcherQueueController != null)
                _compositor = new Compositor();

            var interop = _compositor.As<ICompositorInterop>();

            var display = egl.Display as AngleWin32EglDisplay;

            _graphicsDevice = interop.CreateGraphicsDevice(display.GetDirect3DDevice());
        }

        public ICompositionDrawingSurfaceInterop InitialiseWindowCompositionTree(IntPtr hwnd, out Windows.UI.Composition.Visual surfaceVisual, out IBlurHost blurHost)
        {
            var target = CreateDesktopWindowTarget(hwnd);

            var surface = _graphicsDevice.CreateDrawingSurface(new Windows.Foundation.Size(0, 0),
                Windows.Graphics.DirectX.DirectXPixelFormat.B8G8R8A8UIntNormalized,
                Windows.Graphics.DirectX.DirectXAlphaMode.Premultiplied);

            var surfaceInterop = surface.As<ICompositionDrawingSurfaceInterop>();

            var brush = _compositor.CreateSurfaceBrush(surface);
            var visual = _compositor.CreateSpriteVisual();

            visual.Brush = brush;
            visual.RelativeSizeAdjustment = new System.Numerics.Vector2(1, 1);

            var container = _compositor.CreateContainerVisual();

            target.Root = container;

            var blur = CreateBlur();

            blurHost = new CompositionBlurHost(blur);

            container.Children.InsertAtTop(blur);

            container.Children.InsertAtTop(visual);

            visual.CompositeMode = CompositionCompositeMode.SourceOver;

            surfaceVisual = container;

            return surfaceInterop;
        }

        private SpriteVisual CreateBlur()
        {
            var blurEffect = new GaussianBlurEffect(new CompositionEffectSourceParameter("backdrop"));
            var blurEffectFactory = _compositor.CreateEffectFactory(blurEffect);

            var blurBrush = blurEffectFactory.CreateBrush();
            var backDropBrush = _compositor.CreateBackdropBrush();

            blurBrush.SetSourceParameter("backdrop", backDropBrush);

            var saturateEffect = new SaturationEffect(blurEffect);
            var satEffectFactory = _compositor.CreateEffectFactory(saturateEffect);

            var satBrush = satEffectFactory.CreateBrush();
            satBrush.SetSourceParameter("backdrop", backDropBrush);

            var visual = _compositor.CreateSpriteVisual();
            visual.IsVisible = false;

            visual.RelativeSizeAdjustment = new System.Numerics.Vector2(1.0f, 1.0f);
            visual.Brush = satBrush;

            return visual;
        }

        private CompositionTarget CreateDesktopWindowTarget(IntPtr window)
        {
            var interop = _compositor.As<global::Windows.UI.Composition.Desktop.ICompositorDesktopInterop>();

            interop.CreateDesktopWindowTarget(window, false, out var windowTarget);
            return Windows.UI.Composition.Desktop.DesktopWindowTarget.FromAbi(windowTarget);
        }

        private void EnsureDispatcherQueue()
        {
            if (_dispatcherQueueController == null)
            {
                DispatcherQueueOptions options = new DispatcherQueueOptions();
                options.apartmentType = DISPATCHERQUEUE_THREAD_APARTMENTTYPE.DQTAT_COM_NONE;
                options.threadType = DISPATCHERQUEUE_THREAD_TYPE.DQTYPE_THREAD_CURRENT;
                options.dwSize = Marshal.SizeOf(typeof(DispatcherQueueOptions));

                CreateDispatcherQueueController(options, out var queue);
                _dispatcherQueueController = Windows.System.DispatcherQueueController.FromAbi(queue);
            }
        }
    }
}

