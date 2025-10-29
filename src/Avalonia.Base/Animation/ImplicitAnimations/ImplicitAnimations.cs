using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;

namespace Avalonia.Animation
{
    public class OffsetImplicitAnimation : KeyFrameImplicitAnimation
    {
        /// <inheritdoc/>
        protected internal override string Property => "Offset";

        /// <inheritdoc/>
        public override Rendering.Composition.Animations.CompositionAnimation? GetCompositionAnimation(Visual visual)
        {
            var compositor = ElementComposition.GetElementVisual(visual)?.Compositor;

            if (compositor == null)
                return null;

            var animation = compositor.CreateVector3KeyFrameAnimation();

            SetBaseValues(animation);

            return animation;
        }
    }
    
    public class OpacityImplicitAnimation : KeyFrameImplicitAnimation
    {
        /// <inheritdoc/>
        protected internal override string Property => "Opacity";

        /// <inheritdoc/>
        public override Rendering.Composition.Animations.CompositionAnimation? GetCompositionAnimation(Visual visual)
        {
            var compositor = ElementComposition.GetElementVisual(visual)?.Compositor;

            if (compositor == null)
                return null;

            var animation = compositor.CreateDoubleKeyFrameAnimation();

            SetBaseValues(animation);

            return animation;
        }
    }
    
    public class VisibleImplicitAnimation : KeyFrameImplicitAnimation
    {
        /// <inheritdoc/>
        protected internal override string Property => "Visible";

        /// <inheritdoc/>
        public override Rendering.Composition.Animations.CompositionAnimation? GetCompositionAnimation(Visual visual)
        {
            var compositor = ElementComposition.GetElementVisual(visual)?.Compositor;

            if (compositor == null)
                return null;

            var animation = compositor.CreateBooleanKeyFrameAnimation();

            SetBaseValues(animation);

            return animation;
        }
    }
    
    public class ClipToBoundsImplicitAnimation : KeyFrameImplicitAnimation
    {
        /// <inheritdoc/>
        protected internal override string Property => "ClipToBounds";

        /// <inheritdoc/>
        public override Rendering.Composition.Animations.CompositionAnimation? GetCompositionAnimation(Visual visual)
        {
            var compositor = ElementComposition.GetElementVisual(visual)?.Compositor;

            if (compositor == null)
                return null;

            var animation = compositor.CreateBooleanKeyFrameAnimation();

            SetBaseValues(animation);

            return animation;
        }
    }
    
    public class SizeImplicitAnimation : KeyFrameImplicitAnimation
    {
        /// <inheritdoc/>
        protected internal override string Property => "Size";

        /// <inheritdoc/>
        public override Rendering.Composition.Animations.CompositionAnimation? GetCompositionAnimation(Visual visual)
        {
            var compositor = ElementComposition.GetElementVisual(visual)?.Compositor;

            if (compositor == null)
                return null;

            var animation = compositor.CreateVectorKeyFrameAnimation();

            SetBaseValues(animation);

            return animation;
        }
    }
    
    public class AnchorPointImplicitAnimation : KeyFrameImplicitAnimation
    {
        /// <inheritdoc/>
        protected internal override string Property => "AnchorPoint";

        /// <inheritdoc/>
        public override Rendering.Composition.Animations.CompositionAnimation? GetCompositionAnimation(Visual visual)
        {
            var compositor = ElementComposition.GetElementVisual(visual)?.Compositor;

            if (compositor == null)
                return null;

            var animation = compositor.CreateVectorKeyFrameAnimation();

            SetBaseValues(animation);

            return animation;
        }
    }
    
    public class CenterPointImplicitAnimation : KeyFrameImplicitAnimation
    {
        /// <inheritdoc/>
        protected internal override string Property => "CenterPoint";

        /// <inheritdoc/>
        public override Rendering.Composition.Animations.CompositionAnimation? GetCompositionAnimation(Visual visual)
        {
            var compositor = ElementComposition.GetElementVisual(visual)?.Compositor;

            if (compositor == null)
                return null;

            var animation = compositor.CreateVector3KeyFrameAnimation();

            SetBaseValues(animation);

            return animation;
        }
    }
    
    public class RotationAngleImplicitAnimation : KeyFrameImplicitAnimation
    {
        /// <inheritdoc/>
        protected internal override string Property => "RotationAngle";

        /// <inheritdoc/>
        public override Rendering.Composition.Animations.CompositionAnimation? GetCompositionAnimation(Visual visual)
        {
            var compositor = ElementComposition.GetElementVisual(visual)?.Compositor;

            if (compositor == null)
                return null;

            var animation = compositor.CreateDoubleKeyFrameAnimation();

            SetBaseValues(animation);

            return animation;
        }
    }
    
    public class OrientationImplicitAnimation : KeyFrameImplicitAnimation
    {
        /// <inheritdoc/>
        protected internal override string Property => "Orientation";

        /// <inheritdoc/>
        public override Rendering.Composition.Animations.CompositionAnimation? GetCompositionAnimation(Visual visual)
        {
            var compositor = ElementComposition.GetElementVisual(visual)?.Compositor;

            if (compositor == null)
                return null;

            var animation = compositor.CreateQuaternionKeyFrameAnimation();

            SetBaseValues(animation);

            return animation;
        }
    }
    
    public class ScaleImplicitAnimation : KeyFrameImplicitAnimation
    {
        /// <inheritdoc/>
        protected internal override string Property => "Scale";

        /// <inheritdoc/>
        public override Rendering.Composition.Animations.CompositionAnimation? GetCompositionAnimation(Visual visual)
        {
            var compositor = ElementComposition.GetElementVisual(visual)?.Compositor;

            if (compositor == null)
                return null;

            var animation = compositor.CreateVector3KeyFrameAnimation();

            SetBaseValues(animation);

            return animation;
        }
    }

    public class ExpressionImplicitAnimation : ImplicitAnimation
    {
        public static readonly StyledProperty<string?> TargetProperty = AvaloniaProperty.Register<ExpressionImplicitAnimation, string?>(
            nameof(Target));
        
        public string? Target
        {
            get => GetValue(TargetProperty);
            set => SetValue(TargetProperty, value);
        }
        
        public static readonly StyledProperty<string> ExpressionProperty = AvaloniaProperty.Register<ExpressionImplicitAnimation, string>(
            nameof(Expression));
        
        public string Expression
        {
            get => GetValue(ExpressionProperty);
            set => SetValue(ExpressionProperty, value);
        }

        /// <inheritdoc/>
        protected internal override string? Property => Target;

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == ExpressionProperty || change.Property == TargetProperty)
            {
                RaiseAnimationInvalidated();
            }
        }

        /// <inheritdoc/>
        public override Rendering.Composition.Animations.CompositionAnimation? GetCompositionAnimation(Visual visual)
        {
            var compositor = ElementComposition.GetElementVisual(visual)?.Compositor;

            if (compositor == null || string.IsNullOrEmpty(Expression))
                return null;

            var animation = compositor.CreateExpressionAnimation(Expression);

            SetBaseValues(animation);

            return animation;
        }
    }
}
