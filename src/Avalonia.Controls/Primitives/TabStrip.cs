using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;

namespace Avalonia.Controls.Primitives
{
    public class TabStrip : SelectingItemsControl
    {
        private static readonly FuncTemplate<Panel?> DefaultPanel =
            new(() => new WrapPanel { Orientation = Orientation.Horizontal });

        static TabStrip()
        {
            SelectionModeProperty.OverrideDefaultValue<TabStrip>(SelectionMode.AlwaysSelected);
            FocusableProperty.OverrideDefaultValue(typeof(TabStrip), false);
            ItemsPanelProperty.OverrideDefaultValue<TabStrip>(DefaultPanel);
        }

        protected internal override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey)
        {
            return new TabStripItem();
        }

        protected internal override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
        {
            return NeedsContainer<TabStripItem>(item, out recycleKey);
        }

        protected override bool ShouldTriggerSelection(Visual selectable, PointerEventArgs e) =>
            e.Properties.PointerUpdateKind is PointerUpdateKind.LeftButtonPressed or PointerUpdateKind.LeftButtonReleased && base.ShouldTriggerSelection(selectable, e);

        public override bool UpdateSelectionFromEvent(Control container, RoutedEventArgs eventArgs)
        {
            if (eventArgs is GotFocusEventArgs { NavigationMethod: not NavigationMethod.Directional })
            {
                return false;
            }

            return base.UpdateSelectionFromEvent(container, eventArgs);
        }
    }
}
