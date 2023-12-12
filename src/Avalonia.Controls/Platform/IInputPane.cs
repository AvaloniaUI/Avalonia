using System;
using Avalonia.Animation.Easings;
using Avalonia.Metadata;

namespace Avalonia.Controls.Platform
{
    /// <summary>
    /// Listener for the platform's input pane(eg, software keyboard). Provides access to the input pane height and state.
    /// </summary>
    [NotClientImplementable]
    public interface IInputPane
    {
        /// <summary>
        /// The current input pane state
        /// </summary>
        InputPaneState State { get; }

        /// <summary>
        /// The current input pane bounds.
        /// </summary>
        Rect OccludedRect { get; }

        /// <summary>
        /// Occurs when the input pane's state has changed.
        /// </summary>
        event EventHandler<InputPaneStateEventArgs>? StateChanged;
    }

    /// <summary>
    /// The input pane opened state.
    /// </summary>
    public enum InputPaneState
    {
        /// <summary>
        /// The input pane is either closed, or doesn't form part of the platform insets, i.e. it's floating or is an overlay.
        /// </summary>
        Closed,

        /// <summary>
        /// The input pane is open.
        /// </summary>
        Open
    }

    /// <summary>
    /// Provides state change information about the input pane.
    /// </summary>
    public sealed class InputPaneStateEventArgs : EventArgs
    {
        /// <summary>
        /// The new state of the input pane
        /// </summary>
        public InputPaneState NewState { get; }

        /// <summary>
        /// The initial bounds of the input pane.
        /// </summary>
        public Rect? StartRect { get; }

        /// <summary>
        /// The final bounds of the input pane.
        /// </summary>
        public Rect EndRect { get; }

        /// <summary>
        /// The duration of the input pane's state change animation.
        /// </summary>
        public TimeSpan AnimationDuration { get; }

        /// <summary>
        /// The easing of the input pane's state changed animation.
        /// </summary>
        public IEasing? Easing { get; }

        public InputPaneStateEventArgs(InputPaneState newState, Rect? startRect, Rect endRect, TimeSpan animationDuration, IEasing? easing)
        {
            NewState = newState;
            StartRect = startRect;
            EndRect = endRect;
            AnimationDuration = animationDuration;
            Easing = easing;
        }

        public InputPaneStateEventArgs(InputPaneState newState, Rect? startRect, Rect endRect)
            : this(newState, startRect, endRect, default, null)
        {
        }
    }
}
