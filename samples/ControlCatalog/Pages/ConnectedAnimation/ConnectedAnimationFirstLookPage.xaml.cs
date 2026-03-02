using System.Threading.Tasks;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace ControlCatalog.Pages
{
    public partial class ConnectedAnimationFirstLookPage : UserControl
    {
        private static readonly (string Name, IBrush Color)[] Items =
        {
            ("Red Item", Brushes.Red),
            ("Blue Item", Brushes.Blue),
            ("Green Item", Brushes.Green),
            ("Orange Item", Brushes.Orange),
        };

        private int _lastIndex;
        private bool _isLoaded;

        public ConnectedAnimationFirstLookPage()
        {
            InitializeComponent();
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
            _isLoaded = true;
        }

        private ConnectedAnimationService? GetService()
        {
            var topLevel = TopLevel.GetTopLevel(this);
            return topLevel != null ? ConnectedAnimationService.GetForCurrentView(topLevel) : null;
        }

        private Border GetSourceRect(int index) => index switch
        {
            0 => Rect1,
            1 => Rect2,
            2 => Rect3,
            _ => Rect4,
        };

        private void OnItemClick(object? sender, RoutedEventArgs e)
        {
            if (!_isLoaded || sender is not Button btn) return;
            int index = int.Parse(btn.Tag?.ToString() ?? "0");
            _lastIndex = index;

            var (name, color) = Items[index];
            var sourceRect = GetSourceRect(index);

            var service = GetService();
            service?.PrepareToAnimate("firstLookHero", sourceRect);

            DetailRect.Background = color;
            DetailTitle.Text = name;
            DetailSubtitle.Text = "Detail view";

            ListPanel.IsVisible = false;
            DetailPanel.IsVisible = true;

            var animation = service?.GetAnimation("firstLookHero");
            animation?.TryStart(DetailRect, new Avalonia.Visual[] { DetailTitle, DetailSubtitle });
        }

        private async void OnBackClick(object? sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;

            var service = GetService();
            if (service != null)
            {
                var anim = service.PrepareToAnimate("firstLookBack", DetailRect);
                anim.Configuration = new DirectConnectedAnimationConfiguration();
            }

            ListPanel.IsVisible = true;
            DetailPanel.IsVisible = false;

            await Task.Delay(16);

            var destRect = GetSourceRect(_lastIndex);
            service?.GetAnimation("firstLookBack")?.TryStart(destRect);
        }
    }
}
