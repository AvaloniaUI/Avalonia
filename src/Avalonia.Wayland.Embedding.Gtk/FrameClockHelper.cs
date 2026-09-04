using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Avalonia.Wayland.Embedding;

/// <summary>
/// Hard hack for frame-perfect resize: patches the GObject vtables of the GdkFrameClock class so we can track each
/// clock's freeze/thaw state and neutralize the throttle freeze GTK applies during after-paint.
///
/// We drive embedded clocks by hand during a resize flush (emit paint/after-paint with no GLib loop turn). GTK's
/// after-paint commits a buffer and FREEZES the clock to wait for a <c>wl_surface.frame</c> callback that we deliver
/// out-of-band — so on every forced frame the freeze count would climb and the clock would stall. We undo only the
/// REDUNDANT freeze (the one added while the clock was already frozen from a prior after-paint), leaving the single
/// legit throttle freeze to be cleared by the real frame callback when resizing stops.
///
/// Patching is per-class (all GdkFrameClockIdle instances share one vtable), done once at glue init against a pinned
/// class so its vtable can't be freed. It is harmless for clocks we don't drive: the thunks just track + forward.
/// </summary>
internal static unsafe class FrameClockHelper
{
    private static readonly object s_lock = new();
    // Per-clock tracking state, keyed by the clock instance pointer. Populated in `constructed`, dropped in `dispose`.
    private static readonly Dictionary<IntPtr, ClockState> s_states = new();
    private static bool s_initialized;

    // The class's original vtable slots; we chain to them after tracking.
    private static delegate* unmanaged<IntPtr, void> s_origConstructed;
    private static delegate* unmanaged<IntPtr, void> s_origDispose;
    private static delegate* unmanaged<IntPtr, void> s_origFreeze;
    private static delegate* unmanaged<IntPtr, void> s_origThaw;

    internal sealed class ClockState
    {
        public int FreezeCount;
        public bool InAfterPaint;
        // Set when a freeze lands during after-paint; cleared only when the freeze count returns to 0. I.e. "the clock
        // is sitting frozen because of an after-paint commit" — the precondition for neutralizing the next one.
        public bool FrozenFromAfterPaint;
    }

    /// <summary>
    /// Patch the class behind <paramref name="sampleClock"/> (any live GdkFrameClock instance). Idempotent; call once
    /// at glue init, on the GTK thread, with a throwaway window's clock.
    /// </summary>
    public static void Initialize(IntPtr sampleClock)
    {
        if (s_initialized || sampleClock == IntPtr.Zero)
            return;
        // GTypeInstance: a GObject instance's first pointer is its class (vtable); the class's first field is its GType.
        var klass = (GdkFrameClockClass*)(*(IntPtr*)sampleClock);
        g_type_class_ref((nuint)(nint)klass->ParentClass.GTypeClass); // pin the class so our patch outlives the throwaway window
        PatchClass(klass);
        s_initialized = true;
    }

    private static void PatchClass(GdkFrameClockClass* klass)
    {
        s_origConstructed = (delegate* unmanaged<IntPtr, void>)klass->ParentClass.Constructed;
        s_origDispose = (delegate* unmanaged<IntPtr, void>)klass->ParentClass.Dispose;
        s_origFreeze = (delegate* unmanaged<IntPtr, void>)klass->Freeze;
        s_origThaw = (delegate* unmanaged<IntPtr, void>)klass->Thaw;

        klass->ParentClass.Constructed = (IntPtr)(delegate* unmanaged<IntPtr, void>)&ConstructedThunk;
        klass->ParentClass.Dispose = (IntPtr)(delegate* unmanaged<IntPtr, void>)&DisposeThunk;
        klass->Freeze = (IntPtr)(delegate* unmanaged<IntPtr, void>)&FreezeThunk;
        klass->Thaw = (IntPtr)(delegate* unmanaged<IntPtr, void>)&ThawThunk;
    }

    /// <summary>
    /// Snapshot the clock's state, then on dispose undo the redundant after-paint freeze. Wrap the manual
    /// <c>after-paint</c> emit in a <c>using</c>: the throttle freeze is left in place on the FIRST forced frame
    /// (legit), and undone on every frame thereafter while the clock stays frozen from that first one.
    /// </summary>
    public static ManualAfterPaintScope EnterManualAfterPaint(IntPtr clock)
    {
        ClockState? state;
        lock (s_lock)
            s_states.TryGetValue(clock, out state);
        return new ManualAfterPaintScope(clock, state);
    }

    public readonly struct ManualAfterPaintScope : IDisposable
    {
        private readonly IntPtr _clock;
        private readonly ClockState? _state;
        private readonly bool _wasFrozenFromAfterPaint;
        private readonly int _countBefore;

        internal ManualAfterPaintScope(IntPtr clock, ClockState? state)
        {
            _clock = clock;
            _state = state;
            _wasFrozenFromAfterPaint = state?.FrozenFromAfterPaint ?? false;
            _countBefore = state?.FreezeCount ?? 0;
        }

        public void Dispose()
        {
            // Only neutralize when the clock was ALREADY frozen from a prior after-paint: otherwise this after-paint's
            // freeze is the legit throttle freeze and must stay (the real frame callback clears it).
            if (_state is null || !_wasFrozenFromAfterPaint)
                return;
            var introduced = _state.FreezeCount - _countBefore;
            for (var i = 0; i < introduced; i++)
                ThawTracked(_clock, _state);
        }
    }

