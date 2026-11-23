using System;
using Avalonia.Controls;
using Avalonia.FreeDesktop.DBusIme.Maliit;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Platform;
using Avalonia.Wayland.Screens;

namespace Avalonia.Wayland;

partial class WindowImpl
{
    private void UpdateMaliitGeometry()
    {
        if (_maliitTextInputMethod is not { } method)
            return;

        // Only a fullscreen surface covering the origin output has a known global position.
        PixelPoint? position = null;
        if (_windowState == WindowState.FullScreen && CurrentOutputIds.Count == 1 &&
            AvaloniaLocator.Current.GetService<IScreenImpl>() is SnapshotScreensImpl screens &&
            screens.Lookup(CurrentOutputIds[0]) is { LogicalPosition: { X: 0, Y: 0 } } output &&
            Math.Abs(ClientSize.Width * DesktopScaling - output.LogicalSize.Width) < 1 &&
            Math.Abs(ClientSize.Height * DesktopScaling - output.LogicalSize.Height) < 1)
            position = default(PixelPoint);

        method.UpdateClientSize(ClientSize);
        method.UpdateWindowInfo(position, RenderScaling);
    }

    public override void Hide()
    {
        _maliitTextInputMethod?.SetWindowActive(false);
        base.Hide();
    }

    public override void Dispose()
    {
        _maliitTextInputMethod?.Dispose();
        base.Dispose();
    }

    private sealed class MaliitTextInputMethodImpl : MaliitTextInputMethod
    {
        private readonly WindowImpl _owner;

        internal MaliitTextInputMethodImpl(WindowImpl owner, MaliitInputMethodManager manager)
            : base(manager, 0, hasGlobalCoordinates: false)
        {
            _owner = owner;
            Commit += text =>
            {
                if (_owner.InputRoot is { } root && _owner.IsEnabled)
                    _owner.Input?.Invoke(new RawTextInputEventArgs(_owner.Keyboard, 0, root, text));
            };
        }

        protected override bool TryForwardKey(InputKey input)
        {
            if (!MaliitKeyMapper.TryMap(input, out var key))
                return false;
            if (_owner.InputRoot is not { } root || !_owner.IsEnabled)
                return true;

            var args = new RawKeyEventArgs(_owner.Keyboard, 0, root, key.Type,
                XkbKeyTransform.KeyFromKeysym((uint)key.KeySym), (RawInputModifiers)key.Modifiers,
                PhysicalKey.None, key.Text) { KeyText = key.Text };
            // Apply synchronously before Maliit can query selection again.
            _owner.Input?.Invoke(args);
            if (!args.Handled && key.Type == RawKeyEventType.KeyDown && key.WithText)
                _owner.Input?.Invoke(new RawTextInputEventArgs(_owner.Keyboard, 0, root, key.Text));
            return true;
        }
    }
}
