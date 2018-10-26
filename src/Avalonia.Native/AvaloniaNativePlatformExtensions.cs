// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Controls;
using Avalonia.Native;

namespace Avalonia
{
    public static class AvaloniaNativePlatformExtensions
    {
        public static T UseAvaloniaNative<T>(this T builder,
                                             string libraryPath = null,
                                             Action<AvaloniaNativeOptions> configure = null)
                                             where T : AppBuilderBase<T>, new()
        {
            if (libraryPath == null)
            {
                builder.UseWindowingSubsystem(() => AvaloniaNativePlatform.Initialize(configure));
            }
            else
            {
                builder.UseWindowingSubsystem(() => AvaloniaNativePlatform.Initialize(libraryPath, configure));
            }

            return builder;
        }
    }
}
