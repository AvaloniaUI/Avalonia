using System;
using System.Collections;
using System.Collections.Specialized;
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

        private ItemsPresenterContainerGenerator? _generator;

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
            if (Panel is null)
            {
                Panel = ItemsPanel.Build();
                Panel.SetValue(TemplatedParentProperty, TemplatedParent);
                LogicalChildren.Add(Panel);
                VisualChildren.Add(Panel);
                CreateGeneratorIfSimplePanel();
            }
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == TemplatedParentProperty)
            {
                _generator?.Dispose();
                _generator = null;

                if (change.NewValue is ItemsControl itemsControl)
                {
                    ItemsControl = itemsControl;
                    ((IItemsPresenterHost)itemsControl)?.RegisterItemsPresenter(this);
                    CreateGeneratorIfSimplePanel();
                }
            }
            else if (change.Property == ItemsPanelProperty)
            {
                _generator?.Dispose();
                _generator = null;
                LogicalChildren.Clear();
                VisualChildren.Clear();
                Panel = null;
                InvalidateMeasure();
            }
        }

        private void CreateGeneratorIfSimplePanel()
        {
            if (ItemsControl is null || Panel is null || Panel is IVirtualizingPanel)
                return;

            _generator?.Dispose();
            _generator = new(this);
        }
    }
}
