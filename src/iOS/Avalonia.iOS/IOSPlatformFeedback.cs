using System;
using AudioToolbox;
using Avalonia.Controls.Platform;
using UIKit;

namespace Avalonia.iOS
{
    internal class IOSPlatformFeedback(AvaloniaView avaloniaView) : IPlatformFeedback
    {
        public bool Perform(FeedbackEffect feedback, FeedbackType type)
        {
            var performedFeedback = false;
            var playSound = type != FeedbackType.Haptic;
            var vibrate = type != FeedbackType.Sound;

            if (feedback == FeedbackEffect.Click && playSound)
            {
                var sound = new SystemSound(1104);
                sound.PlaySystemSound();
                performedFeedback = true;
            }

#if !TVOS
            if (vibrate)
            {
                using var generator = OperatingSystem.IsIOSVersionAtLeast(17, 5) || OperatingSystem.IsMacCatalystVersionAtLeast(17, 5)
                    ? UIImpactFeedbackGenerator.GetFeedbackGenerator(FeedbackToImpactStyle(feedback), avaloniaView)
                    : new UIImpactFeedbackGenerator(FeedbackToImpactStyle(feedback));
                generator.ImpactOccurred();
                performedFeedback = true;
            }

            UIImpactFeedbackStyle FeedbackToImpactStyle(FeedbackEffect feedback)
            {
                return feedback switch
                {
                    FeedbackEffect.Click => UIImpactFeedbackStyle.Light,
                    FeedbackEffect.LongPress => UIImpactFeedbackStyle.Medium,
                    _ => throw new ArgumentOutOfRangeException(nameof(feedback), feedback, null)
                };
            }
#else
            _ = avaloniaView;
#endif

            return performedFeedback;
        }
    }
}
