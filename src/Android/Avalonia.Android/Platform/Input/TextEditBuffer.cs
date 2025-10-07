using System;
using Android.Views;
using Android.Views.InputMethods;
using Avalonia.Android.Platform.SkiaPlatform;
using Avalonia.Input.TextInput;

namespace Avalonia.Android.Platform.Input
{
    internal class TextEditBuffer
    {
        private readonly IAndroidInputMethod _textInputMethod;
        private readonly TopLevelImpl _topLevel;
        private TextSelection? _composition;

        public TextEditBuffer(IAndroidInputMethod textInputMethod, TopLevelImpl topLevel)
        {
            _textInputMethod = textInputMethod;
            _topLevel = topLevel;
        }

        public bool HasComposition => Composition is { } composition && composition.Start != composition.End;

        public TextSelection Selection
        {
            get
            {
                var selection = _textInputMethod.Client?.Selection ?? default;
                return new TextSelection(Math.Min(selection.Start, selection.End), Math.Max(selection.Start, selection.End));
            }

            set
            {
                if (_textInputMethod.Client is { } client)
                    client.Selection = value;
            }
        }

        public TextSelection? Composition
        {
            get => _composition; set
            {
                if (value is { } v)
                {
                    var text = Text;
                    var start = Math.Clamp(v.Start, 0, text.Length);
                    var end = Math.Clamp(v.End, 0, text.Length);
                    _composition = new TextSelection(Math.Min(start, end), Math.Max(start, end));
                }
                else
                    _composition = null;
            }
        }

        public Java.Lang.String? SelectedText
        {
            get
            {
                if(_textInputMethod.Client is not { } client || Selection.Start < 0 || Selection.End >= client.Text.Length)
                {
                    return new Java.Lang.String();
                }

                var selection = Selection;

                return new Java.Lang.String(Text.Substring(selection.Start, selection.End - selection.Start));
            }
        }

        public string? ComposingText
        {
            get => !HasComposition ? null : Text?.Substring(Composition!.Value.Start, Composition!.Value.End - Composition!.Value.Start); set
            {
                if (HasComposition)
                {
                    var start = Composition!.Value.Start;
                    Replace(Composition!.Value.Start, Composition!.Value.End, value ?? string.Empty);
                    Composition = new TextSelection(start, start  + (value?.Length ?? 0));
                }
                else
                {
                    var selection = Selection;
                    Replace(selection.Start, selection.End, value ?? string.Empty);
                    Composition = new TextSelection(selection.Start, selection.Start + (value?.Length ?? 0));
                }
            }
        }

        public string Text => _textInputMethod.Client?.Text ?? string.Empty;

        public ExtractedText? ExtractedText
        {
            get
            {
                var text = Text;
                return new ExtractedText
                {
                    Flags = text.Contains('\n') ? 0 : ExtractedTextFlags.SingleLine,
                    PartialStartOffset = -1,
                    PartialEndOffset = text.Length,
                    SelectionStart = Selection.Start,
                    SelectionEnd = Selection.End,
                    StartOffset = 0,
                    Text = new Java.Lang.String(text)
                };
            }
        }

        internal Java.Lang.ICharSequence? GetTextBeforeCursor(int n)
        {
            var start = Math.Max(Selection.Start - n, 0);
            var length = Math.Min(n, Selection.Start);
            return new Java.Lang.String(_textInputMethod.Client?.GetTextInRange(start, length).ToArray() ?? Array.Empty<char>());
        }

        internal Java.Lang.ICharSequence? GetTextAfterCursor(int n)
        {
            return new Java.Lang.String(_textInputMethod.Client?.GetTextInRange(Selection.End, n).ToArray() ?? Array.Empty<char>());
        }

        internal void Remove(int index, int length)
        {
            if (_textInputMethod.Client is { } client)
            {
                client.Selection = new TextSelection(index, index + length);
                if (length > 0)
                    _textInputMethod?.View.DispatchKeyEvent(new KeyEvent(KeyEventActions.Down, Keycode.ForwardDel));
            }
        }

        internal void Replace(int start, int end, string text)
        {
            if (_textInputMethod.Client is { } client)
            {
                var realStart = Math.Min(start, end);
                var realEnd = Math.Max(start, end);
                if (realEnd > realStart)
                {
                    client.Selection = new TextSelection(realStart, realEnd);
                    _textInputMethod?.View.DispatchKeyEvent(new KeyEvent(KeyEventActions.Down, Keycode.ForwardDel));
                }
                _topLevel.TextInput(text);
                var index = realStart + text.Length;
                client.Selection = new TextSelection(index, index);
                Composition = null;
            }
        }
    }
}
