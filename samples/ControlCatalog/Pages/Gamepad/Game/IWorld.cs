using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Input;
using Avalonia.Media;

namespace ControlCatalog.Pages.Gamepad.Game
{
    public interface IWorld
    {
        void Add(GameObjectBase entity);
        void Destroy(GameObjectBase entity);
        void DispatchTick(TimeSpan elapsed);
        void DispatchRender(DrawingContext context);
        void DispatchGamepadInput(GamepadUpdateArgs args);
    }
}
