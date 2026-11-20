using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;

namespace Sandbox
{
    public partial class MainWindow : Window
    {
        private bool _ctrlDown;
        private KeyModifiers? _lastMoveModifiers;

        public MainWindow()
        {
            InitializeComponent();

            ShowWindowButton.Click += (_, _) => ShowSecondWindow();
            ClearButton.Click += (_, _) => Log.Text = "";

            // Handled events are logged too, so that controls under the pointer don't hide them.
            AddHandler(PointerEnteredEvent, (_, e) => Write("PointerEntered", e), RoutingStrategies.Direct, true);
            AddHandler(PointerMovedEvent, OnPointerMoved, RoutingStrategies.Tunnel, true);
            AddHandler(PointerPressedEvent, (_, e) => Write("PointerPressed", e), RoutingStrategies.Tunnel, true);
            TrackCtrlKey(this);
            PointerExited += (_, _) => _lastMoveModifiers = null;

            UpdateCtrlState();
        }

        // Moves are only logged when their modifiers differ from the previous move's.
        private void OnPointerMoved(object? sender, PointerEventArgs e)
        {
            if (e.KeyModifiers == _lastMoveModifiers)
                return;

            _lastMoveModifiers = e.KeyModifiers;
            Write("PointerMoved", e);
        }

        // Follows Ctrl from key events in the window that has keyboard focus, as a reference for
        // what the pointer events should report.
        private void TrackCtrlKey(Window window)
        {
            window.AddHandler(KeyDownEvent, (_, e) => OnCtrlKey(e, true), RoutingStrategies.Tunnel, true);
            window.AddHandler(KeyUpEvent, (_, e) => OnCtrlKey(e, false), RoutingStrategies.Tunnel, true);
        }

        private void OnCtrlKey(KeyEventArgs e, bool down)
        {
            if (e.Key is not (Key.LeftCtrl or Key.RightCtrl) || down == _ctrlDown)
                return;

            _ctrlDown = down;
            UpdateCtrlState();
            Append($"{(down ? "KeyDown" : "KeyUp"),-15} {e.Key}");
        }

        private void UpdateCtrlState() => CtrlState.Text = $"Ctrl held (from key events): {(_ctrlDown ? "yes" : "no")}";

        private void Write(string name, PointerEventArgs e)
        {
            var p = e.GetPosition(this);
            var warning = _ctrlDown && (e.KeyModifiers & KeyModifiers.Control) == 0 ? "   <-- Ctrl is held" : "";
            Append($"{name,-15} ({p.X:0},{p.Y:0}) KeyModifiers={e.KeyModifiers}{warning}");
        }

        private void Append(string line)
        {
            line = $"{DateTime.Now:HH:mm:ss.fff} {line}";
            Console.WriteLine(line);
            Log.Text += line + Environment.NewLine;
            Log.CaretIndex = Log.Text.Length;
        }

        private void ShowSecondWindow()
        {
            var window = new Window
            {
                Title = "Second window",
                Width = 220,
                Height = 120,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
            };

            var close = new Button
            {
                Content = "Close",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            close.Click += (_, _) =>
            {
                Append($"{"Close clicked",-15} Ctrl held={(_ctrlDown ? "yes" : "no")}");
                window.Close();
            };

            window.Content = close;
            TrackCtrlKey(window);
            window.Show(this);
        }
    }
}
