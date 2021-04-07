using System;
using System.Collections.Generic;
using System.Text;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Views.InputMethods;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Input.TextInput;

namespace Avalonia.Android
{
    class AndroidInputMethod<TView> : ITextInputMethodImpl
        where TView: View, IInitEditorInfo
    {
        private readonly TView _host;
        private readonly InputMethodManager _imm;
        private IInputElement _inputElement;

        public AndroidInputMethod(TView host)
        {
            if (host.OnCheckIsTextEditor() == false)
                throw new InvalidOperationException("Host should return true from OnCheckIsTextEditor()");

            _host = host;
            _imm = host.Context.GetSystemService(Context.InputMethodService).JavaCast<InputMethodManager>();

            _host.Focusable = true;
            _host.FocusableInTouchMode = true;
        }

        public void Reset()
        {
            _imm.RestartInput(_host);
        }

        public void SetActive(bool active)
        {
            if (active)
                _host.RequestFocus();
        }

        public void SetCursorRect(Rect rect)
        {
        }

        public void SetOptions(TextInputOptionsQueryEventArgs options)
        {
            if (_inputElement != null)
            {
                _inputElement.PointerReleased -= RestoreSoftKeyboard;
            }

            _inputElement = options.Source as InputElement;

            if (_inputElement == null)
            {
                _imm.HideSoftInputFromWindow(_host.WindowToken, HideSoftInputFlags.None);
            }

            _host.InitEditorInfo((outAttrs) =>
            {
                outAttrs.InputType = options.ContentType switch
                {
                    TextInputContentType.Email => global::Android.Text.InputTypes.TextVariationEmailAddress,
                    TextInputContentType.Number => global::Android.Text.InputTypes.ClassNumber,
                    TextInputContentType.Password => global::Android.Text.InputTypes.TextVariationPassword,
                    TextInputContentType.Phone => global::Android.Text.InputTypes.ClassPhone,
                    TextInputContentType.Url => global::Android.Text.InputTypes.TextVariationUri,
                    _ => global::Android.Text.InputTypes.Null
                };

                if (options.AutoCapitalization)
                {
                    outAttrs.InitialCapsMode = global::Android.Text.CapitalizationMode.Sentences;
                    outAttrs.InputType |= global::Android.Text.InputTypes.TextFlagCapSentences;
                }

                if (options.Multiline)
                    outAttrs.InputType |= global::Android.Text.InputTypes.TextFlagMultiLine;
            });

            Reset();
            _inputElement.PointerReleased += RestoreSoftKeyboard;
            RestoreSoftKeyboard(null, null);
        }

        private void RestoreSoftKeyboard(object sender, PointerReleasedEventArgs e)
        {
            //_imm.ToggleSoftInput(ShowFlags.Implicit, HideSoftInputFlags.NotAlways);
            _imm.ShowSoftInput(_host, ShowFlags.Implicit);
        }
    }
}
