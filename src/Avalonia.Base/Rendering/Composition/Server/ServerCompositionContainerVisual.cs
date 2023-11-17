using System;
using System.Numerics;
using Avalonia.Media;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition.Server
{
    /// <summary>
    /// Server-side counterpart of <see cref="CompositionContainerVisual"/>.
    /// Mostly propagates update and render calls, but is also responsible
    /// for updating adorners in deferred manner
    /// </summary>
    internal partial class ServerCompositionContainerVisual : ServerCompositionVisual
    {
        public ServerCompositionVisualCollection Children { get; private set; } = null!;
        private Rect? _transformedContentBounds;
        private IImmutableEffect? _oldEffect;
        
        protected override void RenderCore(CompositorDrawingContextProxy canvas, Rect currentTransformedClip)
        {
            base.RenderCore(canvas, currentTransformedClip);

            foreach (var ch in Children)
            {
                ch.Render(canvas, currentTransformedClip);
            }
        }

        public override UpdateResult Update(ServerCompositionTarget root)
        {
            var (combinedBounds, oldInvalidated, newInvalidated) = base.Update(root);
            foreach (var child in Children)
            {
                if (child.AdornedVisual != null)
                    root.EnqueueAdornerUpdate(child);
                else
                {
                    var res = child.Update(root);
                    oldInvalidated |= res.InvalidatedOld;
                    newInvalidated |= res.InvalidatedNew;
                    combinedBounds = Rect.Union(combinedBounds, res.Bounds);
                }
            }
            
            // If effect is changed, we need to clean both old and new bounds
            var effectChanged = !Effect.EffectEquals(_oldEffect);
            if (effectChanged)
                oldInvalidated = newInvalidated = true;
            
            // Expand invalidated bounds to the whole content area since we don't actually know what is being sampled
            // We also ignore clip for now since we don't have means to reset it?
            if (_oldEffect != null && oldInvalidated && _transformedContentBounds.HasValue)
                AddEffectPaddedDirtyRect(_oldEffect, _transformedContentBounds.Value);

            if (Effect != null && newInvalidated && combinedBounds.HasValue)
                AddEffectPaddedDirtyRect(Effect, combinedBounds.Value);
            
            _oldEffect = Effect;
            _transformedContentBounds = combinedBounds;

            IsDirtyComposition = false;
            return new(_transformedContentBounds, oldInvalidated, newInvalidated);
        }

        void AddEffectPaddedDirtyRect(IImmutableEffect effect, Rect transformedBounds)
        {
            var padding = effect.GetEffectOutputPadding();
            if (padding == default)
            {
                AddDirtyRect(transformedBounds);
                return;
            }
            
            // We are in a weird position here: bounds are in global coordinates while padding gets applied in local ones
            // Since we have optimizations to AVOID recomputing transformed bounds and since visuals with effects are relatively rare
            // we instead apply the transformation matrix to rescale the bounds
            
            
            // If we only have translation and scale, just scale the padding
            if (CombinedTransformMatrix is
                {
                    M12: 0, M13: 0,
                    M21: 0, M23: 0,
                    M31: 0, M32: 0
                })
                padding = new Thickness(padding.Left * CombinedTransformMatrix.M11,
                    padding.Top * CombinedTransformMatrix.M22,
                    padding.Right * CombinedTransformMatrix.M11,
                    padding.Bottom * CombinedTransformMatrix.M22);
            else
            {
                // Conservatively use the transformed rect size
                var transformedPaddingRect = new Rect().Inflate(padding).TransformToAABB(CombinedTransformMatrix);
                padding = new(Math.Max(transformedPaddingRect.Width, transformedPaddingRect.Height));
            }

            AddDirtyRect(transformedBounds.Inflate(padding));
        }

        partial void Initialize()
        {
            Children = new ServerCompositionVisualCollection(Compositor);
        }
    }
}
