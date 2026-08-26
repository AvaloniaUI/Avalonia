using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Chrome;
using Avalonia.Dialogs;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Collections;
using ControlCatalog.Models;
using MiniMvvm;

namespace ControlCatalog.ViewModels
{
    partial class MainWindowViewModel : ViewModelBase
    {
        private readonly AvaloniaList<PageItem> _filteredPages = [];

        private bool _ignoreListChange;
        private PageItem? _currentItem;

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
            NavigateToPageCommand = MiniCommand.Create<PageItem>(NavigateToPage);

            WindowState = WindowState.Normal;

            WindowStates = new WindowState[]
            {
                WindowState.Minimized,
                WindowState.Normal,
                WindowState.Maximized,
                WindowState.FullScreen,
            };

            TitleBarHeight = -1;
            CanResize = true;
            CanMinimize = true;
            CanMaximize = true;

            Filter();
        }

        public IReadOnlyList<HomeSection> HomeSections =>
            // Home page doesn't have a section title and should be excluded from this list
            field ??= _pageSections.Where(s => !string.IsNullOrEmpty(s.Title)).ToArray();

        public IReadOnlyList<PageItem> Pages => _filteredPages;

        public INavigation? Navigator { get; internal set; }

        public bool ExtendClientAreaEnabled
        {
            get;
            set { RaiseAndSetIfChanged(ref field, value); }
        }

        public double TitleBarHeight
        {
            get;
            set { RaiseAndSetIfChanged(ref field, value); }
        }

        public WindowState WindowState
        {
            get;
            set { RaiseAndSetIfChanged(ref field, value); }
        }

        public WindowState[] WindowStates
        {
            get;
            set { RaiseAndSetIfChanged(ref field, value); }
        }

        public bool IsSystemBarVisible
        {
            get;
            set { RaiseAndSetIfChanged(ref field, value); }
        }

        public bool DisplayEdgeToEdge
        {
            get;
            set { RaiseAndSetIfChanged(ref field, value); }
        }

        public Thickness SafeAreaPadding
        {
            get;
            set { RaiseAndSetIfChanged(ref field, value); }
        }

        public bool CanResize
        {
            get;
            set { RaiseAndSetIfChanged(ref field, value); }
        }

        public bool CanMinimize
        {
            get;
            set { RaiseAndSetIfChanged(ref field, value); }
        }

        public bool CanMaximize
        {
            get;
            set { RaiseAndSetIfChanged(ref field, value); }
        }

        public int SelectedDecorationIndex
        {
            get;
            set => RaiseAndSetIfChanged(ref field, value);
        }

        public TitleBarDecorations TitleBarDecorations
        {
            get;
            set { RaiseAndSetIfChanged(ref field, value); }
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

        public int SelectedPageIndex
        {
            get;
            set
            {
                RaiseAndSetIfChanged(ref field, value);

                if (!_ignoreListChange)
                {
                    NavigateTo(field);

                    if (DisplayMode == SplitViewDisplayMode.CompactOverlay || DisplayMode == SplitViewDisplayMode.Overlay)
                        IsDrawerOpened = false;
                }
            }
        }

        public bool IsDrawerOpened
        {
            get;
            set { RaiseAndSetIfChanged(ref field, value); }
        } = true;

        public SplitViewDisplayMode DisplayMode
        {
            get;
            set { RaiseAndSetIfChanged(ref field, value); }
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
            set { RaiseAndSetIfChanged(ref field, value); }
        }

        public void NavigateToPage(PageItem item)
        {
            // Clear any active search so the target is present in the filtered list.
            if (!string.IsNullOrEmpty(Query))
                Query = "";

            var index = _filteredPages.IndexOf(item);
            if (index >= 0)
                SelectedPageIndex = index;
        }

        public void Filter(string? query = "")
        {
            try
            {
                _ignoreListChange = true;
                _filteredPages.Clear();

                var allPages = _pageSections.SelectMany(cat => cat.Items);

                if (string.IsNullOrWhiteSpace(query))
                {
                    _filteredPages.AddRange(allPages);
                }
                else
                {
                    var querySearchKey = PageItem.CreateSearchKey(query);

                    if (querySearchKey.Length == 0)
                    {
                        _filteredPages.AddRange(allPages);
                    }
                    else
                    {
                        foreach (var item in allPages)
                        {
                            if (item.MatchesSearch(querySearchKey))
                            {
                                _filteredPages.Add(item);
                            }
                        }
                    }
                }
            }
            finally
            {
                _ignoreListChange = false;
                if (_currentItem != null)
                {
                    var newIndex = _filteredPages.IndexOf(_currentItem);
                    if (newIndex != -1)
                    {
                        SelectedPageIndex = newIndex;
                    }
                }
            }
        }

        private async void NavigateTo(int pageIndex)
        {
            if (pageIndex < 0 || pageIndex >= Pages.Count || Navigator is null)
                return;

            var item = Pages[pageIndex];
            var page = item.CreatePage();

            if (page.GetType() != Navigator.NavigationStack.LastOrDefault()?.GetType())
            {
                _currentItem = item;
                await Navigator.ReplaceAsync(page);
            }
        }
    }
}
