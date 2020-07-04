using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Platform;

namespace Avalonia.Native
{
    class OsxManagedPopupPositionerPopupImplHelper : ManagedPopupPositionerPopupImplHelper
    {
        public OsxManagedPopupPositionerPopupImplHelper(IWindowBaseImpl parent, MoveResizeDelegate moveResize) : base(parent, moveResize)
        {

        }

        public override double Scaling => 1;
    }
}
