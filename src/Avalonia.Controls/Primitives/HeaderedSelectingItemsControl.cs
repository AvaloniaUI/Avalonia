using System;
using Avalonia.Collections;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.LogicalTree;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// Represents a <see cref="SelectingItemsControl"/> with a related header.
    /// </summary>
    public class HeaderedSelectingItemsControl : SelectingItemsControl, IContentPresenterHost
    {
        private IDisposable? _itemsBinding;
        private ItemsControl? _prepareItemContainerOnAttach;

        /// <summary>
        /// Defines the <see cref="Header"/> property.
        /// </summary>
        public static readonly StyledProperty<object?> HeaderProperty =
            HeaderedContentControl.HeaderProperty.AddOwner<HeaderedSelectingItemsControl>();

        /// <summary>
        /// Defines the <see cref="HeaderTemplate"/> property.
        /// </summary>
        public static readonly StyledProperty<IDataTemplate?> HeaderTemplateProperty =
            HeaderedItemsControl.HeaderTemplateProperty.AddOwner<HeaderedSelectingItemsControl>();

        /// <summary>
        /// Initializes static members of the <see cref="ContentControl"/> class.
        /// </summary>
        static HeaderedSelectingItemsControl()
        {
            HeaderProperty.Changed.AddClassHandler<HeaderedSelectingItemsControl>((x, e) => x.HeaderChanged(e));
        }

        /// <summary>
        /// Gets or sets the content of the control's header.
        /// </summary>
        public object? Header
        {
            get => GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        /// <summary>
        /// Gets or sets the data template used to display the header content of the control.
        /// </summary>
        public IDataTemplate? HeaderTemplate
        {
            get => GetValue(HeaderTemplateProperty);
            set => SetValue(HeaderTemplateProperty, value);
        }

        /// <summary>
        /// Gets the header presenter from the control's template.
        /// </summary>
        public ContentPresenter? HeaderPresenter
        {
            get;
            private set;
        }

        /// <inheritdoc/>
        IAvaloniaList<ILogical> IContentPresenterHost.LogicalChildren => LogicalChildren;

        /// <inheritdoc/>
        bool IContentPresenterHost.RegisterContentPresenter(ContentPresenter presenter)
        {
            return RegisterContentPresenter(presenter);
        }

        protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnAttachedToLogicalTree(e);

            if (_prepareItemContainerOnAttach is not null)
            {
                PrepareItemContainer(_prepareItemContainerOnAttach);
                _prepareItemContainerOnAttach = null;
            }
        }

        /// <summary>
        /// Called when an <see cref="ContentPresenter"/> is registered with the control.
        /// </summary>
        /// <param name="presenter">The presenter.</param>
        protected virtual bool RegisterContentPresenter(ContentPresenter presenter)
        {
            if (presenter.Name == "PART_HeaderPresenter")
            {
                HeaderPresenter = presenter;
                return true;
            }

            return false;
        }

        internal void PrepareItemContainer(ItemsControl parent)
        {
            _itemsBinding?.Dispose();
            _itemsBinding = null;

            var item = Header;

            if (item is null)
            {
                _prepareItemContainerOnAttach = null;
                return;
            }

            var headerTemplate = HeaderTemplate ?? parent.ItemTemplate;

            if (headerTemplate is null)
            {
                if (((ILogical)this).IsAttachedToLogicalTree)
                    headerTemplate = this.FindDataTemplate(item);
                else
                    _prepareItemContainerOnAttach = parent;
            }

            if (headerTemplate is ITreeDataTemplate treeTemplate &&
                treeTemplate.Match(item) &&
                treeTemplate.ItemsSelector(item) is { } itemsBinding)
            {
                _itemsBinding = BindingOperations.Apply(this, ItemsSourceProperty, itemsBinding, null);
            }
        }

        private void HeaderChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (e.OldValue is ILogical oldChild)
            {
                LogicalChildren.Remove(oldChild);
            }

            if (e.NewValue is ILogical newChild)
            {
                LogicalChildren.Add(newChild);
            }
        }
    }
}
