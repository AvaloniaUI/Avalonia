// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls.Generators;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;

namespace Avalonia.Controls
{
    /// <summary>
    /// A tab control that displays a tab strip along with the content of the selected tab.
    /// </summary>
    public class TabControl : SelectingItemsControl, IContentPresenterHost
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
            new FuncTemplate<IPanel>(() => new WrapPanel());

        /// <summary>
        /// Initializes static members of the <see cref="TabControl"/> class.
        /// </summary>
        static TabControl()
        {
            SelectionModeProperty.OverrideDefaultValue<TabControl>(SelectionMode.AlwaysSelected);
            ItemsPanelProperty.OverrideDefaultValue<TabControl>(DefaultPanel);
            AffectsMeasure<TabControl>(TabStripPlacementProperty);
            SelectedIndexProperty.Changed.AddClassHandler<TabControl>((x, e) => x.UpdateSelectedContent(e));
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
        /// Gets or sets the tabstrip placement of the TabControl.
        /// </summary>
        public Dock TabStripPlacement
        {
            get { return GetValue(TabStripPlacementProperty); }
            set { SetValue(TabStripPlacementProperty, value); }
        }

        /// <summary>
        /// Gets or sets the default data template used to display the content of the selected tab.
        /// </summary>
        public IDataTemplate ContentTemplate
        {
            get { return GetValue(ContentTemplateProperty); }
            set { SetValue(ContentTemplateProperty, value); }
        }

        /// <summary>
        /// Gets or sets the content of the selected tab.
        /// </summary>
        /// <value>
        /// The content of the selected tab.
        /// </value>
        public object SelectedContent
        {
            get { return GetValue(SelectedContentProperty); }
            internal set { SetValue(SelectedContentProperty, value); }
        }

        /// <summary>
        /// Gets or sets the content template for the selected tab.
        /// </summary>
        /// <value>
        /// The content template of the selected tab.
        /// </value>
        public IDataTemplate SelectedContentTemplate
        {
            get { return GetValue(SelectedContentTemplateProperty); }
            internal set { SetValue(SelectedContentTemplateProperty, value); }
        }

        internal ItemsPresenter ItemsPresenterPart { get; private set; }

        internal IContentPresenter ContentPart { get; private set; }

        /// <inheritdoc/>
        IAvaloniaList<ILogical> IContentPresenterHost.LogicalChildren => LogicalChildren;

        /// <inheritdoc/>
        bool IContentPresenterHost.RegisterContentPresenter(IContentPresenter presenter)
        {
            return RegisterContentPresenter(presenter);
        }

        protected override void OnContainersMaterialized(ItemContainerEventArgs e)
        {
            base.OnContainersMaterialized(e);

            if (SelectedContent != null || SelectedIndex == -1)
            {
                return;
            }

            var container = (TabItem)ItemContainerGenerator.ContainerFromIndex(SelectedIndex);

            if (container == null)
            {
                return;
            }

            UpdateSelectedContent(container);
        }

        private void UpdateSelectedContent(AvaloniaPropertyChangedEventArgs e)
        {
            var index = (int)e.NewValue;

            if (index == -1)
            {
                SelectedContentTemplate = null;

                SelectedContent = null;

                return;
            }

            var container = (TabItem)ItemContainerGenerator.ContainerFromIndex(index);

            if (container == null)
            {
                return;
            }

            UpdateSelectedContent(container);
        }

        private void UpdateSelectedContent(IContentControl item)
        {
            if (SelectedContentTemplate != item.ContentTemplate)
            {
                SelectedContentTemplate = item.ContentTemplate;
            }

            if (SelectedContent != item.Content)
            {
                SelectedContent = item.Content;
            }
        }

        /// <summary>
        /// Called when an <see cref="IContentPresenter"/> is registered with the control.
        /// </summary>
        /// <param name="presenter">The presenter.</param>
        protected virtual bool RegisterContentPresenter(IContentPresenter presenter)
        {
            if (presenter.Name == "PART_SelectedContentHost")
            {
                ContentPart = presenter;
                return true;
            }

            return false;
        }

        protected override IItemContainerGenerator CreateItemContainerGenerator()
        {
            return new TabItemContainerGenerator(this);
        }

        protected override void OnTemplateApplied(TemplateAppliedEventArgs e)
        {
            base.OnTemplateApplied(e);

            ItemsPresenterPart = e.NameScope.Get<ItemsPresenter>("PART_ItemsPresenter");
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

            if (e.MouseButton == MouseButton.Left && e.Pointer.Type == PointerType.Mouse)
            {
                e.Handled = UpdateSelectionFromEventSource(e.Source);
            }
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            if (e.InitialPressMouseButton == MouseButton.Left && e.Pointer.Type != PointerType.Mouse)
            {
                var container = GetContainerFromEventSource(e.Source);
                if (container != null
                    && container.GetVisualsAt(e.GetPosition(container))
                        .Any(c => container == c || container.IsVisualAncestorOf(c)))
                {
                    e.Handled = UpdateSelectionFromEventSource(e.Source);
                }
            }
        }
    }
}
