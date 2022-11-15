using System;
using System.Runtime.InteropServices;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.MicroCom;
using Avalonia.Native.Interop;
using Avalonia.OpenGL;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using JetBrains.Annotations;
using MicroCom.Runtime;

namespace Avalonia.Native
{
    class AvaloniaNativePlatform : IWindowingPlatform
    {
        private readonly IAvaloniaNativeFactory _factory;
        private AvaloniaNativePlatformOptions _options;
        private AvaloniaNativePlatformOpenGlInterface _platformGl;

        [DllImport("libAvaloniaNative")]
        static extern IntPtr CreateAvaloniaNative();

        internal static readonly KeyboardDevice KeyboardDevice = new KeyboardDevice();
        [CanBeNull] internal static Compositor Compositor { get; private set; }

        public static AvaloniaNativePlatform Initialize(IntPtr factory, AvaloniaNativePlatformOptions options)
        {
            var result = new AvaloniaNativePlatform(MicroComRuntime.CreateProxyFor<IAvaloniaNativeFactory>(factory, true));
            result.DoInitialize(options);

            return result;
        }

        delegate IntPtr CreateAvaloniaNativeDelegate();

        public static AvaloniaNativePlatform Initialize(AvaloniaNativePlatformOptions options)
        {
            if (options.AvaloniaNativeLibraryPath != null)
            {
                var loader = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                    (IDynLoader)new Win32Loader() :
                    new UnixLoader();

                var lib = loader.LoadLibrary(options.AvaloniaNativeLibraryPath);
                var proc = loader.GetProcAddress(lib, "CreateAvaloniaNative", false);
                var d = Marshal.GetDelegateForFunctionPointer<CreateAvaloniaNativeDelegate>(proc);


                return Initialize(d(), options);
            }
            else
                return Initialize(CreateAvaloniaNative(), options);
        }

        public void SetupApplicationMenuExporter()
        {
            var exporter = new AvaloniaNativeMenuExporter(_factory);
        }

        public void SetupApplicationName()
        {
            if (!string.IsNullOrWhiteSpace(Application.Current.Name))
            {
                _factory.MacOptions.SetApplicationTitle(Application.Current.Name);
            }
        }

        private AvaloniaNativePlatform(IAvaloniaNativeFactory factory)
        {
            _factory = factory;
        }

        class GCHandleDeallocator : NativeCallbackBase, IAvnGCHandleDeallocatorCallback
        {
            public void FreeGCHandle(IntPtr handle)
            {
                GCHandle.FromIntPtr(handle).Free();
            }
        }

        void DoInitialize(AvaloniaNativePlatformOptions options)
        {
            _options = options;

            var applicationPlatform = new AvaloniaNativeApplicationPlatform();

            _factory.Initialize(new GCHandleDeallocator(), applicationPlatform);
            if (_factory.MacOptions != null)
            {
                var macOpts = AvaloniaLocator.Current.GetService<MacOSPlatformOptions>() ?? new MacOSPlatformOptions();

                _factory.MacOptions.SetShowInDock(macOpts.ShowInDock ? 1 : 0);
                _factory.MacOptions.SetDisableSetProcessName(macOpts.DisableSetProcessName ? 1 : 0);
                _factory.MacOptions.SetDisableAppDelegate(macOpts.DisableAvaloniaAppDelegate ? 1 : 0);
            }

            AvaloniaLocator.CurrentMutable
                .Bind<IPlatformThreadingInterface>()
                .ToConstant(new PlatformThreadingInterface(_factory.CreatePlatformThreadingInterface()))
                .Bind<ICursorFactory>().ToConstant(new CursorFactory(_factory.CreateCursorFactory()))
                .Bind<IPlatformIconLoader>().ToSingleton<IconLoader>()
                .Bind<IKeyboardDevice>().ToConstant(KeyboardDevice)
                .Bind<IPlatformSettings>().ToSingleton<DefaultPlatformSettings>()
                .Bind<IWindowingPlatform>().ToConstant(this)
                .Bind<IClipboard>().ToConstant(new ClipboardImpl(_factory.CreateClipboard()))
                .Bind<IRenderTimer>().ToConstant(new DefaultRenderTimer(60))
                .Bind<PlatformHotkeyConfiguration>().ToConstant(new PlatformHotkeyConfiguration(KeyModifiers.Meta, wholeWordTextActionModifiers: KeyModifiers.Alt))
                .Bind<IMountedVolumeInfoProvider>().ToConstant(new MacOSMountedVolumeInfoProvider())
                .Bind<IPlatformDragSource>().ToConstant(new AvaloniaNativeDragSource(_factory))
                .Bind<IPlatformLifetimeEventsImpl>().ToConstant(applicationPlatform)
                .Bind<INativeApplicationCommands>().ToConstant(new MacOSNativeMenuCommands(_factory.CreateApplicationCommands()));

            var renderLoop = new RenderLoop();
            AvaloniaLocator.CurrentMutable.Bind<IRenderLoop>().ToConstant(renderLoop);

            var hotkeys = AvaloniaLocator.Current.GetService<PlatformHotkeyConfiguration>();
            hotkeys.MoveCursorToTheStartOfLine.Add(new KeyGesture(Key.Left, hotkeys.CommandModifiers));
            hotkeys.MoveCursorToTheStartOfLineWithSelection.Add(new KeyGesture(Key.Left, hotkeys.CommandModifiers | hotkeys.SelectionModifiers));
            hotkeys.MoveCursorToTheEndOfLine.Add(new KeyGesture(Key.Right, hotkeys.CommandModifiers));
            hotkeys.MoveCursorToTheEndOfLineWithSelection.Add(new KeyGesture(Key.Right, hotkeys.CommandModifiers | hotkeys.SelectionModifiers));
            
            if (_options.UseGpu)
            {
                try
                {
                    _platformGl = new AvaloniaNativePlatformOpenGlInterface(_factory.ObtainGlDisplay());
                    AvaloniaLocator.CurrentMutable
                        .Bind<IPlatformOpenGlInterface>().ToConstant(_platformGl)
                        .Bind<IPlatformGpu>().ToConstant(_platformGl);

                }
                catch (Exception)
                {
                    // ignored
                }
            }
            

            if (_options.UseDeferredRendering && _options.UseCompositor)
            {
                Compositor = new Compositor(renderLoop, _platformGl);
            }
        }

        public ITrayIconImpl CreateTrayIcon()
        {
            return new TrayIconImpl(_factory);
        }

        public IWindowImpl CreateWindow()
        {
            return new WindowImpl(_factory, _options, _platformGl);
        }

        public IWindowImpl CreateEmbeddableWindow()
        {
            throw new NotImplementedException();
        }
    }

    public class AvaloniaNativeMacOptions
    {
        private readonly IAvnMacOptions _opts;
        private bool _showInDock;
        internal AvaloniaNativeMacOptions(IAvnMacOptions opts)
        {
            _opts = opts;
            ShowInDock = true;
        }

        public bool ShowInDock
        {
            get => _showInDock;
            set
            {
                _showInDock = value;
                _opts.SetShowInDock(value ? 1 : 0);
            }
        }
    }
}
