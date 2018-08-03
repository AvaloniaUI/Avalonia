// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

using Avalonia.Controls.Generators;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;

namespace Avalonia.Controls
{
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
            ContentControl.ContentTemplateProperty.AddOwner<TabControl>();

        /// <summary>
        /// The selected content property
        /// </summary>
        public static readonly StyledProperty<object> SelectedContentProperty =
            AvaloniaProperty.Register<TabControl, object>(nameof(SelectedContent));

        /// <summary>
        /// The selected content template property
        /// </summary>
        public static readonly StyledProperty<IDataTemplate> SelectedContentTemplateProperty =
            AvaloniaProperty.Register<TabControl, IDataTemplate>(nameof(SelectedContentTemplate));

        /// <summary>
        /// The default value for the <see cref="ItemsControl.ItemsPanel"/> property.
        /// </summary>
        private static readonly FuncTemplate<IPanel> DefaultPanel =
            new FuncTemplate<IPanel>(() => new WrapPanel { Orientation = Orientation.Horizontal });

        internal ItemsPresenter ItemsPresenterPart { get; private set; }

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
            return new TabControlContainerGenerator(this);
        }

        private class TabControlContainerGenerator : ItemContainerGenerator<TabItem>
        {
            public TabControlContainerGenerator(TabControl owner)
                : base(owner, ContentControl.ContentProperty, ContentControl.ContentTemplateProperty)
            {
                Owner = owner;
            }

            public new TabControl Owner { get; }

            protected override IControl CreateContainer(object item)
            {
                var tabItem = (TabItem)base.CreateContainer(item);

                tabItem.ParentTabControl = Owner;

                if (tabItem.Header == null)
                {
                    if (item is IHeadered headered)
                    {
                        if (tabItem.Header != headered.Header)
                        {
                            tabItem.Header = headered.Header;
                        }
                    }
                    else
                    {
                        if (!(tabItem.DataContext is IControl))
                        {
                            tabItem.Header = tabItem.DataContext;
                        }
                    }
                }

                if (tabItem.Content == null)
                {
                    //Only update the ContentTemplate if no content is set otherwise the default template would be used for static content
                    if (tabItem.ContentTemplate == null)
                    {
                        tabItem[!TabItem.ContentTemplateProperty] = Owner[!TabControl.ContentTemplateProperty];
                    }

                    tabItem[!TabItem.ContentProperty] = tabItem[!TabItem.DataContextProperty];
                }

                return tabItem;
            }
        }

        protected override void OnTemplateApplied(TemplateAppliedEventArgs e)
        {
            base.OnTemplateApplied(e);

            ItemsPresenterPart = e.NameScope.Find<ItemsPresenter>("PART_ItemsPresenter");

            if (ItemsPresenterPart == null)
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
            if (obj.Source != this)
            {
                return;
            }

            if (obj.RemovedItems.Count > 0)
            {
                SelectedContent = null;

                SelectedContentTemplate = null;
            }
        }
    }
}
