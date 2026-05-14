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
            new PageItem("Composition", () => new CompositionPage(), Icons.Layers),
            new PageItem("Accelerator", () => new AcceleratorPage(), Icons.Keyboard),
            new PageItem("Acrylic", () => new AcrylicPage(), Icons.Blur),
            new PageItem("AdornerLayer", () => new AdornerLayerPage(), Icons.Sparkle),
            new PageItem("AutoCompleteBox",() => new AutoCompleteBoxPage(), Icons.TextInput),
            new PageItem("Border",() => new BorderPage(), Icons.Border),
            new PageItem("BitmapCache",() => new BitmapCachePage(), Icons.Lightning),
            new PageItem("Buttons",() => new ButtonsPage(), Icons.CursorClick),
            new PageItem("ButtonSpinner",() => new ButtonSpinnerPage(), Icons.Spinner),
            new PageItem("Calendar",() => new CalendarPage(), Icons.Calendar),
            new PageItem("Canvas",() => new CanvasPage(), Icons.Canvas),
            new PageItem("CommandBar",() => new CommandBarPage(), Icons.Terminal),
            new PageItem("Carousel",() => new Pages.CarouselPage(), Icons.Slides),
            new PageItem("CarouselPage",() => new CarouselDemoPage(), Icons.Slides),
            new PageItem("CheckBox",() => new CheckBoxPage(), Icons.Checkbox),
            new PageItem("Clipboard",() => new ClipboardPage(), Icons.Clipboard),
            new PageItem("ColorPicker",() => new ColorPickerPage(), Icons.Palette),
            new PageItem("ComboBox",() => new ComboBoxPage(), Icons.Dropdown),
            new PageItem("Container Queries",() => new ContainerQueryPage(), Icons.Container),
            new PageItem("ContentPage",() => new ContentDemoPage(), Icons.Document),
            new PageItem("ContextFlyout",() => new ContextFlyoutPage(), Icons.Menu),
            new PageItem("ContextMenu",() => new ContextMenuPage(), Icons.Menu),
            new PageItem("Cursor",() => new CursorPage(), Icons.Cursor),
            new PageItem("Custom Drawing",() => new CustomDrawing(), Icons.Brush),
            new PageItem("DataGrid",() => new DataGridPage(), Icons.Grid),
            new PageItem("Data Validation",() => new DataValidationPage(), Icons.Shield),
            new PageItem("Date/Time Picker",() => new DateTimePickerPage(), Icons.Clock),
            new PageItem("CalendarDatePicker",() => new CalendarDatePickerPage(), Icons.Calendar),
            new PageItem("Dialogs",() => new DialogsPage(), Icons.Dialog),
            new PageItem("Drag+Drop",() => new DragAndDropPage(), Icons.DragDrop),
            new PageItem("DrawerPage",() => new DrawerDemoPage(), Icons.Drawer),
            new PageItem("Expander",() => new ExpanderPage(), Icons.Expand),
            new PageItem("Flyouts",() => new FlyoutsPage(), Icons.Flyout),
            new PageItem("Focus",() => new FocusPage(), Icons.Target),
            new PageItem("Gestures",() => new GesturePage(), Icons.Gesture),
            new PageItem("Image",() => new ImagePage(), Icons.Image),
            new PageItem("Label",() => new LabelsPage(), Icons.Tag),
            new PageItem("LayoutTransformControl",() => new LayoutTransformControlPage(), Icons.Transform),
            new PageItem("ListBox",() => new ListBoxPage(), Icons.List),
            new PageItem("Menu",() => new MenuPage(), Icons.Menu),
            new PageItem("NavigationPage",() => new NavigationDemoPage(), Icons.Navigation),
            new PageItem("Notifications",() => new NotificationsPage(), Icons.Bell),
            new PageItem("NumericUpDown",() => new NumericUpDownPage(), Icons.Number),
            new PageItem("OpenGL",() => new OpenGlPage(), Icons.Cube3D),
            new PageItem("OpenGL Lease",() => new OpenGlLeasePage(), Icons.Cube3D),
            new PageItem("PipsPager",() => new PipsPagerPage(), Icons.HorizontalDots),
            new PageItem("Platform Information",() => new PlatformInfoPage(), Icons.Info),
            new PageItem("Pointers",() => new PointersPage(), Icons.Cursor),
            new PageItem("ProgressBar",() => new ProgressBarPage(), Icons.Progress),
            new PageItem("RadioButton",() => new RadioButtonPage(), Icons.Radio),
            new PageItem("RefreshContainer",() => new RefreshContainerPage(), Icons.Refresh),
            new PageItem("RelativePanel",() => new RelativePanelPage(), Icons.Layout),
            new PageItem("ScrollViewer",() => new ScrollViewerPage(), Icons.Scroll),
            new PageItem("Slider",() => new SliderPage(), Icons.Tune),
            new PageItem("SplitView",() => new SplitViewPage(), Icons.Split),
            new PageItem("TabbedPage",() => new TabbedDemoPage(), Icons.Tab),
            new PageItem("TabControl",() => new TabControlPage(), Icons.Tab),
            new PageItem("TabStrip",() => new TabStripPage(), Icons.Tab),
            new PageItem("TextBox",() => new TextBoxPage(), Icons.TextInput),
            new PageItem("TextBlock",() => new TextBlockPage(), Icons.TextInput),
            new PageItem("Theme Variants",() => new ThemePage(), Icons.Theme),
            new PageItem("ToggleSwitch",() => new ToggleSwitchPage(), Icons.Toggle),
            new PageItem("ToolTip",() => new ToolTipPage(), Icons.Tooltip),
            new PageItem("TransitioningContentControl",() => new TransitioningContentControlPage(), Icons.Transition),
            new PageItem("TreeView",() => new TreeViewPage(), Icons.Tree),
            new PageItem("Viewbox",() => new ViewboxPage(), Icons.Viewbox),
            new PageItem("Native Embed",() => new NativeEmbedPage(), Icons.Puzzle),
            new PageItem("Window Customizations",() => new WindowCustomizationsPage(), Icons.Window),
            new PageItem("HeaderedContentControl",() => new HeaderedContentPage(), Icons.Header),
            new PageItem("Screens",() => new ScreenPage(), Icons.Monitor),
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

    class PageItem(string header, Func<object> factory, string? iconData = null)
    {
        public string Header { get; } = header;
        public Func<object> Factory { get; } = factory;
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
    }
}
