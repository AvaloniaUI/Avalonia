using System.Numerics;
using Avalonia.Animation.Animators;
using Avalonia.Animation.Easings;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;

namespace Avalonia.Animation.CompositionAnimations
{
    internal class ScalarCompositionAnimator : CompositionAnimator<float>
    {
        protected override KeyFrameAnimation? GetAnimation(Visual visual)
        {
            var compositor = ElementComposition.GetElementVisual(visual)?.Compositor;

            if (compositor == null)
                return null;

            return compositor.CreateScalarKeyFrameAnimation();
        }

        protected override void SetKeyFrame(KeyFrameAnimation animation, AnimatorKeyFrame keyframe, Easing easing)
        {
            if (animation is ScalarKeyFrameAnimation scalarKeyFrameAnimation && keyframe.Value is float value)
                scalarKeyFrameAnimation.InsertKeyFrame((float)keyframe.Cue.CueValue, value, easing);
        }
    }

    internal class DoubleCompositionAnimator : CompositionAnimator<double>
    {
        protected override KeyFrameAnimation? GetAnimation(Visual visual)
        {
            var compositor = ElementComposition.GetElementVisual(visual)?.Compositor;

            if (compositor == null)
                return null;

            return compositor.CreateDoubleKeyFrameAnimation();
        }

        protected override void SetKeyFrame(KeyFrameAnimation animation, AnimatorKeyFrame keyframe, Easing easing)
        {
            if (animation is DoubleKeyFrameAnimation doubleKeyFrameAnimation && keyframe.Value is double value)
                doubleKeyFrameAnimation.InsertKeyFrame((float)keyframe.Cue.CueValue, value, easing);
        }
    }

    internal class BooleanCompositionAnimator : CompositionAnimator<bool>
    {
        protected override KeyFrameAnimation? GetAnimation(Visual visual)
        {
            var compositor = ElementComposition.GetElementVisual(visual)?.Compositor;

            if (compositor == null)
                return null;

            return compositor.CreateBooleanKeyFrameAnimation();
        }

        protected override void SetKeyFrame(KeyFrameAnimation animation, AnimatorKeyFrame keyframe, Easing easing)
        {
            if (animation is BooleanKeyFrameAnimation booleanKeyFrameAnimation && keyframe.Value is bool value)
                booleanKeyFrameAnimation.InsertKeyFrame((float)keyframe.Cue.CueValue, value, easing);
        }
    }

    internal class ColorCompositionAnimator : CompositionAnimator<Color>
    {
        protected override KeyFrameAnimation? GetAnimation(Visual visual)
        {
            var compositor = ElementComposition.GetElementVisual(visual)?.Compositor;

            if (compositor == null)
                return null;

            return compositor.CreateColorKeyFrameAnimation();
        }

        protected override void SetKeyFrame(KeyFrameAnimation animation, AnimatorKeyFrame keyframe, Easing easing)
        {
            if (animation is ColorKeyFrameAnimation colorKeyFrameAnimation && keyframe.Value is Color value)
                colorKeyFrameAnimation.InsertKeyFrame((float)keyframe.Cue.CueValue, value, easing);
        }
    }

    internal class VectorCompositionAnimator : CompositionAnimator<Vector>
    {
        protected override KeyFrameAnimation? GetAnimation(Visual visual)
        {
            var compositor = ElementComposition.GetElementVisual(visual)?.Compositor;

            if (compositor == null)
                return null;

            return compositor.CreateVectorKeyFrameAnimation();
        }

        protected override void SetKeyFrame(KeyFrameAnimation animation, AnimatorKeyFrame keyframe, Easing easing)
        {
            if (animation is VectorKeyFrameAnimation vectorKeyFrameAnimation && keyframe.Value is Vector value)
                vectorKeyFrameAnimation.InsertKeyFrame((float)keyframe.Cue.CueValue, value, easing);
        }
    }

    internal class Vector2CompositionAnimator : CompositionAnimator<Vector2>
    {
        protected override KeyFrameAnimation? GetAnimation(Visual visual)
        {
            var compositor = ElementComposition.GetElementVisual(visual)?.Compositor;

            if (compositor == null)
                return null;

            return compositor.CreateVector2KeyFrameAnimation();
        }

        protected override void SetKeyFrame(KeyFrameAnimation animation, AnimatorKeyFrame keyframe, Easing easing)
        {
            if (animation is Vector2KeyFrameAnimation vector2KeyFrameAnimation && keyframe.Value is Vector2 value)
                vector2KeyFrameAnimation.InsertKeyFrame((float)keyframe.Cue.CueValue, value, easing);
        }
    }

    internal class Vector3CompositionAnimator : CompositionAnimator<Vector3>
    {
        protected override KeyFrameAnimation? GetAnimation(Visual visual)
        {
            var compositor = ElementComposition.GetElementVisual(visual)?.Compositor;

            if (compositor == null)
                return null;

            return compositor.CreateVector3KeyFrameAnimation();
        }

        protected override void SetKeyFrame(KeyFrameAnimation animation, AnimatorKeyFrame keyframe, Easing easing)
        {
            if (animation is Vector3KeyFrameAnimation vector3KeyFrameAnimation && keyframe.Value is Vector3 value)
                vector3KeyFrameAnimation.InsertKeyFrame((float)keyframe.Cue.CueValue, value, easing);
        }
    }

    internal class Vector3DCompositionAnimator : CompositionAnimator<Vector3D>
    {
        protected override KeyFrameAnimation? GetAnimation(Visual visual)
        {
            var compositor = ElementComposition.GetElementVisual(visual)?.Compositor;

            if (compositor == null)
                return null;

            var animation = compositor.CreateVector3DKeyFrameAnimation();

            return animation;
        }

        protected override void SetKeyFrame(KeyFrameAnimation animation, AnimatorKeyFrame keyframe, Easing easing)
        {
            if (animation is Vector3DKeyFrameAnimation vector3DKeyFrameAnimation && keyframe.Value is Vector3D value)
            {
                vector3DKeyFrameAnimation.InsertKeyFrame((float)keyframe.Cue.CueValue, value, easing);
            }
        }
    }

    internal class Vector4CompositionAnimator : CompositionAnimator<Vector4>
    {
        protected override KeyFrameAnimation? GetAnimation(Visual visual)
        {
            var compositor = ElementComposition.GetElementVisual(visual)?.Compositor;

            if (compositor == null)
                return null;

            return compositor.CreateVector4KeyFrameAnimation();
        }

        protected override void SetKeyFrame(KeyFrameAnimation animation, AnimatorKeyFrame keyframe, Easing easing)
        {
            if (animation is Vector4KeyFrameAnimation vector4KeyFrameAnimation && keyframe.Value is Vector4 value)
                vector4KeyFrameAnimation.InsertKeyFrame((float)keyframe.Cue.CueValue, value, easing);
        }
    }

    internal class QuaternionCompositionAnimator : CompositionAnimator<Quaternion>
    {
        protected override KeyFrameAnimation? GetAnimation(Visual visual)
        {
            var compositor = ElementComposition.GetElementVisual(visual)?.Compositor;

            if (compositor == null)
                return null;

            return compositor.CreateQuaternionKeyFrameAnimation();
        }

        protected override void SetKeyFrame(KeyFrameAnimation animation, AnimatorKeyFrame keyframe, Easing easing)
        {
            if (animation is QuaternionKeyFrameAnimation quaternionKeyFrameAnimation && keyframe.Value is Quaternion value)
                quaternionKeyFrameAnimation.InsertKeyFrame((float)keyframe.Cue.CueValue, value, easing);
        }
    }
}
