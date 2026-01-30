using Avalonia.Rendering.Composition;

namespace Avalonia.Animation
{
    public class OffsetCompositionAnimation : CompositionAnimation
    {
        /// <inheritdoc/>
        protected override Rendering.Composition.Animations.CompositionAnimation? GetCompositionAnimation(Visual visual)
        {
            var compositor = ElementComposition.GetElementVisual(visual)?.Compositor;

            if (compositor == null)
                return null;

            var animation = compositor.CreateVector3KeyFrameAnimation();

            animation.Target = "Offset";

            SetAnimationValues(animation);

            return animation;
        }
    }

    public class OpacityCompositionAnimation : CompositionAnimation
    {
        /// <inheritdoc/>
        protected override Rendering.Composition.Animations.CompositionAnimation? GetCompositionAnimation(Visual visual)
        {
            var compositor = ElementComposition.GetElementVisual(visual)?.Compositor;

            if (compositor == null)
                return null;

            var animation = compositor.CreateScalarKeyFrameAnimation();

            animation.Target = "Opacity";

            SetAnimationValues(animation);

            return animation;
        }
    }

    public class VisibleCompositionAnimation : CompositionAnimation
    {
        /// <inheritdoc/>
        protected override Rendering.Composition.Animations.CompositionAnimation? GetCompositionAnimation(Visual visual)
        {
            var compositor = ElementComposition.GetElementVisual(visual)?.Compositor;

            if (compositor == null)
                return null;

            var animation = compositor.CreateBooleanKeyFrameAnimation();

            animation.Target = "Visible";

            SetAnimationValues(animation);

            return animation;
        }
    }

    public class ClipToBoundsCompositionAnimation : CompositionAnimation
    {
        /// <inheritdoc/>
        protected override Rendering.Composition.Animations.CompositionAnimation? GetCompositionAnimation(Visual visual)
        {
            var compositor = ElementComposition.GetElementVisual(visual)?.Compositor;

            if (compositor == null)
                return null;

            var animation = compositor.CreateBooleanKeyFrameAnimation();

            animation.Target = "ClipToBounds";

            SetAnimationValues(animation);

            return animation;
        }
    }

    public class SizeCompositionAnimation : CompositionAnimation
    {
        /// <inheritdoc/>
        protected override Rendering.Composition.Animations.CompositionAnimation? GetCompositionAnimation(Visual visual)
        {
            var compositor = ElementComposition.GetElementVisual(visual)?.Compositor;

            if (compositor == null)
                return null;

            var animation = compositor.CreateVectorKeyFrameAnimation();

            animation.Target = "Size";

            SetAnimationValues(animation);

            return animation;
        }
    }

    public class AnchorPointCompositionAnimation : CompositionAnimation
    {
        /// <inheritdoc/>
        protected override Rendering.Composition.Animations.CompositionAnimation? GetCompositionAnimation(Visual visual)
        {
            var compositor = ElementComposition.GetElementVisual(visual)?.Compositor;

            if (compositor == null)
                return null;

            var animation = compositor.CreateVectorKeyFrameAnimation();

            animation.Target = "AnchorPoint";

            SetAnimationValues(animation);

            return animation;
        }
    }

    public class CenterPointCompositionAnimation : CompositionAnimation
    {
        /// <inheritdoc/>
        protected override Rendering.Composition.Animations.CompositionAnimation? GetCompositionAnimation(Visual visual)
        {
            var compositor = ElementComposition.GetElementVisual(visual)?.Compositor;

            if (compositor == null)
                return null;

            var animation = compositor.CreateVector3KeyFrameAnimation();

            animation.Target = "CenterPoint";

            SetAnimationValues(animation);

            return animation;
        }
    }

    public class RotationAngleCompositionAnimation : CompositionAnimation
    {
        /// <inheritdoc/>
        protected override Rendering.Composition.Animations.CompositionAnimation? GetCompositionAnimation(Visual visual)
        {
            var compositor = ElementComposition.GetElementVisual(visual)?.Compositor;

            if (compositor == null)
                return null;

            var animation = compositor.CreateScalarKeyFrameAnimation();

            animation.Target = "RotationAngle";

            SetAnimationValues(animation);

            return animation;
        }
    }

    public class OrientationCompositionAnimation : CompositionAnimation
    {
        /// <inheritdoc/>
        protected override Rendering.Composition.Animations.CompositionAnimation? GetCompositionAnimation(Visual visual)
        {
            var compositor = ElementComposition.GetElementVisual(visual)?.Compositor;

            if (compositor == null)
                return null;

            var animation = compositor.CreateQuaternionKeyFrameAnimation();

            animation.Target = "Orientation";

            SetAnimationValues(animation);

            return animation;
        }
    }

    public class ScaleCompositionAnimation : CompositionAnimation
    {
        /// <inheritdoc/>
        protected override Rendering.Composition.Animations.CompositionAnimation? GetCompositionAnimation(Visual visual)
        {
            var compositor = ElementComposition.GetElementVisual(visual)?.Compositor;

            if (compositor == null)
                return null;

            var animation = compositor.CreateVector3KeyFrameAnimation();

            animation.Target = "Scale";

            SetAnimationValues(animation);

            return animation;
        }
    }

    public class TransformMatrixCompositionAnimation : CompositionAnimation
    {
        /// <inheritdoc/>
        protected override Rendering.Composition.Animations.CompositionAnimation? GetCompositionAnimation(Visual visual)
        {
            var compositor = ElementComposition.GetElementVisual(visual)?.Compositor;

            if (compositor == null)
                return null;

            var animation = compositor.CreateVector3KeyFrameAnimation();

            animation.Target = "TransformMatrix";

            SetAnimationValues(animation);

            return animation;
        }
    }

    public class ExpressionCompositionAnimation : CompositionAnimation
    {
        public static readonly StyledProperty<string?> TargetProperty = AvaloniaProperty.Register<ExpressionCompositionAnimation, string?>(
            nameof(Target));

        public string? Target
        {
            get => GetValue(TargetProperty);
            set => SetValue(TargetProperty, value);
        }

        public static readonly StyledProperty<string> ExpressionProperty = AvaloniaProperty.Register<ExpressionCompositionAnimation, string>(
            nameof(Expression));

        public string Expression
        {
            get => GetValue(ExpressionProperty);
            set => SetValue(ExpressionProperty, value);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == ExpressionProperty || change.Property == TargetProperty)
            {
                RaiseAnimationInvalidated();
            }
        }

        /// <inheritdoc/>
        protected override Rendering.Composition.Animations.CompositionAnimation? GetCompositionAnimation(Visual visual)
        {
            var compositor = ElementComposition.GetElementVisual(visual)?.Compositor;

            if (compositor == null || string.IsNullOrEmpty(Expression))
                return null;

            var animation = compositor.CreateExpressionAnimation(Expression);

            animation.Target = Target;

            SetAnimationValues(animation);

            return animation;
        }
    }
}
