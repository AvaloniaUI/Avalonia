using System;
using System.Collections.Generic;
using System.Text;

namespace ControlCatalog.Pages.Gamepad.Game
{
    public interface IExecutionSystem
    {
        void Tick(TimeSpan elapsed);
        void OnAdd(IEntity composite);
        void OnRemove(IEntity composite);
    }
}
