using Avalonia;

internal static class ImpellerAndroidExtensions
{
    public static AppBuilder UseImpellerIfEnabled(this AppBuilder builder)
    {
#if AVALONIA_IMPELLER
        // Impeller's Vulkan swapchains can't be sized here, so it renders with OpenGL ES.
        return builder
            .With(new AndroidPlatformOptions { RenderingMode = [AndroidRenderingMode.Egl] })
            .UseImpeller();
#else
        return builder;
#endif
    }
}
