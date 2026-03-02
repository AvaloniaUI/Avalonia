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

        public NavigationPageBackButtonPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object? sender, RoutedEventArgs e)
        {
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

        private void OnPushStandard(object? sender, RoutedEventArgs e)
        {
            var page = CreatePage($"Page {_pushCount + 1}", "Standard page with default back arrow.", null);
            DemoNav.Push(page);
        }

        private void OnPushNoBack(object? sender, RoutedEventArgs e)
        {
            var page = CreatePage($"No Back #{_pushCount + 1}", "IsBackButtonVisible = false\n\nThe back arrow is hidden.\nUse the Pop button to go back.", null);
            NavigationPage.SetHasBackButton(page, false);
            DemoNav.Push(page);
            AddLog($"HasBackButton=false on \"{page.Header}\"");
        }

        private void OnPushDisabledBack(object? sender, RoutedEventArgs e)
        {
            var page = CreatePage($"Disabled Back #{_pushCount + 1}", "IsBackButtonEnabled = false\n\nThe back arrow is visible but disabled.\nUse the Pop button to go back.", null);
            NavigationPage.SetIsBackButtonEnabled(page, false);
            DemoNav.Push(page);
            AddLog($"IsBackButtonEnabled=false on \"{page.Header}\"");
        }

        private void OnPushCustomText(object? sender, RoutedEventArgs e)
        {
            var text = string.IsNullOrWhiteSpace(BackContentInput?.Text) ? "Cancel" : BackContentInput!.Text;
            var page = CreatePage($"Text Back #{_pushCount + 1}", $"BackButtonContent = \"{text}\"\n\nThe back button shows custom text.", null);
            NavigationPage.SetBackButtonContent(page, text);
            DemoNav.Push(page);
            AddLog($"BackButtonContent=\"{text}\" on \"{page.Header}\"");
        }

        private void OnPushCustomIcon(object? sender, RoutedEventArgs e)
        {
            var page = CreatePage($"Icon Back #{_pushCount + 1}", "BackButtonContent = PathIcon (×)\n\nThe back button shows a custom icon.", null);
            NavigationPage.SetBackButtonContent(page, new TextBlock
            {
                Text = "✕",
                FontSize = 16,
                VerticalAlignment = VerticalAlignment.Center
            });
            DemoNav.Push(page);
            AddLog($"BackButtonContent=icon on \"{page.Header}\"");
        }

        private void OnPushIconTextBack(object? sender, RoutedEventArgs e)
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
                        Text = "✕",
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
            DemoNav.Push(page);
            AddLog($"BackButtonContent=icon+text on \"{page.Header}\"");
        }

        private void OnPushGuarded(object? sender, RoutedEventArgs e)
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

            DemoNav.Push(page);
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
            var color = bg ?? PageColors[_pushCount % PageColors.Length];
            return new ContentPage
            {
                Header = title,
                Background = new SolidColorBrush(color),
                Content = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Spacing = 12,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = title,
                            FontSize = 28,
                            FontWeight = FontWeight.Bold,
                            HorizontalAlignment = HorizontalAlignment.Center,
                        },
                        new TextBlock
                        {
                            Text = body,
                            FontSize = 14,
                            Opacity = 0.6,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            TextAlignment = TextAlignment.Center,
                            TextWrapping = TextWrapping.Wrap,
                            MaxWidth = 360,
                        }
                    }
                },
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch
            };
        }
    }
}
