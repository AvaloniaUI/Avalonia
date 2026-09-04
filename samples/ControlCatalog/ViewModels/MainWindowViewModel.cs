using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Chrome;
using Avalonia.Dialogs;
using Avalonia.Media;
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
            SettingsItem = new PageItem("Settings", () => new SettingsPage(SettingsViewModel), StreamGeometry.Parse(Icons.Settings), "Overview of everything in the catalog", null);
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

        public bool ExpandAllSections
        {
            get;
            set => RaiseAndSetIfChanged(ref field, value);
        }

        public string? OpenedSection
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
            set => RaiseAndSetIfChanged(ref field, value);
        } = true;

        public bool ShowSearchInTitleBar
        {
            get;
            set => RaiseAndSetIfChanged(ref field, value);
        } = false;

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
            // Clear any active search so the target is present in the filtered list.
            if (!string.IsNullOrEmpty(Query))
                Query = "";

            NavigateTo(item);
        }

        public void Filter(string? query = "")
        {
            ExpandAllSections = false;

            // Left panel items are sorted alphabetically
            var allPages = _pageSections
                .SelectMany(cat => cat.Items?.ToArray() ?? Array.Empty<PageItem>())
                .OrderBy(p => p.Header);

            var querySearchKey = query != null ? PageItem.CreateSearchKey(query) : "";
            var isDefaultVisible = string.IsNullOrWhiteSpace(query) || string.IsNullOrWhiteSpace(querySearchKey);

            foreach (var page in allPages)
            {
                page.IsVisible = isDefaultVisible;
            }

            if (!string.IsNullOrWhiteSpace(querySearchKey))
            {
                ExpandAllSections = true;
                foreach (var item in allPages)
                {
                    if (item.MatchesSearch(querySearchKey))
                    {
                        item.IsVisible = true;
                    }
                }
            }
        }

        private async void NavigateTo(PageItem? item)
        {
            if (item is null || Navigator is null)
                return;

            var page = item.CreatePage();

            if (item != CurrentPageItem)
            {
                CurrentPageItem = item;
                OpenedSection = item.Section;
                await Navigator.ReplaceAsync(page);

                if (DisplayMode == SplitViewDisplayMode.CompactOverlay || DisplayMode == SplitViewDisplayMode.Overlay)
                    IsDrawerOpened = false;
            }
        }
    }
}
