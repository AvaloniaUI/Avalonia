using System;
using System.Diagnostics;
using Avalonia.Animation.Easings;
using Avalonia.Controls.Platform;
using Foundation;
using UIKit;

#nullable enable
namespace Avalonia.iOS;

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

        var startFrame = UIKeyboard.FrameBeginFromNotification(notification);
        var endFrame = UIKeyboard.FrameEndFromNotification(notification);
        var duration = UIKeyboard.AnimationDurationFromNotification(notification);
        var curve = (UIViewAnimationOptions)UIKeyboard.AnimationCurveFromNotification(notification);
        IEasing? easing =
            curve.HasFlag(UIViewAnimationOptions.CurveLinear) ? new LinearEasing()
            : curve.HasFlag(UIViewAnimationOptions.CurveEaseIn) ? new QuadraticEaseIn()
            : curve.HasFlag(UIViewAnimationOptions.CurveEaseOut) ? new QuadraticEaseOut()
            : curve.HasFlag(UIViewAnimationOptions.CurveEaseInOut) ? new QuadraticEaseInOut()
            : null;

        var startRect = new Rect(startFrame.X, startFrame.Y, startFrame.Width, startFrame.Height);
        OccludedRect = new Rect(endFrame.X, endFrame.Y, endFrame.Width, endFrame.Height);

        Debug.WriteLine($"iOS {State} {startFrame} {endFrame} {duration} {curve}");
        StateChanged?.Invoke(this, new InputPaneStateEventArgs(
            State, startRect, OccludedRect, TimeSpan.FromSeconds(duration), easing));
    }
}
