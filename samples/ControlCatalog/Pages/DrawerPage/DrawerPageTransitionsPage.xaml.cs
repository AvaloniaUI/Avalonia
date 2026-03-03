using System;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace ControlCatalog.Pages
{
    public partial class DrawerPageTransitionsPage : UserControl
    {
        private string _selectedTransition = "None";

        public DrawerPageTransitionsPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object? sender, RoutedEventArgs e)
        {
            // Null out the default transition — OnTransitionChanged runs during init before DetailNav exists.
            DetailNav.PageTransition = null;
            await DetailNav.PushAsync(BuildPage("Home", _selectedTransition), null);
        }

        private void OnTransitionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (DetailNav == null) return;

            _selectedTransition = TransitionCombo.SelectedIndex switch
            {
                1 => "CrossFade",
                2 => "PageSlide (H)",
                3 => "PageSlide (V)",
                4 => "Composite (Slide + Fade)",
                _ => "None"
            };

            DetailNav.PageTransition = TransitionCombo.SelectedIndex switch
            {
                1 => new CrossFade(TimeSpan.FromMilliseconds(300)),
                2 => new PageSlide(TimeSpan.FromMilliseconds(300), PageSlide.SlideAxis.Horizontal),
                3 => new PageSlide(TimeSpan.FromMilliseconds(300), PageSlide.SlideAxis.Vertical),
                4 => new CompositePageTransition
                {
                    PageTransitions =
                    {
                        new PageSlide(TimeSpan.FromMilliseconds(300), PageSlide.SlideAxis.Horizontal),
                        new CrossFade(TimeSpan.FromMilliseconds(300))
                    }
                },
                _ => null
            };
        }

        private async void OnSectionClick(object? sender, RoutedEventArgs e)
        {
            if (sender is not Button button) return;
            var section = button.Tag?.ToString() ?? "Home";

            DemoDrawer.IsOpen = false;

            await DetailNav.ReplaceAsync(BuildPage(section, _selectedTransition));
        }

        private static ContentPage BuildPage(string section, string transitionName)
        {
            var (iconPath, body) = section switch
            {
                "Home" =>
                    ("M10,20V14H14V20H19V12H22L12,3L2,12H5V20H10Z",
                     "Your dashboard with recent activity, quick actions, and personalized content."),
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

            var page = NavigationDemoHelper.MakeSectionPage(section, iconPath, section, body, 0, $"Transition: {transitionName}");
            NavigationPage.SetHasNavigationBar(page, false);
            return page;
        }
    }
}
