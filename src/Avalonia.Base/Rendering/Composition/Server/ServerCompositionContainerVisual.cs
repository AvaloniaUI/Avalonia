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
        private LtrbRect? _transformedContentBounds;
        private IImmutableEffect? _oldEffect;
        
        protected override void RenderCore(ServerVisualRenderContext context, LtrbRect currentTransformedClip)
        {
            base.RenderCore(context, currentTransformedClip);

            if (context.RenderChildren)
            {
                foreach (var ch in Children)
                {
                    ch.Render(context, currentTransformedClip);
                }
            }
        }

        public override UpdateResult Update(ServerCompositionTarget root, ref CompositionMatrix parentCombinedTransform)
        {
            var (combinedBounds, oldInvalidated, newInvalidated) = base.Update(root, ref parentCombinedTransform);
            foreach (var child in Children)
            {
                if (child.AdornedVisual != null)
                    root.EnqueueAdornerUpdate(child);
                else
                {
                    var res = child.Update(root, ref GlobalTransformMatrix);
                    oldInvalidated |= res.InvalidatedOld;
                    newInvalidated |= res.InvalidatedNew;
                    combinedBounds = LtrbRect.FullUnion(combinedBounds, res.Bounds);
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

        protected override LtrbRect GetEffectBounds() => _transformedContentBounds ?? default;

        void AddEffectPaddedDirtyRect(IImmutableEffect effect, LtrbRect transformedBounds)
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

            if (!CombinedTransformMatrix.GuaranteedIdentity)
            {

                // If we only have translation and scale, just scale the padding
                if (CombinedTransformMatrix.GuaranteedTranslateAndScaleOnly)
                {
                    padding = new Thickness(padding.Left * CombinedTransformMatrix.ScaleX,
                        padding.Top * CombinedTransformMatrix.ScaleY,
                        padding.Right * CombinedTransformMatrix.ScaleX,
                        padding.Bottom * CombinedTransformMatrix.ScaleY);
                }
                else
                {
                    // Conservatively use the transformed rect size
                    var transformedPaddingRect =
                        new Rect().Inflate(padding).TransformToAABB(CombinedTransformMatrix.ToMatrix());
                    padding = new(Math.Max(transformedPaddingRect.Width, transformedPaddingRect.Height));
                }

            }

            AddDirtyRect(transformedBounds.Inflate(padding));
        }

        partial void Initialize()
        {
            Children = new ServerCompositionVisualCollection(Compositor);
        }
    }
}
