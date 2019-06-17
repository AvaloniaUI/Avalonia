using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.iOS;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Shared.PlatformSupport;

namespace Avalonia
{
    public static class iOSApplicationExtensions
    {
        public static T UseiOS<T>(this T builder) where T : AppBuilderBase<T>, new()
        {
            builder.UseWindowingSubsystem(iOSPlatform.Initialize, "iOS");
            return builder;
        }
    }
}

namespace Avalonia.iOS
{
    public class iOSPlatform
    {
        internal static MouseDevice MouseDevice;
        internal static KeyboardDevice KeyboardDevice;

        public static void Initialize()
        {
            MouseDevice = new MouseDevice();
            KeyboardDevice = new KeyboardDevice();

            AvaloniaLocator.CurrentMutable
                .Bind<IRuntimePlatform>().ToSingleton<StandardRuntimePlatform>()
                .Bind<IClipboard>().ToTransient<Clipboard>()
                // TODO: what does this look like for iOS??
                //.Bind<ISystemDialogImpl>().ToTransient<SystemDialogImpl>()
                .Bind<IStandardCursorFactory>().ToTransient<CursorFactory>()
                .Bind<IPlatformSettings>().ToSingleton<PlatformSettings>()
                .Bind<IPlatformThreadingInterface>().ToConstant(PlatformThreadingInterface.Instance)
                .Bind<IPlatformIconLoader>().ToSingleton<PlatformIconLoader>()
                .Bind<IWindowingPlatform>().ToSingleton<WindowingPlatformImpl>()
                .Bind<IRenderTimer>().ToSingleton<DisplayLinkRenderTimer>()
                .Bind<PlatformHotkeyConfiguration>().ToSingleton<PlatformHotkeyConfiguration>()
                .Bind<IRenderLoop>().ToSingleton<RenderLoop>();
        }
    }
}
