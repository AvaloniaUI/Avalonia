using System;
using AudioToolbox;
using Avalonia.Controls;
using UIKit;

namespace Avalonia.iOS;

internal class PlatformFeedback : IPlatformFeedback
{
    public bool Play(SoundEffects soundEffects)
    {
        if (soundEffects is SoundEffects.Click)
        {
            var sound = new SystemSound(1104);
            sound.PlaySystemSound();
        }

        return false;
    }

    public bool Vibrate(HapticFeedback hapticFeedback)
    {
        if (hapticFeedback is HapticFeedback.Click or HapticFeedback.LongPress)
        {
            using var generator = new UIImpactFeedbackGenerator(FeedbackToImpactStyle(hapticFeedback));
            generator.Prepare();
            generator.ImpactOccurred();
            return true;
        }

        return false;
    }

    public bool Vibrate(int duration, int amplitude = -1)
    {
        return false;
    }
    
    private static UIImpactFeedbackStyle FeedbackToImpactStyle(HapticFeedback feedback)
    {
        return feedback switch
        {
            HapticFeedback.Click => UIImpactFeedbackStyle.Light,
            HapticFeedback.LongPress => UIImpactFeedbackStyle.Medium,
            _ => throw new ArgumentOutOfRangeException(nameof(feedback), feedback, null)
        };
    }
}
