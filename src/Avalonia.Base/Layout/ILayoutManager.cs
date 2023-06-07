using System;
using Avalonia.Metadata;

namespace Avalonia.Layout
{
    /// <summary>
    /// Manages measuring and arranging of controls.
    /// </summary>
    [PrivateApi]
    public interface ILayoutManager : IDisposable
    {
        /// <summary>
        /// Raised when the layout manager completes a layout pass.
        /// </summary>
        event EventHandler LayoutUpdated;

        /// <summary>
        /// Notifies the layout manager that a control requires a measure.
        /// </summary>
        /// <param name="control">The control.</param>
        void InvalidateMeasure(Layoutable control);

        /// <summary>
        /// Notifies the layout manager that a control requires an arrange.
        /// </summary>
        /// <param name="control">The control.</param>
        void InvalidateArrange(Layoutable control);

        /// <summary>
        /// Executes a layout pass.
        /// </summary>
        /// <remarks>
        /// You should not usually need to call this method explictly, the layout manager will
        /// schedule layout passes itself.
        /// </remarks>
        void ExecuteLayoutPass();

        /// <summary>
        /// Executes the initial layout pass on a layout root.
        /// </summary>
        /// <remarks>
        /// You should not usually need to call this method explictly, the layout root will call
        /// it to carry out the initial layout of the control.
        /// </remarks>
        void ExecuteInitialLayoutPass();

        /// <summary>
        /// Registers a control as wanting to receive effective viewport notifications.
        /// </summary>
        /// <param name="control">The control.</param>
        void RegisterEffectiveViewportListener(Layoutable control);

        /// <summary>
        /// Registers a control as no longer wanting to receive effective viewport notifications.
        /// </summary>
        /// <param name="control">The control.</param>
        void UnregisterEffectiveViewportListener(Layoutable control);
    }
}
