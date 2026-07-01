using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Input.TextInput;
using Avalonia.Threading;

using static Avalonia.Win32.Interop.UnmanagedMethods;

namespace Avalonia.Win32.Input
{
    /// <summary>
    /// A Windows input method editor based on Windows Input Method Manager (IMM32).
    /// </summary>
    internal class Imm32InputMethod : ITextInputMethodImpl
    {
        public IntPtr Hwnd { get; private set; }
        private IntPtr _currentHimc;
        private WindowImpl? _parent;

        private Imm32CaretManager _caretManager;

        private ushort _langId;
        private const int CaretMargin = 1;

        private bool _ignoreComposition;

        public TextInputMethodClient? Client { get; private set; }

        [MemberNotNullWhen(true, nameof(Client))]
        public bool IsActive => Client != null;

        public bool IsComposing { get; set; }

        public bool ShowCompositionWindow => false;

        public string? Composition { get; internal set; }

        public void CreateCaret()
        {
            _caretManager.TryCreate(Hwnd);
        }

        public void EnableImm()
        {
            var himc = ImmGetContext(Hwnd);

            if(himc == IntPtr.Zero)
            {
                himc = ImmCreateContext();
            }

            if(himc != _currentHimc)
            {
                if(_currentHimc != IntPtr.Zero)
                {
                    DisableImm();
                }

                ImmAssociateContext(Hwnd, himc);

                ImmReleaseContext(Hwnd, himc);

                _currentHimc = himc;

                _caretManager.TryCreate(Hwnd);
            }
        }

        public void DisableImm()
        {
            _caretManager.TryDestroy();

            Reset();

            ImmAssociateContext(Hwnd, IntPtr.Zero);

            _caretManager.TryDestroy();

            _currentHimc = IntPtr.Zero;
        }

        public void SetLanguageAndWindow(WindowImpl parent, IntPtr hwnd, IntPtr HKL)
        {
            Hwnd = hwnd;
            _parent = parent;

            var langId = PRIMARYLANGID(LGID(HKL));

            if (IsActive)
            {
                if (langId != _langId)
                {
                    DisableImm();
                    EnableImm();
                }
            }

            _langId = langId;
        }

        public void ClearLanguageAndWindow()
        {
            DisableImm();

            Hwnd = IntPtr.Zero;
            _parent = null;
            Client = null;
            _langId = 0;

            IsComposing = false;
        }

        //Dependant on CurrentThread. When Avalonia will support Multiple Dispatchers -
        //every Dispatcher should have their own InputMethod.
        public static Imm32InputMethod Current { get; } = new();

        public void Reset()
        {
            Dispatcher.UIThread.Post(() =>
            {
                var himc = ImmGetContext(Hwnd);

                if (himc != IntPtr.Zero)
                {
                    if (IsComposing)
                    {
                        _ignoreComposition = true;
                    }

                    if (_parent != null)
                    {
                        _parent._ignoreWmChar = true;
                    }

                    ImmNotifyIME(himc, NI_COMPOSITIONSTR, CPS_COMPLETE, 0);

                    ImmReleaseContext(Hwnd, himc);

                    IsComposing = false;

                    Composition = null;
                }
            });
        }

        public void SetClient(TextInputMethodClient? client)
        {
            if(Client != null)
            {
                Composition = null;

                Client.SetPreeditText(null);
            }

            Client = client;

            Dispatcher.UIThread.Post(() =>
            {
                if (IsActive)
                {
                    EnableImm();
                }
                else
                {
                    // A renderer process have moved its input focus to a password input
                    // when there is an ongoing composition, e.g. a user has clicked a
                    // mouse button and selected a password input while composing a text.
                    // For this case, we have to complete the ongoing composition and
                    // clean up the resources attached to this object BEFORE DISABLING THE IME.

                    DisableImm();
                }
            });
        }

