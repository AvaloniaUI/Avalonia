using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Rendering.Composition;

namespace ControlCatalog.Pages
{
    public partial class GesturePullPage : UserControl
    {
        private bool _isInit;

        public GesturePullPage()
        {
            InitializeComponent();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            if (_isInit)
            {
                return;
            }

            _isInit = true;

            SetPullHandlers(TopPullZone, false);
            SetPullHandlers(BottomPullZone, true);
            SetPullHandlers(RightPullZone, true);
            SetPullHandlers(LeftPullZone, false);
        }

        private void SetPullHandlers(Control control, bool inverse)
        {
            var ball = control.FindLogicalDescendantOfType<Border>();

            Vector3D defaultOffset = default;

            CompositionVisual? ballCompositionVisual = null;

            if (ball != null)
            {
                InitComposition(ball);
            }
            else
            {
                return;
            }

            control.LayoutUpdated += (s, e) =>
            {
                InitComposition(ball!);
                if (ballCompositionVisual != null)
                {
                    defaultOffset = ballCompositionVisual.Offset;
                }
            };

            control.AddHandler(InputElement.PullGestureEvent, (s, e) =>
            {
                InitComposition(ball!);
                if (ballCompositionVisual != null)
                {
                    ballCompositionVisual.Offset = defaultOffset + new Vector3D(e.Delta.X * 0.4f, e.Delta.Y * 0.4f, 0) * (inverse ? -1 : 1);
                    e.Handled = true;
                }
            });

            control.AddHandler(InputElement.PullGestureEndedEvent, (s, e) =>
            {
                InitComposition(ball!);
                if (ballCompositionVisual != null)
                {
                    ballCompositionVisual.Offset = defaultOffset;
                }
            });

            void InitComposition(Control control)
            {
                ballCompositionVisual = ElementComposition.GetElementVisual(ball);

                if (ballCompositionVisual != null)
                {
                    var offsetAnimation = ballCompositionVisual.Compositor.CreateVector3KeyFrameAnimation();
                    offsetAnimation.Target = "Offset";
                    offsetAnimation.InsertExpressionKeyFrame(1.0f, "this.FinalValue");
                    offsetAnimation.Duration = TimeSpan.FromMilliseconds(100);

                    var implicitAnimations = ballCompositionVisual.Compositor.CreateImplicitAnimationCollection();
                    implicitAnimations["Offset"] = offsetAnimation;

                    ballCompositionVisual.ImplicitAnimations = implicitAnimations;
                }
            }
        }
    }
}
