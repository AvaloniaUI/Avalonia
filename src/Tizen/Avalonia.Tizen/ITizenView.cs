using Avalonia.Controls.Platform;
using Avalonia.Input;

namespace Avalonia.Tizen;

internal interface ITizenView
{
    Size ClientSize { get; }
    double Scaling { get; }
    IInputRoot InputRoot { get; set; }
    INativeControlHostImpl NativeControlHost { get; }
}
