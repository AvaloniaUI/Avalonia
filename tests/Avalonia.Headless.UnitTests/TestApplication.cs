#nullable enable

using System;
using Avalonia.Themes.Simple;

namespace Avalonia.Headless.UnitTests;

public class TestApplication : Application
{
    public TestApplication()
    {
        Styles.Add(new SimpleTheme());
    }

    /// <summary>
    /// Enabled by the PerTest projects only, so that both mouse device modes are covered.
    /// </summary>
    public static bool UsesSharedMouseDevice =>
#if PERTEST
        true;
#else
        false;
#endif

    private static readonly AvaloniaHeadlessPlatformOptions s_options = new()
    {
        UseHeadlessDrawing = false,
        OverlayPopups = false,
        UseSharedMouseDevice = UsesSharedMouseDevice
    };

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<TestApplication>()
        .UseHarfBuzz()
        .UseSkia()
        .UseHeadless(s_options);

    /// <summary>
    /// Headless options that are re-read on every use rather than captured during platform
    /// initialization, so a test can change them for its own duration via <see cref="Reconfigure"/>.
    /// Null leaves the current value untouched.
    /// </summary>
    public sealed record HeadlessRuntimeOptions
    {
        public bool? SupportsWindowPositioning { get; init; }
        public bool? UseLogicalDesktopCoordinates { get; init; }
    }

    /// <summary>
    /// Applies the non-null options for the lifetime of the returned scope, then restores the previous values.
    /// </summary>
    public static IDisposable Reconfigure(HeadlessRuntimeOptions options)
    {
        var previous = new HeadlessRuntimeOptions
        {
            SupportsWindowPositioning = s_options.SupportsWindowPositioning,
            UseLogicalDesktopCoordinates = s_options.UseLogicalDesktopCoordinates
        };
        Apply(options);
        return new RestoreScope(previous);
    }

    private static void Apply(HeadlessRuntimeOptions options)
    {
        if (options.SupportsWindowPositioning is { } supportsWindowPositioning)
            s_options.SupportsWindowPositioning = supportsWindowPositioning;
        if (options.UseLogicalDesktopCoordinates is { } useLogicalDesktopCoordinates)
            s_options.UseLogicalDesktopCoordinates = useLogicalDesktopCoordinates;
    }

    private sealed class RestoreScope(HeadlessRuntimeOptions previous) : IDisposable
    {
        private HeadlessRuntimeOptions? _previous = previous;

        public void Dispose()
        {
            if (_previous is { } p)
            {
                _previous = null;
                Apply(p);
            }
        }
    }
}
