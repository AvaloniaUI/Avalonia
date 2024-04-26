using System;
using Avalonia.Controls.Platform;
using Avalonia.Media;

namespace Avalonia.iOS;

internal class InsetsManager : InsetsManagerBase
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
                OnSafeAreaChanged(new SafeAreaChangedArgs(SafeAreaPadding));
                DisplayEdgeToEdgeChanged?.Invoke(this, _displayEdgeToEdge);
            };
        }
    }

    public override bool? IsSystemBarVisible
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
    public event EventHandler<bool>? DisplayEdgeToEdgeChanged;

    public override bool DisplayEdgeToEdge
    {
        get => _displayEdgeToEdge;
        set
        {
            if (_displayEdgeToEdge != value)
            {
                _displayEdgeToEdge = value;
                DisplayEdgeToEdgeChanged?.Invoke(this, value);
                OnSafeAreaChanged(new SafeAreaChangedArgs(SafeAreaPadding));
            }
        }
    }

    public override Thickness SafeAreaPadding => _displayEdgeToEdge ? _controller?.SafeAreaPadding ?? default : default;

    public override Color? SystemBarColor { get; set; }
}
