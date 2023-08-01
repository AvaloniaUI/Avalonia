﻿using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.OpenGL.Egl;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.Tizen.Platform.Input;
using Avalonia.Tizen.Platform;
using Avalonia.Threading;

namespace Avalonia.Tizen;

class TizenPlatform
{
    public static readonly TizenPlatform Instance = new TizenPlatform();
    public static TizenPlatformOptions Options { get; private set; }
    internal static NuiGlPlatform GlPlatform { get; set; }
    internal static Compositor Compositor { get; private set; }

    public static void Initialize()
    {
        Options = AvaloniaLocator.Current.GetService<TizenPlatformOptions>() ?? new TizenPlatformOptions();
        
        AvaloniaLocator.CurrentMutable
            .Bind<ICursorFactory>().ToTransient<CursorFactoryStub>()
            .Bind<IWindowingPlatform>().ToConstant(new WindowingPlatformStub())
            .Bind<IKeyboardDevice>().ToSingleton<TizenKeyboardDevice>()
            .Bind<IPlatformSettings>().ToSingleton<TizenPlatformSettings>()
            .Bind<IPlatformThreadingInterface>().ToConstant(new TizenThreadingInterface())
            .Bind<IPlatformIconLoader>().ToSingleton<PlatformIconLoaderStub>()
            .Bind<IRenderTimer>().ToConstant(new TizenRenderTimer())
            .Bind<PlatformHotkeyConfiguration>().ToSingleton<PlatformHotkeyConfiguration>();

        Compositor = new Compositor(AvaloniaLocator.Current.GetService<IPlatformGraphics>());
    }
}
