using System;
using Avalonia.Rendering.Composition.Server;

namespace Avalonia.Rendering.Composition
{
    /// <summary>
    /// A collection of CompositionVisual objects
    /// </summary>
    public partial class CompositionVisualCollection : CompositionObject
    {
        private readonly CompositionVisual _owner;
        internal CompositionVisualCollection(CompositionVisual parent, ServerCompositionVisualCollection server) : base(parent.Compositor, server)
        {
            _owner = parent;
            InitializeDefaults();
        }
        
        public void InsertAbove(CompositionVisual newChild, CompositionVisual sibling)
        {
            var idx = _list.IndexOf(sibling);
            if (idx == -1)
                throw new InvalidOperationException();
            
            Insert(idx + 1, newChild);
        }
        
        public void InsertBelow(CompositionVisual newChild, CompositionVisual sibling)
        {
            var idx = _list.IndexOf(sibling);
            if (idx == -1)
                throw new InvalidOperationException();
            Insert(idx, newChild);
        }

        public void InsertAtTop(CompositionVisual newChild) => Insert(_list.Count, newChild);

        public void InsertAtBottom(CompositionVisual newChild) => Insert(0, newChild);

        public void RemoveAll() => Clear();

        partial void OnAdded(CompositionVisual item) => item.Parent = _owner;

        partial void OnBeforeReplace(CompositionVisual oldItem, CompositionVisual newItem)
        {
            if (oldItem != newItem)
                OnBeforeAdded(newItem);
        }

        partial void OnReplace(CompositionVisual oldItem, CompositionVisual newItem)
        {
            if (oldItem != newItem)
            {
                OnRemoved(oldItem);
                OnAdded(newItem);
            }
        }

        partial void OnRemoved(CompositionVisual item) => item.Parent = null;

        partial void OnBeforeClear()
        {
            foreach (var i in this)
                i.Parent = null;
        }

        partial void OnBeforeAdded(CompositionVisual item)
        {
            if (item.Parent != null)
                throw new InvalidOperationException("Visual already has a parent");
            item.Parent = _owner;
        }
    }
}
