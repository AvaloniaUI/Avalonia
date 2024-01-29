using System;
using Avalonia.Metadata;
using UIKit;

namespace Avalonia.iOS;

[Unstable]
public interface IAvaloniaViewController
{
#if !TVOS
    UIStatusBarStyle PreferredStatusBarStyle { get; set; }
#endif
    bool PrefersStatusBarHidden { get; set; }
    Thickness SafeAreaPadding { get; }
    event EventHandler? SafeAreaPaddingChanged;
}

/// <inheritdoc cref="IAvaloniaViewController" />
public class DefaultAvaloniaViewController : UIViewController, IAvaloniaViewController
{
#if !TVOS
    private UIStatusBarStyle? _preferredStatusBarStyle;
#endif
    private bool? _prefersStatusBarHidden;
    
    /// <inheritdoc/>
    public override void ViewDidLayoutSubviews()
    {
        base.ViewDidLayoutSubviews();
        var size = View?.Frame.Size ?? default;
        var frame = View?.SafeAreaLayoutGuide.LayoutFrame ?? default;
        var safeArea = new Thickness(frame.Left, frame.Top, size.Width - frame.Right, size.Height - frame.Bottom);
        if (SafeAreaPadding != safeArea)
        {
            SafeAreaPadding = safeArea;
            SafeAreaPaddingChanged?.Invoke(this, EventArgs.Empty);
        }
    }

#if !TVOS
    /// <inheritdoc/>
    public override bool PrefersStatusBarHidden()
    {
        return _prefersStatusBarHidden ??= base.PrefersStatusBarHidden();
    }

    /// <inheritdoc/>
    public override UIStatusBarStyle PreferredStatusBarStyle()
    {
        // don't set _preferredStatusBarStyle value if it's null, so we can keep "default" there instead of actual app style.
        return _preferredStatusBarStyle ?? base.PreferredStatusBarStyle();
    }

    UIStatusBarStyle IAvaloniaViewController.PreferredStatusBarStyle
    {
        get => _preferredStatusBarStyle ?? UIStatusBarStyle.Default;
        set
        {
            _preferredStatusBarStyle = value;
            SetNeedsStatusBarAppearanceUpdate();
        }
    }
#endif

    bool IAvaloniaViewController.PrefersStatusBarHidden
    {
        get => _prefersStatusBarHidden ?? false; // false is default on ios/ipados
        set
        {
            _prefersStatusBarHidden = value;
#if !TVOS
            SetNeedsStatusBarAppearanceUpdate();
#endif
        }
    }

    /// <inheritdoc/>
    public Thickness SafeAreaPadding { get; private set; }

    /// <inheritdoc/>
    public event EventHandler? SafeAreaPaddingChanged;
}
