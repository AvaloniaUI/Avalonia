using System;

namespace Avalonia.Animation
{
    /// <summary>
    /// Base class for connected animation configurations that control
    /// the visual style and physics of the transition.
    /// </summary>
    internal abstract class ConnectedAnimationConfiguration
    {
    }

    /// <summary>
    /// Produces a gravity-physics effect suitable for forward navigation:
    /// the element arcs slightly as it travels and casts an animated shadow.
    /// This is the default configuration when none is specified.
    /// </summary>
    /// <remarks>
    /// Use <see cref="DirectConnectedAnimationConfiguration"/> for back navigation
    /// and <see cref="BasicConnectedAnimationConfiguration"/> for a plain transition.
    /// </remarks>
    internal class GravityConnectedAnimationConfiguration : ConnectedAnimationConfiguration
    {
        /// <summary>
        /// Gets or sets whether a drop shadow is rendered beneath the element
        /// during the gravity arc.  Defaults to <see langword="true"/>.
        /// </summary>
        public bool IsShadowEnabled { get; set; } = true;
    }

    /// <summary>
    /// Produces a direct, linear translation suitable for back navigation.
    /// No gravity arc or shadow is applied, and the default duration is shorter (150 ms).
    /// </summary>
    /// <remarks>
    /// Assign this to <see cref="ConnectedAnimation.Configuration"/> before calling
    /// <c>TryStart</c> on the return animation to animate back to the source view.
    /// </remarks>
    internal class DirectConnectedAnimationConfiguration : ConnectedAnimationConfiguration
    {
        /// <summary>
        /// Gets or sets the duration of the animation.
        /// When <see langword="null"/> a default of 150 ms is used.
        /// </summary>
        public TimeSpan? Duration { get; set; }
    }

    /// <summary>
    /// Produces a simple ease-in-out transition between the source and destination elements
    /// with no gravity arc or shadow. Duration is taken from
    /// <see cref="ConnectedAnimationService.DefaultDuration"/>.
    /// </summary>
    internal class BasicConnectedAnimationConfiguration : ConnectedAnimationConfiguration
    {
    }
}
