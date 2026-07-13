using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Dialogs;
using System;
using System.ComponentModel.DataAnnotations;
using Avalonia;
using MiniMvvm;
using Avalonia.Collections;
using ControlCatalog.Pages;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

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
            new PageItem("Home", () => new HomePage(), Icons.Home, "Overview of everything in the catalog"),
            new PageItem("Composition", () => new CompositionPage(), Icons.Layers, "Composition-layer animations and effects", "Media & Graphics"),
            new PageItem("Accelerator", () => new AcceleratorPage(), Icons.Keyboard, "Keyboard shortcuts that invoke commands", "Interaction"),
            new PageItem("Acrylic", () => new AcrylicPage(), Icons.Blur, "Translucent acrylic window materials", "Media & Graphics"),
            new PageItem("AdornerLayer", () => new AdornerLayerPage(), Icons.Sparkle, "Overlay visuals on top of other controls", "Status & Feedback"),
            new PageItem("AutoCompleteBox",() => new AutoCompleteBoxPage(), Icons.TextInput, "Text input with completion suggestions", "Text"),
            new PageItem("Border",() => new BorderPage(), Icons.Border, "Decorate elements with borders and corner radii", "Layout"),
            new PageItem("BitmapCache",() => new BitmapCachePage(), Icons.Lightning, "Cache visuals as bitmaps for performance", "Media & Graphics"),
            new PageItem("Buttons",() => new ButtonsPage(), Icons.CursorClick, "Button, RepeatButton, ToggleButton and friends", "Basic Input"),
            new PageItem("ButtonSpinner",() => new ButtonSpinnerPage(), Icons.Spinner, "Content with increment and decrement buttons", "Basic Input"),
            new PageItem("Calendar",() => new CalendarPage(), Icons.Calendar, "A month calendar for selecting dates", "Date & Time"),
            new PageItem("Canvas",() => new CanvasPage(), Icons.Canvas, "Position children at explicit coordinates", "Layout"),
            new PageItem("CommandBar",() => new CommandBarPage(), Icons.Terminal, "A toolbar of commands with an overflow menu", "Menus & Flyouts"),
            new PageItem("Carousel",() => new Pages.CarouselPage(), Icons.Slides, "Cycle through a collection of items", "Collections & Data"),
            new PageItem("CarouselPage",() => new CarouselDemoPage(), Icons.Slides, "Swipeable page-based navigation", "Navigation & Pages"),
            new PageItem("CheckBox",() => new CheckBoxPage(), Icons.Checkbox, "Two- and three-state check boxes", "Basic Input"),
            new PageItem("Clipboard",() => new ClipboardPage(), Icons.Clipboard, "Read from and write to the system clipboard", "Interaction"),
            new PageItem("ColorPicker",() => new ColorPickerPage(), Icons.Palette, "Pick colors from spectrum and palette views", "Basic Input"),
            new PageItem("ComboBox",() => new ComboBoxPage(), Icons.Dropdown, "A drop-down list of selectable items", "Basic Input"),
            new PageItem("Container Queries",() => new ContainerQueryPage(), Icons.Container, "Styles that respond to container size", "Layout"),
            new PageItem("ContentPage",() => new ContentDemoPage(), Icons.Document, "A page that hosts a single content view", "Navigation & Pages"),
            new PageItem("ContextFlyout",() => new ContextFlyoutPage(), Icons.Menu, "Attach flyouts shown on right-click", "Menus & Flyouts"),
            new PageItem("ContextMenu",() => new ContextMenuPage(), Icons.Menu, "Traditional right-click context menus", "Menus & Flyouts"),
            new PageItem("Cursor",() => new CursorPage(), Icons.Cursor, "Change the pointer cursor over elements", "Interaction"),
            new PageItem("Custom Drawing",() => new CustomDrawing(), Icons.Brush, "Render custom geometry in code", "Media & Graphics"),
            new PageItem("DataGrid",() => new DataGridPage(), Icons.Grid, "Tabular data with sorting and editing", "Collections & Data"),
            new PageItem("Data Validation",() => new DataValidationPage(), Icons.Shield, "Display validation errors from bindings", "Status & Feedback"),
            new PageItem("Date/Time Picker",() => new DateTimePickerPage(), Icons.Clock, "Spinner-style date and time pickers", "Date & Time"),
            new PageItem("CalendarDatePicker",() => new CalendarDatePickerPage(), Icons.Calendar, "A date picker with a drop-down calendar", "Date & Time"),
            new PageItem("Dialogs",() => new DialogsPage(), Icons.Dialog, "File pickers and modal dialog windows", "Status & Feedback"),
            new PageItem("Drag+Drop",() => new DragAndDropPage(), Icons.DragDrop, "Drag data within and between applications", "Interaction"),
            new PageItem("DrawerPage",() => new DrawerDemoPage(), Icons.Drawer, "A page with a sliding navigation drawer", "Navigation & Pages"),
            new PageItem("Expander",() => new ExpanderPage(), Icons.Expand, "A header that expands to reveal content", "Layout"),
            new PageItem("Flyouts",() => new FlyoutsPage(), Icons.Flyout, "Lightweight popups anchored to controls", "Menus & Flyouts"),
            new PageItem("Focus",() => new FocusPage(), Icons.Target, "Track and control keyboard focus", "Interaction"),
            new PageItem("Gestures",() => new GesturePage(), Icons.Gesture, "Tap, scroll and pinch gesture recognition", "Interaction"),
            new PageItem("Image",() => new ImagePage(), Icons.Image, "Display bitmaps with different stretch modes", "Media & Graphics"),
            new PageItem("Label",() => new LabelsPage(), Icons.Tag, "Captions with access keys for other controls", "Text"),
            new PageItem("LayoutTransformControl",() => new LayoutTransformControlPage(), Icons.Transform, "Apply transforms that affect layout", "Layout"),
            new PageItem("ListBox",() => new ListBoxPage(), Icons.List, "A selectable, virtualized list of items", "Collections & Data"),
            new PageItem("Menu",() => new MenuPage(), Icons.Menu, "Menu bars with nested menu items", "Menus & Flyouts"),
            new PageItem("NavigationPage",() => new NavigationDemoPage(), Icons.Navigation, "Stack-based page navigation", "Navigation & Pages"),
            new PageItem("Notifications",() => new NotificationsPage(), Icons.Bell, "Toast-style in-app notifications", "Status & Feedback"),
            new PageItem("NumericUpDown",() => new NumericUpDownPage(), Icons.Number, "Numeric input with spinner buttons", "Basic Input"),
            new PageItem("OpenGL",() => new OpenGlPage(), Icons.Cube3D, "Embed custom OpenGL rendering", "Media & Graphics"),
            new PageItem("OpenGL Lease",() => new OpenGlLeasePage(), Icons.Cube3D, "Low-level access to the OpenGL context", "Media & Graphics"),
            new PageItem("PipsPager",() => new PipsPagerPage(), Icons.HorizontalDots, "Dot-style pager for paginated content", "Collections & Data"),
            new PageItem("Platform Information",() => new PlatformInfoPage(), Icons.Info, "Runtime platform and capability info", "Window & Platform"),
            new PageItem("Pointers",() => new PointersPage(), Icons.Cursor, "Raw pointer input and capture", "Interaction"),
            new PageItem("ProgressBar",() => new ProgressBarPage(), Icons.Progress, "Determinate and indeterminate progress", "Status & Feedback"),
            new PageItem("RadioButton",() => new RadioButtonPage(), Icons.Radio, "Mutually exclusive option groups", "Basic Input"),
            new PageItem("RefreshContainer",() => new RefreshContainerPage(), Icons.Refresh, "Pull-to-refresh for scrollable content", "Collections & Data"),
            new PageItem("RelativePanel",() => new RelativePanelPage(), Icons.Layout, "Arrange children relative to each other", "Layout"),
            new PageItem("ScrollViewer",() => new ScrollViewerPage(), Icons.Scroll, "Scrollable viewport over large content", "Layout"),
            new PageItem("Slider",() => new SliderPage(), Icons.Tune, "Select a value from a continuous range", "Basic Input"),
            new PageItem("SplitView",() => new SplitViewPage(), Icons.Split, "A collapsible pane beside content", "Navigation & Pages"),
            new PageItem("TabbedPage",() => new TabbedDemoPage(), Icons.Tab, "Tab-based page navigation", "Navigation & Pages"),
            new PageItem("TabControl",() => new TabControlPage(), Icons.Tab, "Switch between tabbed content views", "Navigation & Pages"),
            new PageItem("TabStrip",() => new TabStripPage(), Icons.Tab, "A standalone strip of selectable tabs", "Navigation & Pages"),
            new PageItem("TextBox",() => new TextBoxPage(), Icons.TextInput, "Single- and multi-line text editing", "Text"),
            new PageItem("TextBlock",() => new TextBlockPage(), Icons.TextInput, "Styled read-only text display", "Text"),
            new PageItem("Theme Variants",() => new ThemePage(), Icons.Theme, "Switch between light and dark variants", "Window & Platform"),
            new PageItem("ToggleSwitch",() => new ToggleSwitchPage(), Icons.Toggle, "An on/off switch with a sliding knob", "Basic Input"),
            new PageItem("ToolTip",() => new ToolTipPage(), Icons.Tooltip, "Hover hints for any control", "Status & Feedback"),
            new PageItem("TransitioningContentControl",() => new TransitioningContentControlPage(), Icons.Transition, "Animate between content changes", "Media & Graphics"),
            new PageItem("TreeView",() => new TreeViewPage(), Icons.Tree, "Hierarchical data with expandable nodes", "Collections & Data"),
            new PageItem("Viewbox",() => new ViewboxPage(), Icons.Viewbox, "Scale content to fit available space", "Layout"),
            new PageItem("Native Embed",() => new NativeEmbedPage(), Icons.Puzzle, "Host native platform controls", "Window & Platform"),
            new PageItem("Window Customizations",() => new WindowCustomizationsPage(), Icons.Window, "Custom chrome, decorations and sizing", "Window & Platform"),
            new PageItem("HeaderedContentControl",() => new HeaderedContentPage(), Icons.Header, "Content paired with a header", "Layout"),
            new PageItem("Screens",() => new ScreenPage(), Icons.Monitor, "Enumerate displays and their bounds", "Window & Platform"),
        };

        /// <summary>Section order for the home page card grid.</summary>
        private static readonly string[] s_categoryOrder =
        {
            "Basic Input",
            "Text",
            "Collections & Data",
            "Date & Time",
            "Menus & Flyouts",
            "Navigation & Pages",
            "Layout",
            "Media & Graphics",
            "Status & Feedback",
            "Interaction",
            "Window & Platform",
        };

        private IReadOnlyList<HomeSection>? _homeSections;

        public IReadOnlyList<HomeSection> HomeSections => _homeSections ??=
            s_categoryOrder
                .Select(category => new HomeSection(category, _items.Where(i => i.Category == category).ToArray()))
                .Where(section => section.Items.Count > 0)
                .ToArray();

        public void NavigateToPage(PageItem item)
        {
            // Clear any active search so the target is present in the filtered list.
            if (!string.IsNullOrEmpty(Query))
                Query = "";

            var index = Pages.IndexOf(item);
            if (index >= 0)
                SelectedPageIndex = index;
        }

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

            if (item != null)
            {
                var view = item.Factory();
                if (view is Page page && view.GetType() != Navigator.NavigationStack.LastOrDefault()?.GetType())
                {
                    _currentItem = item;
                    await Navigator.ReplaceAsync(page);
                }
            }
        }
    }

    class HomeSection(string title, IReadOnlyList<PageItem> items)
    {
        public string Title { get; } = title;
        public IReadOnlyList<PageItem> Items { get; } = items;
    }

    class PageItem(string header, Func<object> factory, string? iconData = null, string? description = null, string? category = null)
    {
        public string Header { get; } = header;
        public Func<object> Factory { get; } = factory;
        public string? IconData { get; } = iconData;
        public string? Description { get; } = description;
        public string? Category { get; } = category;
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
    }
}
