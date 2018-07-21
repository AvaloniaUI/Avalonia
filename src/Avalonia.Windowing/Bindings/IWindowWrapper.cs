using System;
namespace Avalonia.Windowing.Bindings
{
    /// <summary>
    /// An interface used to abstract between Window and GlWindow.
    /// </summary>
    public interface IWindowWrapper : IDisposable
    {
        void SetTitle(string title);
    }
}
