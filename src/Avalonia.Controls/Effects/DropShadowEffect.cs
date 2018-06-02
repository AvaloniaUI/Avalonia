using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Visuals.Effects;
using System;

namespace Avalonia.Controls.Effects
{
    public class DropShadowEffect: Effect
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
            get => GetValue(OffsetXProperty);
            set { SetValue(OffsetXProperty, value); }
        }

        /// <summary>
        /// Gets or sets 
        /// </summary>
        public double OffsetY
        {
            get => GetValue(OffsetYProperty);
            set { SetValue(OffsetYProperty, value); }
        }

        /// <summary>
        /// Gets or sets 
        /// </summary>
        public double Blur
        {
            get => GetValue(BlurProperty);
            set { SetValue(BlurProperty, value); }
        }

        /// <summary>
        /// Gets or sets 
        /// </summary>
        public Color Color
        {
            get => GetValue(ColorProperty);
            set { SetValue(ColorProperty, value); }
        }

        private IDropShadowEffectImpl _platformImpl;

        static DropShadowEffect()
        {
            OffsetXProperty.Changed.Subscribe(EffectChanged);
            OffsetYProperty.Changed.Subscribe(EffectChanged);
            ColorProperty.Changed.Subscribe(EffectChanged);
            ColorProperty.Changed.Subscribe(EffectChanged);
        }

        private static void EffectChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Sender is DropShadowEffect sender)
            {
                sender.RaiseChanged();
            }
        }

        public override IEffectImpl PlatformImpl
        {
            get
            {
                if (_platformImpl == null)
                {
                    var factory = AvaloniaLocator.Current.GetService<IPlatformRenderInterface>();
                    _platformImpl = factory.CreateDropShadowEffect(OffsetX, OffsetY, Blur, Color);
                    _isDirty = false;
                }

                if (_isDirty)
                {
                    _platformImpl.OffsetX = OffsetX;
                    _platformImpl.OffsetY = OffsetY;
                    _platformImpl.Color = Color;
                    _platformImpl.Blur = Blur;
                    _isDirty = false;
                }

                return (IEffectImpl)_platformImpl;
            }
        }
    }
}
