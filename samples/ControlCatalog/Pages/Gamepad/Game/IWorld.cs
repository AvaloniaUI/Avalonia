using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Input;
using Avalonia.Media;

namespace ControlCatalog.Pages.Gamepad.Game
{
    public interface IWorld
    {
        void OnAdd(IEntity entity);
        void OnRemove(IEntity entity);
        void DispatchTick(TimeSpan elapsed);
        void DispatchRender(DrawingContext context);
        void DispatchGamepadInput(GamepadUpdateArgs args);
        void RegisterSystem(IExecutionSystem system);
        void DeregisterSystem(IExecutionSystem system);
    }
}
