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

        private DispatcherTimer _timer;

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
                control.PointerEnter -= ControlPointerEnter;
                control.PointerLeave -= ControlPointerLeave;
            }

            if (e.NewValue != null)
            {
                control.PointerEnter += ControlPointerEnter;
                control.PointerLeave += ControlPointerLeave;
            }
        }

        /// <summary>
        /// Called when the pointer enters a control with an attached tooltip.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private void ControlPointerEnter(object sender, PointerEventArgs e)
        {
            StopTimer();

            var control = (Control)sender;
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
        private void ControlPointerLeave(object sender, PointerEventArgs e)
        {
            var control = (Control)sender;
            Close(control);
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

            if ((control as IVisual).IsAttachedToVisualTree)
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
