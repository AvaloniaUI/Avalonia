using System;
using Avalonia.Controls.Platform;
using Avalonia.Media;
using UIKit;

namespace Avalonia.iOS;
#nullable enable

internal class InsetsManager : IInsetsManager
{
    private readonly AvaloniaView _view;
    private IAvaloniaViewController? _controller;
    private bool _displayEdgeToEdge;

    public InsetsManager(AvaloniaView view)
    {
        _view = view;
    }

    internal void InitWithController(IAvaloniaViewController controller)
    {
        _controller = controller;
        if (_controller is not null)
        {
            _controller.SafeAreaPaddingChanged += (_, _) =>
            {
                SafeAreaChanged?.Invoke(this, new SafeAreaChangedArgs(SafeAreaPadding));
                DisplayEdgeToEdgeChanged?.Invoke(this, _displayEdgeToEdge);
            };
        }
    }

    public SystemBarTheme? SystemBarTheme
    {
        get => _controller?.PreferredStatusBarStyle switch
        {
            UIStatusBarStyle.LightContent => Controls.Platform.SystemBarTheme.Dark,
            UIStatusBarStyle.DarkContent => Controls.Platform.SystemBarTheme.Light,
            _ => null
        };
        set
        {
            if (_controller != null)
            {
                _controller.PreferredStatusBarStyle = value switch
                {
                    Controls.Platform.SystemBarTheme.Light => UIStatusBarStyle.DarkContent,
                    Controls.Platform.SystemBarTheme.Dark => UIStatusBarStyle.LightContent,
                    null => UIStatusBarStyle.Default,
                    _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
                };
            }
        }
    }

    public bool? IsSystemBarVisible
    {
        get => _controller?.PrefersStatusBarHidden == false;
        set
        {
            if (_controller is not null)
            {
                _controller.PrefersStatusBarHidden = value == false;
            }
        }
    }
    public event EventHandler<SafeAreaChangedArgs>? SafeAreaChanged;
    public event EventHandler<bool>? DisplayEdgeToEdgeChanged;

    public bool DisplayEdgeToEdge
    {
        get => _displayEdgeToEdge;
        set
        {
            if (_displayEdgeToEdge != value)
            {
                _displayEdgeToEdge = value;
                DisplayEdgeToEdgeChanged?.Invoke(this, value);
            }
        }
    }

    public Thickness SafeAreaPadding => _controller?.SafeAreaPadding ?? default;

    public Color? SystemBarColor { get; set; }
}
