using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Remote.Protocol.Input;
using Avalonia.Threading;
using Key = Avalonia.Input.Key;
using PhysicalKey = Avalonia.Input.PhysicalKey;

namespace Avalonia.Android.Previewer
{
    internal class PreviewPresentation : Presentation
    {
        private readonly Context? _outerContext;
        private readonly int _port;
        private readonly Assembly? _assembly;
        private PreviewerConnection? _connection;
        private Preview? _preview;
        private PreviewImageReader? _reader;
        private AvaloniaView? _view;
        private float _renderScaling = 1;

        internal MouseDevice? TouchDevice => _view?.TopLevelImpl.PointerHelper.MouseDevice;
        internal static KeyboardDevice? KeyboardDevice => AvaloniaLocator.Current.GetService<IKeyboardDevice>() as KeyboardDevice;

        public AvaloniaView? View { get => _view; set => _view = value; }

        public float RenderScaling
        {
            get => _renderScaling;
            internal set
            {
                _renderScaling = value;
                if (PreviewDisplay.Instance?.Surface is { } surface)
                {
                    surface.Scaling = _renderScaling;
                }

                _preview?.Invalidate();
            }
        }

        public PreviewPresentation(Context? outerContext, Display? display, int port, Assembly? assembly) : base(outerContext, display)
        {
            _outerContext = outerContext;
            _port = port;
            _assembly = assembly;
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
            Justification = "<Pending>")]
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            var display = PreviewDisplay.Instance!;

            View = new AvaloniaView(Context);
            _connection = new PreviewerConnection(this);
            _preview = new Preview(this, View.TopLevel, _assembly);
            _reader = new PreviewImageReader(display.Surface!, _connection);
            _connection.Listen(_port);
            SetContentView(View);
        }

        public async Task UpdateXaml(string xaml)
        {
            if (_preview != null)
                await _preview.UpdateXamlAsync(xaml);
        }

        public void SendInput(InputEventMessageBase input)
        {
            switch (input)
            {
                case PointerMovedEventMessage pointer:
                    Dispatcher.UIThread.Post(() =>
                    {
                        View?.TopLevelImpl?.Input?.Invoke(new RawPointerEventArgs(
                            TouchDevice!,
                            0,
                            View?.TopLevelImpl?.InputRoot!,
                            RawPointerEventType.Move,
                            new Point(pointer.X, pointer.Y),
                            GetAvaloniaRawInputModifiers(pointer.Modifiers)));
                    }, DispatcherPriority.Input);
                    break;

                case PointerPressedEventMessage pressed:
                    Dispatcher.UIThread.Post(() =>
                    {
                        View?.TopLevelImpl?.Input?.Invoke(new RawPointerEventArgs(
                            TouchDevice!,
                            0,
                            View?.TopLevelImpl.InputRoot!,
                            GetAvaloniaEventType(pressed.Button, true),
                            new Point(pressed.X, pressed.Y),
                            GetAvaloniaRawInputModifiers(pressed.Modifiers)));
                    }, DispatcherPriority.Input);
                    break;

                case PointerReleasedEventMessage released:
                    Dispatcher.UIThread.Post(() =>
                    {
                        View?.TopLevelImpl?.Input?.Invoke(new RawPointerEventArgs(
                            TouchDevice!,
                            0,
                            View?.TopLevelImpl.InputRoot!,
                            GetAvaloniaEventType(released.Button, false),
                            new Point(released.X, released.Y),
                            GetAvaloniaRawInputModifiers(released.Modifiers)));
                    }, DispatcherPriority.Input);
                    break;

                case ScrollEventMessage scroll:
                    Dispatcher.UIThread.Post(() =>
                    {
                        View?.TopLevelImpl?.Input?.Invoke(new RawMouseWheelEventArgs(
                            TouchDevice!,
                            0,
                            View?.TopLevelImpl.InputRoot!,
                            new Point(scroll.X, scroll.Y),
                            new Vector(scroll.DeltaX, scroll.DeltaY),
                            GetAvaloniaRawInputModifiers(scroll.Modifiers)));
                    }, DispatcherPriority.Input);
                    break;

                case KeyEventMessage key:
                    Dispatcher.UIThread.Post(() =>
                    {
                        Dispatcher.UIThread.RunJobs(DispatcherPriority.Input + 1);

                        View?.TopLevelImpl?.Input?.Invoke(new RawKeyEventArgs(
                            KeyboardDevice!,
                            0,
                            View?.TopLevelImpl.InputRoot!,
                            key.IsDown ? RawKeyEventType.KeyDown : RawKeyEventType.KeyUp,
                            (Key)key.Key,
                            GetAvaloniaRawInputModifiers(key.Modifiers),
                            (PhysicalKey)key.PhysicalKey,
                            key.KeySymbol));
                    }, DispatcherPriority.Input);
                    break;

                case TextInputEventMessage text:
                    Dispatcher.UIThread.Post(() =>
                    {
                        Dispatcher.UIThread.RunJobs(DispatcherPriority.Input + 1);

                        View?.TopLevelImpl?.Input?.Invoke(new RawTextInputEventArgs(
                            KeyboardDevice!,
                            0,
                            View?.TopLevelImpl.InputRoot!,
                            text.Text));
                    }, DispatcherPriority.Input);
                    break;
            }
        }

        private static RawPointerEventType GetAvaloniaEventType(Remote.Protocol.Input.MouseButton button, bool pressed)
        {
            switch (button)
            {
                case Remote.Protocol.Input.MouseButton.Left:
                    return pressed ? RawPointerEventType.LeftButtonDown : RawPointerEventType.LeftButtonUp;

                case Remote.Protocol.Input.MouseButton.Middle:
                    return pressed ? RawPointerEventType.MiddleButtonDown : RawPointerEventType.MiddleButtonUp;

                case Remote.Protocol.Input.MouseButton.Right:
                    return pressed ? RawPointerEventType.RightButtonDown : RawPointerEventType.RightButtonUp;

                default:
                    return RawPointerEventType.Move;
            }
        }

        private static RawInputModifiers GetAvaloniaRawInputModifiers(InputModifiers[]? modifiers)
        {
            var result = RawInputModifiers.None;

            if (modifiers == null)
            {
                return result;
            }

            foreach (var modifier in modifiers)
            {
                switch (modifier)
                {
                    case InputModifiers.Control:
                        result |= RawInputModifiers.Control;
                        break;

                    case InputModifiers.Alt:
                        result |= RawInputModifiers.Alt;
                        break;

                    case InputModifiers.Shift:
                        result |= RawInputModifiers.Shift;
                        break;

                    case InputModifiers.Windows:
                        result |= RawInputModifiers.Meta;
                        break;

                    case InputModifiers.LeftMouseButton:
                        result |= RawInputModifiers.LeftMouseButton;
                        break;

                    case InputModifiers.MiddleMouseButton:
                        result |= RawInputModifiers.MiddleMouseButton;
                        break;

                    case InputModifiers.RightMouseButton:
                        result |= RawInputModifiers.RightMouseButton;
                        break;
                }
            }

            return result;
        }
    }
}
