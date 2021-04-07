using System;
using System.Collections.Generic;
using System.Text;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Views.InputMethods;
using Avalonia.Input.TextInput;

namespace Avalonia.Android
{
    class AndroidInputMethod<TView> : ITextInputMethodImpl
        where TView: View, IInitEditorInfo
    {
        private readonly TView _host;
        private readonly InputMethodManager _imm;

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
            //throw new NotImplementedException();
        }
    }
}
