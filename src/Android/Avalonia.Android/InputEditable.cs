using System;
using Android.Runtime;
using Android.Text;
using Android.Views;
using Android.Views.InputMethods;
using Avalonia.Android.Platform.SkiaPlatform;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Java.Lang;
using static System.Net.Mime.MediaTypeNames;

namespace Avalonia.Android
{
    internal class InputEditable : SpannableStringBuilder
    {
        private readonly TopLevelImpl _topLevel;
        private readonly IAndroidInputMethod _inputMethod;
        private int _currentBatchLevel;
        private string _previousText;

        public InputEditable(TopLevelImpl topLevel, IAndroidInputMethod inputMethod)
        {
            _topLevel = topLevel;
            _inputMethod = inputMethod;
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

        public bool IsInBatchEdit => _currentBatchLevel > 0;

        public void BeginBatchEdit()
        {
            _currentBatchLevel++;

            if(_currentBatchLevel == 1)
            {
                _previousText = ToString();
            }
        }

        public void EndBatchEdit()
        {
            if (_currentBatchLevel == 1)
            {
                _inputMethod.Client.SelectInSurroundingText(-1, _previousText.Length);
                var time = DateTime.Now.TimeOfDay;
                var currentText = ToString();

                if (string.IsNullOrEmpty(currentText))
                {
                    _inputMethod.View.DispatchKeyEvent(new KeyEvent(KeyEventActions.Down, Keycode.ForwardDel));
                }
                else
                {
                    var rawTextEvent = new RawTextInputEventArgs(KeyboardDevice.Instance, (ulong)time.Ticks, _topLevel.InputRoot, currentText);
                    _topLevel.Input(rawTextEvent);
                }
                _inputMethod.Client.SelectInSurroundingText(Selection.GetSelectionStart(this), Selection.GetSelectionEnd(this));

                _previousText = "";
            }

            _currentBatchLevel--;
        }

        public void UpdateString(string? text)
        {
            if(text != ToString())
            {
                Clear();
                Insert(0, text);
            }
        }
    }
}
