using System;
using Android.Views;
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
            get => _textInputMethod.Client?.Selection ?? default; set
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
                    _composition = new TextSelection(start, end);
                }
                else
                    _composition = null;
            }
        }

        public string? SelectedText
        {
            get
            {
                if(_textInputMethod.Client is not { } client || Selection.Start < 0 || Selection.End >= client.SurroundingText.Length)
                {
                    return "";
                }

                return client.SurroundingText.Substring(Selection.Start, Selection.End - Selection.Start);
            }
        }

        public string? ComposingText
        {
            get => !HasComposition ? null : Text?.Substring(Composition!.Value.Start, Composition!.Value.End - Composition!.Value.Start); set
            {
                if (HasComposition)
                {
                    var start = Composition!.Value.Start;
                    Remove(start, Composition!.Value.End - start);
                    Insert(start, value ?? "");
                    Composition = new TextSelection(start, start  + (value?.Length ?? 0));
                }
                else
                {
                    var start = Selection.Start;
                    Remove(start, Selection.End - start);
                    Insert(start, value ?? "");
                    Composition = new TextSelection(start, start + (value?.Length ?? 0));
                }
            }
        }

        public string Text => _textInputMethod.Client?.SurroundingText ?? "";

        internal void Insert(int index, string text)
        {
            if (_textInputMethod.Client is { } client)
            {
                client.Selection = new TextSelection(index, index);
                _topLevel.TextInput(text);
            }
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
    }
}
