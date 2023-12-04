using Avalonia.Input;

namespace Avalonia.Tizen;

internal class NuiKeyboardNavigationHandler : IKeyboardNavigationHandler
{
    private IInputRoot? _owner;
    public void SetOwner(IInputRoot owner)
    {
        if (_owner != null)
        {
            throw new InvalidOperationException("AccessKeyHandler owner has already been set.");
        }

        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _owner.AddHandler(InputElement.KeyDownEvent, OnKeyDown);
    }

    public void Move(IInputElement element, NavigationDirection direction, KeyModifiers keyModifiers = KeyModifiers.None)
    {
        throw new NotImplementedException();
    }
    
    void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Tab)
        {
            var current = FocusManager.GetFocusManager(e.Source as IInputElement)?.GetFocusedElement();
            var direction = (e.KeyModifiers & KeyModifiers.Shift) == 0 ?
                NavigationDirection.Next : NavigationDirection.Previous;
            Move(current, direction, e.KeyModifiers);
            e.Handled = true;
        }
    }
}
