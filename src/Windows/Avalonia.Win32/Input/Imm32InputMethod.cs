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
                // A reconversion target armed as the structured composition range must survive
                // until the first composition update replaces it in place; clearing the preedit
                // or deleting the selection here would edit the target text.
                if (Client is IStructuredTextInput { CompositionRange: { IsEmpty: false } })
                {
                    IsComposing = true;
                    return;
                }

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

        /// <summary>
        /// Handles a WM_IME_REQUEST message. Returns <c>true</c> when the request was handled and
        /// <paramref name="result"/> holds the value to return to the IME; otherwise the caller should
        /// fall back to the default window procedure.
        /// </summary>
        public bool HandleImeRequest(IntPtr wParam, IntPtr lParam, out IntPtr result)
        {
            result = IntPtr.Zero;

            if (!IsActive || !Client.SupportsSurroundingText)
            {
                return false;
            }

            switch (ToInt32(wParam))
            {
                case IMR_RECONVERTSTRING:
                    result = WriteReconvertString(lParam);
                    return true;
                case IMR_DOCUMENTFEED:
                    // TSF based IMEs (modern Microsoft IME, Google Japanese Input) reconvert through
                    // the CUAS bridge, which asks for the document via IMR_DOCUMENTFEED first and only
                    // follows up with IMR_RECONVERTSTRING once it gets an answer. Feed it the caret line
                    // and selection while there is no active composition; during composition the default
                    // handling is left alone, exactly as for ordinary typing.
                    if (IsComposing)
                    {
                        return false;
                    }
                    result = WriteReconvertString(lParam);
                    return true;
                case IMR_CONFIRMRECONVERTSTRING:
                    result = HandleConfirmReconvertString(lParam);
                    return true;
                default:
                    // Composition window/font, candidate window and char-position requests are left
                    // to the default window procedure.
                    return false;
            }
        }

        // Writes the document the IME reconverts: the caret line as the string and the current
        // selection as the composition/target range. Serves IMR_RECONVERTSTRING and IMR_DOCUMENTFEED.
        private IntPtr WriteReconvertString(IntPtr lParam)
        {
            var text = Client!.SurroundingText;
            var selection = Client.Selection;

            var start = Math.Min(selection.Start, selection.End);
            var length = Math.Abs(selection.End - selection.Start);

            return (IntPtr)Imm32ReconversionHelper.Write(lParam, text.AsSpan(), start, length);
        }

        // The IME echoes back the range it settled on (it may snap to word boundaries). Record it as
        // the reconversion target so the following composition replaces exactly that text.
        private IntPtr HandleConfirmReconvertString(IntPtr lParam)
        {
            if (!Imm32ReconversionHelper.ReadCompRange(lParam, out var start, out var length))
            {
                return IntPtr.Zero;
            }

            SetReconversionTarget(start, length);

            return new IntPtr(1);
        }

        // The composition the IME opens next replaces exactly this range (surrounding-text space).
        // An in-document client anchors the structured composition range directly: the composition
        // updates then replace the target in place and the IME's clause attributes decorate it, so
        // no transient selection highlight is needed. Overlay clients keep the selection sync -
        // their composition replaces the selection.
        private void SetReconversionTarget(int start, int length)
        {
            if (Client is IStructuredTextInput structured && Client.SupportsInDocumentComposition)
            {
                // Legacy Selection is relative to SurroundingText; the structured selection is the
                // same range in document space. Their difference locates the surrounding text.
                var legacySelection = Client.Selection;
                var origin = structured.Selection.Start.Offset -
                             Math.Min(legacySelection.Start, legacySelection.End);

                structured.CompositionRange = structured.RangeAt(origin + start, length);
                return;
            }

            Client!.Selection = new TextSelection(start, start + length);
        }

        /// <summary>
        /// Starts reconversion of the current selection without waiting for the IME to ask. Used for
        /// gestures the IME does not own (e.g. Ctrl+Backspace): we push the selected text into the IME
        /// with <c>SCS_SETRECONVERTSTRING</c>. Returns <c>true</c> when reconversion was started.
        /// </summary>
        public bool TryReconvert()
        {
            if (!IsActive || !Client.SupportsSurroundingText)
            {
                return false;
            }

            var selection = Client.Selection;
            var start = Math.Min(selection.Start, selection.End);
            var length = Math.Abs(selection.End - selection.Start);

            // Only take over the key when the user actually selected text to reconvert; otherwise leave
            // the gesture to its normal handling (e.g. delete word).
            if (length == 0)
            {
                return false;
            }

            var himc = ImmGetContext(Hwnd);

            if (himc == IntPtr.Zero)
            {
                return false;
            }

            try
            {
                // Bail unless the active IME accepts an application-supplied reconvert string.
                var caps = ImmGetProperty(GetKeyboardLayout(0), IGP_SETCOMPSTR);

                if ((caps & SCS_CAP_SETRECONVERTSTRING) == 0)
                {
                    return false;
                }

                var text = Client.SurroundingText;
                var size = Imm32ReconversionHelper.GetRequiredSize(text.Length);
                var buffer = Marshal.AllocHGlobal(size);

                try
                {
                    // Set dwSize so the helper fills the buffer we own.
                    Marshal.WriteInt32(buffer, 0, size);
                    Imm32ReconversionHelper.Write(buffer, text.AsSpan(), start, length);

                    // Let the IME snap the range to its dictionary boundaries, then record the target
                    // so the composition that follows replaces exactly that text.
                    ImmSetCompositionString(himc, SCS_QUERYRECONVERTSTRING, buffer, (uint)size, IntPtr.Zero, 0);

                    if (Imm32ReconversionHelper.ReadCompRange(buffer, out var compStart, out var compLength))
                    {
                        SetReconversionTarget(compStart, compLength);
                    }

                    return ImmSetCompositionString(himc, SCS_SETRECONVERTSTRING, buffer, (uint)size, IntPtr.Zero, 0);
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
