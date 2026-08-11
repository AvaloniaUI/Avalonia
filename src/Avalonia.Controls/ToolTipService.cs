using System;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Reactive;
using Avalonia.Threading;

namespace Avalonia.Controls
{
    /// <summary>
    /// Handles <see cref="ToolTip"/> interaction with controls.
    /// </summary>
    internal sealed class ToolTipService : IToolTipService, IDisposable
    {
        private readonly IDisposable _subscriptions;

        private Control? _tipControl;
        private bool _pointerOverTip;
        private bool _pointerOverOwner;
        private int _pendingCloseId;
        private long _lastTipCloseTime;
        private DispatcherTimer? _timer;

        public ToolTipService(IInputManager inputManager)
        {
            _subscriptions = new CompositeDisposable(
                inputManager.Process.Subscribe(InputManager_OnProcess),
                ToolTip.ServiceEnabledProperty.Changed.Subscribe(ServiceEnabledChanged),
                ToolTip.TipProperty.Changed.Subscribe(TipChanged));
        }

        public void Dispose()
        {
            StopTimer();
            CancelPendingClose();
            _subscriptions.Dispose();
        }

        private void InputManager_OnProcess(RawInputEventArgs e)
        {
            if (e is RawPointerEventArgs pointerEvent)
            {
                var currentTip = _tipControl?.GetValue(ToolTip.ToolTipProperty);

                // The input root of the tip itself, for popup windows only, not overlays.
                var tipPopupInputRoot = (currentTip?.PopupHost as Visual)?.GetInputRoot() is { } root &&
                                   root.RootElement != _tipControl?.VisualRoot ?
                    root :
                    null;

                var isTipEvent = tipPopupInputRoot is not null && e.Root == tipPopupInputRoot;
                var isTipOwnerWindowEvent = e.Root.RootElement == _tipControl?.VisualRoot;

                if (isTipEvent || isTipOwnerWindowEvent)
                {
                    // The pointer is on one of the two windows (tip or owner) involved: remember which one, and cancel any
                    // close scheduled by a previous LeaveWindow on the other one.
                    if (pointerEvent.Type != RawPointerEventType.LeaveWindow)
                    {
                        _pointerOverTip = isTipEvent;
                        _pointerOverOwner = isTipOwnerWindowEvent;
                        CancelPendingClose();
                    }
                    else if (isTipEvent)
                    {
                        _pointerOverTip = false;
                    }
                    else
                    {
                        _pointerOverOwner = false;
                    }
                }

                switch (pointerEvent.Type)
                {
                    case RawPointerEventType.Move:
                        Update(pointerEvent.Root, pointerEvent.InputHitTestResult.element as Visual);
                        break;

                    case RawPointerEventType.LeaveWindow:
                        if ((isTipEvent && !_pointerOverOwner) || (isTipOwnerWindowEvent && !_pointerOverTip))
                        {
                            if (tipPopupInputRoot is not null)
                            {
                                // The pointer is leaving one of the two windows, but there's a chance it's going to
                                // the other one. Schedule a close and cancel it if we receive another event.
                                SchedulePendingClose();
                            }
                            else
                            {
                                CloseCurrentTip();
                            }
                        }

                        break;

                    case RawPointerEventType.LeftButtonDown:
                    case RawPointerEventType.RightButtonDown:
                    case RawPointerEventType.MiddleButtonDown:
                    case RawPointerEventType.XButton1Down:
                    case RawPointerEventType.XButton2Down:
                        ClearTip();
                        break;
                }

                void ClearTip()
                {
                    StopTimer();
                    _tipControl?.ClearValue(ToolTip.IsOpenProperty);
                    _pointerOverTip = false;
                }
            }
        }

        public void Update(IInputRoot root, Visual? candidateToolTipHost)
        {
            var currentToolTip = _tipControl?.GetValue(ToolTip.ToolTipProperty);

            if (root == currentToolTip?.PopupHost?.HostedVisualTreeRoot?.GetInputRoot())
            {
                // Don't update while the pointer is over a tooltip
                return;
            }

            while (candidateToolTipHost != null)
            {
                if (candidateToolTipHost == currentToolTip) // when OverlayPopupHost is in use, the tooltip is in the same window as the host control
                    return;

                if (candidateToolTipHost is Control control)
                {
                    if (!ToolTip.GetServiceEnabled(control))
                        return;

                    if (ToolTip.GetTip(control) != null && (control.IsEffectivelyEnabled || ToolTip.GetShowOnDisabled(control)))
                        break;
                }

                candidateToolTipHost = candidateToolTipHost?.VisualParent;
            }

            var newControl = candidateToolTipHost as Control;

            if (newControl == _tipControl)
            {
                return;
            }

            OnTipControlChanged(_tipControl, newControl);
            _tipControl = newControl;
            _pointerOverTip = false;
            _pointerOverOwner = false;
        }

