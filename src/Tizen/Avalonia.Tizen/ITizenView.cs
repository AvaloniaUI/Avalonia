using Avalonia.Input;

namespace Avalonia.Tizen;

public interface ITizenView
{
    Size ClientSize { get; }
    double Scaling { get; }
    IInputRoot InputRoot { get; set; }
}
