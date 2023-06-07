using System;
using Avalonia.Media.Immutable;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Media
{
    /// <summary>
    /// A brush that draws with a linear gradient.
    /// </summary>
    public sealed class LinearGradientBrush : GradientBrush, ILinearGradientBrush
    {
        /// <summary>
        /// Defines the <see cref="StartPoint"/> property.
        /// </summary>
        public static readonly StyledProperty<RelativePoint> StartPointProperty =
            AvaloniaProperty.Register<LinearGradientBrush, RelativePoint>(
                nameof(StartPoint),
                RelativePoint.TopLeft);

        /// <summary>
        /// Defines the <see cref="EndPoint"/> property.
        /// </summary>
        public static readonly StyledProperty<RelativePoint> EndPointProperty =
            AvaloniaProperty.Register<LinearGradientBrush, RelativePoint>(
                nameof(EndPoint), 
                RelativePoint.BottomRight);

        /// <summary>
        /// Gets or sets the start point for the gradient.
        /// </summary>
        public RelativePoint StartPoint
        {
            get { return GetValue(StartPointProperty); }
            set { SetValue(StartPointProperty, value); }
        }

        /// <summary>
        /// Gets or sets the end point for the gradient.
        /// </summary>
        public RelativePoint EndPoint
        {
            get { return GetValue(EndPointProperty); }
            set { SetValue(EndPointProperty, value); }
        }

        /// <inheritdoc/>
        public override IImmutableBrush ToImmutable()
        {
            return new ImmutableLinearGradientBrush(this);
        }
        
        internal override Func<Compositor, ServerCompositionSimpleBrush> Factory =>
            static c => new ServerCompositionSimpleLinearGradientBrush(c.Server);

        private protected override void SerializeChanges(Compositor c, BatchStreamWriter writer)
        {
            base.SerializeChanges(c, writer);
            ServerCompositionSimpleLinearGradientBrush.SerializeAllChanges(writer, StartPoint, EndPoint);
        }
    }
}
