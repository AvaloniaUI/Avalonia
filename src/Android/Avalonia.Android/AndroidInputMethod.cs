using System;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Text;
using Android.Views;
using Android.Views.InputMethods;
using Avalonia.Android.Platform.SkiaPlatform;
using Avalonia.Input;
using Avalonia.Input.TextInput;
using Java.Lang;

namespace Avalonia.Android
{
    internal interface IAndroidInputMethod
    {
        public ITextInputMethodClient Client { get; }
        
        public bool IsActive { get; }
    }
    
    class AndroidInputMethod<TView> : ITextInputMethodImpl, IAndroidInputMethod
        where TView: View, IInitEditorInfo
    {
        private readonly TView _host;
        private readonly InputMethodManager _imm;
        private ITextInputMethodClient _client;
        private InputConnectionImpl _inputConnection;

        public AndroidInputMethod(TView host)
        {
            if (host.OnCheckIsTextEditor() == false)
                throw new InvalidOperationException("Host should return true from OnCheckIsTextEditor()");

            _host = host;
            _imm = host.Context.GetSystemService(Context.InputMethodService).JavaCast<InputMethodManager>();

            _host.Focusable = true;
            _host.FocusableInTouchMode = true;
            _host.ViewTreeObserver.AddOnGlobalLayoutListener(new SoftKeyboardListener(_host));
        }

        public bool IsActive => Client != null;

        public ITextInputMethodClient Client => _client;

        public void Reset()
        {
            _imm.RestartInput(_host);
        }

        public void SetClient(ITextInputMethodClient client)
        {
            _client = client;
            
            if (IsActive)
            {
                _host.RequestFocus();
                Reset();
                _imm.ShowSoftInput(_host, ShowFlags.Implicit);
            }
            else
                _imm.HideSoftInputFromWindow(_host.WindowToken, HideSoftInputFlags.None);         
        }

        public void SetCursorRect(Rect rect)
        {
        }

        public void SetOptions(TextInputOptions options)
        {
            _inputConnection = new InputConnectionImpl(_host, this);

            _host.InitEditorInfo((_host, outAttrs) =>
            {
                outAttrs.InputType = options.ContentType switch
                {
                    TextInputContentType.Email => global::Android.Text.InputTypes.TextVariationEmailAddress,
                    TextInputContentType.Number => global::Android.Text.InputTypes.ClassNumber,
                    TextInputContentType.Password => global::Android.Text.InputTypes.TextVariationPassword,
                    TextInputContentType.Digits => global::Android.Text.InputTypes.ClassPhone,
                    TextInputContentType.Url => global::Android.Text.InputTypes.TextVariationUri,
                    _ => global::Android.Text.InputTypes.ClassText
                };

                if (options.AutoCapitalization)
                {
                    outAttrs.InitialCapsMode = global::Android.Text.CapitalizationMode.Sentences;
                    outAttrs.InputType |= global::Android.Text.InputTypes.TextFlagCapSentences;
                }

                if (options.Multiline)
                    outAttrs.InputType |= global::Android.Text.InputTypes.TextFlagMultiLine;

                outAttrs.ImeOptions |= ImeFlags.NoFullscreen | ImeFlags.NoExtractUi;

                return _inputConnection;
            });
        }

        private void RestoreSoftKeyboard(object sender, PointerReleasedEventArgs e)
        {
            _imm.ShowSoftInput(_host, ShowFlags.Implicit);
        }
    }
}
