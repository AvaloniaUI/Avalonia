using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Selection;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.VisualTree;

namespace Avalonia.Controls
{
    /// <summary>
    /// An <see cref="ItemsControl"/> in which individual items can be selected.
    /// </summary>
    [TemplatePart("PART_ScrollViewer", typeof(IScrollable))]
    public class ListBox : SelectingItemsControl
    {
        /// <summary>
        /// The default value for the <see cref="ItemsControl.ItemsPanel"/> property.
        /// </summary>
        private static readonly FuncTemplate<Panel?> DefaultPanel =
            new(() => new VirtualizingStackPanel());

        /// <summary>
        /// Defines the <see cref="Scroll"/> property.
        /// </summary>
        public static readonly DirectProperty<ListBox, IScrollable?> ScrollProperty =
            AvaloniaProperty.RegisterDirect<ListBox, IScrollable?>(nameof(Scroll), o => o.Scroll);

        /// <summary>
        /// Defines the <see cref="SelectedItems"/> property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("AvaloniaProperty", "AVP1010",
            Justification = "This property is owned by SelectingItemsControl, but protected there. ListBox changes its visibility.")]
        public static readonly new DirectProperty<SelectingItemsControl, IList?> SelectedItemsProperty =
            SelectingItemsControl.SelectedItemsProperty;

        /// <summary>
        /// Defines the <see cref="Selection"/> property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("AvaloniaProperty", "AVP1010",
            Justification = "This property is owned by SelectingItemsControl, but protected there. ListBox changes its visibility.")]
        public static readonly new DirectProperty<SelectingItemsControl, ISelectionModel> SelectionProperty =
            SelectingItemsControl.SelectionProperty;

        /// <summary>
        /// Defines the <see cref="SelectionMode"/> property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("AvaloniaProperty", "AVP1010",
            Justification = "This property is owned by SelectingItemsControl, but protected there. ListBox changes its visibility.")]
        public static readonly new StyledProperty<SelectionMode> SelectionModeProperty =
            SelectingItemsControl.SelectionModeProperty;

        private IScrollable? _scroll;

        /// <summary>
        /// Initializes static members of the <see cref="ItemsControl"/> class.
        /// </summary>
        static ListBox()
        {
            ItemsPanelProperty.OverrideDefaultValue<ListBox>(DefaultPanel);
            KeyboardNavigation.TabNavigationProperty.OverrideDefaultValue(
                typeof(ListBox),
                KeyboardNavigationMode.Once);
        }

        /// <summary>
        /// Gets the scroll information for the <see cref="ListBox"/>.
        /// </summary>
        public IScrollable? Scroll
        {
            get => _scroll;
            private set => SetAndRaise(ScrollProperty, ref _scroll, value);
        }

        /// <inheritdoc/>
        public new IList? SelectedItems
        {
            get => base.SelectedItems;
            set => base.SelectedItems = value;
        }

        /// <inheritdoc/>
        public new ISelectionModel Selection
        {
            get => base.Selection;
            set => base.Selection = value;
        }

        /// <summary>
        /// Gets or sets the selection mode.
        /// </summary>
        /// <remarks>
        /// Note that the selection mode only applies to selections made via user interaction.
        /// Multiple selections can be made programmatically regardless of the value of this property.
        /// </remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("AvaloniaProperty", "AVP1012",
            Justification = "This property is owned by SelectingItemsControl, but protected there. ListBox changes its visibility.")]
        public new SelectionMode SelectionMode
        {
            get => base.SelectionMode;
            set => base.SelectionMode = value;
        }

        /// <summary>
        /// Selects all items in the <see cref="ListBox"/>.
        /// </summary>
        public void SelectAll() => Selection.SelectAll();

        /// <summary>
        /// Deselects all items in the <see cref="ListBox"/>.
        /// </summary>
        public void UnselectAll() => Selection.Clear();

        protected internal override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey)
        {
            return new ListBoxItem();
        }

        protected internal override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
        {
            return NeedsContainer<ListBoxItem>(item, out recycleKey);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            var hotkeys = Application.Current!.PlatformSettings?.HotkeyConfiguration;
            var ctrl = hotkeys is not null && e.KeyModifiers.HasAllFlags(hotkeys.CommandModifiers);

            if (!ctrl &&
                e.Key.ToNavigationDirection() is { } direction && 
                direction.IsDirectional())
            {
                e.Handled |= MoveSelection(
                    direction,
                    WrapSelection,
                    e.KeyModifiers.HasAllFlags(KeyModifiers.Shift));
            }
            else if (SelectionMode.HasAllFlags(SelectionMode.Multiple) &&
                hotkeys is not null && hotkeys.SelectAll.Any(x => x.Matches(e)))
            {
                Selection.SelectAll();
                e.Handled = true;
            }
            else if (e.Key == Key.Space || e.Key == Key.Enter)
            {
                UpdateSelectionFromEventSource(
                    e.Source,
                    true,
                    e.KeyModifiers.HasFlag(KeyModifiers.Shift),
                    ctrl);
            }

            base.OnKeyDown(e);
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            Scroll = e.NameScope.Find<IScrollable>("PART_ScrollViewer");
        }

        internal bool UpdateSelectionFromPointerEvent(Control source, PointerEventArgs e)
        {
            var hotkeys = Application.Current!.PlatformSettings?.HotkeyConfiguration;
            var toggle = hotkeys is not null && e.KeyModifiers.HasAllFlags(hotkeys.CommandModifiers);

            return UpdateSelectionFromEventSource(
                source,
                true,
                e.KeyModifiers.HasAllFlags(KeyModifiers.Shift),
                toggle,
                e.GetCurrentPoint(source).Properties.IsRightButtonPressed);
        }
    }
}
