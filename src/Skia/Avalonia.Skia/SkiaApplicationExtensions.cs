using Avalonia.Controls;
using Avalonia.Skia;

// ReSharper disable once CheckNamespace
namespace Avalonia
{
    /// <summary>
    /// Skia application extensions.
    /// </summary>
    public static class SkiaApplicationExtensions
    {
        /// <summary>
        /// Enable Skia renderer.
        /// </summary>
        /// <param name="builder">Builder.</param>
        /// <returns>Configure builder.</returns>
        public static AppBuilder UseSkia(this AppBuilder builder)
        {
            return builder.UseRenderingSubsystem(() => SkiaPlatform.Initialize(
                AvaloniaLocator.Current.GetService<SkiaOptions>() ?? new SkiaOptions()),
                "Skia");
        }
    }
}
