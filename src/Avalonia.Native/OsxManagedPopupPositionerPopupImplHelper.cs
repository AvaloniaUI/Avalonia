using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Platform;

namespace Avalonia.Native
{
    class OsxManagedPopupPositionerPopupImplHelper : ManagedPopupPositionerPopupImplHelper
    {
        public OsxManagedPopupPositionerPopupImplHelper(IWindowBaseImpl parent, MoveResizeDelegate moveResize) : base(parent, moveResize)
        {

        }
        public override Point TranslatePoint(Point pt) => pt;

        public override Size TranslateSize(Size size) => size;
    }
}
