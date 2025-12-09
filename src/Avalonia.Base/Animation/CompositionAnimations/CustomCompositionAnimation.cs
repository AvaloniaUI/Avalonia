using Avalonia.Rendering.Composition;

namespace Avalonia.Animation
{
    public abstract class CustomCompositionAnimation : CompositionAnimation
    {
        public static readonly StyledProperty<string?> TargetProperty = AvaloniaProperty.Register<CustomCompositionAnimation, string?>(
            nameof(Target));

        public string? Target
        {
            get => GetValue(TargetProperty);
            set => SetValue(TargetProperty, value);
        }

        protected override void SetAnimationValues(Rendering.Composition.Animations.CompositionAnimation animation)
        {
            base.SetAnimationValues(animation);

            animation.Target = Target;
        }
    }

    public class CompositionScalerImplicitAnimation : CustomCompositionAnimation
    {
        public override Rendering.Composition.Animations.CompositionAnimation? GetCompositionAnimation(Visual parent)
        {
            var compositor = ElementComposition.GetElementVisual(parent)?.Compositor;

            if (compositor == null)
                return null;

            var animation = compositor.CreateScalarKeyFrameAnimation();

            SetAnimationValues(animation);

            return animation;
        }
    }

    public class CompositionVector3ImplicitAnimation : CustomCompositionAnimation
    {
        public override Rendering.Composition.Animations.CompositionAnimation? GetCompositionAnimation(Visual parent)
        {
            var compositor = ElementComposition.GetElementVisual(parent)?.Compositor;

            if (compositor == null)
                return null;

            var animation = compositor.CreateVector3KeyFrameAnimation();

            SetAnimationValues(animation);

            return animation;
        }
    }

    public class CompositionDoubleImplicitAnimation : CustomCompositionAnimation
    {
        public override Rendering.Composition.Animations.CompositionAnimation? GetCompositionAnimation(Visual parent)
        {
            var compositor = ElementComposition.GetElementVisual(parent)?.Compositor;

            if (compositor == null)
                return null;

            var animation = compositor.CreateDoubleKeyFrameAnimation();

            SetAnimationValues(animation);

            return animation;
        }
    }

    public class CompositionQuaternionImplicitAnimation : CustomCompositionAnimation
    {
        public override Rendering.Composition.Animations.CompositionAnimation? GetCompositionAnimation(Visual parent)
        {
            var compositor = ElementComposition.GetElementVisual(parent)?.Compositor;

            if (compositor == null)
                return null;

            var animation = compositor.CreateQuaternionKeyFrameAnimation();

            SetAnimationValues(animation);

            return animation;
        }
    }

    public class CompositionVector2ImplicitAnimation : CustomCompositionAnimation
    {
        public override Rendering.Composition.Animations.CompositionAnimation? GetCompositionAnimation(Visual parent)
        {
            var compositor = ElementComposition.GetElementVisual(parent)?.Compositor;

            if (compositor == null)
                return null;

            var animation = compositor.CreateVector2KeyFrameAnimation();

            SetAnimationValues(animation);

            return animation;
        }
    }

    public class CompositionVector3DImplicitAnimation : CustomCompositionAnimation
    {
        public override Rendering.Composition.Animations.CompositionAnimation? GetCompositionAnimation(Visual parent)
        {
            var compositor = ElementComposition.GetElementVisual(parent)?.Compositor;

            if (compositor == null)
                return null;

            var animation = compositor.CreateVector3DKeyFrameAnimation();

            SetAnimationValues(animation);

            return animation;
        }
    }

    public class CompositionVectorImplicitAnimation : CustomCompositionAnimation
    {
        public override Rendering.Composition.Animations.CompositionAnimation? GetCompositionAnimation(Visual parent)
        {
            var compositor = ElementComposition.GetElementVisual(parent)?.Compositor;

            if (compositor == null)
                return null;

            var animation = compositor.CreateVectorKeyFrameAnimation();

            SetAnimationValues(animation);

            return animation;
        }
    }

    public class CompositionVector4ImplicitAnimation : CustomCompositionAnimation
    {
        public override Rendering.Composition.Animations.CompositionAnimation? GetCompositionAnimation(Visual parent)
        {
            var compositor = ElementComposition.GetElementVisual(parent)?.Compositor;

            if (compositor == null)
                return null;

            var animation = compositor.CreateVector4KeyFrameAnimation();

            SetAnimationValues(animation);

            return animation;
        }
    }

    public class CompositionBooleanImplicitAnimation : CustomCompositionAnimation
    {
        public override Rendering.Composition.Animations.CompositionAnimation? GetCompositionAnimation(Visual parent)
        {
            var compositor = ElementComposition.GetElementVisual(parent)?.Compositor;

            if (compositor == null)
                return null;

            var animation = compositor.CreateBooleanKeyFrameAnimation();

            SetAnimationValues(animation);

            return animation;
        }
    }

    public class CompositionColorImplicitAnimation : CustomCompositionAnimation
    {
        public override Rendering.Composition.Animations.CompositionAnimation? GetCompositionAnimation(Visual parent)
        {
            var compositor = ElementComposition.GetElementVisual(parent)?.Compositor;

            if (compositor == null)
                return null;

            var animation = compositor.CreateColorKeyFrameAnimation();

            SetAnimationValues(animation);

            return animation;
        }
    }
}
