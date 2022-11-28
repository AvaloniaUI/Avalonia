using System;
using System.Diagnostics;

namespace Avalonia.Controls.Presenters
{
    /// <summary>
    /// Presents items inside an <see cref="Avalonia.Controls.ItemsControl"/>.
    /// </summary>
    public class ItemsPresenter : Control
    {
        /// <summary>
        /// Defines the <see cref="ItemsPanel"/> property.
        /// </summary>
        public static readonly StyledProperty<ITemplate<Panel>> ItemsPanelProperty =
            ItemsControl.ItemsPanelProperty.AddOwner<ItemsPresenter>();

        private PanelContainerGenerator? _generator;

        /// <summary>
        /// Gets or sets a template which creates the <see cref="Panel"/> used to display the items.
        /// </summary>
        public ITemplate<Panel> ItemsPanel
        {
            get => GetValue(ItemsPanelProperty);
            set => SetValue(ItemsPanelProperty, value);
        }

        /// <summary>
        /// Gets the panel used to display the items.
        /// </summary>
        public Panel? Panel { get; private set; }

        /// <summary>
        /// Gets the owner <see cref="ItemsControl"/>.
        /// </summary>
        internal ItemsControl? ItemsControl { get; private set; }

        public override sealed void ApplyTemplate()
        {
            if (Panel is null && ItemsControl is not null)
            {
                Panel = ItemsPanel.Build();
                Panel.SetValue(TemplatedParentProperty, TemplatedParent);
                LogicalChildren.Add(Panel);
                VisualChildren.Add(Panel);

                if (Panel is VirtualizingPanel v)
                    v.Attach(ItemsControl);
                else
                    CreateSimplePanelGenerator();
            }
        }

        internal void ScrollIntoView(int index)
        {
            if (Panel is VirtualizingPanel v)
                v.ScrollIntoView(index);
            else if (index >= 0 && index < Panel?.Children.Count)
                Panel.Children[index].BringIntoView();
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == TemplatedParentProperty)
            {
                ResetState();
                ItemsControl = null;

                if (change.NewValue is ItemsControl itemsControl)
                {
                    ItemsControl = itemsControl;
                    ((IItemsPresenterHost)itemsControl)?.RegisterItemsPresenter(this);
                }
            }
            else if (change.Property == ItemsPanelProperty)
            {
                ResetState();
                InvalidateMeasure();
            }
        }

        private void ResetState()
        {
            _generator?.Dispose();
            _generator = null;
            LogicalChildren.Clear();
            VisualChildren.Clear();
            (Panel as VirtualizingPanel)?.Detach();
            Panel = null;
        }

        private void CreateSimplePanelGenerator()
        {
            Debug.Assert(Panel is not VirtualizingPanel);

            if (ItemsControl is null || Panel is null)
                return;

            _generator?.Dispose();
            _generator = new(this);
        }

        internal Control? ContainerFromIndex(int index)
        {
            if (Panel is VirtualizingPanel v)
                return v.ContainerFromIndex(index);
            return index >= 0 && index < Panel?.Children.Count ? Panel.Children[index] : null;
        }

        internal int IndexFromContainer(Control container)
        {
            if (Panel is VirtualizingPanel v)
                return v.IndexFromContainer(container);
            return Panel?.Children.IndexOf(container) ?? -1;
        }

    }
}
