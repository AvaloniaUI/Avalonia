using System;
namespace Avalonia.Windowing.Bindings
{
    /// <summary>
    /// An interface used to abstract between Window and GlWindow.
    /// </summary>
    public interface IWindowWrapper : IDisposable
    {
        IntPtr Id { get; }
        void SetTitle(string title);
        void SetSize(double width, double height);
        (double, double) GetSize();
        (double, double) GetPosition();
        void Show();
    }
}
