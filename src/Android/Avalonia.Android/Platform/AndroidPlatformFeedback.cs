using Android.Content;
using Android.Media;
using Android.Views;
using Avalonia.Controls.Platform;

namespace Avalonia.Android.Platform
{
    internal class AndroidPlatformFeedback(View view) : IPlatformFeedback
    {
        public bool Perform(FeedbackEffect feedback, FeedbackType type)
        {
            var playSound = type != FeedbackType.Haptic;
            var vibrate = type != FeedbackType.Sound;

            switch (feedback)
            {
                case FeedbackEffect.Click:
                    if (playSound)
                    {
                        (view.Context?.GetSystemService(Context.AudioService) as AudioManager)?.PlaySoundEffect(SoundEffect.KeyClick);
                    }
                    if (vibrate)
                    {
                        view.PerformHapticFeedback(FeedbackConstants.ContextClick);
                    }
                    break;
                case FeedbackEffect.LongPress:
                    if (vibrate)
                    {
                        view.PerformHapticFeedback(FeedbackConstants.LongPress);
                    }
                    break;
                default:
                    break;
            }

            return true;
        }
    }
}
