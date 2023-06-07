using System;
using Avalonia.Media.Immutable;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Media
{
    /// <summary>
    /// Paints an area with a swept circular gradient.
    /// </summary>
    public sealed class ConicGradientBrush : GradientBrush, IConicGradientBrush
    {
        /// <summary>
        /// Defines the <see cref="Center"/> property.
        /// </summary>
        public static readonly StyledProperty<RelativePoint> CenterProperty =
            AvaloniaProperty.Register<ConicGradientBrush, RelativePoint>(
                nameof(Center),
                RelativePoint.Center);

        /// <summary>
        /// Defines the <see cref="Angle"/> property.
        /// </summary>
        public static readonly StyledProperty<double> AngleProperty =
            AvaloniaProperty.Register<ConicGradientBrush, double>(
                nameof(Angle),
                0);
        
        /// <summary>
        /// Gets or sets the center point of the gradient.
        /// </summary>
        public RelativePoint Center
        {
            get { return GetValue(CenterProperty); }
            set { SetValue(CenterProperty, value); }
        }

        /// <summary>
        /// Gets or sets the angle of the start and end of the sweep, measured from above the center point.
        /// </summary>
        public double Angle
        {
            get { return GetValue(AngleProperty); }
            set { SetValue(AngleProperty, value); }
        }

        /// <inheritdoc/>
        public override IImmutableBrush ToImmutable()
        {
            return new ImmutableConicGradientBrush(this);
        }

        internal override Func<Compositor, ServerCompositionSimpleBrush> Factory =>
            static c => new ServerCompositionSimpleConicGradientBrush(c.Server);

        private protected override void SerializeChanges(Compositor c, BatchStreamWriter writer)
        {
            base.SerializeChanges(c, writer);
            ServerCompositionSimpleConicGradientBrush.SerializeAllChanges(writer, Angle, Center);
        }
    }
}
