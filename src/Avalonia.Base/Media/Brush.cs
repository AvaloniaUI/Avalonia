using System;
using System.ComponentModel;
using Avalonia.Animation;
using Avalonia.Animation.Animators;
using Avalonia.Media.Immutable;
using Avalonia.Reactive;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Drawing;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Media
{
    /// <summary>
    /// Describes how an area is painted.
    /// </summary>
    [TypeConverter(typeof(BrushConverter))]
    public abstract class Brush : Animatable, IBrush, ICompositionRenderResource<IBrush>, ICompositorSerializable
    {
        /// <summary>
        /// Defines the <see cref="Opacity"/> property.
        /// </summary>
        public static readonly StyledProperty<double> OpacityProperty =
            AvaloniaProperty.Register<Brush, double>(nameof(Opacity), 1.0);

        /// <summary>
        /// Defines the <see cref="Transform"/> property.
        /// </summary>
        public static readonly StyledProperty<ITransform?> TransformProperty =
            AvaloniaProperty.Register<Brush, ITransform?>(nameof(Transform));

        /// <summary>
        /// Defines the <see cref="TransformOrigin"/> property
        /// </summary>
        public static readonly StyledProperty<RelativePoint> TransformOriginProperty =
            AvaloniaProperty.Register<Brush, RelativePoint>(nameof(TransformOrigin));

        /// <summary>
        /// Gets or sets the opacity of the brush.
        /// </summary>
        public double Opacity
        {
            get { return GetValue(OpacityProperty); }
            set { SetValue(OpacityProperty, value); }
        }

        /// <summary>
        /// Gets or sets the transform of the brush.
        /// </summary>
        public ITransform? Transform
        {
            get { return GetValue(TransformProperty); }
            set { SetValue(TransformProperty, value); }
        }

        /// <summary>
        /// Gets or sets the origin of the brush <see cref="Transform"/>
        /// </summary>
        public RelativePoint TransformOrigin
        {
            get => GetValue(TransformOriginProperty);
            set => SetValue(TransformOriginProperty, value);
        }

        /// <summary>
        /// Parses a brush string.
        /// </summary>
        /// <param name="s">The brush string.</param>
        /// <returns>The <see cref="Color"/>.</returns>
        public static IBrush Parse(string s)
        {
            _ = s ?? throw new ArgumentNullException(nameof(s));

            if (s.Length > 0)
            {
                if (s[0] == '#')
                {
                    return new ImmutableSolidColorBrush(Color.Parse(s));
                }

                var brush = KnownColors.GetKnownBrush(s);
                if (brush != null)
                {
                    return brush;
                }
            }

            throw new FormatException($"Invalid brush string: '{s}'.");
        }
        
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            if (change.Property == TransformProperty) 
                _resource.ProcessPropertyChangeNotification(change);

            RegisterForSerialization();
            
            base.OnPropertyChanged(change);
        }
        
        private protected void RegisterForSerialization() =>
            _resource.RegisterForInvalidationOnAllCompositors(this);

        private CompositorResourceHolder<ServerCompositionSimpleBrush> _resource;

        IBrush ICompositionRenderResource<IBrush>.GetForCompositor(Compositor c) => _resource.GetForCompositor(c);

        internal abstract Func<Compositor, ServerCompositionSimpleBrush> Factory { get; }

        void ICompositionRenderResource.AddRefOnCompositor(Compositor c)
        {
            if (_resource.CreateOrAddRef(c, this, out _, Factory))
                OnReferencedFromCompositor(c);
        }

        private protected virtual void OnReferencedFromCompositor(Compositor c)
        {
            if (Transform is ICompositionRenderResource<ITransform> resource)
                resource.AddRefOnCompositor(c);
        }

        void ICompositionRenderResource.ReleaseOnCompositor(Compositor c)
        {
            if(_resource.Release(c))
                OnUnreferencedFromCompositor(c);
        }
        
        protected virtual void OnUnreferencedFromCompositor(Compositor c)
        {
            if (Transform is ICompositionRenderResource<ITransform> resource)
                resource.ReleaseOnCompositor(c);
        }

        SimpleServerObject? ICompositorSerializable.TryGetServer(Compositor c) => _resource.TryGetForCompositor(c);

        private protected virtual void SerializeChanges(Compositor c, BatchStreamWriter writer)
        {
            ServerCompositionSimpleBrush.SerializeAllChanges(writer, Opacity, TransformOrigin, Transform.GetServer(c));
        }
        
        void ICompositorSerializable.SerializeChanges(Compositor c, BatchStreamWriter writer) => SerializeChanges(c, writer);
    }
}
