using System;
using System.Diagnostics;
using System.Runtime.Versioning;
using Avalonia.Animation.Easings;
using Avalonia.Controls.Platform;
using Foundation;
using UIKit;

namespace Avalonia.iOS;

[UnsupportedOSPlatform("tvos")]
[SupportedOSPlatform("maccatalyst")]
[SupportedOSPlatform("ios")]
internal sealed class UIKitInputPane : IInputPane
{
    public static UIKitInputPane Instance { get; } = new();
    
    public UIKitInputPane()
    {
        NSNotificationCenter
            .DefaultCenter
            .AddObserver(UIKeyboard.WillShowNotification, KeyboardUpNotification);
        NSNotificationCenter
            .DefaultCenter
            .AddObserver(UIKeyboard.WillHideNotification, KeyboardDownNotification);
    }

    public InputPaneState State { get; private set; }
    public Rect OccludedRect { get; private set; }
    public event EventHandler<InputPaneStateEventArgs>? StateChanged;

    private void KeyboardDownNotification(NSNotification obj) => RaiseEventFromNotification(false, obj);

    private void KeyboardUpNotification(NSNotification obj) => RaiseEventFromNotification(true, obj);

    private void RaiseEventFromNotification(bool isUp, NSNotification notification)
    {
        State = isUp ? InputPaneState.Open : InputPaneState.Closed;
#if MACCATALYST
        OccludedRect = default;
        StateChanged?.Invoke(this, new InputPaneStateEventArgs(
            State, null, OccludedRect));
#else
        var startFrame = UIKeyboard.FrameBeginFromNotification(notification);
        var endFrame = UIKeyboard.FrameEndFromNotification(notification);
        var duration = UIKeyboard.AnimationDurationFromNotification(notification);
        var curve = (UIViewAnimationOptions)UIKeyboard.AnimationCurveFromNotification(notification);
        IEasing? easing =
            curve.HasFlag(UIViewAnimationOptions.CurveLinear) ? new LinearEasing()
            : curve.HasFlag(UIViewAnimationOptions.CurveEaseIn) ? new SineEaseIn()
            : curve.HasFlag(UIViewAnimationOptions.CurveEaseOut) ? new SineEaseOut()
            : curve.HasFlag(UIViewAnimationOptions.CurveEaseInOut) ? new SineEaseInOut()
            : null;

        var startRect = new Rect(startFrame.X, startFrame.Y, startFrame.Width, startFrame.Height);
        OccludedRect = new Rect(endFrame.X, endFrame.Y, endFrame.Width, endFrame.Height);

        StateChanged?.Invoke(this, new InputPaneStateEventArgs(
            State, startRect, OccludedRect, TimeSpan.FromSeconds(duration), easing));
#endif
    }
}
