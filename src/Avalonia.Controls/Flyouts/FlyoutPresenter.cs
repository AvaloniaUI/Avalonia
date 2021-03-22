using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.LogicalTree;

namespace Avalonia.Controls
{
    public class FlyoutPresenter : ContentControl
    {
        public static readonly StyledProperty<CornerRadius> CornerRadiusProperty =
            AvaloniaProperty.Register<FlyoutPresenter, CornerRadius>(nameof(CornerRadius));

        public CornerRadius CornerRadius
        {
            get => GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                var host = this.FindLogicalAncestorOfType<Popup>();
                if (host != null)
                {
                    host.IsOpen = false;
                    e.Handled = true;
                }
            }

            base.OnKeyDown(e);            
        }
    }
}
