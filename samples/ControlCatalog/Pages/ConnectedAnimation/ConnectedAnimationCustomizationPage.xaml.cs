using System;
using System.Threading.Tasks;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace ControlCatalog.Pages
{
    public partial class ConnectedAnimationCustomizationPage : UserControl
    {
        private static readonly (string Name, IBrush Color)[] Items =
        {
            ("Red Item", Brushes.Red),
            ("Blue Item", Brushes.Blue),
            ("Purple Item", new SolidColorBrush(Avalonia.Media.Color.FromRgb(128, 0, 128))),
            ("Teal Item", Brushes.Teal),
        };

        private int _lastIndex;
        private bool _isLoaded;

        public ConnectedAnimationCustomizationPage()
        {
            InitializeComponent();

            DurationSlider.PropertyChanged += (_, e) =>
            {
                if (e.Property.Name == "Value" && DurationLabel != null)
                {
                    DurationLabel.Text = $"{(int)DurationSlider.Value} ms";
                    UpdateCurrentLabel();
                }
            };

            ConfigCombo.SelectionChanged += (_, _) =>
            {
                ShadowCheck.IsEnabled = ConfigCombo.SelectedIndex == 0;
                UpdateCurrentLabel();
            };
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

        private void UpdateCurrentLabel()
        {
            var configName = ConfigCombo.SelectedIndex switch
            {
                1 => "Direct",
                2 => "Basic",
                _ => "Gravity"
            };
            var text = $"{configName}, {(int)DurationSlider.Value}ms";
            CurrentLabel.Text = text;
            SubtitleLabel.Text = text;
        }

        private ConnectedAnimationConfiguration CreateConfig() => ConfigCombo.SelectedIndex switch
        {
            1 => new DirectConnectedAnimationConfiguration(),
            2 => new BasicConnectedAnimationConfiguration(),
            _ => new GravityConnectedAnimationConfiguration
            {
                IsShadowEnabled = ShadowCheck.IsChecked == true
            }
        };

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
            if (service != null)
            {
                service.DefaultDuration = TimeSpan.FromMilliseconds(DurationSlider.Value);
                var anim = service.PrepareToAnimate("customHero", sourceRect);
                anim.Configuration = CreateConfig();
            }

            DetailRect.Background = color;
            DetailTitle.Text = name;
            DetailSubtitle.Text = "Detail view";

            ListPanel.IsVisible = false;
            DetailPanel.IsVisible = true;

            var animation = service?.GetAnimation("customHero");
            animation?.TryStart(DetailRect, new Avalonia.Visual[] { DetailTitle, DetailSubtitle });
        }

        private async void OnBackClick(object? sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;

            var service = GetService();
            if (service != null)
            {
                var anim = service.PrepareToAnimate("customBack", DetailRect);
                anim.Configuration = new DirectConnectedAnimationConfiguration();
            }

            ListPanel.IsVisible = true;
            DetailPanel.IsVisible = false;

            await Task.Delay(16);

            var destRect = GetSourceRect(_lastIndex);
            service?.GetAnimation("customBack")?.TryStart(destRect);
        }
    }
}
