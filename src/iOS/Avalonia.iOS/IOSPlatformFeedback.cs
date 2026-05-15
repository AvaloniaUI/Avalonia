using System;
using AudioToolbox;
using Avalonia.Controls.Platform;
using UIKit;

namespace Avalonia.iOS
{
    internal class IOSPlatformFeedback(AvaloniaView avaloniaView) : IPlatformFeedback
    {
        public bool Perform(FeedbackAction feedback, FeedbackType type)
        {
            var performedFeedback = false;
            var playSound = type != FeedbackType.Haptic;
            var vibrate = type != FeedbackType.Sound;

            if (feedback == FeedbackAction.Click && playSound)
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

            UIImpactFeedbackStyle FeedbackToImpactStyle(FeedbackAction feedback)
            {
                if (feedback == FeedbackAction.Click)
                {
                    return UIImpactFeedbackStyle.Light;
                }
                else if(feedback == FeedbackAction.Hold)
                {
                    return UIImpactFeedbackStyle.Medium;
                }
                throw new ArgumentException($"{feedback.Key} is not a valid FeedbackAction for iOS", nameof(feedback), null);
            }
#else
            _ = avaloniaView;
#endif

            return performedFeedback;
        }
    }
}
