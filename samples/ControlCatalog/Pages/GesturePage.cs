using System;
using System.Diagnostics;
using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.Rendering.Composition;
using Avalonia.VisualTree;

namespace ControlCatalog.Pages
{
    public class GesturePage : UserControl
    {
        private bool _isInit;
        private float _currentScale;

        public GesturePage()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            if(_isInit)
            {
                return;
            }

            _isInit = true;

            SetPullHandlers(this.Find<Border>("TopPullZone"), false);
            SetPullHandlers(this.Find<Border>("BottomPullZone"), true);
            SetPullHandlers(this.Find<Border>("RightPullZone"), true);
            SetPullHandlers(this.Find<Border>("LeftPullZone"), false);

            var image = this.Find<Image>("PinchImage");
            SetPinchHandlers(image);

            var reset = this.Find<Button>("ResetButton");

            reset!.Click += (s, e) =>
            {
                var compositionVisual = ElementComposition.GetElementVisual(image);

                if(compositionVisual!= null)
                {
                    _currentScale = 1;
                    compositionVisual.Scale = new Vector3(1,1,1);
                    image.InvalidateMeasure();
                }
            };
                
        }

        private void SetPinchHandlers(Control? control)
        {
            if (control == null)
            {
                return;
            }

            _currentScale = 1;
            Vector3 currentOffset = default;
            bool isZooming = false;

            CompositionVisual? compositionVisual = null;

            void InitComposition(Control visual)
            {
                if (compositionVisual != null)
                {
                    return;
                }

                compositionVisual = ElementComposition.GetElementVisual(visual);
            }

            control.LayoutUpdated += (s, e) =>
            {
                InitComposition(control!);
                if (compositionVisual != null)
                {
                    compositionVisual.Scale = new(_currentScale, _currentScale, 1);

                    if(currentOffset == default)
                    {
                        currentOffset = compositionVisual.Offset;
                    }
                }
            };

            control.AddHandler(Gestures.PinchEvent, (s, e) =>
            {
                InitComposition(control!);

                isZooming = true;

                if(compositionVisual != null)
                {
                    var scale = _currentScale * (float)e.Scale;

                    compositionVisual.Scale = new(scale, scale, 1);
                }
            });

            control.AddHandler(Gestures.PinchEndedEvent, (s, e) =>
            {
                InitComposition(control!);

                isZooming = false;

                if (compositionVisual != null)
                {
                    _currentScale = compositionVisual.Scale.X;
                }
            });

            control.AddHandler(Gestures.ScrollGestureEvent, (s, e) =>
            {
                InitComposition(control!);

                if (compositionVisual != null && !isZooming)
                {
                    currentOffset -= new Vector3((float)e.Delta.X, (float)e.Delta.Y, 0);

                    compositionVisual.Offset = currentOffset;
                }
            });
        }

        private void SetPullHandlers(Control? control, bool inverse)
        {
            if (control == null)
            {
                return;
            }

            var ball = control.FindLogicalDescendantOfType<Border>();

            Vector3 defaultOffset = default;

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

            control.AddHandler(Gestures.PullGestureEvent, (s, e) =>
            {
                Vector3 center = new((float)control.Bounds.Center.X, (float)control.Bounds.Center.Y, 0);
                InitComposition(ball!);
                if (ballCompositionVisual != null)
                {
                    ballCompositionVisual.Offset = defaultOffset + new System.Numerics.Vector3((float)e.Delta.X * 0.4f, (float)e.Delta.Y * 0.4f, 0) * (inverse ? -1 : 1);
                }
            });

            control.AddHandler(Gestures.PullGestureEndedEvent, (s, e) =>
            {
                InitComposition(ball!);
                if (ballCompositionVisual != null)
                {
                    ballCompositionVisual.Offset = defaultOffset;
                }
            });

            void InitComposition(Control control)
            {
                if (ballCompositionVisual != null)
                {
                    return;
                }

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
