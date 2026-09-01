using System;
using System.Collections.Generic;
using Avalonia.Controls;
using ControlCatalog.Pages;
using Avalonia.Media;
using ControlCatalog.Models;

namespace ControlCatalog.ViewModels;

partial class MainWindowViewModel
{
    private readonly HomeSection[] _pageSections =
    [
        Section("", s =>
        {
            s.Add<HomePage>("Home", Icons.Home, "Overview of everything in the catalog");
        }),
        Section("Basic Input", s =>
        {
            s.Add<ButtonsPage>("Buttons", Icons.CursorClick, "Button, RepeatButton, ToggleButton and friends");
            s.Add<ButtonSpinnerPage>("ButtonSpinner", Icons.Spinner, "Content with increment and decrement buttons");
            s.Add<CheckBoxPage>("CheckBox", Icons.Checkbox, "Two- and three-state check boxes");
            s.Add<ColorPickerPage>("ColorPicker", Icons.Palette, "Pick colors from spectrum and palette views");
            s.Add<ComboBoxPage>("ComboBox", Icons.Dropdown, "A drop-down list of selectable items");
            s.Add<NumericUpDownPage>("NumericUpDown", Icons.Number, "Numeric input with spinner buttons");
            s.Add<RadioButtonPage>("RadioButton", Icons.Radio, "Mutually exclusive option groups");
            s.Add<SliderPage>("Slider", Icons.Tune, "Select a value from a continuous range");
            s.Add<ToggleSwitchPage>("ToggleSwitch", Icons.Toggle, "An on/off switch with a sliding knob");
        }),
        Section("Text", s => 
        {
            s.Add<AutoCompleteBoxPage>("AutoCompleteBox", Icons.TextInput, "Text input with completion suggestions");
            s.Add<LabelsPage>("Label", Icons.Tag, "Captions with access keys for other controls");
            s.Add<TextBoxPage>("TextBox", Icons.TextInput, "Single- and multi-line text editing");
            s.Add<TextBlockPage>("TextBlock", Icons.TextInput, "Styled read-only text display");
        }),
        Section("Collections & Data", s => 
        {
            s.Add<Pages.CarouselPage>("Carousel", Icons.Slides, "Cycle through a collection of items");
            s.Add<ListBoxPage>("ListBox", Icons.List, "A selectable, virtualized list of items");
            s.Add<PipsPagerPage>("PipsPager", Icons.HorizontalDots, "Dot-style pager for paginated content");
            s.Add<RefreshContainerPage>("RefreshContainer", Icons.Refresh, "Pull-to-refresh for scrollable content");
            s.Add<TableViewPage>("TableView", Icons.Grid, "Tabular data with resizable, sortable columns");
            s.Add<TreeViewPage>("TreeView", Icons.Tree, "Hierarchical data with expandable nodes");
        }),
        Section("Date & Time", s => 
        {
            s.Add<CalendarPage>("Calendar", Icons.Calendar, "A month calendar for selecting dates");
            s.Add<CalendarDatePickerPage>("CalendarDatePicker", Icons.Calendar, "A date picker with a drop-down calendar");
            s.Add<DateTimePickerPage>("Date/Time Picker", Icons.Clock, "Spinner-style date and time pickers");
        }),
        Section("Menus & Flyouts", s => 
        {
            s.Add<CommandBarPage>("CommandBar", Icons.Terminal, "A toolbar of commands with an overflow menu");
            s.Add<ContextFlyoutPage>("ContextFlyout", Icons.Menu, "Attach flyouts shown on right-click");
            s.Add<ContextMenuPage>("ContextMenu", Icons.Menu, "Traditional right-click context menus");
            s.Add<FlyoutsPage>("Flyouts", Icons.Flyout, "Lightweight popups anchored to controls");
            s.Add<MenuPage>("Menu", Icons.Menu, "Menu bars with nested menu items");
        }),
        Section("Navigation & Pages", s => 
        {
            s.Add<CarouselDemoPage>("CarouselPage", Icons.Slides, "Swipeable page-based navigation");
            s.Add<ContentDemoPage>("ContentPage", Icons.Document, "A page that hosts a single content view");
            s.Add<DrawerDemoPage>("DrawerPage", Icons.Drawer, "A page with a sliding navigation drawer");
            s.Add<NavigationDemoPage>("NavigationPage", Icons.Navigation, "Stack-based page navigation");
            s.Add<SplitViewPage>("SplitView", Icons.Split, "A collapsible pane beside content");
            s.Add<TabbedDemoPage>("TabbedPage", Icons.Tab, "Tab-based page navigation");
            s.Add<TabControlPage>("TabControl", Icons.Tab, "Switch between tabbed content views");
            s.Add<TabStripPage>("TabStrip", Icons.Tab, "A standalone strip of selectable tabs");
        }),
        Section("Layout", s => 
        {
            s.Add<BorderPage>("Border", Icons.Border, "Decorate elements with borders and corner radii");
            s.Add<CanvasPage>("Canvas", Icons.Canvas, "Position children at explicit coordinates");
            s.Add<ContainerQueryPage>("Container Queries", Icons.Container, "Styles that respond to container size");
            s.Add<ExpanderPage>("Expander", Icons.Expand, "A header that expands to reveal content");
            s.Add<FlexPage>("Flex Panel", Icons.Grid, "Flexible, CSS-style child layout");
            s.Add<HeaderedContentPage>("HeaderedContentControl", Icons.Header, "Content paired with a header");
            s.Add<LayoutTransformControlPage>("LayoutTransformControl", Icons.Transform, "Apply transforms that affect layout");
            s.Add<RelativePanelPage>("RelativePanel", Icons.Layout, "Arrange children relative to each other");
            s.Add<ScrollViewerPage>("ScrollViewer", Icons.Scroll, "Scrollable viewport over large content");
            s.Add<ViewboxPage>("Viewbox", Icons.Viewbox, "Scale content to fit available space");
            s.Add<WrapPanelPage>("WrapPanel", Icons.Layout, "Wrap children onto multiple lines");
        }),
        Section("Media & Graphics", s => 
        {
            s.Add<AcrylicPage>("Acrylic", Icons.Blur, "Translucent acrylic window materials");
            s.Add<BitmapCachePage>("BitmapCache", Icons.Lightning, "Cache visuals as bitmaps for performance");
            s.Add<CompositionPage>("Composition", Icons.Layers, "Composition-layer animations and effects");
            s.Add<CustomDrawing>("Custom Drawing", Icons.Brush, "Render custom geometry in code");
            s.Add<ImagePage>("Image", Icons.Image, "Display bitmaps with different stretch modes");
            s.Add<OpenGlPage>("OpenGL", Icons.Cube3D, "Embed custom OpenGL rendering");
            s.Add<OpenGlLeasePage>("OpenGL Lease", Icons.Cube3D, "Low-level access to the OpenGL context");
            s.Add<TransitioningContentControlPage>("TransitioningContentControl", Icons.Transition, "Animate between content changes");
        }),
        Section("Status & Feedback", s => 
        {
            s.Add<AdornerLayerPage>("AdornerLayer", Icons.Sparkle, "Overlay visuals on top of other controls");
            s.Add<DataValidationPage>("Data Validation", Icons.Shield, "Display validation errors from bindings");
            s.Add<DialogsPage>("Dialogs", Icons.Dialog, "File pickers and modal dialog windows");
            s.Add<NotificationsPage>("Notifications", Icons.Bell, "Toast-style in-app notifications");
            s.Add<ProgressBarPage>("ProgressBar", Icons.Progress, "Determinate and indeterminate progress");
            s.Add<ToolTipPage>("ToolTip", Icons.Tooltip, "Hover hints for any control");
        }),
        Section("Interaction", s => 
        {
            s.Add<AcceleratorPage>("Accelerator", Icons.Keyboard, "Keyboard shortcuts that invoke commands");
            s.Add<ClipboardPage>("Clipboard", Icons.Clipboard, "Read from and write to the system clipboard");
            s.Add<CursorPage>("Cursor", Icons.Cursor, "Change the pointer cursor over elements");
            s.Add<DragAndDropPage>("Drag+Drop", Icons.DragDrop, "Drag data within and between applications");
            s.Add<FocusPage>("Focus", Icons.Target, "Track and control keyboard focus");
            s.Add<GesturePage>("Gestures", Icons.Gesture, "Tap, scroll and pinch gesture recognition");
            s.Add<PointersPage>("Pointers", Icons.Cursor, "Raw pointer input and capture");
        }),
        Section("Window & Platform", s => 
        {
            s.Add<NativeEmbedPage>("Native Embed", Icons.Puzzle, "Host native platform controls");
            s.Add<PlatformInfoPage>("Platform Information", Icons.Info, "Runtime platform and capability info");
            s.Add<PlatformSettingsPage>("Platform Settings", Icons.Tune, "Platform-specific system settings");
            s.Add<ScreenPage>("Screens", Icons.Monitor, "Enumerate displays and their bounds");
            s.Add<ThemePage>("Theme Variants", Icons.Theme, "Switch between light and dark variants");
            s.Add<WindowCustomizationsPage>("Window Customizations", Icons.Window, "Custom chrome, decorations and sizing");
        })
    ];

    private static HomeSection Section(string title, Action<HomeSectionBuilder> builderCallback)
    {
        var builder = new HomeSectionBuilder(title);
        builderCallback(builder);
        return new HomeSection(title, builder.ToArray());
    }

    private class HomeSectionBuilder(string title) : List<PageItem>
    {
        public void Add<TPageType>(string header, string iconPath, string description) where TPageType : Page, new()
        {
            var iconGeometry = StreamGeometry.Parse(iconPath);
            Add(new PageItem(header, () => new TPageType(), iconGeometry, description, title));
        }
    }
}
