using Avalonia.Controls;
using ControlCatalog.Pages;
using Avalonia.Media;
using ControlCatalog.Models;

namespace ControlCatalog.ViewModels;

partial class MainWindowViewModel
{
    private readonly HomeSection[] _pageSections =
    [
        new("",
        [
            Page<HomePage>("Home", Icons.Home, "Overview of everything in the catalog"),
        ]),
        new("Basic Input",
        [
            Page<ButtonsPage>("Buttons", Icons.CursorClick, "Button, RepeatButton, ToggleButton and friends"),
            Page<ButtonSpinnerPage>("ButtonSpinner", Icons.Spinner, "Content with increment and decrement buttons"),
            Page<CheckBoxPage>("CheckBox", Icons.Checkbox, "Two- and three-state check boxes"),
            Page<ColorPickerPage>("ColorPicker", Icons.Palette, "Pick colors from spectrum and palette views"),
            Page<ComboBoxPage>("ComboBox", Icons.Dropdown, "A drop-down list of selectable items"),
            Page<NumericUpDownPage>("NumericUpDown", Icons.Number, "Numeric input with spinner buttons"),
            Page<RadioButtonPage>("RadioButton", Icons.Radio, "Mutually exclusive option groups"),
            Page<SliderPage>("Slider", Icons.Tune, "Select a value from a continuous range"),
            Page<ToggleSwitchPage>("ToggleSwitch", Icons.Toggle, "An on/off switch with a sliding knob"),
        ]),
        new("Text", 
        [
            Page<AutoCompleteBoxPage>("AutoCompleteBox", Icons.TextInput, "Text input with completion suggestions"),
            Page<LabelsPage>("Label", Icons.Tag, "Captions with access keys for other controls"),
            Page<TextBoxPage>("TextBox", Icons.TextInput, "Single- and multi-line text editing"),
            Page<TextBlockPage>("TextBlock", Icons.TextInput, "Styled read-only text display"),
        ]),
        new("Collections & Data", 
        [
            Page<Pages.CarouselPage>("Carousel", Icons.Slides, "Cycle through a collection of items"),
            Page<ListBoxPage>("ListBox", Icons.List, "A selectable, virtualized list of items"),
            Page<PipsPagerPage>("PipsPager", Icons.HorizontalDots, "Dot-style pager for paginated content"),
            Page<RefreshContainerPage>("RefreshContainer", Icons.Refresh, "Pull-to-refresh for scrollable content"),
            Page<TableViewPage>("TableView", Icons.Grid, "Tabular data with resizable, sortable columns"),
            Page<TreeViewPage>("TreeView", Icons.Tree, "Hierarchical data with expandable nodes"),
        ]),
        new("Date & Time", 
        [
            Page<CalendarPage>("Calendar", Icons.Calendar, "A month calendar for selecting dates"),
            Page<CalendarDatePickerPage>("CalendarDatePicker", Icons.Calendar, "A date picker with a drop-down calendar"),
            Page<DateTimePickerPage>("Date/Time Picker", Icons.Clock, "Spinner-style date and time pickers"),
        ]),
        new("Menus & Flyouts", 
        [
            Page<CommandBarPage>("CommandBar", Icons.Terminal, "A toolbar of commands with an overflow menu"),
            Page<ContextFlyoutPage>("ContextFlyout", Icons.Menu, "Attach flyouts shown on right-click"),
            Page<ContextMenuPage>("ContextMenu", Icons.Menu, "Traditional right-click context menus"),
            Page<FlyoutsPage>("Flyouts", Icons.Flyout, "Lightweight popups anchored to controls"),
            Page<MenuPage>("Menu", Icons.Menu, "Menu bars with nested menu items"),
        ]),
        new("Navigation & Pages", 
        [
            Page<CarouselDemoPage>("CarouselPage", Icons.Slides, "Swipeable page-based navigation"),
            Page<ContentDemoPage>("ContentPage", Icons.Document, "A page that hosts a single content view"),
            Page<DrawerDemoPage>("DrawerPage", Icons.Drawer, "A page with a sliding navigation drawer"),
            Page<NavigationDemoPage>("NavigationPage", Icons.Navigation, "Stack-based page navigation"),
            Page<SplitViewPage>("SplitView", Icons.Split, "A collapsible pane beside content"),
            Page<TabbedDemoPage>("TabbedPage", Icons.Tab, "Tab-based page navigation"),
            Page<TabControlPage>("TabControl", Icons.Tab, "Switch between tabbed content views"),
            Page<TabStripPage>("TabStrip", Icons.Tab, "A standalone strip of selectable tabs"),
        ]),
        new("Layout", 
        [
            Page<BorderPage>("Border", Icons.Border, "Decorate elements with borders and corner radii"),
            Page<CanvasPage>("Canvas", Icons.Canvas, "Position children at explicit coordinates"),
            Page<ContainerQueryPage>("Container Queries", Icons.Container, "Styles that respond to container size"),
            Page<ExpanderPage>("Expander", Icons.Expand, "A header that expands to reveal content"),
            Page<FlexPage>("Flex Panel", Icons.Grid, "Flexible, CSS-style child layout"),
            Page<HeaderedContentPage>("HeaderedContentControl", Icons.Header, "Content paired with a header"),
            Page<LayoutTransformControlPage>("LayoutTransformControl", Icons.Transform, "Apply transforms that affect layout"),
            Page<RelativePanelPage>("RelativePanel", Icons.Layout, "Arrange children relative to each other"),
            Page<ScrollViewerPage>("ScrollViewer", Icons.Scroll, "Scrollable viewport over large content"),
            Page<ViewboxPage>("Viewbox", Icons.Viewbox, "Scale content to fit available space"),
            Page<WrapPanelPage>("WrapPanel", Icons.Layout, "Wrap children onto multiple lines"),
        ]),
        new("Media & Graphics", 
        [
            Page<AcrylicPage>("Acrylic", Icons.Blur, "Translucent acrylic window materials"),
            Page<BitmapCachePage>("BitmapCache", Icons.Lightning, "Cache visuals as bitmaps for performance"),
            Page<CompositionPage>("Composition", Icons.Layers, "Composition-layer animations and effects"),
            Page<CustomDrawing>("Custom Drawing", Icons.Brush, "Render custom geometry in code"),
            Page<ImagePage>("Image", Icons.Image, "Display bitmaps with different stretch modes"),
            Page<OpenGlPage>("OpenGL", Icons.Cube3D, "Embed custom OpenGL rendering"),
            Page<OpenGlLeasePage>("OpenGL Lease", Icons.Cube3D, "Low-level access to the OpenGL context"),
            Page<TransitioningContentControlPage>("TransitioningContentControl", Icons.Transition, "Animate between content changes"),
        ]),
        new("Status & Feedback", 
        [
            Page<AdornerLayerPage>("AdornerLayer", Icons.Sparkle, "Overlay visuals on top of other controls"),
            Page<DataValidationPage>("Data Validation", Icons.Shield, "Display validation errors from bindings"),
            Page<DialogsPage>("Dialogs", Icons.Dialog, "File pickers and modal dialog windows"),
            Page<NotificationsPage>("Notifications", Icons.Bell, "Toast-style in-app notifications"),
            Page<ProgressBarPage>("ProgressBar", Icons.Progress, "Determinate and indeterminate progress"),
            Page<ToolTipPage>("ToolTip", Icons.Tooltip, "Hover hints for any control"),
        ]),
        new("Interaction", 
        [
            Page<AcceleratorPage>("Accelerator", Icons.Keyboard, "Keyboard shortcuts that invoke commands"),
            Page<ClipboardPage>("Clipboard", Icons.Clipboard, "Read from and write to the system clipboard"),
            Page<CursorPage>("Cursor", Icons.Cursor, "Change the pointer cursor over elements"),
            Page<DragAndDropPage>("Drag+Drop", Icons.DragDrop, "Drag data within and between applications"),
            Page<FocusPage>("Focus", Icons.Target, "Track and control keyboard focus"),
            Page<GesturePage>("Gestures", Icons.Gesture, "Tap, scroll and pinch gesture recognition"),
            Page<PointersPage>("Pointers", Icons.Cursor, "Raw pointer input and capture"),
        ]),
        new("Window & Platform", 
        [
            Page<NativeEmbedPage>("Native Embed", Icons.Puzzle, "Host native platform controls"),
            Page<PlatformInfoPage>("Platform Information", Icons.Info, "Runtime platform and capability info"),
            Page<PlatformSettingsPage>("Platform Settings", Icons.Tune, "Platform-specific system settings"),
            Page<ScreenPage>("Screens", Icons.Monitor, "Enumerate displays and their bounds"),
            Page<ThemePage>("Theme Variants", Icons.Theme, "Switch between light and dark variants"),
            Page<WindowCustomizationsPage>("Window Customizations", Icons.Window, "Custom chrome, decorations and sizing"),
        ])
    ];

    private static PageItem Page<TPageType>(string header, string iconPath, string description) where TPageType : Page, new()
    {
        var iconGeometry = StreamGeometry.Parse(iconPath);
        return new PageItem(header, () => new TPageType(), iconGeometry, description);
    }
}
