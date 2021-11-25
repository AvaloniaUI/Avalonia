using System;
using Avalonia.Input.TextInput;
using Avalonia.Threading;

using static Avalonia.Win32.Interop.UnmanagedMethods;

namespace Avalonia.Win32.Input
{
    /// <summary>
    /// A Windows input method editor based on Windows Input Method Manager (IMM32).
    /// </summary>
    class Imm32InputMethod : ITextInputMethodImpl
    {
        public IntPtr HWND { get; private set; }
        private IntPtr _defaultImc;
        private WindowImpl _parent;
        private bool _active;
        private bool _systemCaret;
        private bool _showCompositionWindow;
        private bool _showCandidateList;
        private ushort _langId;
        private const int _caretMargin = 1;
        
        public void SetLanguageAndWindow(WindowImpl parent, IntPtr hwnd, IntPtr HKL)
        {
            if (HWND != hwnd)
            {
                _defaultImc = IntPtr.Zero;
            }
            HWND = hwnd;
            _parent = parent;
            _active = false;
            _langId = PRIMARYLANGID(LGID(HKL));
            _showCompositionWindow = true;
            _showCandidateList = true;

            IsComposing = false;
        }

        //Dependant on CurrentThread. When Avalonia will support Multiple Dispatchers -
        //every Dispatcher should have their own InputMethod.
        public static Imm32InputMethod Current { get; } = new Imm32InputMethod();

        private IntPtr DefaultImc
        {
            get
            {
                if (_defaultImc == IntPtr.Zero &&
                    HWND != IntPtr.Zero)
                {
                    _defaultImc = ImmGetContext(HWND);
                    ImmReleaseContext(HWND, _defaultImc);
                }

                if (_defaultImc == IntPtr.Zero)
                {
                    _defaultImc = ImmCreateContext();
                }

                return _defaultImc;
            }
        }

        public void Reset()
        {
            if (IsComposing)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    ImmNotifyIME(DefaultImc, NI_COMPOSITIONSTR, CPS_COMPLETE, 0);
                    ImmReleaseContext(HWND, DefaultImc);
                });
            }
        }

        public void SetActive(bool active)
        {
            _active = active;
            Dispatcher.UIThread.Post(() =>
            {
                if (active)
                {
                    if (DefaultImc != IntPtr.Zero)
                    {
                        if (_langId == LANG_ZH || _langId == LANG_JA)
                        {
                            _systemCaret = CreateCaret(HWND, IntPtr.Zero, 2, 10);
                        }
                        ImmAssociateContext(HWND, _defaultImc);
                    }
                }
                else
                {
                    ImmAssociateContext(HWND, IntPtr.Zero);
                    if (_systemCaret)
                    {
                        DestroyCaret();
                        _systemCaret = false;
                    }
                }
            });
        }

        public void SetCursorRect(Rect rect)
        {
            var focused = GetActiveWindow() == HWND;
            if (!focused)
            {
                return;
            }
            Dispatcher.UIThread.Post(() =>
            {
                IntPtr himc = DefaultImc;
                if (himc == IntPtr.Zero)
                {
                    return;
                }

                // see: https://chromium.googlesource.com/experimental/chromium/src/+/bf09a5036ccfb77d2277247c66dc55daf41df3fe/chrome/browser/ime_input.cc
                // see: https://engine.chinmaygarde.com/window__win32_8cc_source.html

                var p1 = rect.TopLeft;
                var p2 = rect.BottomRight;
                var s = _parent?.DesktopScaling ?? 1;
                var (x1, y1, x2, y2) = ((int)(p1.X * s), (int)(p1.Y * s), (int)(p2.X * s), (int)(p2.Y * s));

                if (_showCompositionWindow)
                {
                    var compForm = new COMPOSITIONFORM
                    {
                        dwStyle = CFS_POINT,
                        ptCurrentPos = new POINT { X = x1, Y = y1 },
                    };
                    ImmSetCompositionWindow(himc, ref compForm);
                }

                if (_showCandidateList)
                {
                    var candidateForm = new CANDIDATEFORM
                    {
                        dwIndex = 0,
                        dwStyle = CFS_CANDIDATEPOS,
                        ptCurrentPos = new POINT { X = x2, Y = y2 }
                    };
                    ImmSetCandidateWindow(himc, ref candidateForm);

                    if (_systemCaret)
                    {
                        SetCaretPos(x2, y2);
                    }

                    if (_langId == LANG_KO)
                    {
                        y2 += _caretMargin;
                    }

                    candidateForm = new CANDIDATEFORM
                    {
                        dwIndex = 0,
                        dwStyle = CFS_EXCLUDE,
                        ptCurrentPos = new POINT { X = x2, Y = y2 },
                        rcArea = new RECT { left = x2, top = y2, right = x2, bottom = y2 + _caretMargin}
                    };
                }

                ImmReleaseContext(HWND, himc);
            });
        }

        public void SetOptions(TextInputOptionsQueryEventArgs options)
        {
            // ???
        }
        
        public bool IsComposing { get; set; }

        ~Imm32InputMethod()
        {
            if (_systemCaret)
            {
                _systemCaret = false;
                DestroyCaret();
            }
        }
    }
}
