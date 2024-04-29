using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.Tizen.Platform.Input;
using Avalonia.Tizen.Platform;

namespace Avalonia.Tizen;

internal class TizenPlatform
{
    public static readonly TizenPlatform Instance = new();

    private static NuiGlPlatform? s_glPlatform;
    private static Compositor? s_compositor;

    internal static NuiGlPlatform GlPlatform
    {
        get => s_glPlatform ?? throw new InvalidOperationException($"{nameof(TizenPlatform)} hasn't been initialized");
        private set => s_glPlatform = value;
    }

    internal static Compositor Compositor
    {
        get => s_compositor ?? throw new InvalidOperationException($"{nameof(TizenPlatform)} hasn't been initialized");
        private set => s_compositor = value;
    }

    internal static TizenThreadingInterface ThreadingInterface { get; } = new();

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
