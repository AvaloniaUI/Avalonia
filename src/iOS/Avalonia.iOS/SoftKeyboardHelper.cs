using Avalonia.Controls;
using Avalonia.Input;

namespace Avalonia.iOS
{
    public class SoftKeyboardHelper
    {
        private AvaloniaView _oldView;
        
        public void UpdateKeyboard(IInputElement focusedElement)
        {
            if (_oldView?.IsFirstResponder == true)
                _oldView?.ResignFirstResponder();
            _oldView = null;
            
            //TODO: Raise a routed event to determine if any control wants to become the text input handler 
            if (focusedElement is TextBox)
            {
                var view = ((focusedElement.VisualRoot as TopLevel)?.PlatformImpl as AvaloniaView.TopLevelImpl)?.View;
                view?.BecomeFirstResponder();
            }
        }
    }
}