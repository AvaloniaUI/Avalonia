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
        private Key _repeatKey;
        private PhysicalKey _repeatPhysicalKey;
        private RawInputModifiers _repeatModifiers;
        private string? _repeatKeySymbol;
        private int _keyRepeatDelay;
        private int _keyRepeatRate;

        void IWSurfaceEventSink.OnKeyDown(ulong timestamp, Key key, RawInputModifiers modifiers,
            PhysicalKey physicalKey, string? keySymbol)
        {
            if (_inputRoot is null)
                return;
            ScheduleInput(new RawKeyEventArgs(_keyboard, timestamp, _inputRoot,
                RawKeyEventType.KeyDown, key, modifiers, physicalKey, keySymbol));
        }

        void IWSurfaceEventSink.OnKeyUp(ulong timestamp, Key key, RawInputModifiers modifiers, PhysicalKey physicalKey,
            string? keySymbol)
        {
            if (_inputRoot is null)
                return;
            ScheduleInput(new RawKeyEventArgs(_keyboard, timestamp, _inputRoot,
                RawKeyEventType.KeyUp, key, modifiers, physicalKey, keySymbol));
        }

        void IWSurfaceEventSink.OnKeyboardLeave()
        {
            if (_inputRoot is null)
                return;
            ScheduleInput(new RawKeyboardLeaveEventArgs(_keyboard, 0, _inputRoot));
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

            if (args is RawKeyEventArgs keyArgs)
            {
                if (keyArgs.Type == RawKeyEventType.KeyDown)
                {
                    StartKeyRepeat(keyArgs.Key, keyArgs.PhysicalKey, keyArgs.Modifiers, keyArgs.KeySymbol);
                }
                else if (keyArgs.Type == RawKeyEventType.KeyUp && _repeatKey == keyArgs.Key)
                {
                    StopKeyRepeat();
                }
            }

            return false;
        }

        private void StartKeyRepeat(Key key, PhysicalKey physicalKey, RawInputModifiers modifiers, string? keySymbol)
        {
            StopKeyRepeat();
            if (_keyRepeatRate <= 0)
                return;

            // Modifier keys don't repeat
            if (key is Key.LeftShift or Key.RightShift or Key.LeftCtrl or Key.RightCtrl
                or Key.LeftAlt or Key.RightAlt or Key.LWin or Key.RWin)
                return;

            _repeatKey = key;
            _repeatPhysicalKey = physicalKey;
            _repeatModifiers = modifiers;
            _repeatKeySymbol = keySymbol;

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
        }

        private void OnKeyRepeatTick(object? sender, EventArgs e)
        {
            if (_inputRoot is null || _keyRepeatTimer == null)
            {
                StopKeyRepeat();
                return;
            }

            // After first tick (initial delay), switch to repeat interval
            var intervalMs = Math.Max(1, 1000 / _keyRepeatRate);
            var repeatInterval = TimeSpan.FromMilliseconds(intervalMs);
            if (_keyRepeatTimer.Interval != repeatInterval)
                _keyRepeatTimer.Interval = repeatInterval;

            _p.Input?.Invoke(new RawKeyEventArgs(_keyboard, 0, _inputRoot,
                RawKeyEventType.KeyDown, _repeatKey, _repeatModifiers, _repeatPhysicalKey, _repeatKeySymbol));
        }
    }

    /// <summary>
    /// Internal event args for keyboard leave, routed through the event queue
    /// to be serialized with other input events.
    /// </summary>
    private class RawKeyboardLeaveEventArgs(IInputDevice device, ulong timestamp, IInputRoot root)
        : RawInputEventArgs(device, timestamp, root);
}
