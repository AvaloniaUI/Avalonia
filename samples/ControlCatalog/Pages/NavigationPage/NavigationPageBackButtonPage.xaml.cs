using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace ControlCatalog.Pages
{
    public partial class NavigationPageBackButtonPage : UserControl
    {
        private static readonly Color[] PageColors =
        {
            Color.Parse("#E3F2FD"), Color.Parse("#F3E5F5"), Color.Parse("#E8F5E9"),
            Color.Parse("#FFF3E0"), Color.Parse("#FCE4EC"), Color.Parse("#E0F7FA"),
        };

        private int _pushCount;
        private readonly List<string> _logs = new();

        public NavigationPageBackButtonPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object? sender, RoutedEventArgs e)
        {
            await DemoNav.PushAsync(CreatePage("Home", "Tap a push button on the right to see different back button styles.", null), null);
        }

        private void OnGlobalBackButtonChanged(object? sender, RoutedEventArgs e)
        {
            if (DemoNav == null)
                return;
            DemoNav.IsBackButtonVisible = IsBackButtonVisibleCheck.IsChecked == true;
            AddLog($"IsBackButtonVisible = {DemoNav.IsBackButtonVisible}");
        }

        private void OnPushStandard(object? sender, RoutedEventArgs e)
        {
            var page = CreatePage("Standard", "Default back button.", null);
            DemoNav.Push(page);
            AddLog("Pushed: standard back button");
        }

        private void OnPushNoBack(object? sender, RoutedEventArgs e)
        {
            var page = CreatePage("No Back", "Back button hidden.", null);
            NavigationPage.SetHasBackButton(page, false);
            DemoNav.Push(page);
            AddLog("Pushed: HasBackButton=false");
        }

        private void OnPushDisabledBack(object? sender, RoutedEventArgs e)
        {
            var page = CreatePage("Disabled Back", "Back button is disabled.", null);
            NavigationPage.SetIsBackButtonEnabled(page, false);
            DemoNav.Push(page);
            AddLog("Pushed: IsBackButtonEnabled=false");
        }

        private void OnPushCustomText(object? sender, RoutedEventArgs e)
        {
            var page = CreatePage("Custom Text", "Back button shows '← Cancel'.", null);
            NavigationPage.SetBackButtonContent(page, "← Cancel");
            DemoNav.Push(page);
            AddLog("Pushed: BackButtonContent='← Cancel'");
        }

        private void OnPushCustomIcon(object? sender, RoutedEventArgs e)
        {
            var page = CreatePage("Custom Icon", "Back button shows a close icon.", null);
            NavigationPage.SetBackButtonContent(page, new TextBlock
            {
                Text = "✕",
                FontSize = 16,
                VerticalAlignment = VerticalAlignment.Center
            });
            DemoNav.Push(page);
            AddLog("Pushed: BackButtonContent=custom icon");
        }

        private void OnPushGuarded(object? sender, RoutedEventArgs e)
        {
            var page = CreatePage("Guarded", "This page has a navigation guard.\nYou will be prompted before leaving.", null);
            page.Navigating += async args =>
            {
                args.Cancel = true;
                AddLog("Navigation blocked by guard!");
                await Task.Delay(1500);
                args.Cancel = false;
                AddLog("Guard released — navigation allowed");
            };
            DemoNav.Push(page);
            AddLog("Pushed: with navigation guard");
        }

        private async void OnPop(object? sender, RoutedEventArgs e) => await DemoNav.PopAsync();

        private void OnClearLog(object? sender, RoutedEventArgs e)
        {
            _logs.Clear();
            LogText.Text = string.Empty;
        }

        private void AddLog(string message)
        {
            _logs.Insert(0, message);
            if (_logs.Count > 6) _logs.RemoveAt(_logs.Count - 1);
            LogText.Text = string.Join('\n', _logs);
        }

        private ContentPage CreatePage(string title, string body, Color? bg)
        {
            _pushCount++;
            var color = bg ?? PageColors[_pushCount % PageColors.Length];
            return new ContentPage
            {
                Header = title,
                Background = new SolidColorBrush(color),
                Content = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Spacing = 8,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = title,
                            FontSize = 18,
                            FontWeight = FontWeight.SemiBold,
                            HorizontalAlignment = HorizontalAlignment.Center
                        },
                        new TextBlock
                        {
                            Text = body,
                            FontSize = 13,
                            Opacity = 0.7,
                            TextWrapping = TextWrapping.Wrap,
                            TextAlignment = TextAlignment.Center,
                            MaxWidth = 240
                        }
                    }
                },
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch
            };
        }
    }
}
