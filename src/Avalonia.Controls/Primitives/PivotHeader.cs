using System;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Rendering.Composition;

namespace Avalonia.Controls.Primitives
{
    public class PivotHeader : SelectingItemsControl
    {
        private static readonly FuncTemplate<Panel> DefaultPanel =
            new FuncTemplate<Panel>(() => new VirtualizingStackPanel { Orientation = Orientation.Horizontal });

        public static readonly StyledProperty<PivotHeaderPlacement> PivotHeaderPlacementProperty =
            Pivot.PivotHeaderPlacementProperty.AddOwner<PivotHeader>();

        public PivotHeaderPlacement PivotHeaderPlacement
        {
            get { return GetValue(PivotHeaderPlacementProperty); }
        }

        static PivotHeader()
        {
            SelectionModeProperty.OverrideDefaultValue<PivotHeader>(SelectionMode.AlwaysSelected);
            FocusableProperty.OverrideDefaultValue(typeof(PivotHeader), false);
            ItemsPanelProperty.OverrideDefaultValue<PivotHeader>(DefaultPanel);
        }

        protected internal override Control CreateContainerForItemOverride() => new PivotHeaderItem();
        protected internal override bool IsItemItsOwnContainerOverride(Control item) => item is PivotHeaderItem;
        protected internal override void PrepareContainerForItemOverride(Control element, object? item, int index)
        {
            if (element is PivotHeaderItem pivotHeaderItem)
            {
                if(ItemTemplate is { } it)
                    pivotHeaderItem.ContentTemplate = it;

                pivotHeaderItem.SetValue(PivotHeaderItem.PivotHeaderPlacementProperty, PivotHeaderPlacement);
            }
            base.PrepareContainerForItemOverride(element, item, index);
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

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if(change.Property == PivotHeaderPlacementProperty)
            {
                Presenter?.Refresh();
            }
        }
    }
}
