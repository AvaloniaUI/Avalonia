using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Perspex.Platform;

namespace Perspex.TinyWM
{
    class PopupImpl : TopLevelImpl, IPopupImpl, IHaveScreenPosition
    {
        public void SetPosition(Point p)
        {
            X = (int) p.X;
            Y = (int) p.Y;
            WindowManager.Scene.RenderRequestedBy(this);
        }

        public override Rect Bounds => new Rect(new Point(X, Y), ClientSize);

        public void Show()
        {
            WindowManager.Scene.AddPopup(this);
        }

        public void Hide()
        {
            WindowManager.Scene.RemovePopup(this);
        }

        public int X { get; set; }
        public int Y { get; set; }
        public override Size ClientSize { get; set; }
    }
}
