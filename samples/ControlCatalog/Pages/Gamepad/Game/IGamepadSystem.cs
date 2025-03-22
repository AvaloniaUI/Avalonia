using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Input;

namespace ControlCatalog.Pages.Gamepad.Game
{
    public interface IGamepadSystem
    {
        void GamepadInput(GamepadUpdateArgs args);
    }
}
