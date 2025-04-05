using System;
using System.Collections.Generic;
using System.Text;
using Avalonia;
using Avalonia.Media;

namespace ControlCatalog.Pages.Gamepad.Game
{
    public class TestPlayerEntity : GameObjectBase
    {
        private Pen _pen = new Pen(Brushes.White);

        public override void OnCreate()
        {
            GameWorld.ActiveWorld.GamepadUpdate += ActiveWorld_GamepadUpdate;

            base.OnCreate();
        }

        private void ActiveWorld_GamepadUpdate(Avalonia.Input.GamepadUpdateArgs obj)
        {
            Velocity = obj.State.LeftAnalogStick * 400;
        }

        public override void OnRender(DrawingContext context)
        {
            var state = context.PushTransform(Matrix.CreateScale(new(1, -1)));
            context.DrawEllipse(Brushes.Black, _pen, (Point)Position, 16, 16);
            state.Dispose();
            var text = new FormattedText($"Depth: {CurrentNode?.Depth ?? -1}", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, 16.0d, Brushes.White);
            context.DrawText(text, new Point(32, 32));

            base.OnRender(context);
        }
    }
}