    // Tracked thaw: mirror GTK's freeze refcount and never thaw below zero (which would trip GTK's "thaw an unfrozen
    // clock" warning). Shared by the thaw thunk and the force-thaw in ManualAfterPaintScope so both stay in sync.
    private static void ThawTracked(IntPtr clock, ClockState state)
    {
        if (state.FreezeCount == 0)
            return;
        state.FreezeCount--;
        if (state.FreezeCount == 0)
            state.FrozenFromAfterPaint = false;
        if (s_origThaw != null)
            s_origThaw(clock);
    }

    private static ClockState? Lookup(IntPtr clock)
    {
        lock (s_lock)
            return s_states.TryGetValue(clock, out var state) ? state : null;
    }

    [UnmanagedCallersOnly]
    private static void ConstructedThunk(IntPtr clock)
    {
        if (s_origConstructed != null)
            s_origConstructed(clock); // chain up first: the object isn't fully constructed until the base runs
        lock (s_lock)
            s_states[clock] = new ClockState();
        // Bracket every after-paint emission: the "before" handler (plain connect, runs first) opens the window, the
        // "after" handler (connect-after, runs last) closes it — so a freeze from GDK's own after-paint handler in
        // between is attributed to after-paint.
        g_signal_connect_data(clock, "after-paint", (IntPtr)(delegate* unmanaged<IntPtr, IntPtr, void>)&AfterPaintEnter, IntPtr.Zero, IntPtr.Zero, 0);
        g_signal_connect_data(clock, "after-paint", (IntPtr)(delegate* unmanaged<IntPtr, IntPtr, void>)&AfterPaintLeave, IntPtr.Zero, IntPtr.Zero, GConnectAfter);
    }

    [UnmanagedCallersOnly]
    private static void DisposeThunk(IntPtr clock)
    {
        lock (s_lock)
            s_states.Remove(clock);
        if (s_origDispose != null)
            s_origDispose(clock);
    }

    [UnmanagedCallersOnly]
    private static void FreezeThunk(IntPtr clock)
    {
        var state = Lookup(clock);
        if (state != null)
        {
            state.FreezeCount++;
            if (state.InAfterPaint)
                state.FrozenFromAfterPaint = true;
        }
        if (s_origFreeze != null)
            s_origFreeze(clock);
    }

    [UnmanagedCallersOnly]
    private static void ThawThunk(IntPtr clock)
    {
        var state = Lookup(clock);
        if (state != null)
            ThawTracked(clock, state);
        else if (s_origThaw != null)
            s_origThaw(clock);
    }

    [UnmanagedCallersOnly]
    private static void AfterPaintEnter(IntPtr clock, IntPtr userData)
    {
        var state = Lookup(clock);
        if (state != null)
            state.InAfterPaint = true;
    }

    [UnmanagedCallersOnly]
    private static void AfterPaintLeave(IntPtr clock, IntPtr userData)
    {
        var state = Lookup(clock);
        if (state != null)
            state.InAfterPaint = false;
    }

    private const int GConnectAfter = 1; // G_CONNECT_AFTER

    // The ABI-stable vtable every GObject class begins with. We name only the slots we patch (dispose, constructed);
    // the rest are opaque pointer-sized slots so the field offsets line up. The trailing private fields matter:
    // GdkFrameClockClass embeds a WHOLE GObjectClass, so its freeze/thaw sit AFTER all of these.
    [StructLayout(LayoutKind.Sequential)]
    private struct GObjectClass
    {
        public IntPtr GTypeClass; // GTypeClass.g_type — the class's GType
        public IntPtr ConstructProperties;
        public IntPtr Constructor;
        public IntPtr SetProperty;
        public IntPtr GetProperty;
        public IntPtr Dispose;
        public IntPtr Finalize;
        public IntPtr DispatchPropertiesChanged;
        public IntPtr Notify;
        public IntPtr Constructed;
        public IntPtr Flags;
        public IntPtr NConstructProperties;
        public IntPtr Pspecs;
        public IntPtr NPspecs;
        public IntPtr PDummy0;
        public IntPtr PDummy1;
        public IntPtr PDummy2;
    }

    // A full GObjectClass followed by the GdkFrameClock vfuncs. We patch freeze/thaw here and constructed/dispose
    // in the parent.
    [StructLayout(LayoutKind.Sequential)]
    private struct GdkFrameClockClass
    {
        public GObjectClass ParentClass;
        public IntPtr GetFrameTime;
        public IntPtr RequestPhase;
        public IntPtr BeginUpdating;
        public IntPtr EndUpdating;
        public IntPtr Freeze;
        public IntPtr Thaw;
    }

    [DllImport("libgobject-2.0.so.0")]
    private static extern IntPtr g_type_class_ref(nuint type);

    [DllImport("libgobject-2.0.so.0", CharSet = CharSet.Ansi)]
    private static extern ulong g_signal_connect_data(IntPtr instance, string detailedSignal, IntPtr handler, IntPtr data, IntPtr destroy, int connectFlags);
}
