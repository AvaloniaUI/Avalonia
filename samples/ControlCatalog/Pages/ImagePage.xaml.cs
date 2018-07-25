using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Effects;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace ControlCatalog.Pages
{
    public class ImagePage : UserControl
    {
        private Image iconImage;
        public ImagePage()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            iconImage = this.Get<Image>("Icon");

            var rValue = this.FindControl<Slider>("rslider").GetObservable(Slider.ValueProperty);
            rValue.Subscribe(value => this.SetEffectColor(value, this.FindControl<Slider>("gslider").Value, this.FindControl<Slider>("bslider").Value));

            var gValue = this.FindControl<Slider>("gslider").GetObservable(Slider.ValueProperty);
            gValue.Subscribe(value => this.SetEffectColor(this.FindControl<Slider>("rslider").Value, value, this.FindControl<Slider>("bslider").Value));
                
            var bValue = this.FindControl<Slider>("bslider").GetObservable(Slider.ValueProperty);
            bValue.Subscribe(value => this.SetEffectColor(this.FindControl<Slider>("rslider").Value, this.FindControl<Slider>("gslider").Value, value));
        }

        private void SetEffectColor(double r, double g, double b)
        {
            (this.FindControl<Image>("Image").Effect as DropShadowEffect).Color = Color.FromRgb((byte)r, (byte)g, (byte)b);
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            if (iconImage.Source == null)
            {
                var windowRoot = e.Root as Window;
                if (windowRoot != null)
                {
                    using (var stream = new MemoryStream())
                    {
                        windowRoot.Icon.Save(stream);
                        stream.Seek(0, SeekOrigin.Begin);
                        iconImage.Source = new Bitmap(stream);
                    }
                }
            }
        }
    }
}
