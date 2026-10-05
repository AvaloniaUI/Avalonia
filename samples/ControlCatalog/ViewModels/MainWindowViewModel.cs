using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Chrome;
using Avalonia.Dialogs;
using Avalonia.Media;
using Avalonia.Styling;
using ControlCatalog.Models;
using ControlCatalog.Pages;
using MiniMvvm;

namespace ControlCatalog.ViewModels
{
    partial class MainWindowViewModel : ViewModelBase
    {
        public SettingsViewModel SettingsViewModel { get; } = new SettingsViewModel();

        public PageItem HomeItem { get; } = new PageItem("Home", () => new HomePage(), StreamGeometry.Parse(Icons.Home), "Overview of everything in the catalog", null);
        public PageItem SettingsItem { get; }

        public MainWindowViewModel()
        {
            AboutCommand = MiniCommand.CreateFromTask(async () =>
            {
                var dialog = new AboutAvaloniaDialog();

                if ((App.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow is { } mainWindow)
                {
                    await dialog.ShowDialog(mainWindow);
                }
            });
            ExitCommand = MiniCommand.Create(() =>
            {
                (App.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown();
            });
            SettingsItem = new PageItem("Settings", () => new SettingsPage(SettingsViewModel), StreamGeometry.Parse(Icons.Settings), "Theme, transparency and window options", null);
            NavigateToPageCommand = MiniCommand.Create<PageItem>(NavigateToItem);
            SettingsCommand = MiniCommand.Create(async () =>
            {
                if (CurrentPageItem == SettingsItem)
                    return;

                if (Navigator is { } navigator)
                {
                    NavigateToItem(SettingsItem);
                }
            });

            HomeCommand = MiniCommand.Create(async () =>
            {
                if (CurrentPageItem == HomeItem)
                    return;

                if (Navigator is { } navigator)
                {
                    NavigateToItem(HomeItem);
                }
            });

            TitleBarHeight = -1;
            CanResize = true;
            CanMinimize = true;
            CanMaximize = true;

            Filter();
        }

        public IReadOnlyList<HomeSection> HomeSections =>
            // Home page doesn't have a section title and should be excluded from this list
            field ??= _pageSections.Where(s => !string.IsNullOrEmpty(s.Title)).ToArray();

        public INavigation? Navigator { get; internal set; }

        private bool _isSearching;

        public bool ExtendClientAreaEnabled
        {
            get;
            set => RaiseAndSetIfChanged(ref field, value);
        }

        public double TitleBarHeight
        {
            get;
            set => RaiseAndSetIfChanged(ref field, value);
        }

        public bool IsSystemBarVisible
        {
            get;
            set => RaiseAndSetIfChanged(ref field, value);
        }

        public bool DisplayEdgeToEdge
        {
            get;
            set => RaiseAndSetIfChanged(ref field, value);
        }

        public Thickness SafeAreaPadding
        {
            get;
            set => RaiseAndSetIfChanged(ref field, value);
        }

        public bool CanResize
        {
            get;
            set => RaiseAndSetIfChanged(ref field, value);
        }

        public bool CanMinimize
        {
            get;
            set => RaiseAndSetIfChanged(ref field, value);
        }

        public bool CanMaximize
        {
            get;
            set => RaiseAndSetIfChanged(ref field, value);
        }

        public ControlTheme? DecorationsTheme
        {
            get;
            set => RaiseAndSetIfChanged(ref field, value);
        }

        public TitleBarDecorations TitleBarDecorations
        {
            get;
            set => RaiseAndSetIfChanged(ref field, value);
        } = TitleBarDecorations.All;

        public bool ShowTitle
        {
            get => HasTitleBarDecoration(TitleBarDecorations.Title);
            set => SetTitleBarDecoration(TitleBarDecorations.Title, value);
        }

        public bool ShowFullScreenButton
        {
            get => HasTitleBarDecoration(TitleBarDecorations.FullScreenButton);
            set => SetTitleBarDecoration(TitleBarDecorations.FullScreenButton, value);
        }

        public bool ShowMinimizeButton
        {
            get => HasTitleBarDecoration(TitleBarDecorations.MinimizeButton);
            set => SetTitleBarDecoration(TitleBarDecorations.MinimizeButton, value);
        }

        public bool ShowMaximizeButton
        {
            get => HasTitleBarDecoration(TitleBarDecorations.MaximizeButton);
            set => SetTitleBarDecoration(TitleBarDecorations.MaximizeButton, value);
        }

        public bool ShowCloseButton
        {
            get => HasTitleBarDecoration(TitleBarDecorations.CloseButton);
            set => SetTitleBarDecoration(TitleBarDecorations.CloseButton, value);
        }

        public PageItem? CurrentPageItem
        {
            get;
            set => RaiseAndSetIfChanged(ref field, value);
        }

        public bool IsDrawerOpened
        {
            get;
            set
            {
                if (!RaiseAndSetIfChanged(ref field, value))
                {
                    return;
                }

                // With the drawer shut the page list is hidden, so the section itself carries the marker.
                foreach (var section in _pageSections)
                {
                    section.IsExpanded = value && (_isSearching ? section.IsSectionVisible : section.IsCurrent);
                }
            }
        } = true;

        public SplitViewDisplayMode DisplayMode
        {
            get;
            set => RaiseAndSetIfChanged(ref field, value);
        }

        public string? Query
        {
            get;
            set
            {
                RaiseAndSetIfChanged(ref field, value);

                Filter(value);
            }
        } = "";

        private bool HasTitleBarDecoration(TitleBarDecorations decoration)
            => (TitleBarDecorations & decoration) != 0;

        private void SetTitleBarDecoration(TitleBarDecorations decoration, bool value, [CallerMemberName] string? propertyName = null)
        {
            var newDecorations = value ? TitleBarDecorations | decoration : TitleBarDecorations & ~decoration;
            if (newDecorations == TitleBarDecorations)
                return;

            TitleBarDecorations = newDecorations;
            RaisePropertyChanged(propertyName);
            RaisePropertyChanged(nameof(TitleBarDecorations));
        }

        public MiniCommand AboutCommand { get; }

        public MiniCommand ExitCommand { get; }

        public MiniCommand NavigateToPageCommand { get; }

        public MiniCommand SettingsCommand { get; }

        public MiniCommand HomeCommand { get; }

        /// <summary>
        ///    A required DateTime which should demonstrate validation for the DateTimePicker
        /// </summary>
        [Required]
        public DateTime? ValidatedDateExample
        {
            get;
            set => RaiseAndSetIfChanged(ref field, value);
        }

        public Win32Properties.WindowCornerPreference[] Win32WindowCornerPreferences { get; } =
            Enum.GetValues<Win32Properties.WindowCornerPreference>();

        public Win32Properties.WindowCornerPreference Win32WindowCornerPreference
        {
            get;
            set => RaiseAndSetIfChanged(ref field, value);
        }

        public void NavigateToItem(PageItem item)
        {
            _ = NavigateToAsync(item);
        }

        private async Task NavigateToAsync(PageItem? item)
        {
            if (item is null || Navigator is null)
                return;

            // A gallery may have pushed a sample on top of its page. Drop it first: replacing the top page
            // would leave the previous page below the new one, and the shell would show a back button that
            // leads to a page the drawer no longer selects.
            if (Navigator.StackDepth > 1)
                await Navigator.PopToRootAsync(null);

            if (item != CurrentPageItem)
            {
                var page = item.CreatePage();
                CurrentPageItem = item;

                foreach (var section in _pageSections)
                {
                    // A section's own page counts too, so the section stays open when the drawer comes back.
                    section.CurrentPage = section.PageItem == item || section.Title == item.Section ? item : null;

                    if (section.IsCurrent && IsDrawerOpened)
                    {
                        section.IsExpanded = true;
                    }
                }

                await Navigator.ReplaceAsync(page);

                if (DisplayMode == SplitViewDisplayMode.CompactOverlay || DisplayMode == SplitViewDisplayMode.Overlay)
                    IsDrawerOpened = false;
            }
        }

        public void Filter(string? query = "")
        {

            // Left panel items are sorted alphabetically
            var allPages = _pageSections
                .SelectMany(cat => cat.Items?.ToArray() ?? Array.Empty<PageItem>())
                .OrderBy(p => p.Header);

            var querySearchKey = query != null ? PageItem.CreateSearchKey(query) : "";
            var isDefaultVisible = string.IsNullOrWhiteSpace(query) || string.IsNullOrWhiteSpace(querySearchKey);
            _isSearching = !isDefaultVisible;

            foreach (var page in allPages)
            {
                page.IsVisible = isDefaultVisible;
            }

            if (!string.IsNullOrWhiteSpace(querySearchKey))
            {
                foreach (var item in allPages)
                {
                    if (item.MatchesSearch(querySearchKey))
                    {
                        item.IsVisible = true;
                    }
                }
            }

            foreach (var section in _pageSections)
            {
                section.IsExpanded = isDefaultVisible ? section.IsCurrent : section.IsSectionVisible;
            }
        }
    }
}
