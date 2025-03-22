using System;
using System.Collections.Generic;
using System.Text;
using Avalonia;
using Avalonia.Input;
using Avalonia.Media;

namespace ControlCatalog.Pages.Gamepad.Game
{
    public class TestSystem : IRenderSystem, IExecutionSystem, IGamepadSystem
    {
        private Vector _position;
        private Vector _velocity;
        private Pen _pen = new Pen(Brushes.White);

        public void GamepadInput(GamepadUpdateArgs args)
        {
            _velocity = args.State.LeftAnalogStick * 1000;
        }

        public void OnAdd(IEntity composite)
        {
            
        }

        public void OnRemove(IEntity composite)
        {
            
        }

        public void Render(DrawingContext context)
        {
            context.PushTransform(Matrix.CreateScale(new(1, -1)));
            context.DrawEllipse(Brushes.Black, _pen, (Point)_position, 50, 50);
        }

        public void Tick(TimeSpan elapsed)
        {
            _position += _velocity * elapsed.TotalSeconds;
        }
    }
}
