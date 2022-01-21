using System;
using static Avalonia.Win32.Interop.UnmanagedMethods;

namespace Avalonia.Win32.Input
{
    internal struct Imm32CaretManager
    {
        private bool _isCaretCreated;

        public void TryCreate(int _langId, IntPtr hwnd)
        {
            if (!_isCaretCreated)
            {
                if (_langId == LANG_ZH || _langId == LANG_JA)
                {
                    _isCaretCreated = CreateCaret(hwnd, IntPtr.Zero, 2, 10);
                }
            }
        }

        public void TryMove(int x, int y)
        {
            if (_isCaretCreated)
            {
                SetCaretPos(x, y);
            }
        }

        public void TryDestroy()
        {
            if (_isCaretCreated)
            {
                DestroyCaret();
                _isCaretCreated = false;
            }
        }
    }
}
