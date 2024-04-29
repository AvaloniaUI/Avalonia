using System;
using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.Rendering.Composition;
using Avalonia.Utilities;

namespace ControlCatalog.Pages
{
    public class GesturePage : UserControl
    {
        private bool _isInit;
        private double _currentScale;

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

            var image = this.Get<Image>("PinchImage");
            SetPinchHandlers(image);

            var reset = this.Get<Button>("ResetButton");

            reset.Click += (_, _) =>
            {
                var compositionVisual = ElementComposition.GetElementVisual(image);

                if(compositionVisual!= null)
                {
                    _currentScale = 1;
                    compositionVisual.Scale = new (1,1,1);
                    compositionVisual.Offset = default;
                    image.InvalidateMeasure();
                }
            };



            if (this.Find<Slider>("AngleSlider") is { } slider &&
                this.Find<Panel>("RotationGesture") is { } rotationGesture
               )
            {
                rotationGesture.AddHandler(Gestures.PinchEvent, (s, e) =>
                {
                    slider.Value = e.Angle;
                });
            }
        }

        private void SetPinchHandlers(Control? control)
        {
            if (control == null)
            {
                return;
            }

            _currentScale = 1;
            Vector3D currentOffset = default;

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

                if(compositionVisual != null)
                {
                    var scale = _currentScale * (float)e.Scale;

                    if (scale <= 1)
                    {
                        scale = 1;
                        compositionVisual.Offset = default;
                    }

                    compositionVisual.Scale = new(scale, scale, 1);

                    e.Handled = true;
                }
            });

            control.AddHandler(Gestures.PinchEndedEvent, (s, e) =>
            {
                InitComposition(control!);

                if (compositionVisual != null)
                {
                    _currentScale = compositionVisual.Scale.X;
                }
            });

            control.AddHandler(Gestures.ScrollGestureEvent, (s, e) =>
            {
                InitComposition(control!);

                if (compositionVisual != null && _currentScale != 1)
                {
                    currentOffset += new Vector3D(e.Delta.X, e.Delta.Y, 0);

                    var currentSize = control.Bounds.Size * _currentScale;

                    currentOffset = new Vector3D(MathUtilities.Clamp(currentOffset.X, 0, currentSize.Width - control.Bounds.Width),
                        (float)MathUtilities.Clamp(currentOffset.Y, 0, currentSize.Height - control.Bounds.Height),
                        0);

                    compositionVisual.Offset = currentOffset * -1;

                    e.Handled = true;
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

            control.AddHandler(Gestures.PullGestureEvent, (s, e) =>
            {
                Vector3D center = new((float)control.Bounds.Center.X, (float)control.Bounds.Center.Y, 0);
                InitComposition(ball!);
                if (ballCompositionVisual != null)
                {
                    ballCompositionVisual.Offset = defaultOffset + new Vector3D(e.Delta.X * 0.4f, e.Delta.Y * 0.4f, 0) * (inverse ? -1 : 1);

                    e.Handled = true;
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
