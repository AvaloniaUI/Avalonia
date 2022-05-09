using System;
using System.Numerics;
using Avalonia.Platform;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Rendering.Composition
{
    internal class CustomDrawVisual<TData> : CompositionContainerVisual where TData : IEquatable<TData>
    {
        private readonly ICustomDrawVisualHitTest<TData>? _hitTest;

        internal CustomDrawVisual(Compositor compositor, ICustomDrawVisualRenderer<TData> renderer,
            ICustomDrawVisualHitTest<TData>? hitTest) : base(compositor, 
            new ServerCustomDrawVisual<TData>(compositor.Server, renderer))
        {
            _hitTest = hitTest;
        }

        private TData? _data;

        static bool Eq(TData? left, TData? right)
        {
            if (left == null && right == null)
                return true;
            if (left == null)
                return false;
            return left.Equals(right);
        }
        
        public TData? Data
        {
            get => _data;
            set
            {
                if (!Eq(_data, value))
                {
                    ((CustomDrawVisualChanges<TData?>) Changes).Data.Value = value;
                    _data = value;
                }
            }
        }
        
        private protected override IChangeSetPool ChangeSetPool => CustomDrawVisualChanges<TData>.Pool;
    }

    public interface ICustomDrawVisualRenderer<TData>
    {
        void Render(IDrawingContextImpl canvas, TData? data);
    }

    public interface ICustomDrawVisualHitTest<TData>
    {
        bool HitTest(TData data, Vector2 vector2);
    }
}