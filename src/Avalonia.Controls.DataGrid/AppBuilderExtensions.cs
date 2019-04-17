// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Controls;
using Avalonia.Threading;

namespace Avalonia
{
    public static class AppBuilderExtensions
    {
        public static TAppBuilder UseDataGrid<TAppBuilder>(this TAppBuilder builder)
            where TAppBuilder : AppBuilderBase<TAppBuilder>, new()
        {
            // Portable.Xaml doesn't correctly load referenced assemblies and so doesn't
            // find `DataGrid` when loading XAML. Call this method from AppBuilder as a
            // temporary workaround until we fix XAML.
            return builder;
        }
    }
}
