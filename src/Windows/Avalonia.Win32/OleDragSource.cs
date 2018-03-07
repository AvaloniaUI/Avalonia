using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Win32.Interop;

namespace Avalonia.Win32
{
    class OleDragSource : IDropSource
    {
        private const int DRAGDROP_S_USEDEFAULTCURSORS = 0x00040102;
        private const int DRAGDROP_S_DROP = 0x00040100;
        private const int DRAGDROP_S_CANCEL = 0x00040101;

        private const int KEYSTATE_LEFTMB = 1;
        private const int KEYSTATE_MIDDLEMB = 16;
        private const int KEYSTATE_RIGHTMB = 2;
        private static readonly int[] MOUSE_BUTTONS = new int[] { KEYSTATE_LEFTMB, KEYSTATE_MIDDLEMB, KEYSTATE_RIGHTMB };

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

        public int GiveFeedback(int dwEffect)
        {
            if (dwEffect != 0)
                return DRAGDROP_S_USEDEFAULTCURSORS;
            return unchecked((int)UnmanagedMethods.HRESULT.S_OK);
        }
    }
}
