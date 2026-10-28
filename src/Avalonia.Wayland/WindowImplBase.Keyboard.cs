using System;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Threading;
using Avalonia.Wayland.Server.Persistent;

namespace Avalonia.Wayland;

partial class WindowBaseImpl
{
    partial class Sink
    {
        // Key repeat state (managed on UI thread via DispatcherTimer)
        private DispatcherTimer? _keyRepeatTimer;
        private WaylandKeyInfo? _repeatKey;
        private RawInputModifiers _repeatModifiers;
        private int _keyRepeatDelay;
        private int _keyRepeatRate;

        void IWSurfaceEventSink.OnKeyDown(ulong timestamp, RawInputModifiers modifiers, WaylandKeyInfo key)
        {
            if (InputRoot is null)
                return;
            ScheduleInput(CreateKeyEventArgs(timestamp, RawKeyEventType.KeyDown, modifiers, key));
        }

        void IWSurfaceEventSink.OnKeyUp(ulong timestamp, RawInputModifiers modifiers, WaylandKeyInfo key)
        {
            if (InputRoot is null)
                return;
            ScheduleInput(CreateKeyEventArgs(timestamp, RawKeyEventType.KeyUp, modifiers, key));
        }

        void IWSurfaceEventSink.OnHeldKeyChanged(RawInputModifiers modifiers, WaylandKeyInfo key)
        {
            if (InputRoot is null)
                return;
            // Queued like the key down, so that the update can't overtake the key down that starts the repeat
            ScheduleInput(new RawHeldKeyChangedEventArgs(Keyboard, InputRoot, modifiers, key));
        }

        private RawKeyEventArgs CreateKeyEventArgs(ulong timestamp, RawKeyEventType type,
            RawInputModifiers modifiers, WaylandKeyInfo key, bool isRepeat = false)
        {
            var args = isRepeat ?
                new RepeatedRawKeyEventArgs(Keyboard, timestamp, InputRoot!, type, key.Key, modifiers,
                    key.PhysicalKey, key.KeySymbol) :
                new RawKeyEventArgs(Keyboard, timestamp, InputRoot!, type, key.Key, modifiers,
                    key.PhysicalKey, key.KeySymbol);
            args.KeyText = key.KeyText;
            args.KeyTextIgnoringModifiers = key.KeyTextIgnoringModifiers;
            args.ConsumedModifiers = key.ConsumedModifiers;
            args.PlatformKeyInfo = key.PlatformKeyInfo;
            return args;
        }

        void IWSurfaceEventSink.OnKeyboardLeave()
        {
            if (InputRoot is null)
                return;
            ScheduleInput(new RawKeyboardLeaveEventArgs(Keyboard, 0, InputRoot));
        }

        void IWSurfaceEventSink.OnKeyRepeatInfo(int rate, int delay)
        {
            _keyRepeatRate = rate;
            _keyRepeatDelay = delay;
        }

        /// <summary>
        /// Called from DispatchInput on the UI thread to handle keyboard events for key repeat.
        /// Returns true if the event is fully handled and should not be dispatched further.
        /// </summary>
        protected bool HandleKeyboardDispatch(RawInputEventArgs args)
        {
            if (args is RawKeyboardLeaveEventArgs)
            {
                StopKeyRepeat();
                return true;
            }

            if (args is RawHeldKeyChangedEventArgs held)
            {
                if (_repeatKey?.PlatformKeyInfo.ScanCode == held.Key.PlatformKeyInfo.ScanCode)
                {
                    _repeatKey = held.Key;
                    _repeatModifiers = held.Modifiers;
                }
                return true;
            }

            if (args is RawKeyEventArgs keyArgs and not RepeatedRawKeyEventArgs)
            {
                if (keyArgs.Type == RawKeyEventType.KeyDown)
                {
                    StartKeyRepeat(keyArgs);
                }
                else if (keyArgs.Type == RawKeyEventType.KeyUp
                         && _repeatKey?.PlatformKeyInfo.ScanCode == keyArgs.PlatformKeyInfo?.ScanCode)
                {
                    StopKeyRepeat();
                }
            }

            return false;
        }

