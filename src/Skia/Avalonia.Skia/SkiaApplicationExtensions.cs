// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
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
        /// <typeparam name="T">Builder type.</typeparam>
        /// <param name="builder">Builder.</param>
        /// <returns>Configure builder.</returns>
        public static T UseSkia<T>(this T builder, Func<ICustomSkiaGpu> gpuFactory = null) where T : AppBuilderBase<T>, new()
        {
            var customGpu = gpuFactory?.Invoke();

            builder.UseRenderingSubsystem(() => SkiaPlatform.Initialize(customGpu), "Skia");
            return builder;
        }
    }
}
