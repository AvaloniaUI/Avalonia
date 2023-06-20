using Avalonia.Controls.Templates;
using Avalonia.Input;
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

        /// <inheritdoc/>
        protected override void OnGotFocus(GotFocusEventArgs e)
        {
            base.OnGotFocus(e);

            if (e.NavigationMethod == NavigationMethod.Directional)
            {
                e.Handled = UpdateSelectionFromEventSource(e.Source);
            }
        }

        /// <inheritdoc/>
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            if (e.Source is Visual source)
            {
                var point = e.GetCurrentPoint(source);

                if (point.Properties.IsLeftButtonPressed)
                {
                    e.Handled = UpdateSelectionFromEventSource(e.Source);
                }
            }
        }
    }
}
