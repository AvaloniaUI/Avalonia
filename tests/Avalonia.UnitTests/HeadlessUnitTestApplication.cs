using System;
using System.Reactive.Disposables;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Themes.Simple;
using Avalonia.Threading;

namespace Avalonia.UnitTests;

/// <summary>
/// Test application running on the real headless platform, set up through <see cref="AppBuilder"/>
/// so that the platform owns its own slice of the locator.
/// </summary>
/// <remarks>
/// This is deliberately not a <see cref="TestServices"/> flavour: the headless platform registers
/// services of its own (keyboard device, clipboard, render loop, platform settings, hotkey config),
/// and <see cref="UnitTestApplication"/> binds the same keys from its service bag, so the two
/// overwrite each other depending on when the platform happens to initialize.
/// </remarks>
public class HeadlessUnitTestApplication : Application
{
    public HeadlessUnitTestApplication()
    {
        Styles.Add(new SimpleTheme());
    }

    public static IDisposable Start(AvaloniaHeadlessPlatformOptions? options = null)
    {
        var scope = AvaloniaLocator.EnterScope();
        var oldContext = SynchronizationContext.Current;

        try
        {
            Dispatcher.ResetBeforeUnitTests();

            AppBuilder.Configure<HeadlessUnitTestApplication>()
                // Popups default to dedicated top-levels here, matching the desktop platforms
                // and the app used by Avalonia.Headless.UnitTests.
                .UseHeadless(options ?? new AvaloniaHeadlessPlatformOptions { OverlayPopups = false })
                .AfterPlatformServicesSetup(_ => AvaloniaLocator.CurrentMutable
                    .Bind<IFontManagerImpl>().ToConstant(new TestFontManager()))
                .SetupUnsafe();
        }
        catch
        {
            scope.Dispose();
            throw;
        }

        return Disposable.Create(() =>
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                Dispatcher.UIThread.RunJobs();
            }

            (AvaloniaLocator.Current.GetService<IToolTipService>() as ToolTipService)?.Dispose();
            (AvaloniaLocator.Current.GetService<FontManager>() as IDisposable)?.Dispose();
            (AvaloniaLocator.Current.GetService<IInputManager>() as IDisposable)?.Dispose();

            Dispatcher.ResetForUnitTests();
            scope.Dispose();
            Dispatcher.ResetBeforeUnitTests();
            SynchronizationContext.SetSynchronizationContext(oldContext);
        });
    }
}
