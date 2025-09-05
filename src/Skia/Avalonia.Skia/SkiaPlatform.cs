using Avalonia.Media;
using Avalonia.Platform;

namespace Avalonia.Skia
{
    /// <summary>
    /// Skia platform initializer.
    /// </summary>
    public static class SkiaPlatform
    {
        /// <summary>
        /// Initialize Skia platform.
        /// </summary>
        public static void Initialize()
        {
            Initialize(new SkiaOptions());
        }

        public static void Initialize(SkiaOptions options)
        {
            var renderInterface = new PlatformRenderInterface(options.MaxGpuResourceSizeBytes);

            AvaloniaLocator.CurrentMutable
                .Bind<IPlatformRenderInterface>().ToConstant(renderInterface)
                .Bind<FontManager>().ToLazy(() => new FontManager(new FontManagerImpl()))
                .Bind<ITextShaperImpl>().ToConstant(new TextShaperImpl());
        }

        /// <summary>
        /// Default DPI.
        /// </summary>
        public static Vector DefaultDpi => new Vector(96.0f, 96.0f);
    }
}
