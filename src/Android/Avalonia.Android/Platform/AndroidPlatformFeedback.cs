using Android.Content;
using Android.Media;
using Android.Views;
using Avalonia.Controls.Platform;

namespace Avalonia.Android.Platform
{
    internal class AndroidPlatformFeedback(View view) : IPlatformFeedback
    {
        public bool Perform(FeedbackAction feedback, FeedbackType type)
        {
            var playSound = type != FeedbackType.Haptic;
            var vibrate = type != FeedbackType.Sound;

            if(feedback == FeedbackAction.Click)
            {
                if (playSound)
                {
                    (view.Context?.GetSystemService(Context.AudioService) as AudioManager)?.PlaySoundEffect(SoundEffect.KeyClick);
                }
                if (vibrate)
                {
                    view.PerformHapticFeedback(FeedbackConstants.ContextClick);
                }
            }
            else if(feedback == FeedbackAction.Hold)
            {
                if (vibrate)
                {
                    view.PerformHapticFeedback(FeedbackConstants.LongPress);
                }
            }

            return true;
        }
    }
}
