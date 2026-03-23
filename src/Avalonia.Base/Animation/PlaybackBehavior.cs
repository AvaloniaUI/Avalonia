namespace Avalonia.Animation
{
    /// <summary>
    /// Determines whether an animation pauses when its target control is not effectively visible
    /// (see <see cref="Visual.IsEffectivelyVisible"/>).
    /// </summary>
    public enum PlaybackBehavior
    {
        /// <summary>
        /// The system decides based on context. Manually started animations
        /// (via <see cref="Animation.RunAsync(Animatable, System.Threading.CancellationToken)"/>)
        /// and animations that target <see cref="Visual.IsVisibleProperty"/> always play.
        /// Style-applied animations pause when the control is not effectively visible
        /// (see <see cref="Visual.IsEffectivelyVisible"/>).
        /// </summary>
        Auto,

        /// <summary>
        /// The animation always plays regardless of the control's effective visibility state.
        /// </summary>
        Always,

        /// <summary>
        /// The animation pauses when the control is not effectively visible
        /// (see <see cref="Visual.IsEffectivelyVisible"/>).
        /// </summary>
        OnlyIfVisible,
    }
}
