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
        /// <param name="soundEffects">The <see cref="SoundEffects"/> to play.</param>
        internal static bool PlaySoundEffect(this Control control, SoundEffects soundEffects)
        {
            var platformFeedback = TopLevel.GetTopLevel(control)?.PlatformFeedback;
            return platformFeedback?.Play(soundEffects) ?? false;
        }

        /// <summary>
        /// Requests haptic feedback from the platform.
        /// </summary>
        /// <param name="control">The control with the <see cref="HapticFeedback"/> attached to.</param>
        /// <param name="hapticFeedback">Value representing a predefined haptic feedback.</param>
        internal static bool Vibrate(this Control control, HapticFeedback hapticFeedback)
        {
            var platformFeedback = TopLevel.GetTopLevel(control)?.PlatformFeedback;
            return platformFeedback?.Vibrate(hapticFeedback) ?? false;
        }

        /// <summary>
        /// Requests haptic feedback from the platform.
        /// </summary>
        /// <param name="control">The control with the <see cref="HapticFeedback"/> attached to.</param>
        /// <param name="duration">The duration, in milliseconds, of the vibration.</param>
        /// <param name="amplitude">The amplitude of the vibration.</param>
        internal static bool Vibrate(this Control control, int duration, int amplitude = -1)
        {
            var platformFeedback = TopLevel.GetTopLevel(control)?.PlatformFeedback;
            return platformFeedback?.Vibrate(duration, amplitude) ?? false;
        }
    }
}
