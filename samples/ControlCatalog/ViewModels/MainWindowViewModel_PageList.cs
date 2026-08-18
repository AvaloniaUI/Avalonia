using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls;
using ControlCatalog.Pages;

namespace ControlCatalog.ViewModels
{
    partial class MainWindowViewModel
    {
        private int _selectedPageIndex;
        private bool _isDrawerOpened = true;
        private bool _ignoreListChange = false;
        private string? _query = "";
        private PageItem? _currentItem;
        private SplitViewDisplayMode _displayMode;

        private List<PageItem> _items = new()
        {
            new PageItem<CompositionPage>("Composition", Icons.Layers),
            new PageItem<AcceleratorPage>("Accelerator", Icons.Keyboard),
            new PageItem<AcrylicPage>("Acrylic", Icons.Blur),
            new PageItem<AdornerLayerPage>("AdornerLayer", Icons.Sparkle),
            new PageItem<AutoCompleteBoxPage>("AutoCompleteBox", Icons.TextInput),
            new PageItem<BorderPage>("Border", Icons.Border),
            new PageItem<BitmapCachePage>("BitmapCache", Icons.Lightning),
            new PageItem<ButtonsPage>("Buttons", Icons.CursorClick),
            new PageItem<ButtonSpinnerPage>("ButtonSpinner", Icons.Spinner),
            new PageItem<CalendarPage>("Calendar", Icons.Calendar),
            new PageItem<CanvasPage>("Canvas", Icons.Canvas),
            new PageItem<CommandBarPage>("CommandBar", Icons.Terminal),
            new PageItem<Pages.CarouselPage>("Carousel", Icons.Slides),
            new PageItem<CarouselDemoPage>("CarouselPage", Icons.Slides),
            new PageItem<CheckBoxPage>("CheckBox", Icons.Checkbox),
            new PageItem<ClipboardPage>("Clipboard", Icons.Clipboard),
            new PageItem<ColorPickerPage>("ColorPicker", Icons.Palette),
            new PageItem<ComboBoxPage>("ComboBox", Icons.Dropdown),
            new PageItem<ContainerQueryPage>("Container Queries", Icons.Container),
            new PageItem<ContentDemoPage>("ContentPage", Icons.Document),
            new PageItem<ContextFlyoutPage>("ContextFlyout", Icons.Menu),
            new PageItem<ContextMenuPage>("ContextMenu", Icons.Menu),
            new PageItem<CursorPage>("Cursor", Icons.Cursor),
            new PageItem<CustomDrawing>("Custom Drawing", Icons.Brush),
            new PageItem<DataValidationPage>("Data Validation", Icons.Shield),
            new PageItem<DateTimePickerPage>("Date/Time Picker", Icons.Clock),
            new PageItem<CalendarDatePickerPage>("CalendarDatePicker", Icons.Calendar),
            new PageItem<DialogsPage>("Dialogs", Icons.Dialog),
            new PageItem<DragAndDropPage>("Drag+Drop", Icons.DragDrop),
            new PageItem<DrawerDemoPage>("DrawerPage", Icons.Drawer),
            new PageItem<ExpanderPage>("Expander", Icons.Expand),
            new PageItem<FlyoutsPage>("Flyouts", Icons.Flyout),
            new PageItem<FocusPage>("Focus", Icons.Target),
            new PageItem<GesturePage>("Gestures", Icons.Gesture),
            new PageItem<ImagePage>("Image", Icons.Image),
            new PageItem<LabelsPage>("Label", Icons.Tag),
            new PageItem<LayoutTransformControlPage>("LayoutTransformControl", Icons.Transform),
            new PageItem<ListBoxPage>("ListBox", Icons.List),
            new PageItem<MenuPage>("Menu", Icons.Menu),
            new PageItem<NavigationDemoPage>("NavigationPage", Icons.Navigation),
            new PageItem<NotificationsPage>("Notifications", Icons.Bell),
            new PageItem<NumericUpDownPage>("NumericUpDown", Icons.Number),
            new PageItem<OpenGlPage>("OpenGL", Icons.Cube3D),
            new PageItem<OpenGlLeasePage>("OpenGL Lease", Icons.Cube3D),
            new PageItem<PipsPagerPage>("PipsPager", Icons.HorizontalDots),
            new PageItem<PlatformInfoPage>("Platform Information", Icons.Info),
            new PageItem<PointersPage>("Pointers", Icons.Cursor),
            new PageItem<ProgressBarPage>("ProgressBar", Icons.Progress),
            new PageItem<RadioButtonPage>("RadioButton", Icons.Radio),
            new PageItem<RefreshContainerPage>("RefreshContainer", Icons.Refresh),
            new PageItem<RelativePanelPage>("RelativePanel", Icons.Layout),
            new PageItem<ScrollViewerPage>("ScrollViewer", Icons.Scroll),
            new PageItem<SliderPage>("Slider", Icons.Tune),
            new PageItem<SplitViewPage>("SplitView", Icons.Split),
            new PageItem<TabbedDemoPage>("TabbedPage", Icons.Tab),
            new PageItem<TabControlPage>("TabControl", Icons.Tab),
            new PageItem<TabStripPage>("TabStrip", Icons.Tab),
            new PageItem<TableViewPage>("TableView", Icons.Grid),
            new PageItem<TextBoxPage>("TextBox", Icons.TextInput),
            new PageItem<TextBlockPage>("TextBlock", Icons.TextInput),
            new PageItem<ThemePage>("Theme Variants", Icons.Theme),
            new PageItem<ToggleSwitchPage>("ToggleSwitch", Icons.Toggle),
            new PageItem<ToolTipPage>("ToolTip", Icons.Tooltip),
            new PageItem<TransitioningContentControlPage>("TransitioningContentControl", Icons.Transition),
            new PageItem<TreeViewPage>("TreeView", Icons.Tree),
            new PageItem<ViewboxPage>("Viewbox", Icons.Viewbox),
            new PageItem<WrapPanelPage>("WrapPanel", Icons.Layout),
            new PageItem<NativeEmbedPage>("Native Embed", Icons.Puzzle),
            new PageItem<WindowCustomizationsPage>("Window Customizations", Icons.Window),
            new PageItem<HeaderedContentPage>("HeaderedContentControl", Icons.Header),
            new PageItem<ScreenPage>("Screens", Icons.Monitor),
        };