        public void SetCursorRect(Rect rect)
        {
            var focused = GetActiveWindow() == Hwnd;

            if (!focused)
            {
                return;
            }

            Dispatcher.UIThread.Post(() =>
            {
                var himc = ImmGetContext(Hwnd);

                if (himc == IntPtr.Zero)
                {
                    return;
                }

                MoveImeWindow(rect, himc);

                ImmReleaseContext(Hwnd, himc);
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

            _caretManager.TryMove(x2, y2);

            if (ShowCompositionWindow)
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
                y2 += CaretMargin;
            }


            // Chinese, Japanese, and Korean(CJK) IMEs also use the rectangle given to
            // ::ImmSetCandidateWindow() with its 'dwStyle' parameter CFS_EXCLUDE
            // to move their candidate windows when a user disables TSF and CUAS.
            // Therefore, we also set this parameter here.
            var excludeRectangle = new CANDIDATEFORM
            {
                dwIndex = 0,
                dwStyle = CFS_EXCLUDE,
                ptCurrentPos = new POINT {X = x1, Y = y1},
                rcArea = new RECT {left = x1, top = y1, right = x2, bottom = y2 + CaretMargin}
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

        public void CompositionChanged(string? composition)
        {
            Composition = composition;

            if (!IsActive || !Client.SupportsPreedit)
            {
                return;
            }

            Client.SetPreeditText(composition);
        }
        
        public string? GetCompositionString(GCS flag)
        {
            if (!IsComposing)
            {
                return null;
            }

            var himc = ImmGetContext(Hwnd);

            return ImmGetCompositionString(himc, flag);
        }

        public void HandleCompositionStart()
        {
            Composition = null;

            if (IsActive)
            {
                Client.SetPreeditText(null);
                ClearCompositionDecorations();

                if (Client.SupportsSurroundingText && Client.Selection.Start != Client.Selection.End)
                {
                    KeyPress(Key.Delete, PhysicalKey.Delete);
                }
            }

            IsComposing = true;
        }

        public void HandleCompositionEnd(uint timestamp)
        {
            //Cleanup composition state.
            IsComposing = false;

            if (_parent != null && !string.IsNullOrEmpty(Composition))
            {
                var e = new RawTextInputEventArgs(WindowsKeyboardDevice.Instance, timestamp, _parent.Owner, Composition);

                if (_parent.Input != null)
                {
                    _parent.Input(e);

                    _parent._ignoreWmChar = true;
                }
            }

            Composition = null;

            if (IsActive)
            {
                Client.SetPreeditText(null);
                ClearCompositionDecorations();
            }
        }

        public void HandleComposition(IntPtr wParam, IntPtr lParam, uint timestamp)
        {
            if (_ignoreComposition)
            {
                _ignoreComposition = false;

                return;
            }

            var flags = (GCS)ToInt32(lParam);

            if (flags == 0)
            {
                CompositionChanged("");
                ClearCompositionDecorations();
            }

            if ((flags & GCS.GCS_RESULTSTR) != 0)
            {
                var resultString = GetCompositionString(GCS.GCS_RESULTSTR);

                if (_parent != null && !string.IsNullOrEmpty(resultString))
                {
                    Composition = null;

                    // Structured client (in-document composition, e.g. the rich text editor): replace the
                    // composition range with the result in one atomic edit and commit. The legacy path below
                    // clears the preedit first and then re-inserts the result as a separate text-input event -
                    // a clear-then-reinsert that, for an in-document composition, resets the composition range
                    // before the text input arrives, so the client's composition-commit handling is bypassed.
                    if (IsActive && Client is IStructuredTextInput structured &&
                        structured.CompositionRange is { IsEmpty: false } compositionRange)
                    {
                        structured.ReplaceText(compositionRange, resultString);
                        structured.CommitComposition();
                        ClearCompositionDecorations();

                        // The result is already in the document; ignore the WM_CHAR the IME posts next.
                        _parent._ignoreWmChar = true;
                    }
                    else
                    {
                        if (IsActive)
                        {
                            Client.SetPreeditText(null);
                            ClearCompositionDecorations();
                        }

                        var e = new RawTextInputEventArgs(WindowsKeyboardDevice.Instance, timestamp, _parent.Owner, resultString);

                        if (_parent.Input != null)
                        {
                            _parent.Input(e);

                            _parent._ignoreWmChar = true;
                        }
                    }
                }
            }

            if ((flags & GCS.GCS_COMPSTR) != 0)
            {
                var compositionString = GetCompositionString(GCS.GCS_COMPSTR);

                CompositionChanged(compositionString);

                // Feed the per-clause composition attributes (GCS_COMPATTR) to a structured client so the IME
                // chooses the clause highlighting instead of the framework hard-coding a preedit underline.
                UpdateCompositionDecorations();
            }
        }

        // GCS_COMPATTR clause attribute bytes (immdev.h ATTR_*), one per composition WCHAR.
        private const byte ATTR_TARGET_CONVERTED = 0x01;
        private const byte ATTR_CONVERTED = 0x02;
        private const byte ATTR_TARGET_NOTCONVERTED = 0x03;
        private const byte ATTR_INPUT_ERROR = 0x04;

        // Reads GCS_COMPATTR (per-character clause attributes), groups it into contiguous clause runs, and
        // reports them to a structured client as transient decorations over the composition range. A client
        // without a structured CompositionRange (e.g. TextBox's legacy preedit overlay) is skipped, so its
        // existing preedit underline is unaffected.
        private void UpdateCompositionDecorations()
        {
            if (!IsActive || Client is not IStructuredTextInput structured)
            {
                return;
            }

            var range = structured.CompositionRange;
            if (range is null || range.IsEmpty)
            {
                structured.SetInputDecorations(Array.Empty<TextInputDecoration>());
                return;
            }

            var attributes = GetCompositionAttributes();
            if (attributes is null || attributes.Length == 0)
            {
                structured.SetInputDecorations(Array.Empty<TextInputDecoration>());
                return;
            }

            var decorations = BuildCompositionDecorations(structured, range.Start, attributes);
            structured.SetInputDecorations(decorations);
        }

        private void ClearCompositionDecorations()
        {
            if (IsActive && Client is IStructuredTextInput structured)
            {
                structured.SetInputDecorations(Array.Empty<TextInputDecoration>());
            }
        }

        private static IReadOnlyList<TextInputDecoration> BuildCompositionDecorations(
            IStructuredTextInput structured, ITextPointer compositionStart, byte[] attributes)
        {
            var decorations = new List<TextInputDecoration>();

            var runStart = 0;
            for (var i = 1; i <= attributes.Length; i++)
            {
                if (i < attributes.Length && attributes[i] == attributes[runStart])
                {
                    continue;
                }

                var kind = MapAttribute(attributes[runStart]);
                decorations.Add(structured.CreateClauseDecoration(compositionStart, runStart, i - runStart, kind));
                runStart = i;
            }

            return decorations;
        }

        private static TextInputDecorationKind MapAttribute(byte attribute) => attribute switch
        {
            ATTR_TARGET_CONVERTED => TextInputDecorationKind.ConvertedTarget,
            ATTR_CONVERTED => TextInputDecorationKind.Converted,
            ATTR_TARGET_NOTCONVERTED => TextInputDecorationKind.TargetNotConverted,
            ATTR_INPUT_ERROR => TextInputDecorationKind.InputError,
            _ => TextInputDecorationKind.Input
        };

        private byte[]? GetCompositionAttributes()
        {
            if (!IsComposing)
            {
                return null;
            }

            var himc = ImmGetContext(Hwnd);
            if (himc == IntPtr.Zero)
            {
                return null;
            }

            try
            {
                var size = ImmGetCompositionString(himc, GCS.GCS_COMPATTR, IntPtr.Zero, 0);
                if (size <= 0)
                {
                    return null;
                }

                var buffer = Marshal.AllocHGlobal(size);
                try
                {
                    var written = ImmGetCompositionString(himc, GCS.GCS_COMPATTR, buffer, (uint)size);
                    if (written <= 0)
                    {
                        return null;
                    }

                    var attributes = new byte[written];
                    Marshal.Copy(buffer, attributes, 0, written);
                    return attributes;
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
            finally
            {
                ImmReleaseContext(Hwnd, himc);
            }
        }

        private static int ToInt32(IntPtr ptr)
        {
            if (IntPtr.Size == 4)
                return ptr.ToInt32();

            return (int)(ptr.ToInt64() & 0xffffffff);
        }

        private void KeyPress(Key key, PhysicalKey physicalKey)
        {
            if (_parent?.Input != null)
            {
                _parent.Input(new RawKeyEventArgs(KeyboardDevice.Instance!, (ulong)DateTime.Now.Ticks, _parent.Owner,
                RawKeyEventType.KeyDown, key, RawInputModifiers.None, physicalKey, null));

                _parent.Input(new RawKeyEventArgs(KeyboardDevice.Instance!, (ulong)DateTime.Now.Ticks, _parent.Owner,
                RawKeyEventType.KeyUp, key, RawInputModifiers.None, physicalKey, null));

            }
        }

        ~Imm32InputMethod()
        {
            _caretManager.TryDestroy();
        }
    }
}
