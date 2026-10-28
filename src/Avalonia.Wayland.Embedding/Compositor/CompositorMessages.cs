using System;
using NWayland.Server;

namespace Avalonia.Wayland.Embedding.Compositor;

/// <summary>A generic unit of work to run on the compositor thread, delivered via <c>WaylandServer.Post</c>.</summary>
internal sealed class CompositorJob
{
    private readonly Action _action;
    public CompositorJob(Action action) => _action = action;
    public void Run() => _action();
}

internal enum PointerEventKind { Enter, Leave, Motion, Button, Axis }

/// <summary>
/// UI→compositor: a pointer event from the host control, in surface-local DIPs, to forward to the embedded
/// client's wl_pointer (on the host's toplevel surface).
/// </summary>
internal sealed class PointerInputArgs
{
    public PointerInputArgs(uint hostId, PointerEventKind kind, double surfaceX = 0, double surfaceY = 0,
        uint button = 0, bool pressed = false, int axis = 0, double axisValue = 0)
    {
        HostId = hostId;
        Kind = kind;
        SurfaceX = surfaceX;
        SurfaceY = surfaceY;
        Button = button;
        Pressed = pressed;
        Axis = axis;
        AxisValue = axisValue;
    }

    public uint HostId { get; }
    public PointerEventKind Kind { get; }
    public double SurfaceX { get; }
    public double SurfaceY { get; }
    public uint Button { get; }      // evdev code (BTN_LEFT 0x110, …)
    public bool Pressed { get; }
    public int Axis { get; }         // 0 = vertical, 1 = horizontal
    public double AxisValue { get; }
}

internal enum KeyboardEventKind { Enter, Leave, Key }

/// <summary>
/// UI→compositor: a keyboard focus/key event from the host control, to forward to the embedded client's
/// wl_keyboard (on the host's toplevel surface).
/// </summary>
internal sealed class KeyboardInputArgs
{
    public KeyboardInputArgs(uint hostId, KeyboardEventKind kind, uint key = 0, bool pressed = false,
        uint modifiers = 0, bool producesText = false)
    {
        HostId = hostId;
        Kind = kind;
        Key = key;
        Pressed = pressed;
        Modifiers = modifiers;
        ProducesText = producesText;
    }

    public uint HostId { get; }
    public KeyboardEventKind Kind { get; }
    public uint Key { get; }        // evdev keycode
    public bool Pressed { get; }
    public uint Modifiers { get; }  // xkb depressed-mod mask (Shift 1, Control 4, Alt 8, …)
    public bool ProducesText { get; } // the key yields a character (KeySymbol) — suppressed when an IME is active
}

internal enum TextInputEventKind { Enter, Leave, Commit, Preedit }

/// <summary>
/// UI→compositor: a text-input focus/commit/preedit event from the host control, to forward to the embedded
/// client's zwp_text_input_v3 (enter/leave, commit_string, or preedit_string — all followed by done).
/// </summary>
internal sealed class TextInputArgs
{
    public TextInputArgs(uint hostId, TextInputEventKind kind, string? text = null,
        int preeditCursorBegin = 0, int preeditCursorEnd = 0)
    {
        HostId = hostId;
        Kind = kind;
        Text = text;
        PreeditCursorBegin = preeditCursorBegin;
        PreeditCursorEnd = preeditCursorEnd;
    }

    public uint HostId { get; }
    public TextInputEventKind Kind { get; }
    public string? Text { get; }
    // preedit_string cursor span (UTF-8 byte offsets into Text); begin==end is a caret.
    public int PreeditCursorBegin { get; }
    public int PreeditCursorEnd { get; }
}

/// <summary>Posted on shutdown to wake the loop out of <c>epoll_wait</c> and break it.</summary>
internal sealed class StopSentinel
{
    public static readonly StopSentinel Instance = new();
    private StopSentinel() { }
}

/// <summary>
/// Posted right after a client is added so the compositor thread advertises that client's globals
/// before any of the client's network requests (e.g. <c>get_registry</c>) are parsed. Custom events
/// are dequeued before network bytes, so the globals are guaranteed to be in place first.
/// </summary>
internal sealed class SetupClientJob
{
    public SetupClientJob(WaylandClient client) => Client = client;
    public WaylandClient Client { get; }
}

/// <summary>
/// Roundtrip drain marker. When dequeued it is merely <i>added</i> to the compositor's pending list;
/// it is completed only after the network queue is fully drained (loop step 2), so a roundtrip can
/// never complete before the requests it was meant to flush have been processed.
/// </summary>
internal sealed class DrainSentinel
{
    public DrainSentinel(RoundtripTicket ticket) => Ticket = ticket;
    public RoundtripTicket Ticket { get; }
}

/// <summary>Completion flag for a synchronous <c>Roundtrip()</c>. Set on the compositor thread, polled on the UI thread.</summary>
internal sealed class RoundtripTicket
{
    private volatile bool _completed;
    public bool Completed
    {
        get => _completed;
        set => _completed = value;
    }
}

/// <summary>Base for an event delivered compositor→UI; <see cref="Apply"/> runs on the UI thread inside the drain.</summary>
internal abstract class CompositorToUiEvent
{
    public abstract void Apply();
}
