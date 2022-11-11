namespace Avalonia.Controls
{
    public interface IPlatformFeedback
    {
        /// <summary>
        /// Plays a <see cref="SoundEffects"/>
        /// </summary>
        /// <param name="soundEffect">The sound effect to play</param>
        void Play(SoundEffects soundEffects);

        /// <summary>
        /// Requests haptic feedback from the platform
        /// </summary>
        /// <param name="hapticFeedback">Value representing a predefined haptic feedback</param>
        void Vibrate(HapticFeedback hapticFeedback);

        /// <summary>
        /// Requests haptic feedback from the platform
        /// </summary>
        /// <param name="duration">The duration, in milliseconds, of the vibration</param>
        /// <param name="amplitude">The amplitude of the vibration</param>
        void Vibrate(int duration, int amplitude = -1);
    }

    /// <summary>
    /// Predefined platform sound effects
    /// </summary>
    public enum SoundEffects
    {
        Click
    }

    /// <summary>
    /// Predefined platform haptic feedback
    /// </summary>
    public enum HapticFeedback
    {
        Click,
        LongPress,
    }
}
