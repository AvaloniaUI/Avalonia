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

#nullable enable

namespace Avalonia.Native
{
    class AvaloniaNativePlatform : IPlatformSettings, IWindowingPlatform
    {
        private readonly IAvaloniaNativeFactory _factory;
        private AvaloniaNativePlatformOptions _options;
        private AvaloniaNativePlatformOpenGlInterface? _platformGl;

        [DllImport("libAvaloniaNative")]
        static extern IntPtr CreateAvaloniaNative();

        internal static readonly KeyboardDevice KeyboardDevice = new KeyboardDevice();

        public Size DoubleClickSize => new Size(4, 4);

        public TimeSpan DoubleClickTime => TimeSpan.FromMilliseconds(500); //TODO

        public static AvaloniaNativePlatform Initialize(IntPtr factory, AvaloniaNativePlatformOptions options)
        {
            var result =  new AvaloniaNativePlatform(MicroComRuntime.CreateProxyFor<IAvaloniaNativeFactory>(factory, true), options);
            result.DoInitialize();

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

        public void SetupApplicationMenuExporter ()
        {
            var exporter = new AvaloniaNativeMenuExporter(_factory);
        }

        public void SetupApplicationName ()
        {
            if(!string.IsNullOrWhiteSpace(Application.Current?.Name))
            {
                _factory.MacOptions.SetApplicationTitle(Application.Current?.Name);
            }
            else
            {
                _factory.MacOptions.SetApplicationTitle("");
            }
        }

        private AvaloniaNativePlatform(IAvaloniaNativeFactory factory, AvaloniaNativePlatformOptions options)
        {
            _factory = factory;
            _options = options;
        }

        class GCHandleDeallocator : CallbackBase, IAvnGCHandleDeallocatorCallback
        {
            public void FreeGCHandle(IntPtr handle)
            {
                GCHandle.FromIntPtr(handle).Free();
            }
        }
        
        void DoInitialize()
        {
            var applicationPlatform = new AvaloniaNativeApplicationPlatform();
            
            _factory.Initialize(new GCHandleDeallocator(), applicationPlatform);
            if (_factory.MacOptions != null)
            {
                var macOpts = AvaloniaLocator.Current.GetService<MacOSPlatformOptions>() ?? new MacOSPlatformOptions();

                _factory.MacOptions.SetShowInDock(macOpts.ShowInDock ? 1 : 0);
            }

            AvaloniaLocator.CurrentMutable
                .Bind<IPlatformThreadingInterface>()
                .ToConstant(new PlatformThreadingInterface(_factory.CreatePlatformThreadingInterface()))
                .Bind<ICursorFactory>().ToConstant(new CursorFactory(_factory.CreateCursorFactory()))
                .Bind<IPlatformIconLoader>().ToSingleton<IconLoader>()
                .Bind<IKeyboardDevice>().ToConstant(KeyboardDevice)
                .Bind<IPlatformSettings>().ToConstant(this)
                .Bind<IWindowingPlatform>().ToConstant(this)
                .Bind<IClipboard>().ToConstant(new ClipboardImpl(_factory.CreateClipboard()))
                .Bind<IRenderLoop>().ToConstant(new RenderLoop())
                .Bind<IRenderTimer>().ToConstant(new DefaultRenderTimer(60))
                .Bind<ISystemDialogImpl>().ToConstant(new SystemDialogs(_factory.CreateSystemDialogs()))
                .Bind<PlatformHotkeyConfiguration>().ToConstant(new PlatformHotkeyConfiguration(KeyModifiers.Meta, wholeWordTextActionModifiers: KeyModifiers.Alt))
                .Bind<IMountedVolumeInfoProvider>().ToConstant(new MacOSMountedVolumeInfoProvider())
                .Bind<IPlatformDragSource>().ToConstant(new AvaloniaNativeDragSource(_factory))
                .Bind<IPlatformLifetimeEventsImpl>().ToConstant(applicationPlatform)
                .Bind<INativeApplicationCommands>().ToConstant(new MacOSNativeMenuCommands(_factory.CreateApplicationCommands()))
                .Bind<IPlatformBehaviorInhibition>().ToConstant(new PlatformBehaviorInhibition(_factory.CreatePlatformBehaviorInhibition()));

            var hotkeys = AvaloniaLocator.Current.GetService<PlatformHotkeyConfiguration>();

            if (hotkeys != null)
            {
                hotkeys.MoveCursorToTheStartOfLine.Add(new KeyGesture(Key.Left, hotkeys.CommandModifiers));
                hotkeys.MoveCursorToTheStartOfLineWithSelection.Add(new KeyGesture(Key.Left,
                    hotkeys.CommandModifiers | hotkeys.SelectionModifiers));
                hotkeys.MoveCursorToTheEndOfLine.Add(new KeyGesture(Key.Right, hotkeys.CommandModifiers));
                hotkeys.MoveCursorToTheEndOfLineWithSelection.Add(new KeyGesture(Key.Right,
                    hotkeys.CommandModifiers | hotkeys.SelectionModifiers));
            }

            if (_options.UseGpu)
            {
                try
                {
                    AvaloniaLocator.CurrentMutable.Bind<IPlatformOpenGlInterface>()
                        .ToConstant(_platformGl = new AvaloniaNativePlatformOpenGlInterface(_factory.ObtainGlDisplay()));
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }

        public ITrayIconImpl CreateTrayIcon ()
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
}
