using System;
using AudioToolbox;
using Avalonia.Controls;
using UIKit;

namespace Avalonia.iOS
{
    internal class IOSPlatformFeedback(AvaloniaView avaloniaView) : IPlatformFeedback
    {
        private static SystemSound s_defaultSound = new(1104);
        public bool Perform(FeedbackAction feedback, FeedbackType type)
        {
            var performedFeedback = false;
            var playSound = type is FeedbackType.Sound or FeedbackType.Auto;
            var vibrate = type is FeedbackType.Haptic or FeedbackType.Auto;

            if (feedback == FeedbackAction.Click && playSound)
            {
                s_defaultSound.PlaySystemSound();
                performedFeedback = true;
            }

#if !TVOS
            if (vibrate)
            {
                if (FeedbackToImpactStyle(feedback) is { } uIImpactFeedbackStyle)
                {
                    using var generator = OperatingSystem.IsIOSVersionAtLeast(17, 5) || OperatingSystem.IsMacCatalystVersionAtLeast(17, 5)
                        ? UIImpactFeedbackGenerator.GetFeedbackGenerator(uIImpactFeedbackStyle, avaloniaView)
                        : new UIImpactFeedbackGenerator(uIImpactFeedbackStyle);
                    generator.ImpactOccurred();
                    performedFeedback = true;
                }
            }

            UIImpactFeedbackStyle? FeedbackToImpactStyle(FeedbackAction feedback)
            {
                if (feedback == FeedbackAction.Click)
                {
                    return UIImpactFeedbackStyle.Light;
                }
                else if (feedback == FeedbackAction.Hold)
                {
                    return UIImpactFeedbackStyle.Medium;
                }

                return null;
            }
#else
            _ = avaloniaView;
#endif

            return performedFeedback;
        }
    }
}
