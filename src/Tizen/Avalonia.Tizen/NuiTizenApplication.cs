using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Tizen.NUI;
using Window = Tizen.NUI.Window;
using Key = Tizen.NUI.Key;
using Avalonia.Tizen.Platform.Input;
using static System.Net.Mime.MediaTypeNames;
using Avalonia.Logging;

namespace Avalonia.Tizen;

public class NuiTizenApplication<TApp> : NUIApplication
    where TApp : Application, new()
{
    private const string AppLog = "TIZENAPP";

    private SingleViewLifetime? _lifetime;

    class SingleViewLifetime : ISingleViewApplicationLifetime
    {
        public NuiAvaloniaView View { get; }

        public SingleViewLifetime(NuiAvaloniaView view)
        {
            View = view;
        }

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

        _lifetime = new SingleViewLifetime(new NuiAvaloniaView());

        _lifetime.View.HeightResizePolicy = ResizePolicyType.FillToParent;
        _lifetime.View.WidthResizePolicy = ResizePolicyType.FillToParent;
        Window.Instance.GetDefaultLayer().Add(_lifetime.View);
        Window.Instance.RenderingBehavior = RenderingBehaviorType.Continuously;

        Window.Instance.KeyEvent += WindowKeyEvent;

        var builder = AppBuilder.Configure<TApp>().UseTizen();
        CustomizeAppBuilder(builder);

        builder.AfterSetup(_ => _lifetime.View.Initialise());

        builder.SetupWithLifetime(_lifetime);
    }

    private void WindowKeyEvent(object? sender, Window.KeyEventArgs e)
    {
        if ((string.IsNullOrEmpty(e.Key.KeyString) &&
            Enum.TryParse<global::Tizen.Uix.InputMethod.KeyCode>(
                e.Key.KeyPressedName,
                true,
                out var keyCode)) ||
            (e.Key.IsCtrlModifier() &&
            Enum.TryParse(
                $"Keypad{e.Key.KeyPressedName}",
                true,
                out keyCode)))
        {
            var mapped = TizenKeyboardDevice.ConvertKey(keyCode);
            if (mapped == Input.Key.None)
                return;

            var type = GetKeyEventType(e);
            var modifiers = GetModifierKey(e);

            _lifetime?.View?.TopLevelImpl?.Input?.Invoke(
                new RawKeyEventArgs(
                    TizenKeyboardDevice.Instance!,
                    e.Key.Time,
                    _lifetime.View.InputRoot,
                    type,
                    mapped,
                    modifiers));
        }
        else if (e.Key.State == Key.StateType.Up)
        {
            Logger.TryGet(LogEventLevel.Debug, AppLog)?.Log(null, "Triggering text input {text}", e.Key.KeyString);
            _lifetime?.View?.TopLevelImpl?.Input?.Invoke(
                new RawTextInputEventArgs(
                    TizenKeyboardDevice.Instance!,
                    e.Key.Time,
                    _lifetime.View.InputRoot,
                    e.Key.KeyString));
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
