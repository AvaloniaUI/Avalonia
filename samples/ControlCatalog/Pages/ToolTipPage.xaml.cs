using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Interactivity;

namespace ControlCatalog.Pages
{
    public partial class ToolTipPage : UserControl
    {
        public ToolTipPage()
        {
            InitializeComponent();
        }

        private void ToolTipOpening(object? sender, CancelRoutedEventArgs args)
        {
            ((Control)args.Source!).SetValue(ToolTip.TipProperty, "New tip set from ToolTipOpening.");
        }

        public void CustomPlacementCallback(CustomPopupPlacement placement)
        {
            var r = new Random().Next();

            placement.Anchor = (r % 4) switch
            {
                1 => PopupAnchor.Top,
                2 => PopupAnchor.Left,
                3 => PopupAnchor.Right,
                _ => PopupAnchor.Bottom,
            };
            placement.Gravity = (r % 4) switch
            {
                1 => PopupGravity.Top,
                2 => PopupGravity.Left,
                3 => PopupGravity.Right,
                _ => PopupGravity.Bottom,
            };
            placement.Offset = new Point(r % 20, r % 20);
        }
    }
}
