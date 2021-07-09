using System;
using System.Collections.Generic;
using System.Text;

using Avalonia.Input.TextInput;
using Avalonia.Threading;

using static Avalonia.Win32.Interop.UnmanagedMethods;

namespace Avalonia.Win32.Input
{
    /// <summary>
    /// A Windows input method editor based on Windows Input Method Manager (IMM32).
    /// </summary>
    class Imm32InputMethod : ITextInputMethodImpl, IDisposable
    {
        private bool _disposedValue;
        private IntPtr _hwnd;
        private WindowImpl _parent;
        private bool _active;
        private bool _systemCaret;

        public Imm32InputMethod(WindowImpl parent, IntPtr hwnd)
        {
            _parent = parent;
            _hwnd = hwnd;
            _disposedValue = false;
            _active = false;
            _systemCaret = true;
            if (_systemCaret)
            {
                CreateCaret(_hwnd, IntPtr.Zero, 1, 1);
            }
        }

        public void Reset()
        {
            // ???
        }

        public void SetActive(bool active)
        {
            _active = active;
            Dispatcher.UIThread.Post(() =>
            {
                IntPtr himc = ImmGetContext(_hwnd);
                ImmSetActiveContext(himc, active);
                ImmReleaseContext(_hwnd, himc);
            });
        }

        // https://www.geek-share.com/detail/2712493225.html

        public void SetCursorRect(Rect rect)
        {
            if (!_active)
            {
                return;
            }
            Dispatcher.UIThread.Post(() =>
            {
                IntPtr himc = ImmGetContext(_hwnd);
                if (himc == IntPtr.Zero)
                {
                    return;
                }

                // see: https://chromium.googlesource.com/experimental/chromium/src/+/bf09a5036ccfb77d2277247c66dc55daf41df3fe/chrome/browser/ime_input.cc

                var p1 = _parent.PointToScreen(rect.TopLeft);
                var p2 = _parent.PointToScreen(rect.BottomRight);

                var candidateForm = new CANDIDATEFORM
                {
                    dwIndex = 0,
                    dwStyle = CFS_CANDIDATEPOS,
                    ptCurrentPos = new POINT {  X = p1.X, Y = p1.Y }
                };
                ImmSetCandidateWindow(himc, ref candidateForm);

                if (_systemCaret)
                {
                    SetCaretPos(p1.X, p1.Y);
                }

                var compForm = new COMPOSITIONFORM
                {
                    dwStyle = CFS_POINT, 
                    ptCurrentPos = new POINT { X = p1.X, Y = p1.Y },
                    rcArea = new RECT { left = p1.X, top = p1.Y, right = p2.X, bottom = p2.Y}
                };
                ImmSetCompositionWindow(himc, ref compForm);

                compForm.dwStyle = CFS_RECT;
                ImmSetCompositionWindow(himc, ref compForm);

                ImmReleaseContext(_hwnd, himc);
            });
        }

        public void SetOptions(TextInputOptionsQueryEventArgs options)
        {
            // ???
        }

        protected void _dispose()
        {
            if (!_disposedValue)
            {
                _disposedValue = true;
                if (_systemCaret)
                {
                    _systemCaret = false;
                    DestroyCaret();
                } 
            }
        }

        ~Imm32InputMethod()
        {
            _dispose();
        }

        public void Dispose()
        {
            _dispose();
            GC.SuppressFinalize(this);
        }
    }
}
