using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Dialogs;
using System;
using System.ComponentModel.DataAnnotations;
using Avalonia;
using MiniMvvm;
using Avalonia.Collections;
using ControlCatalog.Pages;
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
            new PageItem("Composition", () => new CompositionPage()),
            new PageItem("Accelerator", () => new AcceleratorPage()),
            new PageItem("Acrylic", () => new AcrylicPage()),
            new PageItem("AdornerLayer", () => new AdornerLayerPage()),
            new PageItem("AutoCompleteBox",() => new AutoCompleteBoxPage()),
            new PageItem("Border",() => new BorderPage()),
            new PageItem("BitmapCache",() => new BitmapCachePage()),
            new PageItem("Buttons",() => new ButtonsPage()),
            new PageItem("ButtonSpinner",() => new ButtonSpinnerPage()),
            new PageItem("Calendar",() => new CalendarPage()),
            new PageItem("Canvas",() => new CanvasPage()),
            new PageItem("CommandBar",() => new CommandBarPage()),
            new PageItem("Carousel",() => new CarouselDemoPage()),
            new PageItem("CarouselPage",() => new CarouselDemoPage()),
            new PageItem("CheckBox",() => new CheckBoxPage()),
            new PageItem("Clipboard",() => new ClipboardPage()),
            new PageItem("ColorPicker",() => new ColorPickerPage()),
            new PageItem("ComboBox",() => new ComboBoxPage()),
            new PageItem("Container Queries",() => new ContainerQueryPage()),
            new PageItem("ContentPage",() => new ContentDemoPage()),
            new PageItem("ContextFlyout",() => new ContextFlyoutPage()),
            new PageItem("ContextMenu",() => new ContextMenuPage()),
            new PageItem("Cursor",() => new CursorPage()),
            new PageItem("Custom Drawing",() => new CustomDrawing()),
            new PageItem("DataGrid",() => new DataGridPage()),
            new PageItem("Data Validation",() => new DataValidationPage()),
            new PageItem("Date/Time Picker",() => new DateTimePickerPage()),
            new PageItem("CalendarDatePicker",() => new CalendarDatePickerPage()),
            new PageItem("Dialogs",() => new DialogsPage()),
            new PageItem("Drag+Drop",() => new DragAndDropPage()),
            new PageItem("DrawerPage",() => new DrawerDemoPage()),
            new PageItem("Expander",() => new ExpanderPage()),
            new PageItem("Flyouts",() => new FlyoutsPage()),
            new PageItem("Focus",() => new FocusPage()),
            new PageItem("Gestures",() => new GesturePage()),
            new PageItem("Image",() => new ImagePage()),
            new PageItem("Label",() => new LabelsPage()),
            new PageItem("LayoutTransformControl",() => new LayoutTransformControlPage()),
            new PageItem("ListBox",() => new ListBoxPage()),
            new PageItem("Menu",() => new MenuPage()),
            new PageItem("NavigationPage",() => new NavigationDemoPage()),
            new PageItem("Notifications",() => new NotificationsPage()),
            new PageItem("NumericUpDown",() => new NumericUpDownPage()),
            new PageItem("OpenGL",() => new OpenGlPage()),
            new PageItem("OpenGL Lease",() => new OpenGlLeasePage()),
            new PageItem("PipsPager",() => new PipsPagerPage()),
            new PageItem("Platform Information",() => new PlatformInfoPage()),
            new PageItem("Pointers",() => new PointersPage()),
            new PageItem("ProgressBar",() => new ProgressBarPage()),
            new PageItem("RadioButton",() => new RadioButtonPage()),
            new PageItem("RefreshContainer",() => new RefreshContainerPage()),
            new PageItem("RelativePanel",() => new RelativePanelPage()),
            new PageItem("ScrollViewer",() => new ScrollViewerPage()),
            new PageItem("Slider",() => new SliderPage()),
            new PageItem("SplitView",() => new SplitViewPage()),
            new PageItem("TabbedPage",() => new TabbedDemoPage()),
            new PageItem("TabControl",() => new TabControlPage()),
            new PageItem("TabStrip",() => new TabStripPage()),
            new PageItem("TextBox",() => new TextBoxPage()),
            new PageItem("TextBlock",() => new TextBlockPage()),
            new PageItem("Theme Variants",() => new ThemePage()),
            new PageItem("ToggleSwitch",() => new ToggleSwitchPage()),
            new PageItem("ToolTip",() => new ToolTipPage()),
            new PageItem("TransitioningContentControl",() => new TransitioningContentControlPage()),
            new PageItem("TreeView",() => new TreeViewPage()),
            new PageItem("Viewbox",() => new ViewboxPage()),
            new PageItem("Native Embed",() => new NativeEmbedPage()),
            new PageItem("Window Customizations",() => new WindowCustomizationsPage()),
            new PageItem("HeaderedContentControl",() => new HeaderedContentPage()),
            new PageItem("Screens",() => new ScreenPage()),
        };

        public AvaloniaList<PageItem> Pages { get; } = new AvaloniaList<PageItem>();

        public void Filter(string? query = "")
        {
            try
            {
                _ignoreListChange = true;
                Pages.Clear();

                if (!string.IsNullOrWhiteSpace(query))
                    Pages.AddRange(_items.Where(x => x.Header.Contains(query)));
                else
                    Pages.AddRange(_items);
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

    class PageItem(string header, Func<object> factory)
    {
        public string Header { get; } = header;
        public Func<object> Factory { get; } = factory;

        public bool IsVisible { get; set; } = true;
    }
}
