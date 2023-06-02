using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input.TextInput;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Rendering.Composition;
using Avalonia.Win32.WinRT.Composition;

namespace Sandbox
{
    public partial class MainWindow : Window
    {
        private bool state = false;

        protected override void OnLoaded()
        {
            ButtonAnimate.Click += ButtonAnimateOnClick;
            ButtonAnimateOnClick(null, null);

            var borderVisual = ElementComposition.GetElementVisual(FancyBorder);
            var compositor = borderVisual.Compositor;
            
            var offsetAnimation = compositor.CreateVector3KeyFrameAnimation();
            offsetAnimation.Target = "Offset";
            offsetAnimation.InsertExpressionKeyFrame(1.0f, "this.FinalValue");
            offsetAnimation.Duration = TimeSpan.FromMilliseconds(400);

            var sizeAnimation = compositor.CreateVector2KeyFrameAnimation();
            sizeAnimation.Target = "Size";
            sizeAnimation.InsertExpressionKeyFrame(1.0f, "this.FinalValue");
            sizeAnimation.Duration = TimeSpan.FromMilliseconds(400);

            var implicitAnimations = compositor.CreateImplicitAnimationCollection();
            implicitAnimations["Offset"] = offsetAnimation; 
            implicitAnimations["Size"] = sizeAnimation;

            borderVisual.ImplicitAnimations = implicitAnimations;
            
            base.OnLoaded();
        }

        private void ButtonAnimateOnClick(object sender, RoutedEventArgs e)
        {
            const double dim = 200d;
            var offset = state ? -dim : dim;
            offset /= 2;
            offset += 300d;
            Canvas.SetTop(FancyBorder, (SampleCanvas.Bounds.Y / 2) + offset );
            Canvas.SetLeft(FancyBorder, (SampleCanvas.Bounds.X / 2) +  offset );
            FancyBorder.Width = dim * (state ? 0.3 : 1);
            FancyBorder.Height = dim* (state ? 0.3 : 1);
            state = !state;
        }

        public MainWindow()
        {
            InitializeComponent();
        }
    }
}
