using System;
using Android.Runtime;
using Android.Text;
using Android.Views;
using Android.Views.InputMethods;
using Avalonia.Android.Platform.SkiaPlatform;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Input.TextInput;
using Java.Lang;
using static System.Net.Mime.MediaTypeNames;

namespace Avalonia.Android
{
    internal class InputEditable : SpannableStringBuilder, ITextEditable
    {
        private readonly TopLevelImpl _topLevel;
        private readonly IAndroidInputMethod _inputMethod;
        private readonly AvaloniaInputConnection _avaloniaInputConnection;
        private int _currentBatchLevel;
        private string _previousText;
        private int _previousSelectionStart;
        private int _previousSelectionEnd;

        public event EventHandler TextChanged;
        public event EventHandler SelectionChanged;
        public event EventHandler CompositionChanged;

        public InputEditable(TopLevelImpl topLevel, IAndroidInputMethod inputMethod, AvaloniaInputConnection avaloniaInputConnection)
        {
            _topLevel = topLevel;
            _inputMethod = inputMethod;
            _avaloniaInputConnection = avaloniaInputConnection;
        }

        public InputEditable(ICharSequence text) : base(text)
        {
        }

        public InputEditable(string text) : base(text)
        {
        }

        public InputEditable(ICharSequence text, int start, int end) : base(text, start, end)
        {
        }

        public InputEditable(string text, int start, int end) : base(text, start, end)
        {
        }

        protected InputEditable(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public int SelectionStart
        {
            get => Selection.GetSelectionStart(this); set
            {
                var end = SelectionEnd < 0 ? 0 : SelectionEnd;
                _avaloniaInputConnection.SetSelection(value, end);
                _inputMethod.IMM.UpdateSelection(_topLevel.View, value, end, value, end);
            }
        }
        public int SelectionEnd
        {
            get => Selection.GetSelectionEnd(this); set
            {
                var start = SelectionStart < 0 ? 0 : SelectionStart;
                _avaloniaInputConnection.SetSelection(start, value);
                _inputMethod.IMM.UpdateSelection(_topLevel.View, start, value, start, value);
            }
        }

        public string? Text
        {
            get => ToString(); set
            {
                if (Text != value)
                {
                    Clear();
                    Insert(0, value ?? "");
                }
            }
        }

        public int CompositionStart => BaseInputConnection.GetComposingSpanStart(this);

        public int CompositionEnd => BaseInputConnection.GetComposingSpanEnd(this);

        public void BeginBatchEdit()
        {
            _currentBatchLevel++;

            if (_currentBatchLevel == 1)
            {
                _previousText = ToString();
                _previousSelectionStart =  SelectionStart;
                _previousSelectionEnd = SelectionEnd;
            }
        }

        public void EndBatchEdit()
        {
            if (_currentBatchLevel == 1)
            {
                if(_previousText != Text)
                {
                    TextChanged?.Invoke(this, EventArgs.Empty);
                }

                if (_previousSelectionStart != SelectionStart || _previousSelectionEnd != SelectionEnd)
                {
                    SelectionChanged?.Invoke(this, EventArgs.Empty);
                }
            }

            _currentBatchLevel--;
        }

        public void RaiseCompositionChanged()
        {
            CompositionChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
