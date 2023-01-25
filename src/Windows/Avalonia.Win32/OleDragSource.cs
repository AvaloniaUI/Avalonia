using System.Linq;

using Avalonia.MicroCom;
using Avalonia.Win32.Interop;
using Avalonia.Win32.Win32Com;

namespace Avalonia.Win32
{
    internal class OleDragSource : CallbackBase, IDropSource
    {
        private const int DRAGDROP_S_USEDEFAULTCURSORS = 0x00040102;
        private const int DRAGDROP_S_DROP = 0x00040100;
        private const int DRAGDROP_S_CANCEL = 0x00040101;

        private static readonly int[] MOUSE_BUTTONS = new int[] {
            (int)UnmanagedMethods.ModifierKeys.MK_LBUTTON,
            (int)UnmanagedMethods.ModifierKeys.MK_MBUTTON,
            (int)UnmanagedMethods.ModifierKeys.MK_RBUTTON
        };

        public int QueryContinueDrag(int fEscapePressed, int grfKeyState)
        {
            if (fEscapePressed != 0)
                return DRAGDROP_S_CANCEL;

            int pressedMouseButtons = MOUSE_BUTTONS.Where(mb => (grfKeyState & mb) == mb).Count();

            if (pressedMouseButtons >= 2)
                return DRAGDROP_S_CANCEL;
            if (pressedMouseButtons == 0)
                return DRAGDROP_S_DROP;

            return unchecked((int)UnmanagedMethods.HRESULT.S_OK);
        }

        public int GiveFeedback(DropEffect dwEffect)
        {
            return DRAGDROP_S_USEDEFAULTCURSORS;
        }
    }
}
