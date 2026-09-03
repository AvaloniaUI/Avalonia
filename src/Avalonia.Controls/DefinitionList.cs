using System.Collections;
using System.Collections.Specialized;
using Avalonia.Collections;
using Avalonia.Metadata;

namespace Avalonia.Controls
{
    [AvaloniaList(Separators = new [] { ",", " " })]
    public abstract class DefinitionList<T> : AvaloniaList<T> where T : DefinitionBase
    {
        public DefinitionList()
        {
            ResetBehavior = ResetBehavior.Remove;
            CollectionChanged += OnCollectionChanged;
        }

        internal bool IsDirty = true;
        private Grid? _parent;

        internal Grid? Parent
        {
            get => _parent;
            set => SetParent(value);
        }

        private void SetParent(Grid? value)
        {
            if (_parent == value)
            {
                return;
            }

            _parent = value;

            //  definitions already present when the grid claims the collection never pass through
            //  OnCollectionChanged, so they have to change trees here.
            var idx = 0;

            foreach (T definition in this)
            {
                SetDefinitionParent(definition, value);
                definition.Index = idx++;
            }
        }

        /// <summary>
        /// Moves a definition from its current parent tree to <paramref name="parent"/>. Every route
        /// that changes a definition's owner goes through here.
        /// </summary>
        /// <remarks>
        /// Ownership is more than the Parent pointer: entering a tree also establishes the property
        /// inheritance link a definition needs to see its shared size scope, and leaving one releases
        /// that link and its shared size registration.
        /// </remarks>
        private static void SetDefinitionParent(DefinitionBase definition, Grid? parent)
        {
            if (definition.Parent == parent)
            {
                return;
            }

            if (definition.Parent is not null)
            {
                definition.OnExitParentTree();
            }

            definition.Parent = parent;

            if (parent is not null)
            {
                definition.OnEnterParentTree();
            }
        }

        internal void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            var idx = 0;

            foreach (T definition in this)
            {
                definition.Index = idx++;
            }
            
            UpdateDefinitionParent(e.NewItems, false);
            UpdateDefinitionParent(e.OldItems, true);
            
            IsDirty = true;
        }

        private void UpdateDefinitionParent(IList? items, bool wasRemoved)
        {
            if (items is null)
            {
                return;
            }
            
            var count = items.Count;

            for (var i = 0; i < count; i++)
            {
                SetDefinitionParent((DefinitionBase)items[i]!, wasRemoved ? null : Parent);
            }
        }
    }
}
