using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Tizen.NUI;
using Window = Tizen.NUI.Window;
using Key = Tizen.NUI.Key;
using Avalonia.Tizen.Platform.Input;

namespace Avalonia.Tizen;

public class NuiTizenApplication<TApp> : NUIApplication
    where TApp : Application, new()
{
    private NuiAvaloniaView _view;
    private NuiTizenApplication<TApp>.SingleViewLifetime _lifetime;

    class SingleViewLifetime : ISingleViewApplicationLifetime
    {
        public NuiAvaloniaView View;

        public Control MainView
        {
            get => View.Content;
            set => View.Content = value;
        }
    }

    protected virtual AppBuilder CustomizeAppBuilder(AppBuilder builder) => builder;

    protected override void OnCreate()
    {
        base.OnCreate();
#pragma warning disable CS8601 // Possible null reference assignment.
        TizenThreadingInterface.MainloopContext = SynchronizationContext.Current;
#pragma warning restore CS8601 // Possible null reference assignment.

        _lifetime = new SingleViewLifetime();
        _lifetime.View = _view = new NuiAvaloniaView();

        _view.HeightResizePolicy = ResizePolicyType.FillToParent;
        _view.WidthResizePolicy = ResizePolicyType.FillToParent;
        Window.Instance.GetDefaultLayer().Add(_view);
        Window.Instance.RenderingBehavior = RenderingBehaviorType.Continuously;

        Window.Instance.KeyEvent += WindowKeyEvent;

        var builder = AppBuilder.Configure<TApp>().UseTizen();
        CustomizeAppBuilder(builder);

        builder.AfterSetup(_ =>
        {
            _view.Initialise();
        });

        builder.SetupWithLifetime(_lifetime);
    }

    private void WindowKeyEvent(object? sender, Window.KeyEventArgs e)
    {
        if (Enum.TryParse<global::Tizen.Uix.InputMethod.KeyCode>(e.Key.KeyPressedName, true, out var keyCode) ||
            Enum.TryParse($"Keypad{e.Key.KeyPressedName}", false, out keyCode))
        {
            var mapped = TizenKeyboardDevice.ConvertKey(keyCode);
            if (mapped == Input.Key.None)
                return;

            var type = GetKeyEventType(e);
            var modifiers = GetModifierKey(e);

            _view?.TopLevelImpl?.Input?.Invoke(
                new RawKeyEventArgs(
                    TizenKeyboardDevice.Instance!,
                    e.Key.Time,
                    _view.InputRoot,
                    type,
                    mapped,
                    modifiers));
        }
    }

    private RawKeyEventType GetKeyEventType(Window.KeyEventArgs ev)
    {
        switch (ev.Key.State)
        {
            case Key.StateType.Down:
                return RawKeyEventType.KeyDown;
            case Key.StateType.Up:
                return RawKeyEventType.KeyUp;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private RawInputModifiers GetModifierKey(Window.KeyEventArgs ev)
    {
        var modifiers = RawInputModifiers.None;

        if (ev.Key.IsShiftModifier())
            modifiers |= RawInputModifiers.Shift;

        if (ev.Key.IsAltModifier())
            modifiers |= RawInputModifiers.Alt;

        if (ev.Key.IsCtrlModifier())
            modifiers |= RawInputModifiers.Control;

        return modifiers;
    }
}