        private void SchedulePendingClose()
        {
            var control = _tipControl;
            var closeId = ++_pendingCloseId;

            // Post below input priority: any pointer event is processed before this callback and gets a chance to cancel it.
            Dispatcher.UIThread.Post(
                () =>
                {
                    if (_pendingCloseId == closeId && _tipControl == control)
                        CloseCurrentTip();
                },
                DispatcherPriority.Background);
        }

        private void CancelPendingClose()
            => ++_pendingCloseId;

        private void CloseCurrentTip()
        {
            StopTimer();
            _tipControl?.ClearValue(ToolTip.IsOpenProperty);
            _tipControl = null;
            _pointerOverTip = false;
            _pointerOverOwner = false;
        }

        private void ServiceEnabledChanged(AvaloniaPropertyChangedEventArgs<bool> args)
        {
            if (args.Sender == _tipControl && !ToolTip.GetServiceEnabled(_tipControl))
            {
                StopTimer();
            }
        }

        /// <summary>
        /// called when the <see cref="ToolTip.TipProperty"/> property changes on a control.
        /// </summary>
        /// <param name="e">The event args.</param>
        private void TipChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var control = (Control)e.Sender;

            if (ToolTip.GetIsOpen(control) && e.NewValue != e.OldValue && !(e.NewValue is ToolTip))
            {
                if (e.NewValue is null)
                {
                    Close(control);
                }
                else
                {
                    if (control.GetValue(ToolTip.ToolTipProperty) is { } tip)
                    {
                        tip.Content = e.NewValue;
                    }
                }
            }
        }

        private void OnTipControlChanged(Control? oldValue, Control? newValue)
        {
            StopTimer();

            var closedPreviousTip = false; // avoid race conditions by remembering whether we closed a tooltip in the current call.

            if (oldValue != null && ToolTip.GetIsOpen(oldValue))
            {
                Close(oldValue);
                closedPreviousTip = true;
            }

            if (newValue != null && !ToolTip.GetIsOpen(newValue))
            {
                var betweenShowDelay = ToolTip.GetBetweenShowDelay(newValue);

                int showDelay;

                if (betweenShowDelay >= 0 && (closedPreviousTip || (DateTime.UtcNow.Ticks - _lastTipCloseTime) <= betweenShowDelay * TimeSpan.TicksPerMillisecond))
                {
                    showDelay = 0;
                }
                else
                {
                    showDelay = ToolTip.GetShowDelay(newValue);
                }

                if (showDelay == 0)
                {
                    Open(newValue);
                }
                else
                {
                    StartShowTimer(showDelay, newValue);
                }
            }
        }

        private void ToolTipClosed(object? sender, EventArgs e)
        {
            _lastTipCloseTime = DateTime.UtcNow.Ticks;
            if (sender is ToolTip toolTip)
            {
                toolTip.Closed -= ToolTipClosed;
                toolTip.PointerExited -= ToolTipPointerExited;
            }
        }

        private void ToolTipPointerExited(object? sender, PointerEventArgs e)
        {
            // The pointer has exited the tooltip. Close the tooltip unless the current tooltip source is still the
            // adorned control.
            if (sender is ToolTip { AdornedControl: { } control } && control != _tipControl)
            {
                Close(control);
            }
        }

        private void StartShowTimer(int showDelay, Control control)
        {
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(showDelay), Tag = (this, control) };
            _timer.Tick += (o, e) =>
            {
                if (_timer != null)
                    Open(control);
            };
            _timer.Start();
        }

        private void Open(Control control)
        {
            StopTimer();

            if (control.IsAttachedToVisualTree)
            {
                ToolTip.SetIsOpen(control, true);

                // Value can be coerced back to false, need to double check.
                if (ToolTip.GetIsOpen(control) && control.GetValue(ToolTip.ToolTipProperty) is { } tooltip)
                {
                    tooltip.Closed += ToolTipClosed;
                    tooltip.PointerExited += ToolTipPointerExited;
                }
            }
        }

        private void Close(Control control)
        {
            ToolTip.SetIsOpen(control, false);
        }

        private void StopTimer()
        {
            _timer?.Stop();
            _timer = null;
        }
    }
}
