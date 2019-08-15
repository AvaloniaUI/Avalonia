using System;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.VisualTree;

namespace Avalonia.Controls.Primitives
{
    public interface IPopupHost : IDisposable
    {
        void SetChild(IControl control);
        IContentPresenter Presenter { get; }
        IVisual HostedVisualTreeRoot { get; }

        event EventHandler<TemplateAppliedEventArgs> TemplateApplied;

        void ConfigurePosition(IVisual target, PlacementMode placement, Point offset,
            PopupPositioningEdge anchor = PopupPositioningEdge.None,
            PopupPositioningEdge gravity = PopupPositioningEdge.None);
        void Show();
        void Hide();
        IDisposable BindConstraints(AvaloniaObject popup, StyledProperty<double> widthProperty,
            StyledProperty<double> minWidthProperty, StyledProperty<double> maxWidthProperty,
            StyledProperty<double> heightProperty, StyledProperty<double> minHeightProperty,
            StyledProperty<double> maxHeightProperty, StyledProperty<bool> topmostProperty);
    }
}
