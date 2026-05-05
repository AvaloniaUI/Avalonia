using System;
using Avalonia.Collections.Pooled;
using Avalonia.Rendering.Composition.Server;

namespace Avalonia.Rendering.Composition
{
    /// <summary>
    /// A node in the visual tree that can have children.
    /// </summary>
    public partial class CompositionContainerVisual : CompositionVisual
    {
        internal const int HitTestAabbTreeThreshold = 32;
        private CompositionHitTestAabbTree? _hitTestChildren;

        public CompositionVisualCollection Children { get; private set; } = null!;

        partial void InitializeDefaultsExtra()
        {
            Children = new CompositionVisualCollection(this, Server.Children);
        }

        private protected override void OnRootChangedCore()
        {
            foreach (var ch in Children)
                ch.Root = Root;
            base.OnRootChangedCore();
        }

        internal void AddHitTestChild(CompositionVisual child)
        {
            if (_hitTestChildren == null)
                return;

            var order = Children.IndexOf(child);
            if (order >= 0)
                _hitTestChildren.Update(child, order);

            UpdateHitTestChildOrder();
        }

        internal void RemoveHitTestChild(CompositionVisual child)
        {
            if (_hitTestChildren == null)
                return;

            _hitTestChildren.Remove(child);

            UpdateHitTestChildOrder();
        }

        internal void ClearHitTestChildren()
        {
            _hitTestChildren?.Clear();
        }

        internal void UpdateHitTestChildBounds(CompositionVisual child)
        {
            if (_hitTestChildren == null)
                return;

            if (Children.Count < HitTestAabbTreeThreshold)
            {
                _hitTestChildren = null;
                return;
            }

            _hitTestChildren.UpdateBounds(child);
        }

        internal bool TryQueryHitTestChildren(Point point, PooledList<CompositionVisual> results)
        {
            if (Children.Count < HitTestAabbTreeThreshold)
            {
                _hitTestChildren = null;
                return false;
            }

            _hitTestChildren ??= new CompositionHitTestAabbTree(Children);

            _hitTestChildren.Query(point, results);
            return true;
        }

        internal bool TryQueryFirstHitTestChild(CompositionTarget target, Point point, Func<CompositionVisual, bool>? filter, Func<CompositionVisual, bool>? resultFilter, out CompositionVisual? hit)
        {
            if (Children.Count < HitTestAabbTreeThreshold)
            {
                _hitTestChildren = null;
                hit = null;
                return false;
            }

            _hitTestChildren ??= new CompositionHitTestAabbTree(Children);

            hit = _hitTestChildren.QueryFirst(target, point, filter, resultFilter);
            return true;
        }

        private void UpdateHitTestChildOrder()
        {
            if (_hitTestChildren == null)
                return;

            for (var i = 0; i < Children.Count; i++)
                _hitTestChildren.UpdateOrder(Children[i], i);
        }
    }
}
