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

            //  Definitions already in the collection when the grid claims it never pass through
            //  OnCollectionChanged, so they have to be joined to (and detached from) the parent tree
            //  here. Assigning Parent alone is not enough: OnEnterParentTree also establishes the
            //  property inheritance link the definition needs to see its shared size scope.
            if (_parent is not null)
            {
                foreach (T definition in this)
                {
                    definition.OnExitParentTree();
                }
            }

            _parent = value;

            var idx = 0;

            foreach (T definition in this)
            {
                definition.Parent = value;
                definition.Index = idx++;

                if (value is not null)
                {
                    definition.OnEnterParentTree();
                }
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
                var definition = (DefinitionBase) items[i]!;

                if (wasRemoved)
                {
                    definition.OnExitParentTree();
                }
                else
                {
                    definition.Parent = Parent;
                    definition.OnEnterParentTree();                    
                }
            }
        }
    }
}
