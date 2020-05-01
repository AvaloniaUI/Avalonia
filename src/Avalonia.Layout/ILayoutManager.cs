
namespace Avalonia.Layout
{
    /// <summary>
    /// Manages measuring and arranging of controls.
    /// </summary>
    public interface ILayoutManager
    {
        /// <summary>
        /// Notifies the layout manager that a control requires a measure.
        /// </summary>
        /// <param name="control">The control.</param>
        void InvalidateMeasure(ILayoutable control);

        /// <summary>
        /// Notifies the layout manager that a control requires an arrange.
        /// </summary>
        /// <param name="control">The control.</param>
        void InvalidateArrange(ILayoutable control);

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
        /// <param name="root">The control to lay out.</param>
        /// <remarks>
        /// You should not usually need to call this method explictly, the layout root will call
        /// it to carry out the initial layout of the control.
        /// </remarks>
        void ExecuteInitialLayoutPass(ILayoutRoot root);
    }
}
