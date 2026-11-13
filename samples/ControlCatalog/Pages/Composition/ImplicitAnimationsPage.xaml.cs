using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.VisualTree;

namespace ControlCatalog.Pages.Composition
{
    public partial class ImplicitAnimationsPage : UserControl
    {
        private ImplicitAnimationCollection? _implicitAnimations;

        public ImplicitAnimationsPage()
        {
            InitializeComponent();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
        }

        public static void SetEnableAnimations(Border border, bool value)
        {
            var page = border.FindAncestorOfType<ImplicitAnimationsPage>();
            if (page == null)
            {
                border.AttachedToVisualTree += delegate
                { SetEnableAnimations(border, true); };
                return;
            }

            if (ElementComposition.GetElementVisual(page) == null)
                return;

            page.EnsureImplicitAnimations();
            if (border.GetVisualParent() is Visual visualParent
                && ElementComposition.GetElementVisual(visualParent) is CompositionVisual compositionVisual)
            {
                compositionVisual.ImplicitAnimations = page._implicitAnimations;
            }
        }

        private void EnsureImplicitAnimations()
        {
            if (_implicitAnimations == null)
            {
                var compositor = ElementComposition.GetElementVisual(this)!.Compositor;

                var offsetAnimation = compositor.CreateVector3KeyFrameAnimation();
                offsetAnimation.Target = "Offset";
                offsetAnimation.InsertExpressionKeyFrame(1.0f, "this.FinalValue");
                offsetAnimation.Duration = TimeSpan.FromMilliseconds(400);

                var rotationAnimation = compositor.CreateScalarKeyFrameAnimation();
                rotationAnimation.Target = "RotationAngle";
                rotationAnimation.InsertKeyFrame(.5f, 0.160f);
                rotationAnimation.InsertKeyFrame(1f, 0f);
                rotationAnimation.Duration = TimeSpan.FromMilliseconds(400);

                var animationGroup = compositor.CreateAnimationGroup();
                animationGroup.Add(offsetAnimation);
                animationGroup.Add(rotationAnimation);

                _implicitAnimations = compositor.CreateImplicitAnimationCollection();
                _implicitAnimations["Offset"] = animationGroup;
            }
        }
    }
}