        private void StartKeyRepeat(RawKeyEventArgs keyArgs)
        {
            StopKeyRepeat();
            if (_keyRepeatRate <= 0)
                return;

            // Modifier keys don't repeat
            if (keyArgs.Key is Key.LeftShift or Key.RightShift or Key.LeftCtrl or Key.RightCtrl
                or Key.LeftAlt or Key.RightAlt or Key.LWin or Key.RWin)
                return;

            if (keyArgs.PlatformKeyInfo is not { } platformKeyInfo)
                return;

            _repeatKey = new WaylandKeyInfo(keyArgs.Key, keyArgs.PhysicalKey, keyArgs.KeySymbol, keyArgs.KeyText,
                keyArgs.KeyTextIgnoringModifiers, keyArgs.ConsumedModifiers, platformKeyInfo);
            _repeatModifiers = keyArgs.Modifiers;

            var intervalMs = Math.Max(1, 1000 / _keyRepeatRate);
            _keyRepeatTimer = new DispatcherTimer(
                TimeSpan.FromMilliseconds(intervalMs),
                // Ensure it doesn't block any actual input events that would stop the repeat
                DispatcherPriority.Input - 1,
                Dispatcher.UIThread);
            _keyRepeatTimer.Tick += OnKeyRepeatTick;

            // Start with the initial delay, then switch to repeat interval on first tick
            _keyRepeatTimer.Interval = TimeSpan.FromMilliseconds(_keyRepeatDelay);
            _keyRepeatTimer.Start();
        }

        protected void StopKeyRepeat()
        {
            if (_keyRepeatTimer != null)
            {
                _keyRepeatTimer.Stop();
                _keyRepeatTimer.Tick -= OnKeyRepeatTick;
                _keyRepeatTimer = null;
            }
            _repeatKey = null;
        }

        private void OnKeyRepeatTick(object? sender, EventArgs e)
        {
            if (InputRoot is null || _keyRepeatTimer == null || _repeatKey == null)
            {
                StopKeyRepeat();
                return;
            }

            // After first tick (initial delay), switch to repeat interval
            var intervalMs = Math.Max(1, 1000 / _keyRepeatRate);
            var repeatInterval = TimeSpan.FromMilliseconds(intervalMs);
            if (_keyRepeatTimer.Interval != repeatInterval)
                _keyRepeatTimer.Interval = repeatInterval;

            DispatchInput(CreateKeyEventArgs(0, RawKeyEventType.KeyDown, _repeatModifiers, _repeatKey, true));
        }
    }

    private class RepeatedRawKeyEventArgs(
        IKeyboardDevice device,
        ulong timestamp,
        IInputRoot root,
        RawKeyEventType type,
        Key key,
        RawInputModifiers modifiers,
        PhysicalKey physicalKey,
        string? keySymbol)
        : RawKeyEventArgs(device, timestamp, root, type, key, modifiers, physicalKey, keySymbol);

    /// <summary>
    /// Internal event args for keyboard leave, routed through the event queue
    /// to be serialized with other input events.
    /// </summary>
    private class RawKeyboardLeaveEventArgs(IInputDevice device, ulong timestamp, IInputRoot root)
        : RawInputEventArgs(device, timestamp, root);

    /// <summary>
    /// Internal event args for a modifier change while a key is held, routed through the event queue.
    /// </summary>
    private class RawHeldKeyChangedEventArgs(IInputDevice device, IInputRoot root, RawInputModifiers modifiers,
        WaylandKeyInfo key) : RawInputEventArgs(device, 0, root)
    {
        public RawInputModifiers Modifiers { get; } = modifiers;
        public WaylandKeyInfo Key { get; } = key;
    }
}
