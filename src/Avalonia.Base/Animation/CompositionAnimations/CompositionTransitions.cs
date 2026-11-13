using System.Numerics;
using Avalonia.Rendering.Composition;

namespace Avalonia.Animation
{
    /// <summary>
    /// Represents a composition transition for scalar values.
    /// </summary>
    public class ScalarCompositionTransition : CompositionTransition<float>
    {
        /// <inheritdoc/>
        protected override Rendering.Composition.Animations.CompositionAnimation? GetCompositionAnimation(Visual visual)
        {
            var compositor = ElementComposition.GetElementVisual(visual)?.Compositor;

            if (compositor == null)
                return null;

            var animation = compositor.CreateScalarKeyFrameAnimation();

            SetAnimationValues(animation);

            return animation;
        }
    }

    /// <summary>
    /// Represents a composition transition for double-precision values.
    /// </summary>
    public class DoubleCompositionTransition : CompositionTransition<double>
    {
        /// <inheritdoc/>
        protected override Rendering.Composition.Animations.CompositionAnimation? GetCompositionAnimation(Visual visual)
        {
            var compositor = ElementComposition.GetElementVisual(visual)?.Compositor;

            if (compositor == null)
                return null;

            var animation = compositor.CreateDoubleKeyFrameAnimation();

            SetAnimationValues(animation);

            return animation;
        }
    }

    /// <summary>
    /// Represents a composition transition for quaternion values.
    /// </summary>
    public class QuaternionCompositionTransition : CompositionTransition<Quaternion>
    {
        /// <inheritdoc/>
        protected override Rendering.Composition.Animations.CompositionAnimation? GetCompositionAnimation(Visual visual)
        {
            var compositor = ElementComposition.GetElementVisual(visual)?.Compositor;

            if (compositor == null)
                return null;

            var animation = compositor.CreateQuaternionKeyFrameAnimation();

            SetAnimationValues(animation);

            return animation;
        }
    }

    /// <summary>
    /// Represents a composition transition for Avalonia vector values.
    /// </summary>
    public class VectorCompositionTransition : CompositionTransition<Vector>
    {
        /// <inheritdoc/>
        protected override Rendering.Composition.Animations.CompositionAnimation? GetCompositionAnimation(Visual visual)
        {
            var compositor = ElementComposition.GetElementVisual(visual)?.Compositor;

            if (compositor == null)
                return null;

            var animation = compositor.CreateVectorKeyFrameAnimation();

            SetAnimationValues(animation);

            return animation;
        }
    }

    /// <summary>
    /// Represents a composition transition for two-dimensional vector values.
    /// </summary>
    public class Vector2CompositionTransition : CompositionTransition<Vector2>
    {
        /// <inheritdoc/>
        protected override Rendering.Composition.Animations.CompositionAnimation? GetCompositionAnimation(Visual visual)
        {
            var compositor = ElementComposition.GetElementVisual(visual)?.Compositor;

            if (compositor == null)
                return null;

            var animation = compositor.CreateVector2KeyFrameAnimation();

            SetAnimationValues(animation);

            return animation;
        }
    }

    /// <summary>
    /// Represents a composition transition for four-dimensional vector values.
    /// </summary>
    public class Vector4CompositionTransition : CompositionTransition<Vector4>
    {
        /// <inheritdoc/>
        protected override Rendering.Composition.Animations.CompositionAnimation? GetCompositionAnimation(Visual visual)
        {
            var compositor = ElementComposition.GetElementVisual(visual)?.Compositor;

            if (compositor == null)
                return null;

            var animation = compositor.CreateVector4KeyFrameAnimation();

            SetAnimationValues(animation);

            return animation;
        }
    }

    /// <summary>
    /// Represents a composition transition for three-dimensional vector values.
    /// </summary>
    public class Vector3CompositionTransition : CompositionTransition<Vector3>
    {
        /// <inheritdoc/>
        protected override Rendering.Composition.Animations.CompositionAnimation? GetCompositionAnimation(Visual visual)
        {
            var compositor = ElementComposition.GetElementVisual(visual)?.Compositor;

            if (compositor == null)
                return null;

            var animation = compositor.CreateVector3KeyFrameAnimation();

            SetAnimationValues(animation);

            return animation;
        }
    }

    /// <summary>
    /// Represents a composition transition for three-dimensional double-precision vector values.
    /// </summary>
    public class Vector3DCompositionTransition : CompositionTransition<Vector3D>
    {
        /// <inheritdoc/>
        protected override Rendering.Composition.Animations.CompositionAnimation? GetCompositionAnimation(Visual visual)
        {
            var compositor = ElementComposition.GetElementVisual(visual)?.Compositor;

            if (compositor == null)
                return null;

            var animation = compositor.CreateVector3DKeyFrameAnimation();

            SetAnimationValues(animation);

            return animation;
        }
    }

    /// <summary>
    /// Represents a composition transition for Boolean values.
    /// </summary>
    public class BooleanCompositionTransition : CompositionTransition<bool>
    {
        /// <inheritdoc/>
        protected override Rendering.Composition.Animations.CompositionAnimation? GetCompositionAnimation(Visual visual)
        {
            var compositor = ElementComposition.GetElementVisual(visual)?.Compositor;

            if (compositor == null)
                return null;

            var animation = compositor.CreateBooleanKeyFrameAnimation();

            SetAnimationValues(animation);

            return animation;
        }
    }

    /// <summary>
    /// Represents a composition transition for color values.
    /// </summary>
    public class ColorCompositionTransition : CompositionTransition<Avalonia.Media.Color>
    {
        /// <inheritdoc/>
        protected override Rendering.Composition.Animations.CompositionAnimation? GetCompositionAnimation(Visual visual)
        {
            var compositor = ElementComposition.GetElementVisual(visual)?.Compositor;

            if (compositor == null)
                return null;

            var animation = compositor.CreateColorKeyFrameAnimation();

            SetAnimationValues(animation);

            return animation;
        }
    }

}
