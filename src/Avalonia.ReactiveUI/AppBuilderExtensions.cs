// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Controls;
using Avalonia.Threading;
using ReactiveUI;
using System;
using System.Reactive.Concurrency;
using System.Threading;

namespace Avalonia
{
    public static class AppBuilderExtensions
    {
        public static TAppBuilder UseReactiveUI<TAppBuilder>(this TAppBuilder builder)
            where TAppBuilder : AppBuilderBase<TAppBuilder>, new()
        {
            return builder.AfterSetup(_ =>
            {
                RxApp.MainThreadScheduler = AvaloniaScheduler.Instance;
            });
        }
    }
}
