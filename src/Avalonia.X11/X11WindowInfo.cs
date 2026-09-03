namespace Avalonia.X11;

internal readonly struct X11WindowInfo(X11EventDispatcher.EventHandler eventHandler, X11Window? window)
{
    public X11EventDispatcher.EventHandler EventHandler { get; } = eventHandler;
    public X11Window? Window { get; } = window;
}
