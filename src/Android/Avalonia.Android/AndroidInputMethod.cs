using System;
using Android.Content;
using Android.Runtime;
using Android.Text;
using Android.Views;
using Android.Views.InputMethods;
using Avalonia.Android.Platform.SkiaPlatform;
using Avalonia.Input.TextInput;

namespace Avalonia.Android
{
    internal interface IAndroidInputMethod
    {
        public View View { get; }

        public TextInputMethodClient Client { get; }

        public bool IsActive { get; }

        public InputMethodManager IMM { get; }

        void OnBatchEditedEnded();
    }

    enum CustomImeFlags
    { 
        ActionNone = 0x00000001,
       ActionGo = 0x00000002,
       ActionSearch = 0x00000003,
       ActionSend = 0x00000004,
       ActionNext = 0x00000005,
       ActionDone = 0x00000006,
       ActionPrevious = 0x00000007,
    }

    internal class AndroidInputMethod<TView> : ITextInputMethodImpl, IAndroidInputMethod
        where TView : View, IInitEditorInfo
    {
        private readonly TView _host;
        private readonly InputMethodManager _imm;
        private TextInputMethodClient _client;
        private AvaloniaInputConnection _inputConnection;

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

        public View View => _host;

        public bool IsActive => Client != null;

        public TextInputMethodClient Client => _client;

        public InputMethodManager IMM => _imm;

        public void Reset()
        {

        }

        public void SetClient(TextInputMethodClient client)
        {
            _client = client;

            if (IsActive)
            {
                _host.RequestFocus();

                _imm.RestartInput(View);              

                _imm.ShowSoftInput(_host, ShowFlags.Implicit);

                var selection = Client.Selection;

                _imm.UpdateSelection(_host, selection.Start, selection.End, selection.Start, selection.End);

                var surroundingText = _client.SurroundingText ?? "";

                var extractedText = new ExtractedText
                {
                    Text = new Java.Lang.String(surroundingText),
                    SelectionStart = selection.Start,
                    SelectionEnd = selection.End,
                    PartialEndOffset = surroundingText.Length
                };

                _imm.UpdateExtractedText(_host, _inputConnection?.ExtractedTextToken ?? 0, extractedText);

                _client.SurroundingTextChanged += _client_SurroundingTextChanged;
                _client.SelectionChanged += _client_SelectionChanged;
            }
            else
            {
                _imm.HideSoftInputFromWindow(_host.WindowToken, HideSoftInputFlags.ImplicitOnly);
            }
        }

        private void _client_SelectionChanged(object sender, EventArgs e)
        {
            if (_inputConnection.IsInBatchEdit)
                return;
            OnSelectionChanged();
        }

        private void OnSelectionChanged()
        {
            if (Client is null)
            {
                return;
            }

            var selection = Client.Selection;

            _imm.UpdateSelection(_host, selection.Start, selection.End, selection.Start, selection.End);

            _inputConnection.SetSelection(selection.Start, selection.End);
        }

        private void _client_SurroundingTextChanged(object sender, EventArgs e)
        {
            if (_inputConnection.IsInBatchEdit)
                return;
            OnSurroundingTextChanged();
        }

        public void OnBatchEditedEnded()
        {
            if (_inputConnection.IsInBatchEdit)
                return;

            OnSurroundingTextChanged();
            OnSelectionChanged();
        }

        private void OnSurroundingTextChanged()
        {
            if(_client is null)
            {
                return;
            }

            var surroundingText = _client.SurroundingText ?? "";
            var editableText = _inputConnection.EditableWrapper.ToString();

            if (editableText != surroundingText)
            {
                _inputConnection.EditableWrapper.IgnoreChange = true;

                var diff = GetDiff();

                _inputConnection.Editable.Replace(diff.index, editableText.Length, diff.diff);

                _inputConnection.EditableWrapper.IgnoreChange = false;

                if(diff.index == 0)
                {
                    var selection = _client.Selection;
                    _client.Selection = new TextSelection(selection.Start, 0);
                    _client.Selection = selection;
                }
            }

            (int index, string diff) GetDiff()
            {
                int index = 0;

                var longerLength = Math.Max(surroundingText.Length, editableText.Length);

                for (int i = 0; i < longerLength; i++)
                {
                    if (surroundingText.Length == i || editableText.Length == i || surroundingText[i] != editableText[i])
                    {
                        index = i;
                        break;
                    }
                }

                var diffString = surroundingText.Substring(index, surroundingText.Length - index);

                return (index, diffString);
            }
        }

        public void SetCursorRect(Rect rect)
        {
            
        }

        public void SetOptions(TextInputOptions options)
        {
            _host.InitEditorInfo((topLevel, outAttrs) =>
            {
                if (_client == null)
                    return null;

                _inputConnection = new AvaloniaInputConnection(topLevel, this);

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

                outAttrs.ImeOptions = options.ReturnKeyType switch
                {
                    TextInputReturnKeyType.Return => ImeFlags.NoEnterAction,
                    TextInputReturnKeyType.Go => (ImeFlags)CustomImeFlags.ActionGo,
                    TextInputReturnKeyType.Send => (ImeFlags)CustomImeFlags.ActionSend,
                    TextInputReturnKeyType.Search => (ImeFlags)CustomImeFlags.ActionSearch,
                    TextInputReturnKeyType.Next => (ImeFlags)CustomImeFlags.ActionNext,
                    TextInputReturnKeyType.Previous => (ImeFlags)CustomImeFlags.ActionPrevious,
                    TextInputReturnKeyType.Done => (ImeFlags)CustomImeFlags.ActionDone,
                    _ => options.Multiline ? ImeFlags.NoEnterAction : (ImeFlags)CustomImeFlags.ActionDone
                };

                outAttrs.ImeOptions |= ImeFlags.NoFullscreen | ImeFlags.NoExtractUi;

                return _inputConnection;
            });
        }
    }
}
