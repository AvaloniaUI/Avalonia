// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Platform;
using SkiaSharp;

namespace Avalonia.Skia.Helpers
{
    /// <summary>
    /// Helps with resolving pixel formats to Skia color types.
    /// </summary>
    public static class PixelFormatHelper
    {
        /// <summary>
        /// Resolve given format to Skia color type.
        /// </summary>
        /// <param name="format">Format to resolve.</param>
        /// <returns>Resolved color type.</returns>
        public static SKColorType ResolveColorType(PixelFormat? format)
        {
            var colorType = format?.ToSkColorType() ?? SKImageInfo.PlatformColorType;

            // TODO: This looks like some leftover hack
            var runtimePlatform = AvaloniaLocator.Current?.GetService<IRuntimePlatform>();
            var runtime = runtimePlatform?.GetRuntimeInfo();

            if (runtime?.IsDesktop == true && runtime.Value.OperatingSystem == OperatingSystemType.Linux)
            {
                colorType = SKColorType.Bgra8888;
            }

            return colorType;
        }
    }
}
