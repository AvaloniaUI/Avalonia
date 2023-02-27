﻿using System;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace Avalonia.Browser.Interop;

internal static partial class AvaloniaModule
{
    private static readonly Lazy<Task> s_importMain = new(() =>
    {
        var options = AvaloniaLocator.Current.GetService<BrowserPlatformOptions>() ?? new BrowserPlatformOptions();
        return JSHost.ImportAsync(MainModuleName, options.FrameworkAssetPathResolver!("avalonia.js"));
    });

    private static readonly Lazy<Task> s_importStorage = new(() =>
    {
        var options = AvaloniaLocator.Current.GetService<BrowserPlatformOptions>() ?? new BrowserPlatformOptions();
        return JSHost.ImportAsync(StorageModuleName, options.FrameworkAssetPathResolver!("storage.js"));
    });

    public const string MainModuleName = "avalonia";
    public const string StorageModuleName = "storage";

    public static Task ImportMain() => s_importMain.Value;

    public static Task ImportStorage() => s_importStorage.Value;

    [JSImport("Caniuse.isMobile", AvaloniaModule.MainModuleName)]
    public static partial bool IsMobile();
}
