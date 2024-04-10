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
        private DispatcherTimer? _timer;

        public ToolTipService(IInputManager inputManager)
        {
            _subscriptions = new CompositeDisposable(
                inputManager.Process.Subscribe(InputManager_OnProcess),
                ToolTip.ServiceEnabledProperty.Changed.Subscribe(ServiceEnabledChanged),
                ToolTip.TipProperty.Changed.Subscribe(TipChanged),
                ToolTip.IsOpenProperty.Changed.Subscribe(TipOpenChanged));
        }

        public void Dispose() => _subscriptions.Dispose();

        private void InputManager_OnProcess(RawInputEventArgs e)
        {
            if (e is RawPointerEventArgs pointerEvent)
            {
                switch (pointerEvent.Type)
                {
                    case RawPointerEventType.Move:
                        Update(pointerEvent.InputHitTestResult.element as Visual);
                        break;
                    case RawPointerEventType.LeftButtonDown:
                    case RawPointerEventType.RightButtonDown:
                    case RawPointerEventType.MiddleButtonDown:
                    case RawPointerEventType.XButton1Down:
                    case RawPointerEventType.XButton2Down:
                        StopTimer();
                        _tipControl?.ClearValue(ToolTip.IsOpenProperty);
                        break;
                }
            }
        }

        public void Update(Visual? candidateToolTipHost)
        {
            while (candidateToolTipHost != null)
            {
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

        private void TipOpenChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var control = (Control)e.Sender;

            if (e.OldValue is false && e.NewValue is true)
            {
                control.DetachedFromVisualTree += ControlDetaching;
                control.EffectiveViewportChanged += ControlEffectiveViewportChanged;
            }
            else if (e.OldValue is true && e.NewValue is false)
            {
                control.DetachedFromVisualTree -= ControlDetaching;
                control.EffectiveViewportChanged -= ControlEffectiveViewportChanged;
            }
        }

        private void ControlDetaching(object? sender, VisualTreeAttachmentEventArgs e)
        {
            var control = (Control)sender!;
            control.DetachedFromVisualTree -= ControlDetaching;
            control.EffectiveViewportChanged -= ControlEffectiveViewportChanged;
            Close(control);
        }

        private void OnTipControlChanged(Control? oldValue, Control? newValue)
        {
            StopTimer();

            if (oldValue != null)
            {
                // If the control is showing a tooltip and the pointer is over the tooltip, don't close it.
                if (oldValue.GetValue(ToolTip.ToolTipProperty) is not { IsPointerOver: true })
                    Close(oldValue);
            }

            if (newValue != null)
            {
                var showDelay = ToolTip.GetShowDelay(newValue);
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

        private void ControlEffectiveViewportChanged(object? sender, Layout.EffectiveViewportChangedEventArgs e)
        {
            var control = (Control)sender!;
            var toolTip = control.GetValue(ToolTip.ToolTipProperty);
            toolTip?.RecalculatePosition(control);
        }

        private void ToolTipClosed(object? sender, EventArgs e)
        {
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
            _timer.Tick += (o, e) => Open(control);
            _timer.Start();
        }

        private void Open(Control control)
        {
            StopTimer();

            if (control.IsAttachedToVisualTree)
            {
                ToolTip.SetIsOpen(control, true);

                if (control.GetValue(ToolTip.ToolTipProperty) is { } tooltip)
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
