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
        private AvaloniaNativePlatformOptions _options;

        [DllImport("libAvaloniaNative")]
        static extern IntPtr CreateAvaloniaNative();

        internal static readonly MouseDevice MouseDevice = new MouseDevice();
        internal static readonly KeyboardDevice KeyboardDevice = new KeyboardDevice();

        public Size DoubleClickSize => new Size(4, 4);

        public TimeSpan DoubleClickTime => TimeSpan.FromMilliseconds(500); //TODO

        public static void Initialize(IntPtr factory, AvaloniaNativePlatformOptions options)
        {
            new AvaloniaNativePlatform(new IAvaloniaNativeFactory(factory))
                .DoInitialize(options);
        }

        delegate IntPtr CreateAvaloniaNativeDelegate();

        public static void Initialize(AvaloniaNativePlatformOptions options)
        {
            if (options.AvaloniaNativeLibraryPath != null)
            {
                var loader = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                    (IDynLoader)new Win32Loader() :
                    new UnixLoader();

                var lib = loader.LoadLibrary(options.AvaloniaNativeLibraryPath);
                var proc = loader.GetProcAddress(lib, "CreateAvaloniaNative", false);
                var d = Marshal.GetDelegateForFunctionPointer<CreateAvaloniaNativeDelegate>(proc);


                Initialize(d(), options);
            }
            else
                Initialize(CreateAvaloniaNative(), options);
        }

        private AvaloniaNativePlatform(IAvaloniaNativeFactory factory)
        {
            _factory = factory;
        }

        void DoInitialize(AvaloniaNativePlatformOptions options)
        {
            _options = options;
            _factory.Initialize();
            if (_factory.MacOptions != null)
            {
                var macOpts = AvaloniaLocator.Current.GetService<MacOSPlatformOptions>();
                _factory.MacOptions.ShowInDock = macOpts?.ShowInDock != false ? 1 : 0;
            }

            AvaloniaLocator.CurrentMutable
                .Bind<IPlatformThreadingInterface>()
                .ToConstant(new PlatformThreadingInterface(_factory.CreatePlatformThreadingInterface()))
                .Bind<IStandardCursorFactory>().ToConstant(new CursorFactory(_factory.CreateCursorFactory()))
                .Bind<IPlatformIconLoader>().ToSingleton<IconLoader>()
                .Bind<IPlatformSettings>().ToConstant(this)
                .Bind<IWindowingPlatform>().ToConstant(this)
                .Bind<IClipboard>().ToConstant(new ClipboardImpl(_factory.CreateClipboard()))
                .Bind<IRenderLoop>().ToConstant(new RenderLoop())
                .Bind<IRenderTimer>().ToConstant(new DefaultRenderTimer(60))
                .Bind<ISystemDialogImpl>().ToConstant(new SystemDialogs(_factory.CreateSystemDialogs()))
                .Bind<IWindowingPlatformGlFeature>().ToConstant(new GlPlatformFeature(_factory.ObtainGlFeature()))
                .Bind<PlatformHotkeyConfiguration>()
                .ToConstant(new PlatformHotkeyConfiguration(InputModifiers.Windows));
        }

        public IWindowImpl CreateWindow()
        {
            return new WindowImpl(_factory, _options);
        }

        public IEmbeddableWindowImpl CreateEmbeddableWindow()
        {
            throw new NotImplementedException();
        }

        public IPopupImpl CreatePopup()
        {
            return new PopupImpl(_factory, _options);
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
}
