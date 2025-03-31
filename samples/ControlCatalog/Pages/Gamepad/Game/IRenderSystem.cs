using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Media;

namespace ControlCatalog.Pages.Gamepad.Game
{
    public interface IRenderSystem
    {
        public void Render(DrawingContext context);
    }
}
