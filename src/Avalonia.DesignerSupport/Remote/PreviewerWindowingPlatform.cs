using System;
using System.Collections.Generic;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Platform;
using Avalonia.Remote.Protocol;
using Avalonia.Rendering;

namespace Avalonia.DesignerSupport.Remote
{
    class PreviewerWindowingPlatform : IWindowingPlatform, IPlatformSettings
    {
        static readonly IKeyboardDevice Keyboard = new KeyboardDevice();
        private static IAvaloniaRemoteTransportConnection s_transport;
        private static DetachableTransportConnection s_lastWindowTransport;
        private static PreviewerWindowImpl s_lastWindow;
        public static List<object> PreFlightMessages = new List<object>();
        
        public IWindowImpl CreateWindow() => new WindowStub();

        public IEmbeddableWindowImpl CreateEmbeddableWindow()
        {
            if (s_lastWindow != null)
            {
                s_lastWindowTransport.Dispose();
                try
                {
                    s_lastWindow.Dispose();
                }
                catch
                {
                    //Ignore
                }
            }
            s_lastWindow =
                new PreviewerWindowImpl(s_lastWindowTransport = new DetachableTransportConnection(s_transport));
            foreach (var pf in PreFlightMessages)
                s_lastWindowTransport.FireOnMessage(s_lastWindowTransport, pf);
            return s_lastWindow;
        }

        public IPopupImpl CreatePopup() => new WindowStub();

        public static void Initialize(IAvaloniaRemoteTransportConnection transport)
        {
            s_transport = transport;
            var instance = new PreviewerWindowingPlatform();
            var threading = new InternalPlatformThreadingInterface();
            AvaloniaLocator.CurrentMutable
                .Bind<IClipboard>().ToSingleton<ClipboardStub>()
                .Bind<IStandardCursorFactory>().ToSingleton<CursorFactoryStub>()
                .Bind<IPlatformSettings>().ToConstant(instance)
                .Bind<IPlatformThreadingInterface>().ToConstant(threading)
                .Bind<IRenderLoop>().ToConstant(new RenderLoop())
                .Bind<IRenderTimer>().ToConstant(threading)
                .Bind<ISystemDialogImpl>().ToSingleton<SystemDialogsStub>()
                .Bind<IWindowingPlatform>().ToConstant(instance)
                .Bind<IPlatformIconLoader>().ToSingleton<IconLoaderStub>()
                .Bind<PlatformHotkeyConfiguration>().ToSingleton<PlatformHotkeyConfiguration>();

        }

        public Size DoubleClickSize { get; } = new Size(2, 2);
        public TimeSpan DoubleClickTime { get; } = TimeSpan.FromMilliseconds(500);
    }
}
