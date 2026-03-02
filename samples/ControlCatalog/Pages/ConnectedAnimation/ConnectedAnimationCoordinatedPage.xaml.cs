using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace ControlCatalog.Pages
{
    public partial class ConnectedAnimationCoordinatedPage : UserControl
    {
        private static readonly (string Title, string Subtitle, string Image, string Description)[] Cards =
        {
            ("Mountains", "Alpine peaks", "avares://ControlCatalog/Assets/image1.jpg",
             "Majestic mountain ranges stretching across the horizon, with snow-capped peaks and alpine meadows creating a breathtaking landscape."),
            ("Ocean", "Deep blue", "avares://ControlCatalog/Assets/image2.jpg",
             "Crystal clear waters meeting white sandy shores, where the rhythm of waves creates an eternal symphony of nature."),
            ("Forest", "Dense canopy", "avares://ControlCatalog/Assets/image3.jpg",
             "Ancient trees forming a dense canopy overhead, filtering sunlight into golden beams that illuminate the forest floor."),
            ("Desert", "Golden dunes", "avares://ControlCatalog/Assets/image4.jpg",
             "Vast expanses of golden sand dunes shaped by wind, creating mesmerizing patterns under the blazing sun."),
            ("Sunset", "Warm hues", "avares://ControlCatalog/Assets/image5.jpg",
             "The sky painted in brilliant shades of orange, pink, and purple as the sun dips below the horizon."),
            ("Aurora", "Northern lights", "avares://ControlCatalog/Assets/image6.jpg",
             "Dancing curtains of green and violet light rippling across the arctic sky in a spectacular natural display."),
        };

        private int _lastIndex;
        private bool _isLoaded;

        public ConnectedAnimationCoordinatedPage()
        {
            InitializeComponent();

            for (int i = 0; i < Cards.Length; i++)
            {
                var (title, subtitle, imagePath, _) = Cards[i];
                CardGrid.Children.Add(CreateCard(i, title, subtitle, imagePath));
            }
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

        private Button CreateCard(int index, string title, string subtitle, string imagePath)
        {
            var image = new Image
            {
                Width = 180,
                Height = 120,
                Stretch = Stretch.UniformToFill,
            };

            try
            {
                var uri = new Uri(imagePath);
                image.Source = new Bitmap(AssetLoader.Open(uri));
            }
            catch
            {
            }

            var btn = new Button
            {
                Tag = index,
                Margin = new Thickness(0, 0, 16, 16),
                Padding = new Thickness(0),
                BorderThickness = new Thickness(0),
                Background = Brushes.Transparent,
                Content = new StackPanel
                {
                    Spacing = 4,
                    Children =
                    {
                        new Border
                        {
                            CornerRadius = new CornerRadius(8),
                            ClipToBounds = true,
                            Child = image
                        },
                        new TextBlock
                        {
                            Text = title,
                            FontSize = 14,
                            FontWeight = FontWeight.SemiBold
                        },
                        new TextBlock
                        {
                            Text = subtitle,
                            FontSize = 12,
                            Opacity = 0.5
                        }
                    }
                }
            };

            btn.Click += OnCardClick;
            return btn;
        }

        private void OnCardClick(object? sender, RoutedEventArgs e)
        {
            if (!_isLoaded || sender is not Button btn) return;
            int index = int.Parse(btn.Tag?.ToString() ?? "0");
            _lastIndex = index;

            var (title, subtitle, imagePath, description) = Cards[index];

            var cardContent = btn.Content as StackPanel;
            var imageBorder = cardContent?.Children[0] as Border;

            var service = GetService();
            if (imageBorder != null)
                service?.PrepareToAnimate("coordHero", imageBorder);

            try
            {
                var uri = new Uri(imagePath);
                DetailImage.Source = new Bitmap(AssetLoader.Open(uri));
            }
            catch
            {
            }

            DetailTitle.Text = title;
            DetailSubtitle.Text = subtitle;
            DetailDescription.Text = description;

            CardScrollViewer.IsVisible = false;
            DetailPanel.IsVisible = true;

            var animation = service?.GetAnimation("coordHero");
            animation?.TryStart(DetailImage, new Visual[] { CoordinatedPanel });
        }

        private async void OnBackClick(object? sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;

            var service = GetService();
            if (service != null)
            {
                var anim = service.PrepareToAnimate("coordBack", DetailImage);
                anim.Configuration = new DirectConnectedAnimationConfiguration();
            }

            CardScrollViewer.IsVisible = true;
            DetailPanel.IsVisible = false;

            await Task.Delay(16);

            if (_lastIndex >= 0 && _lastIndex < CardGrid.Children.Count)
            {
                var card = CardGrid.Children[_lastIndex] as Button;
                var cardContent = card?.Content as StackPanel;
                var imageBorder = cardContent?.Children[0] as Border;
                if (imageBorder != null)
                    service?.GetAnimation("coordBack")?.TryStart(imageBorder);
            }
        }
    }
}
