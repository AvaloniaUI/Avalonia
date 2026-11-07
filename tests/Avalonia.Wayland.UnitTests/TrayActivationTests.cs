using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.FreeDesktop;
using Avalonia.FreeDesktop.DBus;
using Avalonia.Logging;
using Avalonia.UnitTests;
using Tmds.DBus.Protocol;
using Xunit;

namespace Avalonia.Wayland.UnitTests;

public class TrayActivationTests : ScopedTestBase
{
    [Fact]
    public async Task SNI_forwards_including_empty_delivery_before_the_click_callback()
    {
        using var connection = new DBusConnection(new DBusConnectionOptions("unix:path=/unused") { AutoConnect = false });
        var observed = new List<string>();
        var item = new StatusNotifierItemDbusObj(connection, "/Menu", observed.Add);
        item.ActivationDelegate += () => observed.Add("click");
        var handler = (IStatusNotifierItemHandler)item;
        await handler.ProvideXdgActivationTokenAsync("token");
        await handler.ActivateAsync(0, 0);
        await handler.ProvideXdgActivationTokenAsync("");
        Assert.Equal(new[] { "token", "click", "" }, observed);

        // X11 and hosts without a backend hook keep their original click behavior.
        var noHook = new StatusNotifierItemDbusObj(connection, "/OtherMenu");
        noHook.ActivationDelegate += () => observed.Add("no hook");
        await ((IStatusNotifierItemHandler)noHook).ProvideXdgActivationTokenAsync("ignored");
        await ((IStatusNotifierItemHandler)noHook).ActivateAsync(0, 0);
        Assert.Equal("no hook", observed[^1]);
    }

    [Fact]
    public async Task Callback_failure_is_logged_and_the_original_exception_is_preserved()
    {
        using var connection = new DBusConnection(new DBusConnectionOptions("unix:path=/unused") { AutoConnect = false });
        var error = new InvalidOperationException("callback failure");
        var item = new StatusNotifierItemDbusObj(connection, "/Menu");
        item.ActivationDelegate += () => throw error;
        var logged = false;
        using var sink = TestLogSink.Start((level, area, _, _, values) =>
        {
            if (area == LogArea.FreeDesktopPlatform && level == LogEventLevel.Warning)
                logged = Array.Exists(values, value => ReferenceEquals(value, error));
        });
        Assert.Same(error, await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await ((IStatusNotifierItemHandler)item).ActivateAsync(0, 0)));
        Assert.True(logged);
    }
}