        public AvaloniaList<PageItem> Pages { get; } = new AvaloniaList<PageItem>();

        public void Filter(string? query = "")
        {
            try
            {
                _ignoreListChange = true;
                Pages.Clear();

                if (string.IsNullOrWhiteSpace(query))
                {
                    Pages.AddRange(_items);
                }
                else
                {
                    var querySearchKey = PageItem.CreateSearchKey(query);

                    if (querySearchKey.Length == 0)
                    {
                        Pages.AddRange(_items);
                    }
                    else
                    {
                        foreach (var item in _items)
                        {
                            if (item.MatchesSearch(querySearchKey))
                            {
                                Pages.Add(item);
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
                    var newIndex = Pages.IndexOf(_currentItem);
                    if (newIndex != -1)
                    {
                        SelectedPageIndex = newIndex;
                    }
                }
            }
        }

        public INavigation? Navigator { get; internal set; }

        public int SelectedPageIndex
        {
            get { return _selectedPageIndex; }
            set
            {
                RaiseAndSetIfChanged(ref _selectedPageIndex, value);

                if (!_ignoreListChange)
                {
                    NavigateTo(_selectedPageIndex);

                    if (DisplayMode == SplitViewDisplayMode.CompactOverlay || DisplayMode == SplitViewDisplayMode.Overlay)
                        IsDrawerOpened = false;
                }
            }
        }

        public bool IsDrawerOpened
        {
            get { return _isDrawerOpened; }
            set { RaiseAndSetIfChanged(ref _isDrawerOpened, value); }
        }

        public SplitViewDisplayMode DisplayMode
        {
            get { return _displayMode; }
            set { RaiseAndSetIfChanged(ref _displayMode, value); }
        }

        public string? Query
        {
            get { return _query; }
            set
            {
                RaiseAndSetIfChanged(ref _query, value);

                Filter(value);
            }
        }

        private async void NavigateTo(int pageIndex)
        {
            if (pageIndex < 0 || pageIndex >= Pages.Count || Navigator is null)
                return;

            var item = Pages[pageIndex];

            if (item != null && _currentItem != item)
            {
                _currentItem = item;

                await item.Navigate(Navigator);
            }
        }
    }

    internal class PageItem<T>(string header, string? iconData = null) : PageItem(header, iconData) where T : Page, new ()
    {
        public override Task Navigate(INavigation navigation)
        {
            return navigation.ReplaceAsync<T>();
        }
    }

    internal class PageItem(string header, string? iconData = null)
    {
        public string Header { get; } = header;
        public string? IconData { get; } = iconData;
        private string SearchKey { get; } = CreateSearchKey(header);

        public bool IsVisible { get; set; } = true;

        public bool MatchesSearch(string searchKey)
        {
            return SearchKey.Contains(searchKey, StringComparison.Ordinal);
        }

        public static string CreateSearchKey(string value)
        {
            var normalizedValue = value.Normalize(NormalizationForm.FormKD);
            var builder = new StringBuilder(normalizedValue.Length);

            foreach (var c in normalizedValue)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(c);

                if (category is UnicodeCategory.NonSpacingMark or
                    UnicodeCategory.SpacingCombiningMark or
                    UnicodeCategory.EnclosingMark)
                {
                    continue;
                }

                if (char.IsLetterOrDigit(c))
                {
                    builder.Append(char.ToUpperInvariant(c));
                }
            }

            return builder.ToString();
        }

        public async virtual Task Navigate(INavigation navigation) { }
    }
}
