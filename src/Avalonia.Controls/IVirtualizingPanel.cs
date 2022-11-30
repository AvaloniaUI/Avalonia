using Avalonia.Layout;

namespace Avalonia.Controls
{
    /// <summary>
    /// A panel that can be used to virtualize items.
    /// </summary>
    public interface IVirtualizingPanel
    {
        /// <summary>
        /// Gets the children of the panel.
        /// </summary>
        Controls Children { get; }

        /// <summary>
        /// Gets or sets the controller for the virtualizing panel.
        /// </summary>
        /// <remarks>
        /// A virtualizing controller is responsible for maintaining the controls in the virtualizing
        /// panel. This property will be set by the controller when virtualization is initialized.
        /// Note that this property may remain null if the panel is added to a control that does
        /// not act as a virtualizing controller.
        /// </remarks>
        IVirtualizingController? Controller { get; set; }

        /// <summary>
        /// Gets a value indicating whether the panel is full.
        /// </summary>
        /// <remarks>
        /// This property should return false until enough children are added to fill the space
        /// passed into the last measure or arrange in the direction of scroll. It should be
        /// updated immediately after a child is added or removed.
        /// </remarks>
        bool IsFull { get; }

        /// <summary>
        /// Gets the number of items that can be removed while keeping the panel full.
        /// </summary>
        /// <remarks>
        /// This property should return the number of children that are completely out of the
        /// panel's current bounds in the direction of scroll. It should be updated after an
        /// arrange.
        /// </remarks>
        int OverflowCount { get; }

        /// <summary>
        /// Gets the direction of scroll.
        /// </summary>
        Orientation ScrollDirection { get; }

        /// <summary>
        /// Gets the average size of the materialized items in the direction of scroll.
        /// </summary>
        double AverageItemSize { get; }

        /// <summary>
        /// Gets or sets a size in pixels by which the content is overflowing the panel, in the
        /// direction of scroll.
        /// </summary>
        /// <remarks>
        /// This may be non-zero even when <see cref="OverflowCount"/> is zero if the last item
        /// overflows the panel bounds.
        /// </remarks>
        double PixelOverflow { get; }

        /// <summary>
        /// Gets or sets the current pixel offset of the items in the direction of scroll.
        /// </summary>
        double PixelOffset { get; set; }

        /// <summary>
        /// Gets or sets the current scroll offset in the cross axis.
        /// </summary>
        double CrossAxisOffset { get; set; }

        /// <summary>
        /// Invalidates the measure of the control and forces a call to 
        /// <see cref="IVirtualizingController.UpdateControls"/> on the next measure.
        /// </summary>
        /// <remarks>
        /// The implementation for this method should call
        /// <see cref="Layoutable.InvalidateMeasure"/> and also ensure that the next call to
        /// <see cref="Layoutable.Measure(Size)"/> calls
        /// <see cref="IVirtualizingController.UpdateControls"/> on the next measure even if
        /// the available size hasn't changed.
        /// </remarks>
        void ForceInvalidateMeasure();
    }
}
