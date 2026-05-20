using System;
using global::Avalonia.Logging;

namespace Avalonia.WinUI;

/// <summary>
/// Thin façade over <see cref="Logger"/> that pins
/// <see cref="LogArea.WinUIPlatform"/> so call sites stay short.
/// Lookups go through <see cref="Logger.TryGet(LogEventLevel, string)"/>
/// — the underlying sink decides which levels are forwarded.
/// </summary>
internal static class WinUILog
{
    public static void Warn(object? source, string message)
        => Logger.TryGet(LogEventLevel.Warning, LogArea.WinUIPlatform)?.Log(source, message);

    public static void Warn(object? source, string message, Exception ex)
        => Logger.TryGet(LogEventLevel.Warning, LogArea.WinUIPlatform)?.Log(source, "{Message}: {Exception}", message, ex);

    public static void Info(object? source, string message)
        => Logger.TryGet(LogEventLevel.Information, LogArea.WinUIPlatform)?.Log(source, message);

    public static void Verbose(object? source, string message)
        => Logger.TryGet(LogEventLevel.Verbose, LogArea.WinUIPlatform)?.Log(source, message);
}
