using Avalonia.Collections.Pooled;
using Avalonia.Rendering.Composition.Server;

namespace Avalonia.Rendering.Composition
{
    /// <summary>
    /// A node in the visual tree that can have children.
    /// </summary>
    public partial class CompositionContainerVisual : CompositionVisual
    {
        internal static readonly int HitTestAabbTreeThreshold = CompositionHitTestAabbTree.IsEnabled ? 32 : int.MaxValue;
        private CompositionHitTestAabbTree? _hitTestChildren;
        private bool _hitTestChildrenDirty = true;

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

        internal void InvalidateHitTestChildren()
        {
            _hitTestChildrenDirty = true;
        }

        internal void UpdateHitTestChildBounds(CompositionVisual child)
        {
            if (_hitTestChildren == null || _hitTestChildrenDirty)
                return;

            if (Children.Count < HitTestAabbTreeThreshold)
            {
                _hitTestChildren.Clear();
                _hitTestChildren = null;
                return;
            }

            if (!_hitTestChildren.Update(child))
                _hitTestChildrenDirty = true;
        }

        internal bool TryQueryHitTestChildren(Point point, PooledList<CompositionVisual> results)
        {
            if (Children.Count < HitTestAabbTreeThreshold)
            {
                _hitTestChildren?.Clear();
                _hitTestChildren = null;
                return false;
            }

            _hitTestChildren ??= new CompositionHitTestAabbTree();

            if (_hitTestChildrenDirty)
            {
                _hitTestChildren.Rebuild(Children);
                _hitTestChildrenDirty = false;
            }

            _hitTestChildren.Query(point, results);
            return true;
        }

        internal bool TryQueryFirstHitTestChild<T>(Point point, ref T hitTest, out CompositionVisual? hit)
            where T : struct, CompositionHitTestAabbTree.IQueryHitTester
        {
            if (Children.Count < HitTestAabbTreeThreshold)
            {
                _hitTestChildren?.Clear();
                _hitTestChildren = null;
                hit = null;
                return false;
            }

            _hitTestChildren ??= new CompositionHitTestAabbTree();

            if (_hitTestChildrenDirty)
            {
                _hitTestChildren.Rebuild(Children);
                _hitTestChildrenDirty = false;
            }

            hit = _hitTestChildren.QueryFirst(point, ref hitTest);
            return true;
        }
    }
}
