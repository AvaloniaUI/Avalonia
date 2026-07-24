using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Collections.Generic;
using System.Linq;
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
        private int? _compositionCursorPosition;
        private IReadOnlyList<TextInputMethodPreeditSegment>? _compositionSegments;

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
            _compositionCursorPosition = null;
            _compositionSegments = null;
        }

        //Dependant on CurrentThread. When Avalonia will support Multiple Dispatchers -
        //every Dispatcher should have their own InputMethod.
        public static Imm32InputMethod Current { get; } = new();

        public void Reset()
        {
            _compositionCursorPosition = null;
            _compositionSegments = null;

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
                    _compositionCursorPosition = null;
                    _compositionSegments = null;
                }
            });
        }

        public void SetClient(TextInputMethodClient? client)
        {
            if(Client != null)
            {
                Composition = null;
                _compositionCursorPosition = null;
                _compositionSegments = null;

                Client.SetPreeditText(null, null, null);
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

        public void CompositionChanged(
            string? composition,
            int? cursorPosition,
            IReadOnlyList<TextInputMethodPreeditSegment>? segments)
        {
            Composition = composition;
            _compositionCursorPosition = cursorPosition;
            _compositionSegments = segments;

            if (!IsActive || !Client.SupportsPreedit)
            {
                return;
            }

            Client.SetPreeditText(composition, cursorPosition, segments);
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

        private int? GetCompositionCursorPosition()
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
                var cursorPosition = ImmGetCompositionString(himc, GCS.GCS_CURSORPOS, IntPtr.Zero, 0);

                return cursorPosition >= 0 ? cursorPosition : null;
            }
            finally
            {
                ImmReleaseContext(Hwnd, himc);
            }
        }

        private IReadOnlyList<TextInputMethodPreeditSegment>? GetCompositionSegments(string? composition, int? cursorPosition)
        {
            if (!IsComposing || string.IsNullOrEmpty(composition) || composition.Length <= 1)
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
                var clauses = GetCompositionClauseOffsets(himc, composition.Length);

                if (clauses is null || clauses.Count <= 2)
                {
                    return null;
                }

                var attributes = GetCompositionAttributes(himc, composition.Length);

                return BuildCompositionSegments(clauses, attributes, composition.Length, cursorPosition);
            }
            finally
            {
                ImmReleaseContext(Hwnd, himc);
            }
        }

        private static byte[]? GetCompositionAttributes(IntPtr himc, int compositionLength)
        {
            var bytes = GetCompositionBytes(himc, GCS.GCS_COMPATTR);

            if (bytes is null || bytes.Length == 0)
            {
                return null;
            }

            return bytes.Length > compositionLength ? bytes.Take(compositionLength).ToArray() : bytes;
        }

        private static IReadOnlyList<int>? GetCompositionClauseOffsets(IntPtr himc, int compositionLength)
        {
            var bytes = GetCompositionBytes(himc, GCS.GCS_COMPCLAUSE);

            if (bytes is null || bytes.Length < sizeof(int) * 2 || bytes.Length % sizeof(int) != 0)
            {
                return null;
            }

            var rawOffsets = new int[bytes.Length / sizeof(int)];
            Buffer.BlockCopy(bytes, 0, rawOffsets, 0, bytes.Length);

            return NormalizeClauseOffsets(rawOffsets, compositionLength);
        }

        private static byte[]? GetCompositionBytes(IntPtr himc, GCS flag)
        {
            var bufferLength = ImmGetCompositionString(himc, flag, IntPtr.Zero, 0);

            if (bufferLength <= 0)
            {
                return null;
            }

            var buffer = new byte[bufferLength];

            unsafe
            {
                fixed (byte* bufferPtr = buffer)
                {
                    var result = ImmGetCompositionString(himc, flag, (IntPtr)bufferPtr, (uint)bufferLength);

                    if (result <= 0)
                    {
                        return null;
                    }
                }
            }

            return buffer;
        }

        internal static IReadOnlyList<int>? NormalizeClauseOffsets(IReadOnlyList<int> rawOffsets, int compositionLength)
        {
            if (rawOffsets.Count < 2 || compositionLength <= 0)
            {
                return null;
            }

            var lastOffset = rawOffsets[rawOffsets.Count - 1];
            var scale = lastOffset switch
            {
                var value when value == compositionLength => 1,
                var value when value == compositionLength * sizeof(char) => sizeof(char),
                _ => 0
            };

            if (scale == 0)
            {
                return null;
            }

            var normalized = new List<int>(rawOffsets.Count);
            var previous = -1;

            foreach (var rawOffset in rawOffsets)
            {
                if (rawOffset < 0 || rawOffset % scale != 0)
                {
                    return null;
                }

                var normalizedOffset = rawOffset / scale;

                if (normalizedOffset < previous || normalizedOffset > compositionLength)
                {
                    return null;
                }

                normalized.Add(normalizedOffset);
                previous = normalizedOffset;
            }

            if (normalized[0] != 0 || normalized[normalized.Count - 1] != compositionLength)
            {
                return null;
            }

            return normalized;
        }

        internal static IReadOnlyList<TextInputMethodPreeditSegment>? BuildCompositionSegments(
            IReadOnlyList<int> clauses,
            IReadOnlyList<byte>? attributes,
            int compositionLength,
            int? cursorPosition)
        {
            if (clauses.Count <= 2 || compositionLength <= 1)
            {
                return null;
            }

            var clampedCursorPosition = cursorPosition is >= 0 && cursorPosition <= compositionLength
                ? cursorPosition.Value
                : compositionLength;

            var targetClauseIndices = new List<int>();

            for (var i = 0; i < clauses.Count - 1; i++)
            {
                var start = clauses[i];
                var end = clauses[i + 1];

                if (end <= start)
                {
                    continue;
                }

                if (ContainsTargetAttribute(attributes, start, end))
                {
                    targetClauseIndices.Add(i);
                }
            }

            var activeClauseIndex = targetClauseIndices.Count switch
            {
                0 => FindClauseIndex(clauses, clampedCursorPosition),
                1 => targetClauseIndices[0],
                _ => FindClauseIndex(clauses, clampedCursorPosition, targetClauseIndices) ?? targetClauseIndices[0]
            };

            if (activeClauseIndex < 0)
            {
                return null;
            }

            var segments = new List<TextInputMethodPreeditSegment>(clauses.Count - 1);

            for (var i = 0; i < clauses.Count - 1; i++)
            {
                var start = clauses[i];
                var end = clauses[i + 1];

                if (start < 0 || end > compositionLength || end <= start)
                {
                    continue;
                }

                segments.Add(new TextInputMethodPreeditSegment(
                    start,
                    end - start,
                    i == activeClauseIndex ?
                        TextInputMethodPreeditSegmentKind.ActiveClause :
                        TextInputMethodPreeditSegmentKind.InactiveClause));
            }

            return segments.Count > 1 ? segments : null;
        }

        private static bool ContainsTargetAttribute(IReadOnlyList<byte>? attributes, int start, int end)
        {
            if (attributes is null || attributes.Count == 0)
            {
                return false;
            }

            var upperBound = Math.Min(end, attributes.Count);

            for (var i = Math.Max(0, start); i < upperBound; i++)
            {
                if (attributes[i] is 0x01 or 0x03)
                {
                    return true;
                }
            }

            return false;
        }

        private static int FindClauseIndex(IReadOnlyList<int> clauses, int cursorPosition)
        {
            for (var i = 0; i < clauses.Count - 1; i++)
            {
                var start = clauses[i];
                var end = clauses[i + 1];

                if (cursorPosition >= start && cursorPosition < end)
                {
                    return i;
                }
            }

            return clauses.Count - 2;
        }

        private static int? FindClauseIndex(IReadOnlyList<int> clauses, int cursorPosition, IReadOnlyList<int> candidates)
        {
            foreach (var clauseIndex in candidates)
            {
                if (clauseIndex < 0 || clauseIndex >= clauses.Count - 1)
                {
                    continue;
                }

                if (cursorPosition >= clauses[clauseIndex] && cursorPosition < clauses[clauseIndex + 1])
                {
                    return clauseIndex;
                }
            }

            return null;
        }

        public void HandleCompositionStart()
        {
            Composition = null;
            _compositionCursorPosition = null;
            _compositionSegments = null;

            if (IsActive)
            {
                Client.SetPreeditText(null, null, null);

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
            _compositionCursorPosition = null;
            _compositionSegments = null;

            if (IsActive)
            {
                Client.SetPreeditText(null, null, null);
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
            var resultChanged = (flags & GCS.GCS_RESULTSTR) != 0;
            
            if (flags == 0)
            {
                CompositionChanged("", null, null);
            }

            if (resultChanged)
            {
                var resultString = GetCompositionString(GCS.GCS_RESULTSTR);

                Composition = null;
                _compositionCursorPosition = null;
                _compositionSegments = null;

                if (IsActive)
                {
                    Client.SetPreeditText(null, null, null);
                }

                if (_parent != null && !string.IsNullOrEmpty(resultString))
                {
                    var e = new RawTextInputEventArgs(WindowsKeyboardDevice.Instance, timestamp, _parent.Owner, resultString);

                    if (_parent.Input != null)
                    {
                        _parent.Input(e);

                        _parent._ignoreWmChar = true;
                    }
                }
            }

            var compositionChanged = (flags & GCS.GCS_COMPSTR) != 0;
            var cursorPositionChanged = (flags & GCS.GCS_CURSORPOS) != 0;
            var segmentChanged = (flags & GCS.GCS_COMPATTR) != 0 || (flags & GCS.GCS_COMPCLAUSE) != 0;

            if (compositionChanged || (cursorPositionChanged && !resultChanged) || segmentChanged)
            {
                var compositionString = compositionChanged
                    ? GetCompositionString(GCS.GCS_COMPSTR)
                    : Composition;

                var cursorPosition = cursorPositionChanged
                    ? GetCompositionCursorPosition()
                    : _compositionCursorPosition;

                var segments = (compositionChanged || segmentChanged)
                    ? GetCompositionSegments(compositionString, cursorPosition)
                    : _compositionSegments;

                CompositionChanged(compositionString, cursorPosition, segments);
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
