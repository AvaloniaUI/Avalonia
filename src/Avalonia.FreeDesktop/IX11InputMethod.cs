using System;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Input.TextInput;

namespace Avalonia.FreeDesktop
{
    public interface IX11InputMethodFactory
    {
        (ITextInputMethodImpl method, IX11InputMethodControl control) CreateClient(IntPtr xid);
    }

    public struct X11InputMethodForwardedKey
    {
        public int KeyVal { get; set; }
        public KeyModifiers Modifiers { get; set; }
        public RawKeyEventType Type { get; set; }
    }
    
    public interface IX11InputMethodControl : IDisposable
    {
        void SetWindowActive(bool active);
        bool IsEnabled { get; }
        ValueTask<bool> HandleEventAsync(RawKeyEventArgs args, int keyVal, int keyCode);
        event Action<string> Commit;
        event Action<X11InputMethodForwardedKey> ForwardKey;
        
        void UpdateWindowInfo(PixelPoint position, double scaling);
    }
}
