// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.
using System;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Native.Interop;
using Avalonia.OpenGL;
using Avalonia.Platform;
using Avalonia.Rendering;

namespace Avalonia.Native
{
    class AvaloniaNativePlatform : IPlatformSettings, IWindowingPlatform
    {
        private readonly IAvaloniaNativeFactory _factory;

        [DllImport("libAvaloniaNative")]
        static extern IntPtr CreateAvaloniaNative();

        internal static readonly MouseDevice MouseDevice = new MouseDevice();
        internal static readonly KeyboardDevice KeyboardDevice = new KeyboardDevice();

        public Size DoubleClickSize => new Size(4, 4);

        public TimeSpan DoubleClickTime => TimeSpan.FromMilliseconds(500); //TODO

        public static void Initialize(IntPtr factory, Action<AvaloniaNativeOptions> configure)
        {
            new AvaloniaNativePlatform(new IAvaloniaNativeFactory(factory))
                .DoInitialize(configure);
        }

        delegate IntPtr CreateAvaloniaNativeDelegate();

        public static void Initialize(string library, Action<AvaloniaNativeOptions> configure)
        {
            var loader = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                                           ? (IDynLoader)new Win32Loader() : new UnixLoader();
            var lib = loader.LoadLibrary(library);
            var proc = loader.GetProcAddress(lib, "CreateAvaloniaNative", false);
            var d = Marshal.GetDelegateForFunctionPointer<CreateAvaloniaNativeDelegate>(proc);


            Initialize(d(), configure);
        }

        public static void Initialize(Action<AvaloniaNativeOptions> configure)
        {
            Initialize(CreateAvaloniaNative(), configure);
        }

        private AvaloniaNativePlatform(IAvaloniaNativeFactory factory)
        {
            _factory = factory;
        }

        void DoInitialize(Action<AvaloniaNativeOptions> configure)
        {
            var opts = new AvaloniaNativeOptions(_factory);
            configure?.Invoke(opts);
            _factory.Initialize();

            AvaloniaLocator.CurrentMutable
                .Bind<IPlatformThreadingInterface>().ToConstant(new PlatformThreadingInterface(_factory.CreatePlatformThreadingInterface()))
                .Bind<IStandardCursorFactory>().ToConstant(new CursorFactory(_factory.CreateCursorFactory()))
                .Bind<IPlatformIconLoader>().ToSingleton<IconLoader>()
                .Bind<IKeyboardDevice>().ToConstant(KeyboardDevice)
                .Bind<IMouseDevice>().ToConstant(MouseDevice)
                .Bind<IPlatformSettings>().ToConstant(this)
                .Bind<IWindowingPlatform>().ToConstant(this)
                .Bind<IClipboard>().ToConstant(new ClipboardImpl(_factory.CreateClipboard()))
                .Bind<IRenderLoop>().ToConstant(new RenderLoop())
                .Bind<IRenderTimer>().ToConstant(new DefaultRenderTimer(60))
                .Bind<ISystemDialogImpl>().ToConstant(new SystemDialogs(_factory.CreateSystemDialogs()))
                .Bind<IWindowingPlatformGlFeature>().ToConstant(new GlPlatformFeature(_factory.ObtainGlFeature()))
                .Bind<PlatformHotkeyConfiguration>().ToConstant(new PlatformHotkeyConfiguration(InputModifiers.Windows))
                .Bind<AvaloniaNativeOptions>().ToConstant(opts);
        }

        public IWindowImpl CreateWindow()
        {
            return new WindowImpl(_factory);
        }

        public IEmbeddableWindowImpl CreateEmbeddableWindow()
        {
            throw new NotImplementedException();
        }

        public IPopupImpl CreatePopup()
        {
            return new PopupImpl(_factory);
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
                _opts.ShowInDock = value ? 1 : 0;
            }
        }
    }

    public class AvaloniaNativeOptions
    {
        public AvaloniaNativeMacOptions MacOptions { get; set; }
        public bool UseDeferredRendering { get; set; } = true;
        public bool UseGpu { get; set; } = true;
        internal AvaloniaNativeOptions(IAvaloniaNativeFactory factory)
        {
            var mac = factory.GetMacOptions();
            if (mac != null)
                MacOptions = new AvaloniaNativeMacOptions(mac);
        }

    }
}
