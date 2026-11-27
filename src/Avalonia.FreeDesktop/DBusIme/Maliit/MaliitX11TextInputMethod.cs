using System;
using System.Threading.Tasks;
using Avalonia.Input.Raw;
using Avalonia.Input.TextInput;

namespace Avalonia.FreeDesktop.DBusIme.Maliit;

internal sealed class MaliitInputMethodFactory : MaliitInputMethodManager, IX11InputMethodFactory
{
    internal MaliitInputMethodFactory(Func<IMaliitSession>? createSession = null) : base(createSession) { }

    public (ITextInputMethodImpl method, IX11InputMethodControl control) CreateClient(IntPtr xid)
    {
        var method = new MaliitX11TextInputMethod(this, xid);
        return (method, method);
    }
}

internal sealed class MaliitX11TextInputMethod : MaliitTextInputMethod,
    IX11InputMethodControl, IX11InputMethodWithInputPane, IX11SynchronousInputMethodControl
{
    internal MaliitX11TextInputMethod(MaliitInputMethodFactory factory, IntPtr windowId)
        : base(factory, unchecked((ulong)windowId.ToInt64())) { }

    public event Action<X11InputMethodForwardedKey>? ForwardKey;
    public ValueTask<bool> HandleEventAsync(RawKeyEventArgs args, int keyVal, int keyCode) => new(false);
    public void UpdateWindowInfo(PixelPoint position, double scaling) => base.UpdateWindowInfo(position, scaling);

    protected override bool TryForwardKey(InputKey input)
    {
        if (!MaliitKeyMapper.TryMap(input, out var key))
            return false;
        ForwardKey?.Invoke(new X11InputMethodForwardedKey
        {
            KeyVal = key.KeySym, Modifiers = key.Modifiers, Type = key.Type,
            Text = key.Text, WithText = key.WithText
        });
        return true;
    }
}
