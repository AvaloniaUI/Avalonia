using System;
using Avalonia.Controls.Platform;
using Avalonia.Media;

namespace Avalonia.iOS;

internal class InsetsManager : IInsetsManager
{
    private IAvaloniaViewController? _controller;
    private bool _displayEdgeToEdge = true;

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
                SafeAreaChanged?.Invoke(this, new SafeAreaChangedArgs(SafeAreaPadding));
            }
        }
    }

    public Thickness SafeAreaPadding => _displayEdgeToEdge ? _controller?.SafeAreaPadding ?? default : default;

    public Color? SystemBarColor { get; set; }
}
