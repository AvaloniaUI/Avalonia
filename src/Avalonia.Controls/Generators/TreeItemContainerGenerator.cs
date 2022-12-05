using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.LogicalTree;

namespace Avalonia.Controls.Generators
{
    /// <summary>
    /// Creates containers for tree items and maintains a list of created containers.
    /// </summary>
    /// <typeparam name="T">The type of the container.</typeparam>
    public class TreeItemContainerGenerator<T> : ItemContainerGenerator<T>, ITreeItemContainerGenerator
        where T : Control, new()
    {
        private TreeView? _treeView;

        /// <summary>
        /// Initializes a new instance of the <see cref="TreeItemContainerGenerator{T}"/> class.
        /// </summary>
        /// <param name="owner">The owner control.</param>
        /// <param name="contentProperty">The container's Content property.</param>
        /// <param name="contentTemplateProperty">The container's ContentTemplate property.</param>
        /// <param name="itemsProperty">The container's Items property.</param>
        /// <param name="isExpandedProperty">The container's IsExpanded property.</param>
        public TreeItemContainerGenerator(
            Control owner,
            AvaloniaProperty contentProperty,
            AvaloniaProperty contentTemplateProperty,
            AvaloniaProperty itemsProperty,
            AvaloniaProperty isExpandedProperty)
            : base(owner, contentProperty, contentTemplateProperty)
        {
            ItemsProperty = itemsProperty ?? throw new ArgumentNullException(nameof(itemsProperty));
            IsExpandedProperty = isExpandedProperty ?? throw new ArgumentNullException(nameof(isExpandedProperty));
            UpdateIndex();
        }

        /// <summary>
        /// Gets the container index for the tree.
        /// </summary>
        public TreeContainerIndex? Index { get; private set; }

        /// <summary>
        /// Gets the item container's Items property.
        /// </summary>
        protected AvaloniaProperty ItemsProperty { get; }

        /// <summary>
        /// Gets the item container's IsExpanded property.
        /// </summary>
        protected AvaloniaProperty IsExpandedProperty { get; }

        /// <inheritdoc/>
        protected override Control? CreateContainer(object? item)
        {
            var container = item as T;

            if (item == null)
            {
                return null;
            }
            else if (container != null)
            {
                Index?.Add(item, container);
                return container;
            }
            else
            {
                var template = GetTreeDataTemplate(item, ItemTemplate);
                var result = new T();

                if (ItemContainerTheme != null)
                {
                    result.SetValue(Control.ThemeProperty, ItemContainerTheme, BindingPriority.Style);
                }

                if (DisplayMemberBinding is not null)
                {
                    result.SetValue(StyledElement.DataContextProperty, item, BindingPriority.Style);
                    result.Bind(ContentProperty, DisplayMemberBinding, BindingPriority.Style);
                }
                else
                {
                    result.SetValue(ContentProperty, template.Build(item), BindingPriority.Style);
                }

                var itemsSelector = template.ItemsSelector(item);

                if (itemsSelector != null)
                {
                    BindingOperations.Apply(result, ItemsProperty, itemsSelector, null);
                }

                if (!(item is Control))
                {
                    result.DataContext = item;
                }

                Index?.Add(item, result);

                return result;
            }
        }

        public override IEnumerable<ItemContainerInfo> Clear()
        {
            var items = base.Clear();
            Index?.Remove(0, items);
            return items;
        }

        public override IEnumerable<ItemContainerInfo> Dematerialize(int startingIndex, int count)
        {
            Index?.Remove(startingIndex, GetContainerRange(startingIndex, count));
            return base.Dematerialize(startingIndex, count);
        }

        public override IEnumerable<ItemContainerInfo> RemoveRange(int startingIndex, int count)
        {
            Index?.Remove(startingIndex, GetContainerRange(startingIndex, count));
            return base.RemoveRange(startingIndex, count);
        }

        public override bool TryRecycle(int oldIndex, int newIndex, object item) => false;

        public void UpdateIndex()
        {
            if (Owner is TreeView treeViewOwner && Index == null)
            {
                Index = new TreeContainerIndex();
                _treeView = treeViewOwner;
            }
            else
            {
                var treeView = Owner.GetSelfAndLogicalAncestors().OfType<TreeView>().FirstOrDefault();
                
                if (treeView != _treeView)
                {
                    Clear();
                    Index = treeView?.ItemContainerGenerator?.Index;
                    _treeView = treeView;
                }
            }
        }

        class WrapperTreeDataTemplate : ITreeDataTemplate
        {
            private readonly IDataTemplate _inner;
            public WrapperTreeDataTemplate(IDataTemplate inner) => _inner = inner;
            public Control? Build(object? param) => _inner.Build(param);
            public bool Match(object? data) => _inner.Match(data);
            public InstancedBinding? ItemsSelector(object item) => null;
        }

        private ITreeDataTemplate GetTreeDataTemplate(object item, IDataTemplate? primary)
        {
            var template = Owner.FindDataTemplate(item, primary) ?? FuncDataTemplate.Default;
            var treeTemplate = template as ITreeDataTemplate ?? new WrapperTreeDataTemplate(template);
            return treeTemplate;
        }
    }
}
