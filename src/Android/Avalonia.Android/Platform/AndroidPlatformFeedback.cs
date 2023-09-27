using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Media;
using Android.OS;
using Android.Views;
using AndroidX.Core.Content;
using Avalonia.Controls;
using SoundEffects = Avalonia.Controls.SoundEffects;

namespace Avalonia.Android.Platform
{
    internal class AndroidPlatformFeedback : IPlatformFeedback
    {
        private View _view;

        public AndroidPlatformFeedback(View view)
        {
            _view = view;
        }

        public bool Play(SoundEffects soundEffects)
        {
            if (_view != null)
            {
                var audioManager = _view.Context.GetSystemService(Context.AudioService) as AudioManager;

                switch (soundEffects)
                {
                    case SoundEffects.Click:
                        audioManager.PlaySoundEffect(SoundEffect.KeyClick);
                        return true;
                }
            }

            return false;
        }

        public bool Vibrate(HapticFeedback hapticFeedback)
        {
            if (_view != null)
            {
                var activity = _view.Context as Activity;
                switch (hapticFeedback)
                {
                    case HapticFeedback.Click:
                        return activity?.Window?.DecorView?.PerformHapticFeedback(FeedbackConstants.ContextClick) ??
                               false;
                    case HapticFeedback.LongPress:
                        return activity?.Window?.DecorView?.PerformHapticFeedback(FeedbackConstants.LongPress) ?? false;
                }
            }

            return false;
        }

        public bool Vibrate(int duration, int amplitude = -1)
        {
            if (_view != null)
            {
                if (ContextCompat.CheckSelfPermission(_view.Context, Manifest.Permission.Vibrate) != Permission.Granted)
                {
                    return false;
                }

                var vibrator = Build.VERSION.SdkInt >= BuildVersionCodes.S ? (_view.Context.GetSystemService(Context.VibratorManagerService) as VibratorManager)?.DefaultVibrator :
                    _view.Context.GetSystemService(Context.VibratorService) as Vibrator;

                if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                {
                    vibrator?.Vibrate(VibrationEffect.CreateOneShot(duration, amplitude == -1 ? VibrationEffect.DefaultAmplitude : amplitude));
                }
                else
                {
                    vibrator?.Vibrate(duration);
                }

                return true;
            }

            return false;
        }
    }
}
