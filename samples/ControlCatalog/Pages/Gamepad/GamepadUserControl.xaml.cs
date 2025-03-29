using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;

namespace ControlCatalog.Pages
{
    public partial class GamepadUserControl : UserControl
    {
        public static readonly StyledProperty<Vector> LeftStickProperty =
            AvaloniaProperty.Register<GamepadUserControl, Vector>(nameof(LeftStick));
        public Vector LeftStick
        {
            get => this.GetValue(LeftStickProperty);
            set => SetValue(LeftStickProperty, value);
        }
        public static readonly StyledProperty<bool> LeftClickProperty =
            AvaloniaProperty.Register<GamepadUserControl, bool>(nameof(LeftClick));
        public bool LeftClick
        {
            get => this.GetValue(LeftClickProperty);
            set => SetValue(LeftClickProperty, value);
        }
        public static readonly StyledProperty<bool> AProperty =
            AvaloniaProperty.Register<GamepadUserControl, bool>(nameof(A));
        public bool A
        {
            get => this.GetValue(AProperty);
            set => SetValue(AProperty, value);
        }
        public static readonly StyledProperty<bool> BProperty =
            AvaloniaProperty.Register<GamepadUserControl, bool>(nameof(B));
        public bool B
        {
            get => this.GetValue(BProperty);
            set => SetValue(BProperty, value);
        }
        public static readonly StyledProperty<bool> XProperty =
            AvaloniaProperty.Register<GamepadUserControl, bool>(nameof(X));
        public bool X
        {
            get => this.GetValue(XProperty);
            set => SetValue(XProperty, value);
        }
        public static readonly StyledProperty<bool> YProperty =
            AvaloniaProperty.Register<GamepadUserControl, bool>(nameof(Y));
        public bool Y
        {
            get => this.GetValue(YProperty);
            set => SetValue(YProperty, value);
        }
        public static readonly StyledProperty<bool> SelectProperty =
            AvaloniaProperty.Register<GamepadUserControl, bool>(nameof(Select));
        public bool Select
        {
            get => this.GetValue(SelectProperty);
            set => SetValue(SelectProperty, value);
        }
        public static readonly StyledProperty<bool> StartProperty =
            AvaloniaProperty.Register<GamepadUserControl, bool>(nameof(Start));
        public bool Start
        {
            get => this.GetValue(StartProperty);
            set => SetValue(StartProperty, value);
        }
        public static readonly StyledProperty<Vector> RightStickProperty =
            AvaloniaProperty.Register<GamepadUserControl, Vector>(nameof(RightStick));
        public Vector RightStick
        {
            get => this.GetValue(RightStickProperty);
            set => SetValue(RightStickProperty, value);
        }
        public static readonly StyledProperty<bool> RightClickProperty =
            AvaloniaProperty.Register<GamepadUserControl, bool>(nameof(RightClick));
        public bool RightClick
        {
            get => this.GetValue(RightClickProperty);
            set => SetValue(RightClickProperty, value);
        }
        public static readonly StyledProperty<Vector> LeftTriggerProperty =
            AvaloniaProperty.Register<GamepadUserControl, Vector>(nameof(LeftTrigger));
        public Vector LeftTrigger
        {
            get => this.GetValue(LeftTriggerProperty);
            set => SetValue(LeftTriggerProperty, value);
        }
        public static readonly StyledProperty<bool> LeftBufferProperty =
            AvaloniaProperty.Register<GamepadUserControl, bool>(nameof(LeftBuffer));
        public bool LeftBuffer
        {
            get => this.GetValue(LeftBufferProperty);
            set => SetValue(LeftBufferProperty, value);
        }
        public static readonly StyledProperty<Vector> RightTriggerProperty =
            AvaloniaProperty.Register<GamepadUserControl, Vector>(nameof(RightTrigger));
        public Vector RightTrigger
        {
            get => this.GetValue(RightTriggerProperty);
            set => SetValue(RightTriggerProperty, value);
        }
        public static readonly StyledProperty<bool> RightBufferProperty =
            AvaloniaProperty.Register<GamepadUserControl, bool>(nameof(RightBuffer));
        public bool RightBuffer
        {
            get => this.GetValue(RightBufferProperty);
            set => SetValue(RightBufferProperty, value);
        }

