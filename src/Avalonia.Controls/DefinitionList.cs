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
            _parent = value;

            var idx = 0;

            foreach (T definition in this)
            {
                definition.Parent = value;
                definition.Index = idx++;
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
