using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;

namespace ControlCatalog.Pages
{
    public partial class DrawerPageCustomFlyoutPage : UserControl
    {
        private Ellipse? _bubble1;
        private Ellipse? _bubble2;
        private DispatcherTimer? _bubbleTimer;
        private double _bubblePhase;

        public DrawerPageCustomFlyoutPage()
        {
            InitializeComponent();
        }

        protected override async void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);

            _bubble1 = this.FindControl<Ellipse>("Bubble1");
            _bubble2 = this.FindControl<Ellipse>("Bubble2");

            DrawerPageControl.PropertyChanged += (_, args) =>
            {
                if (args.Property == DrawerPage.IsOpenProperty)
                    OnDrawerOpenChanged((bool)args.NewValue!);
            };

            await DetailNav.PushAsync(BuildDetailPage("Home"), null);
        }

        private Control[] MenuItems =>
            new Control[] { MenuItem1, MenuItem2, MenuItem3, MenuItem4, MenuItem5, FooterRow };

        private void OnDrawerOpenChanged(bool isOpen)
        {
            if (isOpen)
            {
                StartBubbles();

                foreach (var item in MenuItems)
                {
                    item.Opacity = 1.0;
                    if (item.RenderTransform is TranslateTransform tt)
                        tt.Y = 0;
                }
            }
            else
            {
                StopBubbles();

                foreach (var item in MenuItems)
                {
                    var savedItemT = item.Transitions;
                    item.Transitions = null;
                    item.Opacity = 0.0;
                    item.Transitions = savedItemT;

                    if (item.RenderTransform is TranslateTransform tt)
                    {
                        var savedTT = tt.Transitions;
                        tt.Transitions = null;
                        tt.Y = 25;
                        tt.Transitions = savedTT;
                    }
                }
            }
        }

        private void StartBubbles()
        {
            if (_bubbleTimer != null) return;
            _bubblePhase = 0;
            _bubbleTimer = new DispatcherTimer(DispatcherPriority.Render)
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };
            _bubbleTimer.Tick += OnBubbleTick;
            _bubbleTimer.Start();
        }

        private void StopBubbles()
        {
            if (_bubbleTimer == null) return;
            _bubbleTimer.Stop();
            _bubbleTimer.Tick -= OnBubbleTick;
            _bubbleTimer = null;

            if (_bubble1 != null) _bubble1.RenderTransform = null;
            if (_bubble2 != null) _bubble2.RenderTransform = null;
        }

        private void OnBubbleTick(object? sender, EventArgs e)
        {
            _bubblePhase += 0.012;

            if (_bubble1 != null)
                _bubble1.RenderTransform = new TranslateTransform(
                    x: Math.Sin(_bubblePhase * 0.65) * 10,
                    y: Math.Sin(_bubblePhase) * 14);

            if (_bubble2 != null)
                _bubble2.RenderTransform = new TranslateTransform(
                    x: Math.Sin(_bubblePhase * 0.45 + 1.8) * 7,
                    y: Math.Cos(_bubblePhase * 0.85 + 0.6) * 10);
        }

        private async void OnMenuItemClick(object? sender, RoutedEventArgs e)
        {
            if (sender is not Button button) return;
            var tag = button.Tag?.ToString() ?? "Home";

            DrawerPageControl.IsOpen = false;

            await DetailNav.ReplaceAsync(BuildDetailPage(tag), null);
        }

        private static ContentPage BuildDetailPage(string section)
        {
            var (iconPath, body) = section switch
            {
                "Home" =>
                    ("M10,20V14H14V20H19V12H22L12,3L2,12H5V20H10Z",
                     "Welcome back! Here is your dashboard with recent activity, quick actions, and personalized content."),
                "Explore" =>
                    ("M12,11.5A2.5,2.5 0 0,1 9.5,9A2.5,2.5 0 0,1 12,6.5A2.5,2.5 0 0,1 14.5,9A2.5,2.5 0 0,1 12,11.5M12,2A7,7 0 0,0 5,9C5,14.25 12,22 12,22C12,22 19,14.25 19,9A7,7 0 0,0 12,2Z",
                     "Discover new places, trending topics, and recommended content tailored to your interests."),
                "Messages" =>
                    ("M20,8L12,13L4,8V6L12,11L20,6M20,4H4C2.89,4 2,4.89 2,6V18A2,2 0 0,0 4,20H20A2,2 0 0,0 22,18V6C22,4.89 21.1,4 20,4Z",
                     "Your conversations and notifications. Stay connected with the people who matter."),
                "Profile" =>
                    ("M12,4A4,4 0 0,1 16,8A4,4 0 0,1 12,12A4,4 0 0,1 8,8A4,4 0 0,1 12,4M12,14C16.42,14 20,15.79 20,18V20H4V18C4,15.79 7.58,14 12,14Z",
                     "View and edit your profile, manage privacy settings, and control your account preferences."),
                "Settings" =>
                    ("M12,15.5A3.5,3.5 0 0,1 8.5,12A3.5,3.5 0 0,1 12,8.5A3.5,3.5 0 0,1 15.5,12A3.5,3.5 0 0,1 12,15.5M19.43,12.97C19.47,12.65 19.5,12.33 19.5,12C19.5,11.67 19.47,11.34 19.43,11L21.54,9.37C21.73,9.22 21.78,8.95 21.66,8.73L19.66,5.27C19.54,5.05 19.27,4.96 19.05,5.05L16.56,6.05C16.04,5.66 15.5,5.32 14.87,5.07L14.5,2.42C14.46,2.18 14.25,2 14,2H10C9.75,2 9.54,2.18 9.5,2.42L9.13,5.07C8.5,5.32 7.96,5.66 7.44,6.05L4.95,5.05C4.73,4.96 4.46,5.05 4.34,5.27L2.34,8.73C2.21,8.95 2.27,9.22 2.46,9.37L4.57,11C4.53,11.34 4.5,11.67 4.5,12C4.5,12.33 4.53,12.65 4.57,12.97L2.46,14.63C2.27,14.78 2.21,15.05 2.34,15.27L4.34,18.73C4.46,18.95 4.73,19.04 4.95,18.95L7.44,17.94C7.96,18.34 8.5,18.68 9.13,18.93L9.5,21.58C9.54,21.82 9.75,22 10,22H14C14.25,22 14.46,21.82 14.5,21.58L14.87,18.93C15.5,18.68 16.04,18.34 16.56,17.94L19.05,18.95C19.27,19.04 19.54,18.95 19.66,18.73L21.66,15.27C21.78,15.05 21.73,14.78 21.54,14.63L19.43,12.97Z",
                     "Configure application preferences, notifications, and privacy options."),
                _ => ("M12,2A10,10 0 0,1 22,12A10,10 0 0,1 12,22A10,10 0 0,1 2,12A10,10 0 0,1 12,2Z", "")
            };

            var page = new ContentPage { Header = section };
            NavigationPage.SetHasNavigationBar(page, false);

            var pathIcon = new PathIcon
            {
                Width = 52,
                Height = 52,
                Data = Geometry.Parse(iconPath),
            };
            pathIcon.SetValue(PathIcon.ForegroundProperty, new SolidColorBrush(Color.Parse("#0078D4")));

            var titleBlock = new TextBlock
            {
                Text = section,
                FontSize = 30,
                FontWeight = FontWeight.Bold,
            };

            var bodyBlock = new TextBlock
            {
                Text = body,
                FontSize = 14,
                Opacity = 0.75,
                TextWrapping = TextWrapping.Wrap,
            };

            var panel = new StackPanel { Margin = new Thickness(28, 24), Spacing = 16 };
            panel.Children.Add(pathIcon);
            panel.Children.Add(titleBlock);
            panel.Children.Add(bodyBlock);

            page.Content = new ScrollViewer { Content = panel };
            return page;
        }
    }
}
