using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;

namespace Avalonia.Controls
{
    /// <summary>
    /// A top-level menu control.
    /// </summary>
    public class Menu : MenuBase, IMainMenu
    {
        private static readonly FuncTemplate<Panel?> DefaultPanel =
            new (() => new StackPanel { Orientation = Orientation.Horizontal });

        /// <summary>
        /// Initializes a new instance of the <see cref="Menu"/> class.
        /// </summary>
        public Menu()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Menu"/> class.
        /// </summary>
        /// <param name="interactionHandler">The menu interaction handler.</param>
        public Menu(IMenuInteractionHandler interactionHandler)
            : base(interactionHandler)
        {
        }

        static Menu()
        {
            ItemsPanelProperty.OverrideDefaultValue(typeof(Menu), DefaultPanel);
            KeyboardNavigation.TabNavigationProperty.OverrideDefaultValue(
                typeof(Menu),
                KeyboardNavigationMode.Once);
            AutomationProperties.AccessibilityViewProperty.OverrideDefaultValue<Menu>(AccessibilityView.Control);
            AutomationProperties.ControlTypeOverrideProperty.OverrideDefaultValue<Menu>(AutomationControlType.Menu);
        }

        /// <inheritdoc/>
        public override void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            foreach (var i in ((IMenu)this).SubItems)
            {
                i.Close();
            }

            IsOpen = false;
            SelectedIndex = -1;

            RaiseEvent(new RoutedEventArgs
            {
                RoutedEvent = ClosedEvent,
                Source = this,
            });
        }

        /// <inheritdoc/>
        public override void Open()
        {
            if (IsOpen)
            {
                return;
            }

            IsOpen = true;

            RaiseEvent(new RoutedEventArgs
            {
                RoutedEvent = OpenedEvent,
                Source = this,
            });
        }

        /// <inheritdoc/>
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            var inputRoot = e.Root as TopLevel;

            if (inputRoot?.AccessKeyHandler != null)
            {
                inputRoot.AccessKeyHandler.MainMenu = this;
            }
        }

        protected internal override void PrepareContainerForItemOverride(Control element, object? item, int index)
        {
            base.PrepareContainerForItemOverride(element, item, index);

            // Child menu items should not inherit the menu's ItemContainerTheme as that is specific
            // for top-level menu items.
            if ((element as MenuItem)?.ItemContainerTheme == ItemContainerTheme)
                element.ClearValue(ItemContainerThemeProperty);
        }
    }
}
