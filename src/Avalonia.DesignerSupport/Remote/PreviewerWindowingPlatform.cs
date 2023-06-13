using System;
using System.Collections.Generic;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Platform;
using Avalonia.Remote.Protocol;
using Avalonia.Rendering;
using Avalonia.Threading;

namespace Avalonia.DesignerSupport.Remote
{
    class PreviewerWindowingPlatform : IWindowingPlatform
    {
        static readonly IKeyboardDevice Keyboard = new KeyboardDevice();
        private static IAvaloniaRemoteTransportConnection s_transport;
        private static DetachableTransportConnection s_lastWindowTransport;
        private static PreviewerWindowImpl s_lastWindow;
        public static List<object> PreFlightMessages = new List<object>();

        public ITrayIconImpl CreateTrayIcon() => null;

        public IWindowImpl CreateWindow() => new WindowStub();

        public IWindowImpl CreateEmbeddableWindow()
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

        public static void Initialize(IAvaloniaRemoteTransportConnection transport)
        {
            s_transport = transport;
            var instance = new PreviewerWindowingPlatform();
            AvaloniaLocator.CurrentMutable
                .Bind<ICursorFactory>().ToSingleton<CursorFactoryStub>()
                .Bind<IKeyboardDevice>().ToConstant(Keyboard)
                .Bind<IPlatformSettings>().ToSingleton<DefaultPlatformSettings>()
                .Bind<IDispatcherImpl>().ToConstant(new ManagedDispatcherImpl(null))
                .Bind<IRenderTimer>().ToConstant(new UiThreadRenderTimer(60))
                .Bind<IWindowingPlatform>().ToConstant(instance)
                .Bind<IPlatformIconLoader>().ToSingleton<IconLoaderStub>()
                .Bind<PlatformHotkeyConfiguration>().ToSingleton<PlatformHotkeyConfiguration>();

        }
    }
}
