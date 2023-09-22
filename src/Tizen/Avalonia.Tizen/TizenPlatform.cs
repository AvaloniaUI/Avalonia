using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.OpenGL.Egl;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.Tizen.Platform.Input;
using Avalonia.Tizen.Platform;
using Avalonia.Threading;

namespace Avalonia.Tizen;

internal class TizenPlatform
{
    public static readonly TizenPlatform Instance = new();
    internal static NuiGlPlatform GlPlatform { get; set; }
    internal static Compositor Compositor { get; private set; }
    internal static TizenThreadingInterface ThreadingInterface { get; private set; } = new();

    public static void Initialize()
    {   
        AvaloniaLocator.CurrentMutable
            .Bind<ICursorFactory>().ToTransient<CursorFactoryStub>()
            .Bind<IWindowingPlatform>().ToConstant(new WindowingPlatformStub())
            .Bind<IKeyboardDevice>().ToSingleton<TizenKeyboardDevice>()
            .Bind<IPlatformSettings>().ToSingleton<TizenPlatformSettings>()
            .Bind<IPlatformThreadingInterface>().ToConstant(ThreadingInterface)
            .Bind<IPlatformIconLoader>().ToSingleton<PlatformIconLoaderStub>()
            .Bind<IRenderTimer>().ToConstant(new TizenRenderTimer())
            .Bind<PlatformHotkeyConfiguration>().ToSingleton<PlatformHotkeyConfiguration>()
            .Bind<IPlatformGraphics>().ToConstant(GlPlatform = new NuiGlPlatform());

        Compositor = new Compositor(AvaloniaLocator.Current.GetService<IPlatformGraphics>());
    }
}
