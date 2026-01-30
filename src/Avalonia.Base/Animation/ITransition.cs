using System;
using Avalonia.Metadata;

namespace Avalonia.Animation
{
    /// <summary>
    /// Interface for Transition objects.
    /// </summary>
    [NotClientImplementable, PrivateApi]
    public interface ITransition
    {
    }

    [NotClientImplementable, PrivateApi]
    public interface ICompositionTransition : ITransition
    {
        /// <summary>
        /// Occurs when the transition is invalidated and needs to be re-applied.
        /// </summary>
        event EventHandler? AnimationInvalidated;

        /// <summary>
        /// Gets the composition animation for the specified parent visual.
        /// </summary>
        Rendering.Composition.Animations.CompositionAnimation? GetCompositionAnimation(Visual parent);
    }

    [NotClientImplementable, PrivateApi]
    public interface IPropertyTransition : ITransition
    {
        /// <summary>
        /// Applies the transition to the specified <see cref="Animatable"/>.
        /// </summary>
        internal IDisposable Apply(Animatable control, IClock clock, object? oldValue, object? newValue);

        /// <summary>
        /// Gets the property to be animated.
        /// </summary>
        AvaloniaProperty Property { get; set; }
    }
}
