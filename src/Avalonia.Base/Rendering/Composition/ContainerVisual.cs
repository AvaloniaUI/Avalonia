using System;
using Avalonia.Collections.Pooled;
using Avalonia.Media;
using Avalonia.Rendering.Composition.HitTesting;

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

        internal bool TryQueryHitTestChildren<THitTester, T>(T input, PooledList<CompositionVisual> results)
            where THitTester : struct, ICompositionHitTester<T>
        {
            if (Children.Count < HitTestAabbTreeThreshold)
            {
                _hitTestChildren = null;
                return false;
            }

            _hitTestChildren ??= new CompositionHitTestAabbTree(Children);

            _hitTestChildren.Query<THitTester, T>(input, results, Server.Compositor.Readback.ReadRevision);
            return true;
        }

        internal bool TryQueryFirstHitTestChild<THitTester, T>(
            CompositionTarget target,
            T input,
            Func<CompositionVisual, bool>? filter,
            Func<CompositionVisual, bool>? resultFilter,
            out CompositionVisual? hit)
            where THitTester : struct, ICompositionHitTester<T>
        {
            if (Children.Count < HitTestAabbTreeThreshold)
            {
                _hitTestChildren = null;
                hit = null;
                return false;
            }

            _hitTestChildren ??= new CompositionHitTestAabbTree(Children);

            hit = _hitTestChildren.QueryFirst<THitTester, T>(target, input, filter, resultFilter, Server.Compositor.Readback.ReadRevision);
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
