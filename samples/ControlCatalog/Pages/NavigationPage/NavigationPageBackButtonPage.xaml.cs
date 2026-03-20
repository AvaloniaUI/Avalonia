using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace ControlCatalog.Pages
{
    public partial class NavigationPageBackButtonPage : UserControl
    {
        private bool _initialized;
        private int _pushCount;

        public NavigationPageBackButtonPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object? sender, RoutedEventArgs e)
        {
            if (_initialized)
                return;

            _initialized = true;
            DemoNav.Pushed += (s, ev) => AddLog($"Pushed: \"{ev.Page?.Header}\"");
            DemoNav.Popped += (s, ev) => AddLog($"Popped: \"{ev.Page?.Header}\"");

            await DemoNav.PushAsync(CreatePage("Home", "This is the root page.\nNo back button is shown here.\n\nPush pages from the config panel\nto explore back button behaviors.", null), null);
        }

        private void OnGlobalBackButtonChanged(object? sender, RoutedEventArgs e)
        {
            if (DemoNav == null)
                return;
            DemoNav.IsBackButtonVisible = IsBackButtonVisibleCheck.IsChecked == true;
            AddLog($"IsBackButtonVisible={DemoNav.IsBackButtonVisible}");
        }

        private async void OnPushStandard(object? sender, RoutedEventArgs e)
        {
            var page = CreatePage($"Page {_pushCount + 1}", "Standard page with default back arrow.", null);
            await DemoNav.PushAsync(page);
        }

        private async void OnPushNoBack(object? sender, RoutedEventArgs e)
        {
            var page = CreatePage($"No Back #{_pushCount + 1}", "IsBackButtonVisible = false\n\nThe back arrow is hidden.\nUse the Pop button to go back.", null);
            NavigationPage.SetHasBackButton(page, false);
            await DemoNav.PushAsync(page);
            AddLog($"HasBackButton=false on \"{page.Header}\"");
        }

        private async void OnPushDisabledBack(object? sender, RoutedEventArgs e)
        {
            var page = CreatePage($"Disabled Back #{_pushCount + 1}", "IsBackButtonEnabled = false\n\nThe back arrow is visible but disabled.\nUse the Pop button to go back.", null);
            NavigationPage.SetIsBackButtonEnabled(page, false);
            await DemoNav.PushAsync(page);
            AddLog($"IsBackButtonEnabled=false on \"{page.Header}\"");
        }

        private async void OnPushCustomText(object? sender, RoutedEventArgs e)
        {
            var text = string.IsNullOrWhiteSpace(BackContentInput?.Text) ? "Cancel" : BackContentInput!.Text;
            var page = CreatePage($"Text Back #{_pushCount + 1}", $"BackButtonContent = \"{text}\"\n\nThe back button shows custom text.", null);
            NavigationPage.SetBackButtonContent(page, text);
            await DemoNav.PushAsync(page);
            AddLog($"BackButtonContent=\"{text}\" on \"{page.Header}\"");
        }

        private async void OnPushCustomIcon(object? sender, RoutedEventArgs e)
        {
            var page = CreatePage($"Icon Back #{_pushCount + 1}", "BackButtonContent = PathIcon (x)\n\nThe back button shows a custom icon.", null);
            NavigationPage.SetBackButtonContent(page, new TextBlock
            {
                Text = "\u2715",
                FontSize = 16,
                VerticalAlignment = VerticalAlignment.Center
            });
            await DemoNav.PushAsync(page);
            AddLog($"BackButtonContent=icon on \"{page.Header}\"");
        }

        private async void OnPushIconTextBack(object? sender, RoutedEventArgs e)
        {
            var page = CreatePage($"Icon+Text Back #{_pushCount + 1}", "BackButtonContent = icon + text\n\nThe back button shows both icon and text.", null);

            var content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 4,
                VerticalAlignment = VerticalAlignment.Center,
                Children =
                {
                    new TextBlock
                    {
                        Text = "\u2715",
                        FontSize = 14,
                        VerticalAlignment = VerticalAlignment.Center,
                    },
                    new TextBlock
                    {
                        Text = "Close",
                        VerticalAlignment = VerticalAlignment.Center,
                        FontSize = 14,
                    },
                }
            };

            NavigationPage.SetBackButtonContent(page, content);
            await DemoNav.PushAsync(page);
            AddLog($"BackButtonContent=icon+text on \"{page.Header}\"");
        }

        private async void OnPushGuarded(object? sender, RoutedEventArgs e)
        {
            var useAsync = DeferRadio?.IsChecked == true;
            var mode = useAsync ? "async save" : "cancel";

            var page = CreatePage($"Guarded #{_pushCount + 1}",
                useAsync
                    ? "This page uses an async Navigating handler.\n\nWhen you tap back, it simulates\nan async save (1.5s) before\nallowing the navigation."
                    : "This page cancels back navigation.\n\nTapping back will be blocked.\nUse the Pop button to force-pop.",
                Color.Parse("#FCE4EC"));

            page.Navigating += async (args) =>
            {
                if (args.NavigationType != NavigationType.Pop) return;

                if (useAsync)
                {
                    AddLog("Saving...");
                    await Task.Delay(1500);
                    AddLog("Saved, navigation allowed");
                }
                else
                {
                    args.Cancel = true;
                    AddLog("Navigation CANCELLED");
                }
            };

            await DemoNav.PushAsync(page);
            AddLog($"Guarded page ({mode}) pushed");
        }

        private async void OnPop(object? sender, RoutedEventArgs e) => await DemoNav.PopAsync();

        private async void OnPopToRoot(object? sender, RoutedEventArgs e) => await DemoNav.PopToRootAsync();

        private void OnClearLog(object? sender, RoutedEventArgs e)
        {
            LogPanel.Children.Clear();
        }

        private void AddLog(string message)
        {
            LogPanel.Children.Insert(0, new TextBlock
            {
                Text = message,
                FontFamily = new FontFamily("Cascadia Code,Consolas,Menlo,monospace"),
                FontSize = 10,
                TextWrapping = TextWrapping.Wrap,
            });
        }

        private ContentPage CreatePage(string title, string body, Color? bg)
        {
            _pushCount++;
            var page = NavigationDemoHelper.MakePage(title, body, _pushCount);
            if (bg.HasValue)
                page.Background = new SolidColorBrush(bg.Value);
            return page;
        }
    }
}
