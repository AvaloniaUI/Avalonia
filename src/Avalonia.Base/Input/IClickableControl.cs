using System;
using Avalonia.Interactivity;

namespace Avalonia.Input
{
    /// <summary>
    /// 
    /// </summary>
    public interface IClickableControl
    {
        event EventHandler<RoutedEventArgs> Click;
        void RaiseClick();
        /// <summary>
        /// Gets a value indicating whether this control and all its parents are enabled.
        /// </summary>
        bool IsEffectivelyEnabled { get; }
    }
}
