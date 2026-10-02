using ControlCatalog.Controls;

namespace ControlCatalog.Pages
{
    public partial class CommandBarPage : SampleGalleryPage
    {
        internal static readonly SampleInfo[] Demos =
        {
            // Overview
            new(SampleGroups.Overview, "First Look",     "A CommandBar with primary commands, secondary overflow menu, and custom content area.", () => new CommandBarFirstLookPage()),
            new(SampleGroups.Overview, "Toggle Buttons", "CommandBarToggleButton for stateful actions like Bold, Italic, and Favorite.",              () => new CommandBarTogglePage()),

            // Appearance
            new(SampleGroups.Appearance, "Label Positions", "Configure label position: Bottom (default), Right, or Collapsed (icon only).",       () => new CommandBarLabelPositionPage()),
            new(SampleGroups.Appearance, "Customization",   "Background, Foreground, BorderBrush, BorderThickness, and CornerRadius.",            () => new CommandBarCustomizationPage()),

            // Features
            new(SampleGroups.Features, "Overflow Menu",    "Secondary commands appear in an overflow popup. Configure visibility and sticky behavior.", () => new CommandBarOverflowPage()),
            new(SampleGroups.Features, "Dynamic Overflow", "IsDynamicOverflowEnabled moves primary commands to overflow as space shrinks.",             () => new CommandBarDynamicOverflowPage()),
            new(SampleGroups.Features, "Events & State",  "Observe Opening, Opened, Closing, and Closed while tracking IsOpen, HasSecondaryCommands, and IsOverflowButtonVisible.", () => new CommandBarEventsPage()),
            new(SampleGroups.Features, "Keyboard Navigation", "Up/Down to move between overflow items, Home/End to jump to first/last, Escape to close and return focus.", () => new CommandBarKeyboardPage()),
        };

        public CommandBarPage()
        {
            InitializeComponent();
            Samples = Demos;
        }
    }
}
