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

        partial void OnAdded(CompositionVisual item)
        {
            item.Parent = _owner;
            AddHitTestChild(item);
        }

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

        partial void OnRemoved(CompositionVisual item)
        {
            item.Parent = null;
            RemoveHitTestChild(item);
        }

        partial void OnBeforeClear()
        {
            foreach (var i in this)
                i.Parent = null;
        }

        partial void OnClear() => ClearHitTestChildren();

        partial void OnBeforeAdded(CompositionVisual item)
        {
            if (item.Parent != null)
                throw new InvalidOperationException("Visual already has a parent");
            item.Parent = _owner;
        }

        private void AddHitTestChild(CompositionVisual item)
        {
            if (_owner is CompositionContainerVisual container)
                container.AddHitTestChild(item);
        }

        private void RemoveHitTestChild(CompositionVisual item)
        {
            if (_owner is CompositionContainerVisual container)
                container.RemoveHitTestChild(item);
        }

        private void ClearHitTestChildren()
        {
            if (_owner is CompositionContainerVisual container)
                container.ClearHitTestChildren();
        }
    }
}
