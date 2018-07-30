// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Controls.Generators;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Layout;

namespace Avalonia.Controls
{
    using System;
    using System.Linq;

    using Avalonia.Input;

    /// <summary>
    /// A tab control that displays a tab strip along with the content of the selected tab.
    /// </summary>
    public class TabControl : SelectingItemsControl
    {
        /// <summary>
        /// Defines the <see cref="TabStripPlacement"/> property.
        /// </summary>
        public static readonly StyledProperty<Dock> TabStripPlacementProperty =
            AvaloniaProperty.Register<TabControl, Dock>(nameof(TabStripPlacement), defaultValue: Dock.Top);

        /// <summary>
        /// Defines the <see cref="HorizontalContentAlignment"/> property.
        /// </summary>
        public static readonly StyledProperty<HorizontalAlignment> HorizontalContentAlignmentProperty =
            ContentControl.HorizontalContentAlignmentProperty.AddOwner<TabControl>();

        /// <summary>
        /// Defines the <see cref="VerticalContentAlignment"/> property.
        /// </summary>
        public static readonly StyledProperty<VerticalAlignment> VerticalContentAlignmentProperty =
            ContentControl.VerticalContentAlignmentProperty.AddOwner<TabControl>();

        /// <summary>
        /// Defines the <see cref="ContentTemplate"/> property.
        /// </summary>
        public static readonly StyledProperty<IDataTemplate> ContentTemplateProperty =
            AvaloniaProperty.Register<TabControl, IDataTemplate>(nameof(ContentTemplate));

        public static readonly StyledProperty<object> SelectedContentProperty =
            AvaloniaProperty.Register<TabControl, object>(nameof(SelectedContent));

        public static readonly StyledProperty<IDataTemplate> SelectedContentTemplateProperty =
            AvaloniaProperty.Register<TabControl, IDataTemplate>(nameof(SelectedContentTemplate));

        /// <summary>
        /// The default value for the <see cref="ItemsControl.ItemsPanel"/> property.
        /// </summary>
        private static readonly FuncTemplate<IPanel> DefaultPanel =
            new FuncTemplate<IPanel>(() => new WrapPanel { Orientation = Orientation.Horizontal });

        internal ItemsPresenter TabStripPart { get; private set; }

        internal ContentPresenter ContentPart { get; private set; }

        /// <summary>
        /// Initializes static members of the <see cref="TabControl"/> class.
        /// </summary>
        static TabControl()
        {
            SelectionModeProperty.OverrideDefaultValue<TabControl>(SelectionMode.AlwaysSelected);
            ItemsPanelProperty.OverrideDefaultValue<TabControl>(DefaultPanel);
            SelectionChangedEvent.AddClassHandler<TabControl>(x => x.OnSelectionChanged);
            AffectsMeasure(TabStripPlacementProperty);
        }

        /// <summary>
        /// Gets or sets the horizontal alignment of the content within the control.
        /// </summary>
        public HorizontalAlignment HorizontalContentAlignment
        {
            get { return GetValue(HorizontalContentAlignmentProperty); }
            set { SetValue(HorizontalContentAlignmentProperty, value); }
        }

        /// <summary>
        /// Gets or sets the vertical alignment of the content within the control.
        /// </summary>
        public VerticalAlignment VerticalContentAlignment
        {
            get { return GetValue(VerticalContentAlignmentProperty); }
            set { SetValue(VerticalContentAlignmentProperty, value); }
        }

        /// <summary>
        /// Gets or sets the tabstrip placement of the tabcontrol.
        /// </summary>
        public Dock TabStripPlacement
        {
            get { return GetValue(TabStripPlacementProperty); }
            set { SetValue(TabStripPlacementProperty, value); }
        }

        /// <summary>
        /// Gets or sets the data template used to display the content of the control.
        /// </summary>
        public IDataTemplate ContentTemplate
        {
            get { return GetValue(ContentTemplateProperty); }
            set { SetValue(ContentTemplateProperty, value); }
        }

        /// <summary>
        /// Gets or sets the currently selected content.
        /// </summary>
        /// <value>
        /// The content of the selected.
        /// </value>
        public object SelectedContent
        {
            get { return GetValue(SelectedContentProperty); }
            set { SetValue(SelectedContentProperty, value); }
        }

        /// <summary>
        /// Gets or sets the template for the currently selected content.
        /// </summary>
        /// <value>
        /// The selected content template.
        /// </value>
        public IDataTemplate SelectedContentTemplate
        {
            get { return GetValue(SelectedContentTemplateProperty); }
            set { SetValue(SelectedContentTemplateProperty, value); }
        }

        protected override IItemContainerGenerator CreateItemContainerGenerator()
        {
            return new ItemContainerGenerator<TabItem>(
                this,
                HeaderedContentControl.HeaderProperty,
                HeaderedContentControl.HeaderTemplateProperty);
        }

        protected override void OnTemplateApplied(TemplateAppliedEventArgs e)
        {
            base.OnTemplateApplied(e);

            TabStripPart = e.NameScope.Find<ItemsPresenter>("PART_TabStrip");

            if (TabStripPart == null)
            {
                throw new NotSupportedException("ItemsPresenter not found.");
            }

            ContentPart = e.NameScope.Find<ContentPresenter>("PART_Content");

            if (ContentPart == null)
            {
                throw new NotSupportedException("ContentPresenter not found.");
            }
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

            if (e.MouseButton == MouseButton.Left)
            {
                e.Handled = UpdateSelectionFromEventSource(e.Source);
            }
        }

        private void OnSelectionChanged(SelectionChangedEventArgs obj)
        {
            if (obj.AddedItems.Count > 0)
            {
                if (!(obj.AddedItems[0] is TabItem selectedTapItem))
                {
                    var containerInfo =
                        ItemContainerGenerator.Containers.SingleOrDefault(x => x.Item == obj.AddedItems[0]);

                    if (containerInfo == null)
                    {
                        return;
                    }

                    selectedTapItem = containerInfo.ContainerControl as TabItem;
                }

                if (selectedTapItem == null)
                {
                    SelectedContent = null;

                    SelectedContentTemplate = null;

                    return;
                }

                SelectedContent = selectedTapItem.Content;

                SelectedContentTemplate = selectedTapItem.ContentTemplate ?? ContentTemplate;
            }
            else
            {
                SelectedContent = null;

                SelectedContentTemplate = null;
            }
        }
    }
}
