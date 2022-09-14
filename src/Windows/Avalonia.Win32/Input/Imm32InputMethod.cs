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
        private bool _showCompositionWindow;
        private Imm32CaretManager _caretManager = new();
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

            IsComposing = false;
        }

        public void ClearLanguageAndWindow()
        {
            if (HWND != IntPtr.Zero && _defaultImc != IntPtr.Zero)
            {
                ImmReleaseContext(HWND, _defaultImc);
            }

            _defaultImc = IntPtr.Zero;
            HWND = IntPtr.Zero;
            _parent = null;
            _active = false;
            _langId = 0;
            _showCompositionWindow = false;

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
                    IsComposing = false;
                });
            }
        }

        public void SetClient(ITextInputMethodClient client)
        {
            _active = client is { };
            Dispatcher.UIThread.Post(() =>
            {
                if (_active)
                {
                    if (DefaultImc != IntPtr.Zero)
                    {
                        _caretManager.TryCreate(_langId, HWND);
                        // Load the default IME context.
                        // NOTE(hbono)
                        //   IMM ignores this call if the IME context is loaded. Therefore, we do
                        //   not have to check whether or not the IME context is loaded.
                        ImmAssociateContext(HWND, _defaultImc);
                    }
                }
                else
                {
                    // A renderer process have moved its input focus to a password input
                    // when there is an ongoing composition, e.g. a user has clicked a
                    // mouse button and selected a password input while composing a text.
                    // For this case, we have to complete the ongoing composition and
                    // clean up the resources attached to this object BEFORE DISABLING THE IME.
                    if (IsComposing)
                    {
                        ImmNotifyIME(DefaultImc, NI_COMPOSITIONSTR, CPS_COMPLETE, 0);
                        ImmReleaseContext(HWND, DefaultImc);
                        IsComposing = false;
                    }
                    ImmAssociateContext(HWND, IntPtr.Zero);
                    _caretManager.TryDestroy();
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

                MoveImeWindow(rect, himc);
                ImmReleaseContext(HWND, himc);
            });
        }
        
        // see: https://chromium.googlesource.com/experimental/chromium/src/+/bf09a5036ccfb77d2277247c66dc55daf41df3fe/chrome/browser/ime_input.cc
        // see: https://engine.chinmaygarde.com/window__win32_8cc_source.html
        private void MoveImeWindow(Rect rect, IntPtr himc)
        {
            var p1 = rect.TopLeft;
            var p2 = rect.BottomRight;
            var s = _parent?.DesktopScaling ?? 1;
            var (x1, y1, x2, y2) = ((int) (p1.X * s), (int) (p1.Y * s), (int) (p2.X * s), (int) (p2.Y * s));

            if (!_showCompositionWindow &&
                _langId == LANG_ZH)
            {
                // Chinese IMEs ignore function calls to ::ImmSetCandidateWindow()
                // when a user disables TSF (Text Service Framework) and CUAS (Cicero
                // Unaware Application Support).
                // On the other hand, when a user enables TSF and CUAS, Chinese IMEs
                // ignore the position of the current system caret and uses the
                // parameters given to ::ImmSetCandidateWindow() with its 'dwStyle'
                // parameter CFS_CANDIDATEPOS.
                // Therefore, we do not only call ::ImmSetCandidateWindow() but also
                // set the positions of the temporary system caret.
                var candidateForm = new CANDIDATEFORM
                {
                    dwIndex = 0,
                    dwStyle = CFS_CANDIDATEPOS,
                    ptCurrentPos = new POINT {X = x2, Y = y2}
                };
                ImmSetCandidateWindow(himc, ref candidateForm);
            }
            
            _caretManager.TryMove(x2, y2);

            if (_showCompositionWindow)
            {
                ConfigureCompositionWindow(x1, y1, himc, y2 - y1);
                // Don't need to set the position of candidate window.
                return;
            }

            if (_langId == LANG_KO)
            {
                // Chinese IMEs and Japanese IMEs require the upper-left corner of
                // the caret to move the position of their candidate windows.
                // On the other hand, Korean IMEs require the lower-left corner of the
                // caret to move their candidate windows.
                y2 += _caretMargin;
            }

            // Need to return here since some Chinese IMEs would stuck if set
            // candidate window position with CFS_EXCLUDE style.
            if (_langId == LANG_ZH)
            {
                return;
            }

            // Japanese IMEs and Korean IMEs also use the rectangle given to
            // ::ImmSetCandidateWindow() with its 'dwStyle' parameter CFS_EXCLUDE
            // to move their candidate windows when a user disables TSF and CUAS.
            // Therefore, we also set this parameter here.
            var excludeRectangle = new CANDIDATEFORM
            {
                dwIndex = 0,
                dwStyle = CFS_EXCLUDE,
                ptCurrentPos = new POINT {X = x1, Y = y1},
                rcArea = new RECT {left = x1, top = y1, right = x2, bottom = y2 + _caretMargin}
            };
            ImmSetCandidateWindow(himc, ref excludeRectangle);
        }

        private static void ConfigureCompositionWindow(int x1, int y1, IntPtr himc, int height)
        {
            var compForm = new COMPOSITIONFORM
            {
                dwStyle = CFS_POINT,
                ptCurrentPos = new POINT {X = x1, Y = y1},
            };
            ImmSetCompositionWindow(himc, ref compForm);

            var logFont = new LOGFONT()
            {
                lfHeight = height,
                lfQuality = 5 //CLEARTYPE_QUALITY
            };
            ImmSetCompositionFont(himc, ref logFont);
        }
        
        public void SetOptions(TextInputOptions options)
        {
            // we're skipping this. not usable on windows
        }
        
        public bool IsComposing { get; set; }

        ~Imm32InputMethod()
        {
            _caretManager.TryDestroy();
        }
    }
}
