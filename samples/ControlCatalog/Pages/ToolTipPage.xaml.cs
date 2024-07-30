using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Markup.Xaml;

namespace ControlCatalog.Pages
{
    public class ToolTipPage : UserControl
    {
        public ToolTipPage()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void ToolTipOpening(object? sender, CancelRoutedEventArgs args)
        {
            ((Control)args.Source!).SetValue(ToolTip.TipProperty, "New tip set from ToolTipOpening.");
        }

        public CustomPopupPlacement CustomPlacementCallback(Size popupSize, Rect targetRect, Point offset)
        {
            var r = new Random().Next();
            return new CustomPopupPlacement
            {
                Anchor = (r % 4) switch
                {
                    1 => PopupAnchor.Top,
                    2 => PopupAnchor.Left,
                    3 => PopupAnchor.Right,
                    _ => PopupAnchor.Bottom,
                },
                Gravity = (r % 4) switch
                {
                    1 => PopupGravity.Top,
                    2 => PopupGravity.Left,
                    3 => PopupGravity.Right,
                    _ => PopupGravity.Bottom,
                },
                Offset = new Point(r % 20, r % 20)
            };
        }
    }
}
