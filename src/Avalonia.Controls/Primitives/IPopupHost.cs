using System;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Metadata;
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
    [NotClientImplementable]
    public interface IPopupHost : IDisposable, IFocusScope
    {
        /// <summary>
        /// Gets or sets the fixed width of the popup.
        /// </summary>
        double Width { get; set; }

        /// <summary>
        /// Gets or sets the minimum width of the popup.
        /// </summary>
        double MinWidth { get; set; }

        /// <summary>
        /// Gets or sets the maximum width of the popup.
        /// </summary>
        double MaxWidth { get; set; }

        /// <summary>
        /// Gets or sets the fixed height of the popup.
        /// </summary>
        double Height { get; set; }

        /// <summary>
        /// Gets or sets the minimum height of the popup.
        /// </summary>
        double MinHeight { get; set; }

        /// <summary>
        /// Gets or sets the maximum height of the popup.
        /// </summary>
        double MaxHeight { get; set; }

        /// <summary>
        /// Gets the presenter from the control's template.
        /// </summary>
        ContentPresenter? Presenter { get; }

        /// <summary>
        /// Gets or sets whether the popup appears on top of all other windows.
        /// </summary>
        bool Topmost { get; set; }

        /// <summary>
        /// Gets or sets a transform that will be applied to the popup.
        /// </summary>
        Transform? Transform { get; set; }

        /// <summary>
        /// Gets the root of the visual tree in the case where the popup is presented using a
        /// separate visual tree.
        /// </summary>
        Visual? HostedVisualTreeRoot { get; }

        /// <summary>
        /// Raised when the control's template is applied.
        /// </summary>
        event EventHandler<TemplateAppliedEventArgs>? TemplateApplied;

        /// <summary>
        /// Configures the position of the popup according to a target control and a set of
        /// placement parameters.
        /// </summary>
        /// <param name="target">The placement target.</param>
        /// <param name="placement">The placement mode.</param>
        /// <param name="offset">The offset, in device-independent pixels.</param>
        /// <param name="anchor">The anchor point.</param>
        /// <param name="gravity">The popup gravity.</param>
        /// <param name="constraintAdjustment">Defines how a popup position will be adjusted if the unadjusted position would result in the popup being partly constrained.</param>
        /// <param name="rect">
        /// The anchor rect. If null, the bounds of <paramref name="target"/> will be used.
        /// </param>
        void ConfigurePosition(Visual target, PlacementMode placement, Point offset,
            PopupAnchor anchor = PopupAnchor.None,
            PopupGravity gravity = PopupGravity.None,
            PopupPositionerConstraintAdjustment constraintAdjustment = PopupPositionerConstraintAdjustment.All,
            Rect? rect = null);

        /// <summary>
        /// Sets the control to display in the popup.
        /// </summary>
        /// <param name="control"></param>
        void SetChild(Control? control);

        /// <summary>
        /// Shows the popup.
        /// </summary>
        void Show();

        /// <summary>
        /// Hides the popup.
        /// </summary>
        void Hide();
    }
}
