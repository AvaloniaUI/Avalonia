using System;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Avalonia.Controls
{
    /// <summary>
    /// Handles <see cref="ToolTip"/> interaction with controls.
    /// </summary>
    internal sealed class ToolTipService
    {
        public static ToolTipService Instance { get; } = new ToolTipService();

        private Point _startPosition;
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
                control.RemoveHandler(InputElement.PointerEnteredEvent, ControlPointerEntered);
                control.RemoveHandler(InputElement.PointerExitedEvent, ControlPointerExited);
                control.RemoveHandler(InputElement.PointerPressedEvent, ControlPointerPressed);
                control.RemoveHandler(InputElement.PointerReleasedEvent, ControlPointerReleased);
                control.RemoveHandler(InputElement.PointerMovedEvent, ControlPointerMoved);
            }

            if (e.NewValue != null)
            {
                control.AddHandler(InputElement.PointerEnteredEvent, ControlPointerEntered, handledEventsToo: true);
                control.AddHandler(InputElement.PointerExitedEvent, ControlPointerExited, handledEventsToo: true);
                control.AddHandler(InputElement.PointerPressedEvent, ControlPointerPressed, handledEventsToo: true);
                control.AddHandler(InputElement.PointerReleasedEvent, ControlPointerReleased, handledEventsToo: true);
                control.AddHandler(InputElement.PointerMovedEvent, ControlPointerMoved, handledEventsToo: true);
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
                StartShowTimer(showDelay, control, e);
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
            Close(control);
        }

        private void ControlPointerMoved(object? sender, PointerEventArgs e)
        {
            if (_timer is null)
                return;

            var control = (Control)sender!;
            var distanceLimit = ToolTip.GetShowDistanceLimit(control);

            if (double.IsNaN(distanceLimit) || distanceLimit < 0)
                return;

            var position = e.GetPosition(control);

            Vector moved = position - _startPosition;

            if (moved.Length > distanceLimit)
            {
                _startPosition = position;
                _timer.Stop();
                _timer.Start();
            }
        }

        private void ControlPointerReleased(object? sender, PointerReleasedEventArgs e)
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
                StartShowTimer(showDelay, control, e);
            }
        }

        private void ControlPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            StopTimer();
        }

        private void ControlEffectiveViewportChanged(object? sender, Layout.EffectiveViewportChangedEventArgs e)
        {
            var control = (Control)sender!;
            var toolTip = control.GetValue(ToolTip.ToolTipProperty);
            toolTip?.RecalculatePosition(control);
        }

        private void StartShowTimer(int showDelay, Control control, PointerEventArgs e)
        {
            _startPosition = e.GetPosition(control);
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