        private DirectionalPadControl DPadControl { get; set; }
        private TextBlock DisconnectedTextBlock { get; set; }

        public GamepadUserControl()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            DPadControl = this.Find<DirectionalPadControl>(nameof(DPadControl))!;
            DisconnectedTextBlock = this.Find<TextBlock>(nameof(DisconnectedTextBlock))!;
        }

        public void ReceiveUpdate(GamepadUpdateArgs args)
        {
            var state = args.State;
            LeftStick = state.LeftAnalogStick;
            RightStick = state.RightAnalogStick;
            A = state.GetButtonState(GamepadButton.Button0).Pressed;
            B = state.GetButtonState(GamepadButton.Button1).Pressed;
            X = state.GetButtonState(GamepadButton.Button2).Pressed;
            Y = state.GetButtonState(GamepadButton.Button3).Pressed;
            LeftBuffer = state.GetButtonState(GamepadButton.Button4).Pressed;
            RightBuffer = state.GetButtonState(GamepadButton.Button5).Pressed;
            LeftTrigger = new Vector(state.GetButtonState(GamepadButton.Button6).Value, 0);
            RightTrigger = new Vector(state.GetButtonState(GamepadButton.Button7).Value, 0);
            Select = state.GetButtonState(GamepadButton.Button8).Pressed;
            Start = state.GetButtonState(GamepadButton.Button9).Pressed;
            LeftClick = state.GetButtonState(GamepadButton.Button10).Pressed;
            RightClick = state.GetButtonState(GamepadButton.Button11).Pressed;
            DPadControl.ReceiveUpdate(args);

            if (args.Connected)
            {
                DisconnectedTextBlock.Text = $"[{args.Device}]: [{args.HumanName}]";
            }
            else
            {
                DisconnectedTextBlock.Text = $"[{args.Device}]: [{args.HumanName}] DEVICE LOST!";
            }
        }
    }

    public partial class TwoAxisDrawer : Control
    {
        public static readonly StyledProperty<Vector> ValueProperty = AvaloniaProperty.Register<TwoAxisDrawer, Vector>(nameof(Value));
        public static readonly StyledProperty<IPen> DrawingPenProperty = AvaloniaProperty.Register<TwoAxisDrawer, IPen>(nameof(DrawingPen), new Pen(Brushes.White, thickness: 2.0d, lineCap: PenLineCap.Round));

        static TwoAxisDrawer()
        {
            AffectsRender<TwoAxisDrawer>(ValueProperty);
            AffectsRender<TwoAxisDrawer>(DrawingPenProperty);
        }

        public TwoAxisDrawer()
        {

        }
        public Vector Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
        public IPen DrawingPen { get => GetValue(DrawingPenProperty); set => SetValue(DrawingPenProperty, value); }

        protected override Size MeasureOverride(Size availableSize)
        {
            var min = Math.Min(availableSize.Width, availableSize.Height);
            min = Math.Min(min, ((Visual)Parent!).Bounds.Height);
            return new Size(min, min);
        }

        public override void Render(DrawingContext context)
        {
            Point center = new Point(this.Bounds.Width / 2d, this.Bounds.Height / 2d);

            // because the coordinate space is different
            Vector invec = Value.WithY(Value.Y * -1);
            var mag = invec.Length;
            if (mag > 1)
                mag = 1;
            invec = invec.Normalize() * mag;

            Vector vector = new Vector(center.X * invec.X, center.Y * invec.Y) * 0.9;

            var arrowDestination = center + vector;
            var pen = DrawingPen;

            context.DrawLine(pen, center, arrowDestination);

            if (mag > 0)
            {
                var directionRads = Math.Atan2(vector.Y, vector.X);
                var amount = vector.Length * 0.3;

                var arrowDirection = directionRads + Math.PI * 1.2;

                context.DrawLine(pen, new Point(arrowDestination.X + Math.Cos(arrowDirection) * amount, arrowDestination.Y + Math.Sin(arrowDirection) * amount), arrowDestination);

                arrowDirection = directionRads - Math.PI * 1.2;

                context.DrawLine(pen, new Point(arrowDestination.X + Math.Cos(arrowDirection) * amount, arrowDestination.Y + Math.Sin(arrowDirection) * amount), arrowDestination);
            }
        }
    }
}
