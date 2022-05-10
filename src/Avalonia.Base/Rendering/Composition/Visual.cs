using System.Numerics;

namespace Avalonia.Rendering.Composition
{
    public abstract partial class CompositionVisual
    {
        private CompositionVisual? _parent;
        private CompositionTarget? _root;

        public CompositionVisual? Parent
        {
            get => _parent;
            internal set
            {
                if (_parent == value)
                    return;
                _parent = value;
                Changes.Parent.Value = value?.Server;
                Root = _parent?.Root;
            }
        }
        
        // TODO: hide behind private-ish API
        public CompositionTarget? Root
        {
            get => _root;
            internal set
            {
                var changed = _root != value;
                _root = value;
                Changes.Root.Value = value?.Server;
                if (changed)
                    OnRootChanged();
            }
        }

        private protected virtual void OnRootChanged()
        {
        }


        internal Matrix4x4? TryGetServerTransform()
        {
            if (Root == null)
                return null;
            var i = Root.Server.Readback;
            ref var readback = ref Server.GetReadback(i.ReadIndex);
            
            // CompositionVisual wasn't visible
            if (readback.Revision < i.ReadRevision)
                return null;
            
            // CompositionVisual was reparented (potential race here)
            if (readback.TargetId != Root.Server.Id)
                return null;
            
            return readback.Matrix;
        }
        
        internal object? Tag { get; set; }

        internal virtual bool HitTest(Point point) => true;
    }
}