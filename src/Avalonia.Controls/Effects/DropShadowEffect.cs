using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Visuals.Effects;

namespace Avalonia.Controls.Effects
{
    class DropShadowEffect: AvaloniaObject, IEffect
    {
        /// <summary>
        /// Defines the <see cref="OffsetXProperty"/> property.
        /// </summary>
        public static readonly StyledProperty<double> OffsetXProperty =
            AvaloniaProperty.Register<DropShadowEffect, double>(nameof(OffsetX), 5);

        /// <summary>
        /// Defines the <see cref="OffsetXProperty"/> property.
        /// </summary>
        public static readonly StyledProperty<double> OffsetYProperty =
            AvaloniaProperty.Register<DropShadowEffect, double>(nameof(OffsetY), 5);

        /// <summary>
        /// Defines the <see cref="BlurProperty"/> property.
        /// </summary>
        public static readonly StyledProperty<double> BlurProperty =
            AvaloniaProperty.Register<DropShadowEffect, double>(nameof(Blur), 5);


        /// <summary>
        /// Defines the <see cref="ColorProperty"/> property.
        /// </summary>
        public static readonly StyledProperty<Color> ColorProperty =
            AvaloniaProperty.Register<DropShadowEffect, Color>(nameof(Color), Brushes.Black.Color);

        /// <summary>
        /// Gets or sets 
        /// </summary>
        public double OffsetX
        {
            get { return GetValue(OffsetXProperty); }
            set { SetValue(OffsetXProperty, value); }
        }

        /// <summary>
        /// Gets or sets 
        /// </summary>
        public double OffsetY
        {
            get { return GetValue(OffsetYProperty); }
            set { SetValue(OffsetYProperty, value); }
        }

        /// <summary>
        /// Gets or sets 
        /// </summary>
        public double Blur
        {
            get { return GetValue(BlurProperty); }
            set { SetValue(BlurProperty, value); }
        }

        /// <summary>
        /// Gets or sets 
        /// </summary>
        public Color Color
        {
            get { return GetValue(ColorProperty); }
            set { SetValue(ColorProperty, value); }
        }

        private IDropShadowEffectImpl _platformImpl;

        public IEffectImpl PlatformImpl
        {
            get
            {
                if (_platformImpl == null)
                {
                    var factory = AvaloniaLocator.Current.GetService<IPlatformRenderInterface>();
                    _platformImpl = factory.CreateDropShadowEffect(OffsetX, OffsetY, Blur, Color);
                }

                return (IEffectImpl)_platformImpl;
            }
        }
    }
}
