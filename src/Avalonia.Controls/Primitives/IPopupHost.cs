using System;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.VisualTree;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// Represents the top-level control opened by a <see cref="Popup"/>.
    /// </summary>
    /// <remarks>
    /// A popup host can be either be a popup window created by the operating system
    /// (<see cref="PopupRoot"/>) or an <see cref="OverlayPopupHost"/> which is created
    /// on an <see cref="OverlayLayer"/>.
    /// </remarks>
    public interface IPopupHost : IDisposable
    {
        /// <summary>
        /// Sets the control to display in the popup.
        /// </summary>
        /// <param name="control"></param>
        void SetChild(IControl control);

        /// <summary>
        /// Gets the presenter from the control's template.
        /// </summary>
        IContentPresenter Presenter { get; }

        /// <summary>
        /// Gets the root of the visual tree in the case where the popup is presented using a
        /// separate visual tree.
        /// </summary>
        IVisual HostedVisualTreeRoot { get; }

        /// <summary>
        /// Raised when the control's template is applied.
        /// </summary>
        event EventHandler<TemplateAppliedEventArgs> TemplateApplied;

        /// <summary>
        /// Configures the position of the popup according to a target control and a set of
        /// placement parameters.
        /// </summary>
        /// <param name="target">The placement target.</param>
        /// <param name="placement">The placement mode.</param>
        /// <param name="offset">The offset, in device-independent pixels.</param>
        /// <param name="anchor">The anchor point.</param>
        /// <param name="gravity">The popup gravity.</param>
        /// <param name="rect">
        /// The anchor rect. If null, the bounds of <paramref name="target"/> will be used.
        /// </param>
        void ConfigurePosition(IVisual target, PlacementMode placement, Point offset,
            PopupAnchor anchor = PopupAnchor.None,
            PopupGravity gravity = PopupGravity.None,
            PopupPositionerConstraintAdjustment constraintAdjustment = PopupPositionerConstraintAdjustment.All,
            Rect? rect = null);

        /// <summary>
        /// Shows the popup.
        /// </summary>
        void Show();

        /// <summary>
        /// Hides the popup.
        /// </summary>
        void Hide();

        /// <summary>
        /// Binds the constraints of the popup host to a set of properties, usally those present on
        /// <see cref="Popup"/>.
        /// </summary>
        IDisposable BindConstraints(AvaloniaObject popup, StyledProperty<double> widthProperty,
            StyledProperty<double> minWidthProperty, StyledProperty<double> maxWidthProperty,
            StyledProperty<double> heightProperty, StyledProperty<double> minHeightProperty,
            StyledProperty<double> maxHeightProperty, StyledProperty<bool> topmostProperty);
    }
}
