// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Controls;
using Avalonia.Native;

namespace Avalonia
{
    public static class AvaloniaNativePlatformExtensions
    {
        public static T UseAvaloniaNative<T>(this T builder)
            where T : AppBuilderBase<T>, new()
        {
            builder.UseWindowingSubsystem(() =>
                AvaloniaNativePlatform.Initialize(
                    AvaloniaLocator.Current.GetService<AvaloniaNativePlatformOptions>() ??
                    new AvaloniaNativePlatformOptions()));
            return builder;
        }
    }

    public class AvaloniaNativePlatformOptions
    {
        public bool UseDeferredRendering { get; set; } = true;
        public bool UseGpu { get; set; } = true;
        public string AvaloniaNativeLibraryPath { get; set; }
    }

    // ReSharper disable once InconsistentNaming
    public class MacOSPlatformOptions
    {
        public bool ShowInDock { get; set; } = true;
    }
}
