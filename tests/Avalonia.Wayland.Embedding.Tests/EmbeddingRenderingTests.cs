using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Wayland.Embedding;
using Avalonia.Wayland.Embedding.Hosting;
using NWayland.Protocols.Wayland;
using NWayland.Protocols.XdgShell;

namespace Avalonia.Wayland.Embedding.Tests;

/// <summary>
/// End-to-end rendering tests: an embedded-toolkit stand-in (NWayland client) drives surfaces over the
/// in-process subcompositor, and we assert what reaches the Avalonia UI (auto-host window, surface bitmap,
/// frame-callback throttle, unmap).
/// </summary>
public class EmbeddingRenderingTests
{
    private const uint Red = 0xFFFF0000;   // BGRA/ARGB little-endian: B=0,G=0,R=255,A=255
    private const uint Blue = 0xFF0000FF;  // B=255,G=0,R=0,A=255
    private const string LinuxOnly = "Wayland embedding tests require Linux + libwayland-client";

    [AvaloniaFact]
    public void Mapping_a_toplevel_creates_an_auto_host_window_and_delivers_the_surface_bitmap()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), LinuxOnly);
        var title = nameof(Mapping_a_toplevel_creates_an_auto_host_window_and_delivers_the_surface_bitmap);

        using var client = WaylandTestClient.Connect();
        var top = client.MapToplevel(title, 120, 80, Red);
        WaylandTestHarness.RoundtripAndPump(client);

        Assert.True(top.Configured, "client never received an xdg_surface configure");

        var window = WaylandHosting.AutoWindows.Single(w => w.Title == title);
        var view = Assert.IsType<WaylandSubcompositorControlHost>(window.Content);
        var bitmap = Assert.Single(view.Bitmaps.Values);
        Assert.Equal(new PixelSize(120, 80), bitmap.PixelSize);
    }

    [AvaloniaFact]
    public unsafe void The_delivered_bitmap_carries_the_clients_pixels()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), LinuxOnly);
        var title = nameof(The_delivered_bitmap_carries_the_clients_pixels);

        using var client = WaylandTestClient.Connect();
        client.MapToplevel(title, 64, 48, Red, WlShm.FormatEnum.Xrgb8888);
        WaylandTestHarness.RoundtripAndPump(client);

        var window = WaylandHosting.AutoWindows.Single(w => w.Title == title);
        var frame = window.CaptureRenderedFrame();
        Assert.NotNull(frame);

        using var fb = frame!.Lock();
        var x = frame.PixelSize.Width / 2;
        var y = frame.PixelSize.Height / 2;
        var row = (byte*)fb.Address + (long)y * fb.RowBytes;
        // Channel order is RGBA or BGRA depending on the headless capture; in both, green is byte 1 and
        // alpha is byte 3, and red lands on either byte 0 or byte 2 — so test order-agnostically.
        byte c0 = row[x * 4 + 0], green = row[x * 4 + 1], c2 = row[x * 4 + 2], alpha = row[x * 4 + 3];
        Assert.True(green < 60 && alpha > 200 && Math.Max(c0, c2) > 200 && Math.Min(c0, c2) < 60,
            $"center pixel is not the client's red: bytes [{c0},{green},{c2},{alpha}]");
    }

    [AvaloniaFact]
    public void Frame_callbacks_are_throttled_until_the_ui_renders()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), LinuxOnly);
        var title = nameof(Frame_callbacks_are_throttled_until_the_ui_renders);

        using var client = WaylandTestClient.Connect();
        var top = client.MapToplevel(title, 32, 32, Red, requestFrameCallback: true);

        // The compositor has the buffer commit (and its frame callback) but the UI hasn't rendered it yet:
        // the callback must stay deferred.
        client.Roundtrip();
        Assert.Equal(0, top.FrameDoneCount);

        // Once the surface view renders, the compositor releases the throttled callback.
        WaylandTestHarness.Pump();
        client.Roundtrip();
        Assert.Equal(1, top.FrameDoneCount);
    }

    [AvaloniaFact]
    public void Disconnecting_the_client_closes_the_auto_host_window()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), LinuxOnly);
        var title = nameof(Disconnecting_the_client_closes_the_auto_host_window);

        var client = WaylandTestClient.Connect();
        client.MapToplevel(title, 40, 40, Red);
        WaylandTestHarness.RoundtripAndPump(client);
        Assert.Contains(WaylandHosting.AutoWindows, w => w.Title == title);

        client.Dispose();
        WaylandTestHarness.SettleServerAndPump();
        Assert.DoesNotContain(WaylandHosting.AutoWindows, w => w.Title == title);
    }

    [AvaloniaFact]
    public void A_buffer_renders_even_when_its_pool_is_destroyed_before_commit()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), LinuxOnly);
        var title = nameof(A_buffer_renders_even_when_its_pool_is_destroyed_before_commit);

        using var client = WaylandTestClient.Connect();
        // Destroy the wl_shm_pool right after creating the buffer, before committing — legal Wayland, and the
        // buffer must keep the shared memory mapped on its own (review point #2 / per-buffer mmap).
        client.MapToplevel(title, 100, 60, Red, WlShm.FormatEnum.Xrgb8888, destroyPoolEarly: true);
        WaylandTestHarness.RoundtripAndPump(client);

        var window = WaylandHosting.AutoWindows.Single(w => w.Title == title);
        var view = Assert.IsType<WaylandSubcompositorControlHost>(window.Content);
        var bitmap = Assert.Single(view.Bitmaps.Values);
        Assert.Equal(new PixelSize(100, 60), bitmap.PixelSize);
    }

    [AvaloniaFact]
    public void Pool_creation_seals_the_fd_against_shrinking()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), LinuxOnly);

        using var client = WaylandTestClient.Connect();
        var shm = client.CreatePool(64 * 1024);

        // The compositor adds F_SEAL_SHRINK on pool creation, so the client can no longer shrink the shared
        // fd (a buggy client shrinking it would otherwise SIGBUS the compositor's reads).
        Assert.Equal(-1, LibC.Ftruncate(shm.Fd, 4096));
    }

    [AvaloniaFact] // review point #1
    public void A_sync_subsurfaces_buffer_only_appears_when_the_parent_commits()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), LinuxOnly);
        var title = nameof(A_sync_subsurfaces_buffer_only_appears_when_the_parent_commits);

        using var client = WaylandTestClient.Connect();
        var top = client.MapToplevel(title, 120, 80, Red);
        WaylandTestHarness.RoundtripAndPump(client);

        var window = WaylandHosting.AutoWindows.Single(w => w.Title == title);
        var view = Assert.IsType<WaylandSubcompositorControlHost>(window.Content);

        // Commit the sync subsurface on its own: per wl_subsurface semantics its buffer is cached, not
        // current, so the view must still show only the parent's bitmap.
        client.AddSyncSubsurface(top, 10, 10, 40, 30, Blue);
        WaylandTestHarness.RoundtripAndPump(client);
        Assert.Single(view.Bitmaps.Values);

        // The parent's commit applies the cached subsurface state atomically — now both surfaces are present.
        client.CommitParent(top);
        WaylandTestHarness.RoundtripAndPump(client);
        Assert.Equal(2, view.Bitmaps.Count);
    }

    [AvaloniaFact] // review point #3
    public void A_frame_callback_committed_before_the_first_buffer_is_not_stranded()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), LinuxOnly);
        var title = nameof(A_frame_callback_committed_before_the_first_buffer_is_not_stranded);

        using var client = WaylandTestClient.Connect();
        var top = client.CreateToplevelWithFrameCallbackBeforeBuffer(title);
        client.Roundtrip();

        // The surface never mapped (no buffer), but the callback must still fire rather than wait forever.
        Assert.True(top.FrameDoneCount >= 1, "frame callback committed before the first buffer was stranded");
    }

    [AvaloniaFact] // review point #4
    public void A_frame_callback_with_no_new_content_fires_through_the_render_not_immediately()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), LinuxOnly);
        var title = nameof(A_frame_callback_with_no_new_content_fires_through_the_render_not_immediately);

        using var client = WaylandTestClient.Connect();
        var top = client.MapToplevel(title, 32, 32, Red);
        WaylandTestHarness.RoundtripAndPump(client);
        client.Roundtrip();
        var afterFirstFrame = top.FrameDoneCount; // the map's frame callback has already fired

        // Request a frame callback with no new buffer. It must be throttled to the next render, not fired
        // immediately at network speed.
        client.RequestFrameCallbackOnly(top);
        client.Roundtrip();
        Assert.Equal(afterFirstFrame, top.FrameDoneCount); // no render since the request → not fired yet

        WaylandTestHarness.Pump();
        client.Roundtrip();
        Assert.Equal(afterFirstFrame + 1, top.FrameDoneCount); // fired once the UI rendered
    }

    [Fact] // review point #5
    public void AppendFrameIds_de_duplicates_surface_ids()
    {
        var result = WaylandSubcompositorControlHost.AppendFrameIds(new uint[] { 1, 2 }, new uint[] { 2, 3, 1, 4 });
        Assert.Equal(new uint[] { 1, 2, 3, 4 }, result);
    }

    // Functional smoke test for the protocol-trace path (point #6 made s_protocolTrace volatile). NOTE: this
    // exercises the cross-thread subscribe→invoke→read path but does NOT prove the memory barrier — it would
    // pass without `volatile` too; the barrier is correct-by-inspection.
    [AvaloniaFact]
    public void Protocol_trace_emits_messages_for_client_activity()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), LinuxOnly);

        var count = 0;
        void Handler(string _) => Interlocked.Increment(ref count);
        WaylandEmbeddingSubcompositor.ProtocolTrace += Handler;
        try
        {
            using var client = WaylandTestClient.Connect();
            client.MapToplevel(nameof(Protocol_trace_emits_messages_for_client_activity), 16, 16, Red);
            WaylandTestHarness.RoundtripAndPump(client);
        }
        finally
        {
            WaylandEmbeddingSubcompositor.ProtocolTrace -= Handler;
        }

        Assert.True(Volatile.Read(ref count) > 0, "expected protocol trace lines for client activity");
    }

    [AvaloniaFact] // review point #7
    public async Task WaylandHosting_public_statics_require_the_ui_thread()
    {
        // The registry is UI-thread-affine; calling it from a background thread must fail fast rather than
        // race the non-concurrent dictionaries.
        await Task.Run(() =>
        {
            Assert.Throws<InvalidOperationException>(() => WaylandHosting.GetHost(0));
            Assert.Throws<InvalidOperationException>(() => WaylandHosting.RegisterHost(0, null!));
        });
    }

    [AvaloniaFact] // review follow-up: per-buffer page-alignment math with a non-zero offset
    public unsafe void A_buffer_at_a_page_unaligned_pool_offset_renders_correct_pixels()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), LinuxOnly);
        var title = nameof(A_buffer_at_a_page_unaligned_pool_offset_renders_correct_pixels);

        using var client = WaylandTestClient.Connect();
        // 8192 is page-aligned on common page sizes; +256 forces a non-zero in-page delta in the compositor's
        // mmap (it must map from the aligned base and index by the delta).
        client.MapToplevelAtPoolOffset(title, 48, 32, offset: 8192 + 256, fillPixel: Red);
        WaylandTestHarness.RoundtripAndPump(client);

        var window = WaylandHosting.AutoWindows.Single(w => w.Title == title);
        var view = Assert.IsType<WaylandSubcompositorControlHost>(window.Content);
        var bitmap = Assert.Single(view.Bitmaps.Values);
        Assert.Equal(new PixelSize(48, 32), bitmap.PixelSize);

        var frame = window.CaptureRenderedFrame();
        Assert.NotNull(frame);
        using var fb = frame!.Lock();
        var x = frame.PixelSize.Width / 2;
        var y = frame.PixelSize.Height / 2;
        var row = (byte*)fb.Address + (long)y * fb.RowBytes;
        byte c0 = row[x * 4 + 0], green = row[x * 4 + 1], c2 = row[x * 4 + 2], alpha = row[x * 4 + 3];
        Assert.True(green < 60 && alpha > 200 && Math.Max(c0, c2) > 200 && Math.Min(c0, c2) < 60,
            $"page-unaligned-offset buffer did not render the client's red: bytes [{c0},{green},{c2},{alpha}]");
    }

    [AvaloniaFact] // review follow-up: out-of-bounds create_buffer bails (fatal protocol error)
    public void An_out_of_bounds_buffer_request_disconnects_the_client()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), LinuxOnly);

        using var client = WaylandTestClient.Connect();
        // The compositor can't map a ~4 MB region from a 4 KB pool, so it posts wl_display.no_memory and
        // drops the connection; the client roundtrip then reports the error (< 0).
        Assert.True(client.CreateOversizedBufferAndRoundtrip() < 0,
            "out-of-bounds create_buffer should have errored the connection");
    }

    [AvaloniaFact] // P1: min/max size capture + plumbing
    public void Toplevel_min_and_max_sizes_flow_to_the_host_and_auto_window()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), LinuxOnly);
        var title = nameof(Toplevel_min_and_max_sizes_flow_to_the_host_and_auto_window);

        using var client = WaylandTestClient.Connect();
        client.MapToplevel(title, 120, 80, Red, minWidth: 50, minHeight: 40, maxWidth: 300, maxHeight: 200);
        WaylandTestHarness.RoundtripAndPump(client);

        var window = WaylandHosting.AutoWindows.Single(w => w.Title == title);
        var host = Assert.IsType<WaylandSubcompositorControlHost>(window.Content);

        Assert.Equal(new Size(50, 40), host.EmbeddedMinSize);
        Assert.Equal(new Size(300, 200), host.EmbeddedMaxSize);
        Assert.Equal(50d, window.MinWidth);
        Assert.Equal(40d, window.MinHeight);
        Assert.Equal(300d, window.MaxWidth);
        Assert.Equal(200d, window.MaxHeight);
    }

    [AvaloniaFact] // P1: HiDPI — buffer_scale maps physical pixels to fewer DIPs (crisp content)
    public void A_scaled_buffer_renders_at_reduced_dip_size()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), LinuxOnly);
        var title = nameof(A_scaled_buffer_renders_at_reduced_dip_size);

        using var client = WaylandTestClient.Connect();
        // 200×200 physical pixels at buffer_scale 2 ⇒ a 100×100 DIP surface.
        client.MapToplevel(title, 200, 200, Red, WlShm.FormatEnum.Xrgb8888, bufferScale: 2);
        WaylandTestHarness.RoundtripAndPump(client);

        var window = WaylandHosting.AutoWindows.Single(w => w.Title == title);
        var host = Assert.IsType<WaylandSubcompositorControlHost>(window.Content);
        var bitmap = Assert.Single(host.Bitmaps.Values);

        Assert.Equal(new PixelSize(200, 200), bitmap.PixelSize); // raw client pixels preserved
        Assert.Equal(new Size(100, 100), bitmap.Size);           // laid out at half the DIPs (tagged DPI 192)
    }

    [AvaloniaFact] // P1: HiDPI — compositor advertises preferred_buffer_scale from the host's RenderScaling
    public void Compositor_sends_preferred_buffer_scale_for_a_mapped_toplevel()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), LinuxOnly);
        var title = nameof(Compositor_sends_preferred_buffer_scale_for_a_mapped_toplevel);

        using var client = WaylandTestClient.Connect();
        var top = client.MapToplevel(title, 64, 48, Red);
        WaylandTestHarness.RoundtripAndPump(client); // host attaches → reports RenderScaling → compositor emits
        client.Roundtrip();                          // receive the preferred_buffer_scale event

        Assert.True(top.PreferredScaleReceived, "client never received preferred_buffer_scale");
        Assert.True(top.PreferredBufferScale >= 1);  // equals the headless host's RenderScaling (1 here)
    }

    [AvaloniaFact] // P1: scenario-2 close proxying (Avalonia window close → xdg_toplevel.close)
    public void Closing_the_auto_window_sends_xdg_toplevel_close_to_the_client()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), LinuxOnly);
        var title = nameof(Closing_the_auto_window_sends_xdg_toplevel_close_to_the_client);

        using var client = WaylandTestClient.Connect();
        var top = client.MapToplevel(title, 80, 60, Red);
        WaylandTestHarness.RoundtripAndPump(client);
        Assert.False(top.Closed);

        // A user close only ASKS the client (the close is cancelled); the window stays open until the client acts.
        WaylandHosting.AutoWindows.Single(w => w.Title == title).Close();
        client.Roundtrip();
        Assert.True(top.Closed, "client did not receive xdg_toplevel.close");
        Assert.Contains(WaylandHosting.AutoWindows, w => w.Title == title);

        // The client complies by destroying its toplevel → the window really closes now.
        client.DestroyToplevel(top);
        WaylandTestHarness.SettleServerAndPump();
        Assert.DoesNotContain(WaylandHosting.AutoWindows, w => w.Title == title);
    }

    [AvaloniaFact] // P1: scenario 1 — token embedding into a pre-created Avalonia control
    public void A_toplevel_embedded_via_token_renders_into_the_control_not_an_auto_window()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), LinuxOnly);
        var title = nameof(A_toplevel_embedded_via_token_renders_into_the_control_not_an_auto_window);

        // App side: place the host control in a window and mint its embedding token.
        var host = new WaylandSubcompositorControlHost();
        var appWindow = new Window { Width = 220, Height = 160, Content = host, Title = "app:" + title };
        appWindow.Show();
        WaylandTestHarness.Pump();
        var token = host.GetEmbeddingToken();
        Assert.NotEqual(0u, host.HostId); // control bound to a host id by the round-trip

        // Toolkit side: embed a toplevel with that token, then render a frame.
        using var client = WaylandTestClient.Connect();
        var top = client.EmbedToplevel(title, 100, 60, Red, token);
        WaylandTestHarness.RoundtripAndPump(client);

        Assert.True(top.EmbedBound, "embed_toplevel was rejected");
        Assert.False(top.EmbedRejected);
        // An embedded toplevel renders into the existing control — no auto-window is manufactured.
        Assert.DoesNotContain(WaylandHosting.AutoWindows, w => w.Title == title);
        Assert.True(host.IsEmbeddedSurfaceMapped);
        var bitmap = Assert.Single(host.Bitmaps.Values);
        Assert.Equal(new PixelSize(100, 60), bitmap.PixelSize);

        appWindow.Close();
    }

    [AvaloniaFact] // P1: resize proxy — host control size change → xdg_toplevel.configure
    public void Resizing_the_host_reconfigures_the_embedded_toplevel()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), LinuxOnly);
        var title = nameof(Resizing_the_host_reconfigures_the_embedded_toplevel);

        var host = new WaylandSubcompositorControlHost();
        var appWindow = new Window { Width = 200, Height = 150, Content = host, Title = "app:" + title };
        appWindow.Show();
        WaylandTestHarness.Pump();
        var token = host.GetEmbeddingToken();

        using var client = WaylandTestClient.Connect();
        var top = client.EmbedToplevel(title, 100, 60, Red, token);
        WaylandTestHarness.RoundtripAndPump(client);
        client.Roundtrip();
        Assert.True(top.EmbedBound);

        // Grow the host window; the control re-arranges and the client must be reconfigured to roughly the
        // new size (allow a little slack for any window chrome).
        var configuresBefore = top.ConfigureCount;
        appWindow.Width = 320;
        appWindow.Height = 240;
        WaylandTestHarness.Pump();
        client.Roundtrip();

        Assert.True(top.ConfigureCount > configuresBefore, "resizing the host did not reconfigure the client");
        Assert.InRange(top.LastConfigureWidth, 290, 320);
        Assert.InRange(top.LastConfigureHeight, 210, 240);

        appWindow.Close();
    }

    [AvaloniaFact] // resize flush: a registered client-frame pump re-renders the embedded client at the new size, captured in-line
    public void Resizing_a_host_pulls_the_clients_new_size_frame_during_the_same_avalonia_frame()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), LinuxOnly);
        var title = nameof(Resizing_a_host_pulls_the_clients_new_size_frame_during_the_same_avalonia_frame);

        var host = new WaylandSubcompositorControlHost();
        var appWindow = new Window { Width = 200, Height = 150, Content = host, Title = "app:" + title };
        appWindow.Show();
        WaylandTestHarness.Pump();
        var token = host.GetEmbeddingToken();

        using var client = WaylandTestClient.Connect();
        var top = client.EmbedToplevel(title, 100, 60, Red, token);
        WaylandTestHarness.RoundtripAndPump(client);
        client.Roundtrip();
        Assert.True(top.EmbedBound);
        WaylandTestHarness.Pump();
        Assert.Equal(new PixelSize(100, 60), Assert.Single(host.Bitmaps.Values).PixelSize); // mapped at the initial size
        Assert.NotEqual(0u, host.SurfaceObjectId); // the surface's wayland id was captured at map (the targeting key)
        // a surface must not report resized outside a flush
        Assert.Null(WaylandEmbeddingSubcompositor.ResizedSurfaceSize(host.ConnectionTicket, host.SurfaceObjectId));

        // A client-frame pump that stands in for the GTK glue: it asks (by connection ticket + surface object id, the
        // same key the glue computes via wl_proxy_get_id) for the new LOGICAL SIZE and re-renders at exactly that
        // in-line, so the correct-size buffer is captured in the SAME Avalonia frame instead of a configure-read behind.
        // The flush invokes this between its two roundtrips.
        var pumpCount = 0;
        (int Width, int Height)? appliedSize = null;
        Action pump = () =>
        {
            pumpCount++;
            var size = WaylandEmbeddingSubcompositor.ResizedSurfaceSize(host.ConnectionTicket, host.SurfaceObjectId);
            if (size is not { } s)
                return; // this surface didn't resize → don't repaint it
            appliedSize = s;
            client.AttachFrame(top, s.Width, s.Height, Blue); // apply the queried size directly, like gtk_window_resize
        };
        WaylandEmbeddingSubcompositor.AddClientFramePumpCallback(pump);
        try
        {
            appWindow.Width = 320;
            appWindow.Height = 240;
            WaylandTestHarness.Pump();

            Assert.True(pumpCount > 0, "the client-frame pump was not invoked during the resize flush");
            Assert.NotNull(appliedSize);
            Assert.InRange(appliedSize!.Value.Width, 290, 320);             // the query carried the new logical size
            var bitmap = Assert.Single(host.Bitmaps.Values);
            Assert.Equal(appliedSize.Value.Width, bitmap.PixelSize.Width);  // captured the NEW-size frame, in-line
            Assert.Equal(appliedSize.Value.Height, bitmap.PixelSize.Height);
        }
        finally
        {
            WaylandEmbeddingSubcompositor.RemoveClientFramePumpCallback(pump);
            appWindow.Close();
        }
    }

    [AvaloniaFact] // F2 guard: embedding an already-mapped toplevel is rejected, not silently bound
    public void Embedding_an_already_mapped_toplevel_is_rejected()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), LinuxOnly);
        var title = nameof(Embedding_an_already_mapped_toplevel_is_rejected);

        var host = new WaylandSubcompositorControlHost();
        var appWindow = new Window { Width = 200, Height = 150, Content = host, Title = "app:" + title };
        appWindow.Show();
        WaylandTestHarness.Pump();
        var token = host.GetEmbeddingToken();

        using var client = WaylandTestClient.Connect();
        // Map first (→ auto-window), THEN try to embed — too late: embed must precede map.
        var top = client.MapToplevel(title, 80, 60, Red);
        WaylandTestHarness.RoundtripAndPump(client);

        client.Embed(top, token);
        client.Roundtrip();

        Assert.True(top.EmbedRejected, "embedding an already-mapped toplevel should be rejected");
        Assert.False(top.EmbedBound);
        appWindow.Close();
    }

    [AvaloniaFact] // F9 guard: a pre-v6 client must NOT receive preferred_buffer_scale (would break GTK3 v4)
    public void A_pre_v6_client_does_not_receive_preferred_buffer_scale()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), LinuxOnly);
        var title = nameof(A_pre_v6_client_does_not_receive_preferred_buffer_scale);

        using var client = WaylandTestClient.Connect(compositorVersion: 4);
        var top = client.MapToplevel(title, 48, 32, Red);
        WaylandTestHarness.RoundtripAndPump(client);
        client.Roundtrip();

        Assert.False(top.PreferredScaleReceived, "a v4 wl_surface must not get the v6 preferred_buffer_scale event");
    }

    [AvaloniaFact] // embedding ignores buffer size in layout: the surface stretches to the host's allocation
    public unsafe void An_embedded_surface_is_stretched_to_fill_the_host_allocation()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), LinuxOnly);
        var title = nameof(An_embedded_surface_is_stretched_to_fill_the_host_allocation);

        var host = new WaylandSubcompositorControlHost();
        var appWindow = new Window { Width = 200, Height = 120, Content = host, Title = "app:" + title };
        appWindow.Show();
        WaylandTestHarness.Pump();
        var token = host.GetEmbeddingToken();

        using var client = WaylandTestClient.Connect();
        // A tiny 20×16 buffer in a 200×120 host: it must FILL the host (stretched), not sit at native size.
        client.EmbedToplevel(title, 20, 16, Red, token, WlShm.FormatEnum.Xrgb8888);
        WaylandTestHarness.RoundtripAndPump(client);

        var frame = appWindow.CaptureRenderedFrame();
        Assert.NotNull(frame);
        using var fb = frame!.Lock();
        // Sample at 80% across — outside the native 20×16 region, so it's red only if the surface was stretched.
        var x = (int)(frame.PixelSize.Width * 0.8);
        var y = (int)(frame.PixelSize.Height * 0.8);
        var rowPtr = (byte*)fb.Address + (long)y * fb.RowBytes;
        byte c0 = rowPtr[x * 4 + 0], green = rowPtr[x * 4 + 1], c2 = rowPtr[x * 4 + 2], alpha = rowPtr[x * 4 + 3];
        Assert.True(green < 60 && alpha > 200 && Math.Max(c0, c2) > 200 && Math.Min(c0, c2) < 60,
            $"embedded surface was not stretched to fill the host: bytes [{c0},{green},{c2},{alpha}]");

        appWindow.Close();
    }

    [AvaloniaFact] // P2: pointer input — motion/enter/button forwarded to the client in surface-local coords
    public void Pointer_motion_and_clicks_reach_the_client_in_surface_coordinates()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), LinuxOnly);
        var title = nameof(Pointer_motion_and_clicks_reach_the_client_in_surface_coordinates);

        using var client = WaylandTestClient.Connect();
        client.MapToplevel(title, 100, 80, Red); // window sized to the buffer ⇒ control coords map 1:1 to surface
        WaylandTestHarness.RoundtripAndPump(client);

        var window = WaylandHosting.AutoWindows.Single(w => w.Title == title);
        window.MouseMove(new Point(30, 20));
        window.MouseDown(new Point(30, 20), MouseButton.Left);
        window.MouseUp(new Point(30, 20), MouseButton.Left);
        WaylandTestHarness.Pump();
        client.Roundtrip();

        Assert.True(client.Pointer.EnterCount > 0, "pointer never entered the surface");
        Assert.True(client.Pointer.MotionCount > 0, "no motion delivered");
        Assert.InRange(client.Pointer.LastX, 29, 31);
        Assert.InRange(client.Pointer.LastY, 19, 21);
        Assert.Equal(2, client.Pointer.ButtonCount);          // press + release
        Assert.Equal(0x110u, client.Pointer.LastButton);      // BTN_LEFT
        Assert.False(client.Pointer.LastButtonPressed);       // last event was the release
    }

    [AvaloniaFact] // P2: keyboard input — keymap, focus enter, and key events reach the client
    public void Keyboard_focus_and_keys_reach_the_client()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), LinuxOnly);
        var title = nameof(Keyboard_focus_and_keys_reach_the_client);

        using var client = WaylandTestClient.Connect();
        client.MapToplevel(title, 100, 80, Red);
        WaylandTestHarness.RoundtripAndPump(client);

        var window = WaylandHosting.AutoWindows.Single(w => w.Title == title);
        var host = Assert.IsType<WaylandSubcompositorControlHost>(window.Content);

        host.Focus();
        WaylandTestHarness.Pump();
        window.KeyPressQwerty(PhysicalKey.A, RawInputModifiers.Shift);
        window.KeyReleaseQwerty(PhysicalKey.A, RawInputModifiers.Shift);
        WaylandTestHarness.Pump();
        client.Roundtrip();

        Assert.True(client.Keyboard.KeymapReceived, "client never received a keymap");
        Assert.True(client.Keyboard.EnterCount > 0, "keyboard focus did not enter the surface");
        Assert.Equal(2, client.Keyboard.KeyCount);    // press + release
        Assert.Equal(30u, client.Keyboard.LastKey);   // evdev KEY_A
        Assert.False(client.Keyboard.LastKeyPressed); // last event was the release
        Assert.Equal(1u, client.Keyboard.LastModifiers); // xkb Shift mask
    }

    [AvaloniaFact] // P2: text-input v3 — commit_string forwarded to the embedded client
    public void Text_input_commit_string_reaches_the_client()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), LinuxOnly);
        var title = nameof(Text_input_commit_string_reaches_the_client);

        using var client = WaylandTestClient.Connect();
        client.AutoEnableTextInput = true; // act like a toolkit with an active IME
        client.MapToplevel(title, 100, 80, Red);
        WaylandTestHarness.RoundtripAndPump(client);

        var window = WaylandHosting.AutoWindows.Single(w => w.Title == title);
        var host = Assert.IsType<WaylandSubcompositorControlHost>(window.Content);

        host.Focus();        // → text-input enter sent to the client
        WaylandTestHarness.Pump();
        client.Roundtrip();  // client receives enter → enable + commit (queued)
        client.Roundtrip();  // server processes the enable before any commit_string is sent

        window.KeyTextInput("hi"); // → host OnTextInput → commit_string + done
        WaylandTestHarness.Pump();
        client.Roundtrip();

        Assert.True(client.TextInput.EnterCount > 0, "text-input never entered the surface");
        Assert.Equal("hi", client.TextInput.LastCommit);
        Assert.True(client.TextInput.DoneCount > 0, "commit_string was not followed by done");
    }

    [AvaloniaFact] // P2: with an active IME, raw text keys are suppressed (no double-insert); control keys still pass
    public void Active_text_input_suppresses_raw_text_keys_but_forwards_control_keys()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), LinuxOnly);
        var title = nameof(Active_text_input_suppresses_raw_text_keys_but_forwards_control_keys);

        using var client = WaylandTestClient.Connect();
        client.AutoEnableTextInput = true;
        client.MapToplevel(title, 100, 80, Red);
        WaylandTestHarness.RoundtripAndPump(client);

        var window = WaylandHosting.AutoWindows.Single(w => w.Title == title);
        var host = Assert.IsType<WaylandSubcompositorControlHost>(window.Content);

        host.Focus();
        WaylandTestHarness.Pump();
        client.Roundtrip();  // client receives text-input enter → enable + commit
        client.Roundtrip();  // server processes the enable (IME now active)

        window.KeyPressQwerty(PhysicalKey.A, RawInputModifiers.None);         // printable text → suppressed (IME delivers it)
        window.KeyPressQwerty(PhysicalKey.ArrowLeft, RawInputModifiers.None); // navigation → forwarded
        window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);     // Enter (control char "\r") → forwarded
        window.KeyPressQwerty(PhysicalKey.A, RawInputModifiers.Control);      // Ctrl+A shortcut → forwarded
        WaylandTestHarness.Pump();
        client.Roundtrip();

        // Only the bare printable 'A' is IME-composed (suppressed); navigation, control and shortcut keys pass.
        Assert.Equal(3, client.Keyboard.KeyCount);
        Assert.Contains(105u, client.Keyboard.Keys); // ArrowLeft (KEY_LEFT)
        Assert.Contains(28u, client.Keyboard.Keys);  // Enter (KEY_ENTER) — control keys are NOT swallowed by the IME
        Assert.Contains(30u, client.Keyboard.Keys);  // Ctrl+A (KEY_A) — shortcut forwarded
    }

    [AvaloniaFact] // P3: xdg_toplevel.activated follows the host's WINDOW activation, not per-widget keyboard focus
    public void Window_activation_drives_the_toplevel_activated_state()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), LinuxOnly);
        var title = nameof(Window_activation_drives_the_toplevel_activated_state);

        // A window with the host plus a sibling we can give keyboard focus to (so the host is NOT focused).
        var host = new WaylandSubcompositorControlHost();
        var other = new Button { Content = "other" };
        DockPanel.SetDock(other, Dock.Bottom);
        var panel = new DockPanel();
        panel.Children.Add(other);
        panel.Children.Add(host); // last child fills the remaining area
        var appWindow = new Window { Width = 220, Height = 160, Content = panel, Title = "app:" + title };
        appWindow.Show();
        WaylandTestHarness.Pump(); // the window becomes active
        var token = host.GetEmbeddingToken();

        using var client = WaylandTestClient.Connect();
        var top = client.EmbedToplevel(title, 100, 60, Red, token);
        WaylandTestHarness.RoundtripAndPump(client);
        client.Roundtrip();
        Assert.True(top.EmbedBound);

        // Focus the SIBLING — the host does NOT hold keyboard focus — yet the window is active, so the embedded
        // toplevel must be activated. (The old focus-driven behavior would have cleared it here.)
        other.Focus();
        WaylandTestHarness.RoundtripAndPump(client);
        client.Roundtrip();
        Assert.True(top.LastConfigureActivated,
            "an active window must keep the embedded toplevel activated even when the host isn't focused");
        Assert.True(top.LastConfigureWidth > 0 && top.LastConfigureHeight > 0,
            "activated re-configure collapsed the toplevel size to 0");

        // Deactivating the window clears activated (the toolkit grays itself out).
        var beforeDeactivate = top.ConfigureCount;
        appWindow.Hide();
        WaylandTestHarness.RoundtripAndPump(client);
        client.Roundtrip();
        Assert.True(top.ConfigureCount > beforeDeactivate, "deactivating the window did not send a configure");
        Assert.False(top.LastConfigureActivated, "deactivating the window did not clear the activated state");

        appWindow.Close();
    }

    [AvaloniaFact] // P3: an embedded toplevel in an active auto-window is activated with no keyboard focus involved
    public void Auto_window_activation_is_propagated_to_the_toplevel()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), LinuxOnly);
        var title = nameof(Auto_window_activation_is_propagated_to_the_toplevel);

        using var client = WaylandTestClient.Connect();
        var top = client.MapToplevel(title, 120, 80, Red);
        WaylandTestHarness.RoundtripAndPump(client);
        client.Roundtrip(); // deliver the activation-driven configure

        Assert.True(top.LastConfigureActivated, "an embedded toplevel in an active auto-window should be activated");
    }

    [AvaloniaFact] // P2: wl_keyboard.modifiers is coalesced — re-sent only when the mask changes
    public void Keyboard_modifiers_are_coalesced_across_keys_with_the_same_mask()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), LinuxOnly);
        var title = nameof(Keyboard_modifiers_are_coalesced_across_keys_with_the_same_mask);

        using var client = WaylandTestClient.Connect();
        client.MapToplevel(title, 100, 80, Red);
        WaylandTestHarness.RoundtripAndPump(client);

        var window = WaylandHosting.AutoWindows.Single(w => w.Title == title);
        var host = Assert.IsType<WaylandSubcompositorControlHost>(window.Content);

        host.Focus();
        WaylandTestHarness.Pump();
        client.Roundtrip();
        Assert.Equal(1, client.Keyboard.ModifiersCount); // focus enter establishes the baseline mask once

        window.KeyPressQwerty(PhysicalKey.ArrowLeft, RawInputModifiers.None);  // mask 0 == baseline → no re-send
        window.KeyPressQwerty(PhysicalKey.ArrowRight, RawInputModifiers.None); // mask 0 → no re-send
        window.KeyPressQwerty(PhysicalKey.A, RawInputModifiers.Shift);         // mask changes → one modifiers
        window.KeyPressQwerty(PhysicalKey.B, RawInputModifiers.Shift);         // mask unchanged → no re-send
        WaylandTestHarness.Pump();
        client.Roundtrip();

        Assert.Equal(4, client.Keyboard.KeyCount);
        Assert.Equal(2, client.Keyboard.ModifiersCount); // baseline + the single Shift change only
        Assert.Equal(1u, client.Keyboard.LastModifiers); // last mask sent was Shift
    }

    [AvaloniaFact] // P2: pointer coords are surface-local — offset by the window-geometry origin
    public void Pointer_coordinates_include_the_window_geometry_offset()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), LinuxOnly);
        var title = nameof(Pointer_coordinates_include_the_window_geometry_offset);

        using var client = WaylandTestClient.Connect();
        var top = client.MapToplevel(title, 100, 80, Red); // window sized to the buffer ⇒ coords map 1:1 to content
        WaylandTestHarness.RoundtripAndPump(client);

        // The visible rect sits at a non-zero origin within the surface (as CSD shadow margins would force).
        client.SetWindowGeometry(top, 10, 20, 100, 80);
        WaylandTestHarness.RoundtripAndPump(client);

        var window = WaylandHosting.AutoWindows.Single(w => w.Title == title);
        window.MouseMove(new Point(30, 25));
        WaylandTestHarness.Pump();
        client.Roundtrip();

        Assert.True(client.Pointer.MotionCount > 0, "no motion delivered");
        Assert.InRange(client.Pointer.LastX, 39, 41); // 30 content + 10 geometry-x
        Assert.InRange(client.Pointer.LastY, 44, 46); // 25 content + 20 geometry-y
    }

    [AvaloniaFact] // P2: mouse wheel → wl_pointer.axis delivered to the client
    public void Mouse_wheel_delivers_axis_events_to_the_client()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), LinuxOnly);
        var title = nameof(Mouse_wheel_delivers_axis_events_to_the_client);

        using var client = WaylandTestClient.Connect();
        client.MapToplevel(title, 100, 80, Red);
        WaylandTestHarness.RoundtripAndPump(client);

        var window = WaylandHosting.AutoWindows.Single(w => w.Title == title);
        window.MouseWheel(new Point(40, 30), new Vector(0, -1)); // scroll down one notch
        WaylandTestHarness.Pump();
        client.Roundtrip();

        Assert.True(client.Pointer.AxisCount > 0, "wheel did not deliver wl_pointer.axis");
        Assert.Equal(0, client.Pointer.LastAxis);     // 0 = vertical scroll
        Assert.True(client.Pointer.LastAxisValue > 0, // Avalonia +deltaY (up) → Wayland +value (down)
            $"expected a positive (downward) axis value, got {client.Pointer.LastAxisValue}");
    }

    // ───────────────────────── P3: popups (xdg_popup + xdg_positioner) ─────────────────────────

    [AvaloniaFact] // P3: the popup's positioned configure is computed from the xdg_positioner snapshot
    public void Popup_configure_geometry_is_derived_from_the_positioner()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), LinuxOnly);
        var title = nameof(Popup_configure_geometry_is_derived_from_the_positioner);

        using var client = WaylandTestClient.Connect();
        var top = client.MapToplevel(title, 200, 150, Red);
        WaylandTestHarness.RoundtripAndPump(client);

        // anchor rect (10,20,30,40); anchor BottomLeft ⇒ anchor point (10, 60); gravity BottomRight ⇒ the popup's
        // top-left sits at the anchor point; offset (5,7) ⇒ (15, 67). Size 50×60.
        var popup = client.MapPopup(top, 50, 60, Blue,
            anchorRectX: 10, anchorRectY: 20, anchorRectWidth: 30, anchorRectHeight: 40,
            anchor: XdgPositioner.AnchorEnum.BottomLeft, gravity: XdgPositioner.GravityEnum.BottomRight,
            offsetX: 5, offsetY: 7);
        WaylandTestHarness.RoundtripAndPump(client);

        Assert.True(popup.Configured, "popup never received an xdg_surface configure");
        Assert.True(popup.ConfigureCount > 0, "popup never received an xdg_popup.configure");
        Assert.Equal(15, popup.LastX);
        Assert.Equal(67, popup.LastY);
        Assert.Equal(50, popup.LastWidth);
        Assert.Equal(60, popup.LastHeight);
    }

    [AvaloniaFact] // P3: a mapped popup gets its own host + Avalonia Popup, and its surface bitmap is delivered
    public void Popup_maps_into_its_own_avalonia_popup_and_delivers_its_bitmap()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), LinuxOnly);
        var title = nameof(Popup_maps_into_its_own_avalonia_popup_and_delivers_its_bitmap);

        using var client = WaylandTestClient.Connect();
        var top = client.MapToplevel(title, 200, 150, Red);
        WaylandTestHarness.RoundtripAndPump(client);

        var before = WaylandHosting.Popups.ToList();
        var popup = client.MapPopup(top, 64, 48, Blue);
        WaylandTestHarness.RoundtripAndPump(client);

        var avaloniaPopup = Assert.Single(WaylandHosting.Popups.Where(p => !before.Contains(p)));
        var host = Assert.IsType<WaylandSubcompositorControlHost>(avaloniaPopup.Child);
        var bitmap = Assert.Single(host.Bitmaps.Values);
        Assert.Equal(new PixelSize(64, 48), bitmap.PixelSize);
        Assert.True(popup.BufferReleaseCount > 0, "popup buffer was never released (compositor didn't process it)");
    }

    [AvaloniaFact] // P3: xdg_popup.reposition echoes the token via repositioned, then re-configures
    public void Repositioning_a_popup_echoes_the_token_and_reconfigures()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), LinuxOnly);
        var title = nameof(Repositioning_a_popup_echoes_the_token_and_reconfigures);

        using var client = WaylandTestClient.Connect();
        var top = client.MapToplevel(title, 200, 150, Red);
        WaylandTestHarness.RoundtripAndPump(client);
        var before = WaylandHosting.Popups.ToList();
        var popup = client.MapPopup(top, 50, 60, Blue, anchorRectX: 0, anchorRectY: 0, anchorRectWidth: 20, anchorRectHeight: 20);
        WaylandTestHarness.RoundtripAndPump(client);
        var configuresBeforeReposition = popup.ConfigureCount;
        var host = Assert.IsType<WaylandSubcompositorControlHost>(
            Assert.Single(WaylandHosting.Popups.Where(p => !before.Contains(p))).Child);
        Assert.Equal(50d, host.Width); // sized to the initial positioner

        // Re-place: anchor rect (0,0,20,20), anchor BottomLeft ⇒ (0,20); gravity BottomRight, offset (3,4) ⇒ (3,24).
        client.RepositionPopup(popup, 70, 80, token: 42,
            anchorRectWidth: 20, anchorRectHeight: 20, offsetX: 3, offsetY: 4);
        WaylandTestHarness.RoundtripAndPump(client);

        Assert.Equal(1, popup.RepositionedCount);
        Assert.Equal(42u, popup.LastRepositionToken);
        Assert.True(popup.ConfigureCount > configuresBeforeReposition, "reposition did not re-send a configure");
        Assert.Equal(3, popup.LastX);
        Assert.Equal(24, popup.LastY);
        Assert.Equal(70, popup.LastWidth);
        Assert.Equal(80, popup.LastHeight);
        Assert.Equal(70d, host.Width); // the live Avalonia popup host was re-sized to match (M1)
    }

    [AvaloniaFact] // P3: a grabbing popup is light-dismissable, and dismissing it sends xdg_popup.popup_done
    public void Grabbing_a_popup_enables_light_dismiss_and_dismissal_sends_popup_done()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), LinuxOnly);
        var title = nameof(Grabbing_a_popup_enables_light_dismiss_and_dismissal_sends_popup_done);

        using var client = WaylandTestClient.Connect();
        var top = client.MapToplevel(title, 200, 150, Red);
        WaylandTestHarness.RoundtripAndPump(client);

        var before = WaylandHosting.Popups.ToList();
        var popup = client.MapPopup(top, 64, 48, Blue, grab: true);
        WaylandTestHarness.RoundtripAndPump(client);

        var avaloniaPopup = Assert.Single(WaylandHosting.Popups.Where(p => !before.Contains(p)));
        Assert.True(avaloniaPopup.IsLightDismissEnabled, "grab popup should map to a light-dismiss Avalonia popup");

        // Simulate a light dismiss (click outside) by closing the popup; the host posts popup_done to the client.
        avaloniaPopup.Close();
        WaylandTestHarness.Pump();
        client.Roundtrip();

        Assert.True(popup.PopupDoneCount > 0, "dismissing the popup did not send xdg_popup.popup_done");
    }

    [AvaloniaFact] // P3: the client destroying its popup unmaps it and closes the Avalonia popup
    public void Destroying_a_popup_unmaps_it_and_closes_the_avalonia_popup()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), LinuxOnly);
        var title = nameof(Destroying_a_popup_unmaps_it_and_closes_the_avalonia_popup);

        using var client = WaylandTestClient.Connect();
        var top = client.MapToplevel(title, 200, 150, Red);
        WaylandTestHarness.RoundtripAndPump(client);

        var before = WaylandHosting.Popups.ToList();
        var popup = client.MapPopup(top, 64, 48, Blue);
        WaylandTestHarness.RoundtripAndPump(client);
        Assert.Single(WaylandHosting.Popups.Where(p => !before.Contains(p)));

        popup.Destroy();
        WaylandTestHarness.SettleServerAndPump();

        Assert.Empty(WaylandHosting.Popups.Where(p => !before.Contains(p)));
    }

    [AvaloniaFact] // P3: dismissing a popup with a child sub-popup dismisses the whole stack (newest-first)
    public void Dismissing_a_popup_dismisses_its_child_stack()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), LinuxOnly);
        var title = nameof(Dismissing_a_popup_dismisses_its_child_stack);

        using var client = WaylandTestClient.Connect();
        var top = client.MapToplevel(title, 200, 150, Red);
        WaylandTestHarness.RoundtripAndPump(client);

        var before = WaylandHosting.Popups.ToList();
        var parentPopup = client.MapPopup(top, 80, 60, Blue, grab: true);
        WaylandTestHarness.RoundtripAndPump(client);
        var parentAvaloniaPopup = Assert.Single(WaylandHosting.Popups.Where(p => !before.Contains(p)));

        var childPopup = client.MapPopup(top, 40, 30, Red, grab: true, parentPopup: parentPopup,
            anchorRectWidth: 80, anchorRectHeight: 60);
        WaylandTestHarness.RoundtripAndPump(client);

        // Dismiss the root of the stack; SendPopupDone walks children newest-first, so both get popup_done.
        parentAvaloniaPopup.Close();
        WaylandTestHarness.Pump();
        client.Roundtrip();

        Assert.True(parentPopup.PopupDoneCount > 0, "the parent popup was not dismissed");
        Assert.True(childPopup.PopupDoneCount > 0, "the child popup was not dismissed with its parent");
    }

    [AvaloniaFact] // P3: pointer input is delivered INTO a popup (its own host id resolves on the compositor side)
    public void Pointer_input_reaches_a_popup()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), LinuxOnly);
        var title = nameof(Pointer_input_reaches_a_popup);

        using var client = WaylandTestClient.Connect();
        var top = client.MapToplevel(title, 200, 150, Red);
        WaylandTestHarness.RoundtripAndPump(client);

        var before = WaylandHosting.Popups.ToList();
        var popup = client.MapPopup(top, 64, 48, Blue, grab: true);
        WaylandTestHarness.RoundtripAndPump(client);

        var avaloniaPopup = Assert.Single(WaylandHosting.Popups.Where(p => !before.Contains(p)));
        var host = Assert.IsType<WaylandSubcompositorControlHost>(avaloniaPopup.Child);
        var popupTopLevel = TopLevel.GetTopLevel(host);
        Assert.NotNull(popupTopLevel);

        // A point at the popup host's centre, in the popup root's coordinate space (works whether the headless
        // popup is a separate root or an overlay host).
        var center = host.TranslatePoint(new Point(host.Bounds.Width / 2, host.Bounds.Height / 2), popupTopLevel!)
                     ?? new Point(10, 10);
        popupTopLevel!.MouseMove(center);
        popupTopLevel.MouseDown(center, MouseButton.Left);
        popupTopLevel.MouseUp(center, MouseButton.Left);
        WaylandTestHarness.Pump();
        client.Roundtrip();

        Assert.True(client.Pointer.EnterCount > 0, "pointer never entered the popup surface (popup input not routed)");
        Assert.Equal(2, client.Pointer.ButtonCount);       // press + release reached the popup's client
        Assert.Equal(0x110u, client.Pointer.LastButton);   // BTN_LEFT
    }

    // ───────────────────────── P3: xdg-foreign (export / import / set_parent_of) ─────────────────────────

    [AvaloniaFact] // P3: exporting a toplevel publishes an opaque handle to the client
    public void Exporting_a_toplevel_publishes_a_handle()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), LinuxOnly);
        var title = nameof(Exporting_a_toplevel_publishes_a_handle);

        using var client = WaylandTestClient.Connect();
        var top = client.MapToplevel(title, 120, 90, Red);
        WaylandTestHarness.RoundtripAndPump(client);

        var exported = client.ExportToplevel(top);
        client.Roundtrip();

        Assert.True(exported.HandleReceived, "exporter never sent zxdg_exported_v2.handle");
        Assert.False(string.IsNullOrEmpty(exported.Handle));
    }

    [AvaloniaFact] // P3: importing the handle + set_parent_of owns the child's auto-window to the parent's
    public void Importing_a_handle_and_set_parent_of_owns_the_child_window()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), LinuxOnly);
        var title = nameof(Importing_a_handle_and_set_parent_of_owns_the_child_window);
        var parentTitle = title + "-parent";
        var childTitle = title + "-child";

        using var client = WaylandTestClient.Connect();
        var parent = client.MapToplevel(parentTitle, 200, 150, Red);
        WaylandTestHarness.RoundtripAndPump(client);

        var exported = client.ExportToplevel(parent);
        client.Roundtrip();
        var imported = client.ImportToplevel(exported.Handle!);

        // set_parent_of must precede the child's map (the at-creation owner path real toolkits use).
        var child = client.BeginToplevel(childTitle);
        client.SetForeignParent(imported, child);
        client.Roundtrip();
        client.AttachFrame(child, 120, 90, Blue); // first buffer → child maps, owned by the parent window
        WaylandTestHarness.RoundtripAndPump(client);

        var parentWindow = WaylandHosting.AutoWindows.Single(w => w.Title == parentTitle);
        var childWindow = WaylandHosting.AutoWindows.Single(w => w.Title == childTitle);
        Assert.Same(parentWindow, childWindow.Owner);
    }

    [AvaloniaFact] // P3: importing an unknown handle is inert — set_parent_of has no effect
    public void Importing_an_unknown_handle_is_inert()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), LinuxOnly);
        var title = nameof(Importing_an_unknown_handle_is_inert);
        var childTitle = title + "-child";

        using var client = WaylandTestClient.Connect();
        var imported = client.ImportToplevel("no-such-handle");
        var child = client.BeginToplevel(childTitle);
        client.SetForeignParent(imported, child);
        client.Roundtrip();
        client.AttachFrame(child, 120, 90, Blue);
        WaylandTestHarness.RoundtripAndPump(client);

        var childWindow = WaylandHosting.AutoWindows.Single(w => w.Title == childTitle);
        Assert.Null(childWindow.Owner);
    }

    [AvaloniaFact] // P3 scenario 3: export an Avalonia window; a toolkit imports the handle + set_parent_of → child auto-window owned by it
    public void Exporting_an_avalonia_window_owns_an_importing_childs_window()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), LinuxOnly);
        var title = nameof(Exporting_an_avalonia_window_owns_an_importing_childs_window);
        var childTitle = title + "-child";

        using var client = WaylandTestClient.Connect();

        var ownerWindow = new Window { Title = title, Width = 300, Height = 200 };
        ownerWindow.Show();
        WaylandTestHarness.Pump();

        using var export = WaylandEmbeddingSubcompositor.ExportForeignXdgToplevel(ownerWindow);
        Assert.False(string.IsNullOrEmpty(export.Handle));

        // A toolkit imports the handle and parents its dialog to the exported window BEFORE the child maps.
        var imported = client.ImportToplevel(export.Handle);
        var child = client.BeginToplevel(childTitle);
        client.SetForeignParent(imported, child);
        client.Roundtrip();
        client.AttachFrame(child, 160, 120, Blue); // first buffer → child maps, owned by the Avalonia window
        WaylandTestHarness.RoundtripAndPump(client);

        var childWindow = WaylandHosting.AutoWindows.Single(w => w.Title == childTitle);
        Assert.Same(ownerWindow, childWindow.Owner); // the child auto-window is owned by the exported Avalonia window

        // Revoke the export → a later import of the handle is inert (the next child maps unowned).
        export.Dispose();
        WaylandEmbeddingSubcompositor.Roundtrip(); // the revoke job lands before the re-import resolves
        var lateTitle = title + "-late";
        var lateImported = client.ImportToplevel(export.Handle);
        var lateChild = client.BeginToplevel(lateTitle);
        client.SetForeignParent(lateImported, lateChild);
        client.Roundtrip();
        client.AttachFrame(lateChild, 100, 80, Red);
        WaylandTestHarness.RoundtripAndPump(client);
        Assert.Null(WaylandHosting.AutoWindows.Single(w => w.Title == lateTitle).Owner);

        ownerWindow.Close();
    }

    [AvaloniaFact] // P3 scenario 4: a toolkit exports its toplevel; ImportForeignXdgToplevel resolves the hosting control + window
    public void Importing_a_foreign_handle_resolves_the_hosting_control()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), LinuxOnly);
        var title = nameof(Importing_a_foreign_handle_resolves_the_hosting_control);

        using var client = WaylandTestClient.Connect();
        var top = client.MapToplevel(title, 200, 150, Red); // auto-hosted → headless auto-window
        WaylandTestHarness.RoundtripAndPump(client);
        var window = WaylandHosting.AutoWindows.Single(w => w.Title == title);
        var expectedHost = Assert.IsType<WaylandSubcompositorControlHost>(window.Content);

        var exported = client.ExportToplevel(top); // zxdg_exporter_v2.export_toplevel → handle
        client.Roundtrip();
        Assert.False(string.IsNullOrEmpty(exported.Handle));

        // Avalonia resolves the (out-of-band) handle to the host control hosting the exported toplevel.
        var host = WaylandEmbeddingSubcompositor.ImportForeignXdgToplevel(exported.Handle!);
        Assert.Same(expectedHost, host);
        Assert.Same(window, TopLevel.GetTopLevel(host!)); // walk up to the owning Window for dialog parenting

        Assert.Null(WaylandEmbeddingSubcompositor.ImportForeignXdgToplevel("no-such-handle")); // unknown → null
    }

    [AvaloniaFact] // P3: xdg-foreign-unstable-v1 (the version GTK3 binds) — export publishes a handle; import + set_parent_of owns the child
    public void Exporting_and_importing_over_v1_owns_the_child_window()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), LinuxOnly);
        var title = nameof(Exporting_and_importing_over_v1_owns_the_child_window);
        var parentTitle = title + "-parent";
        var childTitle = title + "-child";

        using var client = WaylandTestClient.Connect();
        var parent = client.MapToplevel(parentTitle, 200, 150, Red);
        WaylandTestHarness.RoundtripAndPump(client);

        var exported = client.ExportToplevelV1(parent); // zxdg_exporter_v1.export → handle
        client.Roundtrip();
        Assert.True(exported.HandleReceived, "exporter never sent zxdg_exported_v1.handle");
        Assert.False(string.IsNullOrEmpty(exported.Handle));

        var imported = client.ImportToplevelV1(exported.Handle!);
        var child = client.BeginToplevel(childTitle);
        client.SetForeignParentV1(imported, child); // before the child maps (the at-creation owner path)
        client.Roundtrip();
        client.AttachFrame(child, 120, 90, Blue);
        WaylandTestHarness.RoundtripAndPump(client);

        var parentWindow = WaylandHosting.AutoWindows.Single(w => w.Title == parentTitle);
        var childWindow = WaylandHosting.AutoWindows.Single(w => w.Title == childTitle);
        Assert.Same(parentWindow, childWindow.Owner);
    }

    [AvaloniaFact] // P3 scenario 3 over v1: a GTK-style client imports an exported Avalonia window via zxdg_importer_v1
    public void Importing_an_exported_avalonia_window_over_v1_owns_the_child_window()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), LinuxOnly);
        var title = nameof(Importing_an_exported_avalonia_window_over_v1_owns_the_child_window);
        var childTitle = title + "-child";

        using var client = WaylandTestClient.Connect();

        var ownerWindow = new Window { Title = title, Width = 300, Height = 200 };
        ownerWindow.Show();
        WaylandTestHarness.Pump();

        using var export = WaylandEmbeddingSubcompositor.ExportForeignXdgToplevel(ownerWindow);
        Assert.False(string.IsNullOrEmpty(export.Handle));

        var imported = client.ImportToplevelV1(export.Handle); // GTK imports via v1
        var child = client.BeginToplevel(childTitle);
        client.SetForeignParentV1(imported, child);
        client.Roundtrip();
        client.AttachFrame(child, 160, 120, Blue);
        WaylandTestHarness.RoundtripAndPump(client);

        var childWindow = WaylandHosting.AutoWindows.Single(w => w.Title == childTitle);
        Assert.Same(ownerWindow, childWindow.Owner);

        ownerWindow.Close();
    }

    // ───────────────────────── P3 scenario 5: mark_content_surface (Avalonia content into a toolkit window) ─────────────────────────

    [AvaloniaFact] // P3: a registered content cookie + a mapped toolkit window binds the Avalonia content overlay
    public void Marking_a_content_surface_attaches_avalonia_content_to_the_host()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), LinuxOnly);
        var title = nameof(Marking_a_content_surface_attaches_avalonia_content_to_the_host);

        using var client = WaylandTestClient.Connect();

        // Avalonia side: a content host with some Avalonia content, minting a cookie shared out-of-band.
        var contentHost = new WaylandSubcompositorAvaloniaContentHost { Content = new Border { Width = 10, Height = 10 } };
        var resolved = false;
        contentHost.Resolved += (_, _) => resolved = true;
        var cookie = contentHost.CreateAttachmentCookie();

        // Toolkit side: map its window, then tag its own surface as the content container with the cookie.
        var top = client.MapToplevel(title, 200, 150, Red);
        WaylandTestHarness.RoundtripAndPump(client);
        var result = client.MarkContentSurface(top, cookie);
        WaylandTestHarness.RoundtripAndPump(client);

        Assert.True(result.Bound, "mark_content_surface was not accepted");
        Assert.False(result.Rejected);
        Assert.True(contentHost.IsResolved);
        Assert.True(resolved, "Resolved event did not fire");

        var window = WaylandHosting.AutoWindows.Single(w => w.Title == title);
        var host = Assert.IsType<WaylandSubcompositorControlHost>(window.Content);
        Assert.NotNull(host.ContentOverlay);

        // Glue reports the visible rect; the overlay is arranged there over the toolkit bitmap.
        contentHost.UpdateContentRect(new Rect(5, 6, 40, 30));
        WaylandTestHarness.Pump();
        Assert.Equal(new Rect(5, 6, 40, 30), host.ContentOverlay!.Bounds);
    }

    [AvaloniaFact] // P3: mark_content_surface is accepted on a realized-but-UNMAPPED toplevel and resolves on map (deferred)
    public void Marking_an_unmapped_content_surface_resolves_when_it_maps()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), LinuxOnly);
        var title = nameof(Marking_an_unmapped_content_surface_resolves_when_it_maps);

        using var client = WaylandTestClient.Connect();

        var contentHost = new WaylandSubcompositorAvaloniaContentHost { Content = new Border { Width = 10, Height = 10 } };
        var resolved = false;
        contentHost.Resolved += (_, _) => resolved = true;
        var cookie = contentHost.CreateAttachmentCookie();

        // Realize the toolkit window WITHOUT mapping it (role-setup commit, no buffer) — the GTK ShowAll-before-pump
        // state — and mark it now, before it draws. The compositor accepts it and defers resolution to the map.
        var top = client.BeginToplevel(title);
        var result = client.MarkContentSurface(top, cookie);
        WaylandTestHarness.RoundtripAndPump(client);

        Assert.True(result.Bound, "a mark on an unmapped toplevel should be accepted (deferred to map)");
        Assert.False(result.Rejected);
        Assert.False(contentHost.IsResolved, "must not resolve before the toolkit window maps (no host id yet)");
        Assert.DoesNotContain(WaylandHosting.AutoWindows, w => w.Title == title); // unmapped → no host control yet

        // The toolkit now draws+commits → the toplevel maps → the deferred mark resolves onto the freshly-created host.
        client.AttachFrame(top, 200, 150, Red);
        WaylandTestHarness.RoundtripAndPump(client);

        Assert.True(contentHost.IsResolved, "the deferred mark did not resolve when the toplevel mapped");
        Assert.True(resolved, "Resolved event did not fire on map");
        var window = WaylandHosting.AutoWindows.Single(w => w.Title == title);
        var host = Assert.IsType<WaylandSubcompositorControlHost>(window.Content);
        Assert.NotNull(host.ContentOverlay);
    }

    [AvaloniaFact] // P3: an unknown content cookie is rejected
    public void Marking_a_content_surface_with_an_unknown_cookie_is_rejected()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), LinuxOnly);
        var title = nameof(Marking_a_content_surface_with_an_unknown_cookie_is_rejected);

        using var client = WaylandTestClient.Connect();
        var top = client.MapToplevel(title, 200, 150, Red);
        WaylandTestHarness.RoundtripAndPump(client);

        var result = client.MarkContentSurface(top, "no-such-cookie");
        WaylandTestHarness.RoundtripAndPump(client);

        Assert.True(result.Rejected, "an unknown cookie should be rejected");
        Assert.False(result.Bound);

        var window = WaylandHosting.AutoWindows.Single(w => w.Title == title);
        var host = Assert.IsType<WaylandSubcompositorControlHost>(window.Content);
        Assert.Null(host.ContentOverlay);
    }

    [AvaloniaFact] // P3: AttachTo binds directly (no protocol), Detach clears the host's content layer
    public void Content_host_attaches_directly_and_detaches()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), LinuxOnly);
        var title = nameof(Content_host_attaches_directly_and_detaches);

        using var client = WaylandTestClient.Connect();
        var top = client.MapToplevel(title, 200, 150, Red);
        WaylandTestHarness.RoundtripAndPump(client);
        var window = WaylandHosting.AutoWindows.Single(w => w.Title == title);
        var host = Assert.IsType<WaylandSubcompositorControlHost>(window.Content);

        var contentHost = new WaylandSubcompositorAvaloniaContentHost { Content = new Border() };
        contentHost.AttachTo(host);
        Assert.True(contentHost.IsResolved);
        Assert.NotNull(host.ContentOverlay);

        contentHost.Detach();
        Assert.False(contentHost.IsResolved);
        Assert.Null(host.ContentOverlay);
    }

    [AvaloniaFact] // P3: Dispose is the deterministic teardown the glue calls — detaches and drops the cookie (finalizer only backstops)
    public void Disposing_a_content_host_detaches_and_drops_its_cookie()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), LinuxOnly);
        var title = nameof(Disposing_a_content_host_detaches_and_drops_its_cookie);

        using var client = WaylandTestClient.Connect();
        var top = client.MapToplevel(title, 200, 150, Red);
        WaylandTestHarness.RoundtripAndPump(client);
        var host = Assert.IsType<WaylandSubcompositorControlHost>(
            WaylandHosting.AutoWindows.Single(w => w.Title == title).Content);

        var contentHost = new WaylandSubcompositorAvaloniaContentHost { Content = new Border() };
        var cookie = contentHost.CreateAttachmentCookie();
        contentHost.AttachTo(host);
        Assert.True(contentHost.IsResolved);
        Assert.NotNull(host.ContentOverlay);

        contentHost.Dispose();
        Assert.False(contentHost.IsResolved); // detached from the host (overlay cleared)
        Assert.Null(host.ContentOverlay);

        // Dispose drops the cookie (not just unbinds) → a later mark with it is inert.
        WaylandTestHarness.SettleServerAndPump(); // let the UnregisterContentCookieJob land before marking
        var result = client.MarkContentSurface(top, cookie);
        WaylandTestHarness.RoundtripAndPump(client);
        Assert.True(result.Rejected, "the cookie should be inert after Dispose");
    }

    [AvaloniaFact] // P3 review H1: when the toolkit window unmaps, the content host auto-detaches (no dangling overlay)
    public void Content_host_detaches_when_the_toolkit_window_unmaps()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), LinuxOnly);
        var title = nameof(Content_host_detaches_when_the_toolkit_window_unmaps);

        using var client = WaylandTestClient.Connect();
        var top = client.MapToplevel(title, 200, 150, Red);
        WaylandTestHarness.RoundtripAndPump(client);
        var host = Assert.IsType<WaylandSubcompositorControlHost>(
            WaylandHosting.AutoWindows.Single(w => w.Title == title).Content);

        var contentHost = new WaylandSubcompositorAvaloniaContentHost { Content = new Border() };
        var detached = false;
        contentHost.Detached += (_, _) => detached = true;
        contentHost.AttachTo(host);
        Assert.NotNull(host.ContentOverlay);

        client.DestroyToplevel(top);          // the toolkit closes its window → host Stop() → EmbeddedSurfaceUnmapped
        WaylandTestHarness.SettleServerAndPump();

        Assert.False(contentHost.IsResolved);
        Assert.True(detached, "content host did not auto-detach on window teardown");
        Assert.Null(host.ContentOverlay);      // the overlay was removed, not left dangling
    }

    [AvaloniaFact] // P3 review H2: re-targeting a content host to a second host detaches the first (no VisualChildren throw)
    public void Content_host_can_be_re_targeted_to_another_host()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), LinuxOnly);
        var title = nameof(Content_host_can_be_re_targeted_to_another_host);

        using var client = WaylandTestClient.Connect();
        var topA = client.MapToplevel(title + "-A", 120, 90, Red);
        var topB = client.MapToplevel(title + "-B", 120, 90, Blue);
        WaylandTestHarness.RoundtripAndPump(client);
        var hostA = Assert.IsType<WaylandSubcompositorControlHost>(
            WaylandHosting.AutoWindows.Single(w => w.Title == title + "-A").Content);
        var hostB = Assert.IsType<WaylandSubcompositorControlHost>(
            WaylandHosting.AutoWindows.Single(w => w.Title == title + "-B").Content);

        var contentHost = new WaylandSubcompositorAvaloniaContentHost { Content = new Border() };
        contentHost.AttachTo(hostA);
        Assert.NotNull(hostA.ContentOverlay);

        contentHost.AttachTo(hostB);            // must not throw (shared presenter detached from A first)
        Assert.Null(hostA.ContentOverlay);      // moved off A
        Assert.NotNull(hostB.ContentOverlay);   // onto B
        Assert.True(contentHost.IsResolved);
    }

    [AvaloniaFact] // P3 review M1: marking a popup's surface is rejected — content attaches only to a toplevel window
    public void Marking_a_popup_surface_is_rejected()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), LinuxOnly);
        var title = nameof(Marking_a_popup_surface_is_rejected);

        using var client = WaylandTestClient.Connect();
        var contentHost = new WaylandSubcompositorAvaloniaContentHost { Content = new Border() };
        var cookie = contentHost.CreateAttachmentCookie();

        var top = client.MapToplevel(title, 200, 150, Red);
        WaylandTestHarness.RoundtripAndPump(client);
        var popup = client.MapPopup(top, 64, 48, Blue);
        WaylandTestHarness.RoundtripAndPump(client);

        var result = client.MarkContentSurface(popup, cookie);
        WaylandTestHarness.RoundtripAndPump(client);

        Assert.True(result.Rejected, "a popup surface should not be a valid content container");
        Assert.False(contentHost.IsResolved);
    }

    [AvaloniaFact] // P3: nested embedding — toolkit → Avalonia content (scenario 5) → toolkit — via the existing pipeline
    public void Nested_embedding_renders_an_inner_toolkit_surface_inside_a_content_overlay()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), LinuxOnly);
        var title = nameof(Nested_embedding_renders_an_inner_toolkit_surface_inside_a_content_overlay);

        using var client = WaylandTestClient.Connect();

        // The scenario-5 content is itself a scenario-1 host that will adopt a SECOND toolkit toplevel.
        var nestedHost = new WaylandSubcompositorControlHost();
        var token = nestedHost.GetEmbeddingToken();
        var contentHost = new WaylandSubcompositorAvaloniaContentHost { Content = nestedHost };
        var cookie = contentHost.CreateAttachmentCookie();

        // Outer toolkit window → auto-window host control.
        var outer = client.MapToplevel(title, 200, 150, Red);
        WaylandTestHarness.RoundtripAndPump(client);
        var outerWindow = WaylandHosting.AutoWindows.Single(w => w.Title == title);
        var outerHost = Assert.IsType<WaylandSubcompositorControlHost>(outerWindow.Content);

        // Tag the outer window's surface as the content container → nestedHost is parented into the outer host.
        var marked = client.MarkContentSurface(outer, cookie);
        WaylandTestHarness.RoundtripAndPump(client); // deliver the bound/rejected result
        Assert.True(marked.Bound);
        contentHost.UpdateContentRect(new Rect(0, 0, 100, 100));
        WaylandTestHarness.Pump();

        Assert.True(contentHost.IsResolved);
        // The nested host now lives in the outer window's visual tree (under the content overlay).
        Assert.Same(outerWindow, TopLevel.GetTopLevel(nestedHost));

        // Inner toolkit toplevel embeds into the nested host (scenario 1, via the token) and renders there.
        var inner = client.EmbedToplevel(title + "-inner", 64, 48, Blue, token);
        WaylandTestHarness.RoundtripAndPump(client);

        Assert.True(inner.EmbedBound, "inner toplevel was not embedded into the nested host");
        var bmp = Assert.Single(nestedHost.Bitmaps.Values);
        Assert.Equal(new PixelSize(64, 48), bmp.PixelSize);
    }

    [AvaloniaFact] // P3: pointer hit-testing targets the sub-surface under the cursor in its local coordinates
    public void Pointer_hit_tests_into_a_subsurface()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), LinuxOnly);
        var title = nameof(Pointer_hit_tests_into_a_subsurface);

        using var client = WaylandTestClient.Connect();
        var top = client.MapToplevel(title, 100, 80, Red); // window is 1:1 with the buffer
        WaylandTestHarness.RoundtripAndPump(client);
        var sub = client.AddSyncSubsurface(top, x: 20, y: 20, width: 30, height: 30, Blue);
        client.CommitParent(top); // applies the sync subsurface into the tree
        WaylandTestHarness.RoundtripAndPump(client);

        var window = WaylandHosting.AutoWindows.Single(w => w.Title == title);

        // Over the subsurface region (20..50): enter targets the SUBSURFACE in its local coords.
        window.MouseMove(new Point(35, 35));
        WaylandTestHarness.Pump();
        client.Roundtrip();
        Assert.Same(sub.Surface, client.Pointer.LastEnterSurface);
        Assert.InRange(client.Pointer.LastX, 14, 16); // 35 − 20 (subsurface origin)
        Assert.InRange(client.Pointer.LastY, 14, 16);

        // Moving onto the root region (outside the subsurface): leave the subsurface, enter the root.
        window.MouseMove(new Point(5, 5));
        WaylandTestHarness.Pump();
        client.Roundtrip();
        Assert.Same(top.Surface, client.Pointer.LastEnterSurface);
        Assert.Same(sub.Surface, client.Pointer.LastLeaveSurface);
        Assert.InRange(client.Pointer.LastX, 4, 6);
        Assert.InRange(client.Pointer.LastY, 4, 6);
    }

    [AvaloniaFact] // P3: wl_pointer.set_cursor applies a client-supplied cursor image to the host (and clears it)
    public void Set_cursor_applies_and_clears_a_custom_cursor()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), LinuxOnly);
        var title = nameof(Set_cursor_applies_and_clears_a_custom_cursor);

        using var client = WaylandTestClient.Connect();
        client.MapToplevel(title, 100, 80, Red);
        WaylandTestHarness.RoundtripAndPump(client);
        var window = WaylandHosting.AutoWindows.Single(w => w.Title == title);
        var host = Assert.IsType<WaylandSubcompositorControlHost>(window.Content);

        // The pointer must enter first (gives the client an enter serial + sets the pointer-over-host id).
        window.MouseMove(new Point(40, 30));
        WaylandTestHarness.Pump();
        client.Roundtrip();
        Assert.True(client.Pointer.EnterCount > 0);

        client.SetCursor(16, 16, Blue, hotspotX: 4, hotspotY: 5);
        WaylandTestHarness.RoundtripAndPump(client);

        Assert.NotNull(host.Cursor);
        Assert.NotNull(host.EmbeddedCursorBitmap);
        Assert.Equal(new PixelSize(16, 16), host.EmbeddedCursorBitmap!.PixelSize);

        // set_cursor(null) clears it back to the host default.
        client.HideCursor();
        WaylandTestHarness.RoundtripAndPump(client);
        Assert.Null(host.EmbeddedCursorBitmap);
        Assert.Null(host.Cursor);
    }

    [AvaloniaFact] // P3 IME: Avalonia composition (the bridge's SetPreeditText) is forwarded as preedit_string
    public void Ime_preedit_is_forwarded_to_the_client()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), LinuxOnly);
        var title = nameof(Ime_preedit_is_forwarded_to_the_client);

        using var client = WaylandTestClient.Connect();
        client.AutoEnableTextInput = true; // the toolkit enables its text-input on focus
        client.MapToplevel(title, 100, 80, Red);
        WaylandTestHarness.RoundtripAndPump(client);
        var window = WaylandHosting.AutoWindows.Single(w => w.Title == title);
        var host = Assert.IsType<WaylandSubcompositorControlHost>(window.Content);

        host.Focus();                                  // text-input enter → the client enables its text-input
        WaylandTestHarness.RoundtripAndPump(client);
        client.Roundtrip();                            // ensure the enable request is processed before composing

        // Simulate the OS IME composing into the focused host (what the platform IME calls on our bridge).
        host.ImeClient.SetPreeditText("ni", 2);
        WaylandTestHarness.RoundtripAndPump(client);

        Assert.True(client.TextInput.PreeditCount > 0, "preedit_string was not forwarded to the client");
        Assert.Equal("ni", client.TextInput.LastPreedit);
        Assert.Equal(2, client.TextInput.LastPreeditCursorBegin); // "ni" = 2 UTF-8 bytes; caret at the end
        Assert.Equal(2, client.TextInput.LastPreeditCursorEnd);
    }

    [AvaloniaFact] // P3 IME: the client's caret rectangle (set_cursor_rectangle) reaches the host's IME bridge
    public void Ime_cursor_rectangle_reaches_the_host_bridge()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), LinuxOnly);
        var title = nameof(Ime_cursor_rectangle_reaches_the_host_bridge);

        using var client = WaylandTestClient.Connect();
        client.AutoEnableTextInput = true;
        client.MapToplevel(title, 100, 80, Red); // 1:1 host ⇒ surface coords map straight to host coords
        WaylandTestHarness.RoundtripAndPump(client);
        var window = WaylandHosting.AutoWindows.Single(w => w.Title == title);
        var host = Assert.IsType<WaylandSubcompositorControlHost>(window.Content);

        host.Focus(); // text-input enter → the focus host id is set so the reverse request routes back
        WaylandTestHarness.RoundtripAndPump(client);
        client.Roundtrip();

        client.SetTextCursorRectangle(10, 20, 2, 16);
        WaylandTestHarness.RoundtripAndPump(client);

        var rect = host.ImeClient.CursorRectangle;
        Assert.InRange(rect.X, 9, 11);
        Assert.InRange(rect.Y, 19, 21);
        Assert.InRange(rect.Width, 1, 3);
        Assert.InRange(rect.Height, 15, 17);
    }

    [AvaloniaFact] // P3 IME review H2: losing focus clears any pending composition in the client
    public void Ime_preedit_is_cleared_when_focus_leaves()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux(), LinuxOnly);
        var title = nameof(Ime_preedit_is_cleared_when_focus_leaves);

        // Embed into a window with a sibling we can move focus to (to blur the host).
        var host = new WaylandSubcompositorControlHost();
        var other = new Button { Content = "other" };
        DockPanel.SetDock(other, Dock.Bottom);
        var panel = new DockPanel();
        panel.Children.Add(other);
        panel.Children.Add(host);
        var appWindow = new Window { Width = 220, Height = 160, Content = panel, Title = "app:" + title };
        appWindow.Show();
        WaylandTestHarness.Pump();
        var token = host.GetEmbeddingToken();

        using var client = WaylandTestClient.Connect();
        client.AutoEnableTextInput = true;
        client.EmbedToplevel(title, 100, 60, Red, token);
        WaylandTestHarness.RoundtripAndPump(client);
        host.Focus(); // text-input enter → the client enables its text-input
        WaylandTestHarness.RoundtripAndPump(client);
        client.Roundtrip();

        host.ImeClient.SetPreeditText("ni", 2);
        WaylandTestHarness.RoundtripAndPump(client);
        Assert.Equal("ni", client.TextInput.LastPreedit);

        // Blur the host (focus the sibling) → text-input leave: the compositor clears the composition first.
        other.Focus();
        WaylandTestHarness.RoundtripAndPump(client);
        client.Roundtrip();
        Assert.Equal("", client.TextInput.LastPreedit); // stale "ni" was cleared, not left dangling
        Assert.True(client.TextInput.LeaveCount > 0);

        appWindow.Close();
    }
}
