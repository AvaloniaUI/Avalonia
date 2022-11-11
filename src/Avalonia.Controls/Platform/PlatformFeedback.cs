using Avalonia.Controls.Platform;
using Avalonia.VisualTree;

namespace Avalonia.Controls
{
    /// <summary>
    /// Helper class to set and trigger feedback effects
    /// </summary>
    public static class PlatformFeedback
    {
        /// <summary>
        /// Plays the <see cref="SoundEffects"/> attached to the control.
        /// </summary>
        /// <param name="control">The control with the <see cref="SoundEffects"/> attached to.</param>
        /// <param name="soundEffects">The <see cref="SoundEffects"/> to play</param>
        internal static void PlaySoundEffect(this Control control, SoundEffects soundEffects)
        {
            var platformFeedback = (control.GetVisualRoot() as TopLevel)?.PlatformFeedbackProvider;
            platformFeedback?.Play(soundEffects);
        }

        /// <summary>
        /// Requests haptic feedback from the platform
        /// </summary>
        /// <param name="control"></param>
        /// <param name="hapticFeedback">Value representing a predefined haptic feedback</param>
        internal static void Vibrate(this Control control, HapticFeedback hapticFeedback)
        {
            var platformFeedback = (control.GetVisualRoot() as TopLevel)?.PlatformFeedbackProvider;
            platformFeedback?.Vibrate(hapticFeedback);
        }

        /// <summary>
        /// Requests haptic feedback from the platform
        /// </summary>
        /// <param name="control"></param>
        /// <param name="duration">The duration, in milliseconds, of the vibration</param>
        /// <param name="amplitude">The amplitude of the vibration</param>
        internal static void Vibrate(this Control control, int duration, int amplitude = -1)
        {
            var platformFeedback = (control.GetVisualRoot() as TopLevel)?.PlatformFeedbackProvider;
            platformFeedback?.Vibrate(duration, amplitude);
        }
    }
}
