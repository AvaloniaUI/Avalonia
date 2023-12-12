using System;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace Avalonia.Controls
{
    /// <summary>
    /// Handles <see cref="ToolTip"/> interaction with controls.
    /// </summary>
    internal sealed class ToolTipService
    {
        public static ToolTipService Instance { get; } = new ToolTipService();

        private DispatcherTimer? _timer;

        private ToolTipService() { }

        /// <summary>
        /// called when the <see cref="ToolTip.TipProperty"/> property changes on a control.
        /// </summary>
        /// <param name="e">The event args.</param>
        internal void TipChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var control = (Control)e.Sender;

            if (e.OldValue != null)
            {
                control.PointerEntered -= ControlPointerEntered;
                control.PointerExited -= ControlPointerExited;
                control.RemoveHandler(InputElement.PointerPressedEvent, ControlPointerPressed);
            }

            if (e.NewValue != null)
            {
                control.PointerEntered += ControlPointerEntered;
                control.PointerExited += ControlPointerExited;
                control.AddHandler(InputElement.PointerPressedEvent, ControlPointerPressed,
                    RoutingStrategies.Bubble | RoutingStrategies.Tunnel | RoutingStrategies.Direct, true);
            }

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

        internal void TipOpenChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var control = (Control)e.Sender;

            if (e.OldValue is false && e.NewValue is true)
            {
                control.DetachedFromVisualTree += ControlDetaching;
                control.EffectiveViewportChanged += ControlEffectiveViewportChanged;
            }
            else if(e.OldValue is true && e.NewValue is false)
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

        /// <summary>
        /// Called when the pointer enters a control with an attached tooltip.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private void ControlPointerEntered(object? sender, PointerEventArgs e)
        {
            StopTimer();

            var control = (Control)sender!;
            var showDelay = ToolTip.GetShowDelay(control);
            if (showDelay == 0)
            {
                Open(control);
            }
            else
            {
                StartShowTimer(showDelay, control);
            }
        }

        /// <summary>
        /// Called when the pointer leaves a control with an attached tooltip.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private void ControlPointerExited(object? sender, PointerEventArgs e)
        {
            var control = (Control)sender!;

            // If the control is showing a tooltip and the pointer is over the tooltip, don't close it.
            if (control.GetValue(ToolTip.ToolTipProperty) is { } tooltip && tooltip.IsPointerOver)
                return;

            Close(control);
        }

        private void ControlPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            StopTimer();
            (sender as AvaloniaObject)?.ClearValue(ToolTip.IsOpenProperty);
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
            // The pointer has exited the tooltip. Close the tooltip unless the pointer is over the
            // adorned control.
            if (sender is ToolTip toolTip &&
                toolTip.AdornedControl is { } control &&
                !control.IsPointerOver)
            {
                Close(control);
            }
        }

        private void StartShowTimer(int showDelay, Control control)
        {
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(showDelay) };
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
            StopTimer();

            ToolTip.SetIsOpen(control, false);
        }

        private void StopTimer()
        {
            _timer?.Stop();
            _timer = null;
        }
    }
}
