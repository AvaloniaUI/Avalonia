// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Input.Platform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Utils;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Data;

namespace Avalonia.Controls
{
    public class TextBox : TemplatedControl, UndoRedoHelper<TextBox.UndoRedoState>.IUndoRedoHost
    {
        public static readonly StyledProperty<bool> AcceptsReturnProperty =
            AvaloniaProperty.Register<TextBox, bool>(nameof(AcceptsReturn));

        public static readonly StyledProperty<bool> AcceptsTabProperty =
            AvaloniaProperty.Register<TextBox, bool>(nameof(AcceptsTab));

        public static readonly DirectProperty<TextBox, int> CaretIndexProperty =
            AvaloniaProperty.RegisterDirect<TextBox, int>(
                nameof(CaretIndex),
                o => o.CaretIndex,
                (o, v) => o.CaretIndex = v);
        
        public static readonly StyledProperty<bool> IsReadOnlyProperty =
            AvaloniaProperty.Register<TextBox, bool>(nameof(IsReadOnly));

        public static readonly DirectProperty<TextBox, int> SelectionStartProperty =
            AvaloniaProperty.RegisterDirect<TextBox, int>(
                nameof(SelectionStart),
                o => o.SelectionStart,
                (o, v) => o.SelectionStart = v);

        public static readonly DirectProperty<TextBox, int> SelectionEndProperty =
            AvaloniaProperty.RegisterDirect<TextBox, int>(
                nameof(SelectionEnd),
                o => o.SelectionEnd,
                (o, v) => o.SelectionEnd = v);

        public static readonly DirectProperty<TextBox, string> TextProperty =
            TextBlock.TextProperty.AddOwner<TextBox>(
                o => o.Text,
                (o, v) => o.Text = v,
                defaultBindingMode: BindingMode.TwoWay,
                enableDataValidation: true);

        public static readonly StyledProperty<TextAlignment> TextAlignmentProperty =
            TextBlock.TextAlignmentProperty.AddOwner<TextBox>();

        public static readonly StyledProperty<TextWrapping> TextWrappingProperty =
            TextBlock.TextWrappingProperty.AddOwner<TextBox>();

        public static readonly StyledProperty<string> WatermarkProperty =
            AvaloniaProperty.Register<TextBox, string>(nameof(Watermark));

        public static readonly StyledProperty<bool> UseFloatingWatermarkProperty =
            AvaloniaProperty.Register<TextBox, bool>(nameof(UseFloatingWatermark));

        struct UndoRedoState : IEquatable<UndoRedoState>
        {
            public string Text { get; }
            public int CaretPosition { get; }

            public UndoRedoState(string text, int caretPosition)
            {
                Text = text;
                CaretPosition = caretPosition;
            }

            public bool Equals(UndoRedoState other) => ReferenceEquals(Text, other.Text) || Equals(Text, other.Text);
        }

        private string _text;
        private int _caretIndex;
        private int _selectionStart;
        private int _selectionEnd;
        private TextPresenter _presenter;
        private UndoRedoHelper<UndoRedoState> _undoRedoHelper;
        private bool _ignoreTextChanges;
        private static readonly string[] invalidCharacters = new String[1]{"\u007f"};

        static TextBox()
        {
            FocusableProperty.OverrideDefaultValue(typeof(TextBox), true);
        }

        public TextBox()
        {
            var horizontalScrollBarVisibility = Observable.CombineLatest(
                this.GetObservable(AcceptsReturnProperty),
                this.GetObservable(TextWrappingProperty),
                (acceptsReturn, wrapping) =>
                {
                    if (acceptsReturn)
                    {
                        return wrapping == TextWrapping.NoWrap ?
                            ScrollBarVisibility.Auto :
                            ScrollBarVisibility.Disabled;
                    }
                    else
                    {
                        return ScrollBarVisibility.Hidden;
                    }
                });
            Bind(
                ScrollViewer.HorizontalScrollBarVisibilityProperty,
                horizontalScrollBarVisibility,
                BindingPriority.Style);
            _undoRedoHelper = new UndoRedoHelper<UndoRedoState>(this);
        }

        public bool AcceptsReturn
        {
            get { return GetValue(AcceptsReturnProperty); }
            set { SetValue(AcceptsReturnProperty, value); }
        }

        public bool AcceptsTab
        {
            get { return GetValue(AcceptsTabProperty); }
            set { SetValue(AcceptsTabProperty, value); }
        }

        public int CaretIndex
        {
            get
            {
                return _caretIndex;
            }

            set
            {
                value = CoerceCaretIndex(value);
                SetAndRaise(CaretIndexProperty, ref _caretIndex, value);
                UndoRedoState state;
                if (_undoRedoHelper.TryGetLastState(out state) && state.Text == Text)
                    _undoRedoHelper.UpdateLastState();
            }
        }
        
        public bool IsReadOnly
        {
            get { return GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }

        public int SelectionStart
        {
            get
            {
                return _selectionStart;
            }

            set
            {
                value = CoerceCaretIndex(value);
                SetAndRaise(SelectionStartProperty, ref _selectionStart, value);
                if (SelectionStart == SelectionEnd)
                {
                    CaretIndex = SelectionStart;
                }
            }
        }

        public int SelectionEnd
        {
            get
            {
                return _selectionEnd;
            }

            set
            {
                value = CoerceCaretIndex(value);
                SetAndRaise(SelectionEndProperty, ref _selectionEnd, value);
                if (SelectionStart == SelectionEnd)
                {
                    CaretIndex = SelectionEnd;
                }
            }
        }

        [Content]
        public string Text
        {
            get { return _text; }
            set
            {
                if (!_ignoreTextChanges)
                {
                    CaretIndex = CoerceCaretIndex(CaretIndex, value?.Length ?? 0);
                    SetAndRaise(TextProperty, ref _text, value);
                }
            }
        }

        public TextAlignment TextAlignment
        {
            get { return GetValue(TextAlignmentProperty); }
            set { SetValue(TextAlignmentProperty, value); }
        }

        public string Watermark
        {
            get { return GetValue(WatermarkProperty); }
            set { SetValue(WatermarkProperty, value); }
        }

        public bool UseFloatingWatermark
        {
            get { return GetValue(UseFloatingWatermarkProperty); }
            set { SetValue(UseFloatingWatermarkProperty, value); }
        }

        public TextWrapping TextWrapping
        {
            get { return GetValue(TextWrappingProperty); }
            set { SetValue(TextWrappingProperty, value); }
        }

        protected override void OnTemplateApplied(TemplateAppliedEventArgs e)
        {
            _presenter = e.NameScope.Get<TextPresenter>("PART_TextPresenter");
            _presenter.Cursor = new Cursor(StandardCursorType.Ibeam);

            if(IsFocused)
            {
                _presenter.ShowCaret();
            }
        }

        protected override void OnGotFocus(GotFocusEventArgs e)
        {
            base.OnGotFocus(e);

            // when navigating to a textbox via the tab key, select all text if
            //   1) this textbox is *not* a multiline textbox
            //   2) this textbox has any text to select
            if (e.NavigationMethod == NavigationMethod.Tab &&
                !AcceptsReturn &&
                Text?.Length > 0)
            {
                SelectionStart = 0;
                SelectionEnd = Text.Length;
            }
            else
            {
                _presenter?.ShowCaret();
            }

            e.Handled = true;
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);
            SelectionStart = 0;
            SelectionEnd = 0;
            _presenter?.HideCaret();
        }

        protected override void OnTextInput(TextInputEventArgs e)
        {
            HandleTextInput(e.Text);
            e.Handled = true;
        }

        private void HandleTextInput(string input)
        {
            if (!IsReadOnly)
            {
                input = RemoveInvalidCharacters(input);
                string text = Text ?? string.Empty;
                int caretIndex = CaretIndex;
                if (!string.IsNullOrEmpty(input))
                {
                    DeleteSelection();
                    caretIndex = CaretIndex;
                    text = Text ?? string.Empty;
                    SetTextInternal(text.Substring(0, caretIndex) + input + text.Substring(caretIndex));
                    CaretIndex += input.Length;
                    SelectionStart = SelectionEnd = CaretIndex;
                    _undoRedoHelper.DiscardRedo();
                }
            }
        }

        public string RemoveInvalidCharacters(string text)
        {
            for (var i = 0; i < invalidCharacters.Length; i++)
            {
                text = text.Replace(invalidCharacters[i], string.Empty);
            }

            return text;
        }

        private async void Copy()
        {
            await ((IClipboard)AvaloniaLocator.Current.GetService(typeof(IClipboard)))
                .SetTextAsync(GetSelection());
        }

        private async void Paste()
        {
            var text = await ((IClipboard)AvaloniaLocator.Current.GetService(typeof(IClipboard))).GetTextAsync();
            if (text == null)
            {
                return;
            }
            _undoRedoHelper.Snapshot();
            HandleTextInput(text);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            string text = Text ?? string.Empty;
            int caretIndex = CaretIndex;
            bool movement = false;
            bool handled = false;
            var modifiers = e.Modifiers;

            switch (e.Key)
            {
                case Key.A:
                    if (modifiers == InputModifiers.Control)
                    {
                        SelectAll();
                        handled = true;
                    }
                    break;
                case Key.C:
                    if (modifiers == InputModifiers.Control)
                    {
                        Copy();
                        handled = true;
                    }
                    break;

                case Key.X:
                    if (modifiers == InputModifiers.Control)
                    {
                        Copy();
                        DeleteSelection();
                        handled = true;
                    }
                    break;

                case Key.V:
                    if (modifiers == InputModifiers.Control)
                    {
                        Paste();
                        handled = true;
                    }

                    break;

                case Key.Z:
                    if (modifiers == InputModifiers.Control)
                    {
                        _undoRedoHelper.Undo();
                        handled = true;
                    }
                    break;
                case Key.Y:
                    if (modifiers == InputModifiers.Control)
                    {
                        _undoRedoHelper.Redo();
                        handled = true;
                    }
                    break;
                case Key.Left:
                    MoveHorizontal(-1, modifiers);
                    movement = true;
                    break;

                case Key.Right:
                    MoveHorizontal(1, modifiers);
                    movement = true;
                    break;

                case Key.Up:
                    movement = MoveVertical(-1, modifiers);
                    break;

                case Key.Down:
                    movement = MoveVertical(1, modifiers);
                    break;

                case Key.Home:
                    MoveHome(modifiers);
                    movement = true;
                    break;

                case Key.End:
                    MoveEnd(modifiers);
                    movement = true;
                    break;

                case Key.Back:
                    if (modifiers == InputModifiers.Control && SelectionStart == SelectionEnd)
                    {
                        SetSelectionForControlBackspace(modifiers);
                    }

                    if (!DeleteSelection() && CaretIndex > 0)
                    {
                        var removedCharacters = 1;
                        // handle deleting /r/n
                        // you don't ever want to leave a dangling /r around. So, if deleting /n, check to see if 
                        // a /r should also be deleted.
                        if (CaretIndex > 1 &&
                            text[CaretIndex - 1] == '\n' &&
                            text[CaretIndex - 2] == '\r')
                        {
                            removedCharacters = 2;
                        }

                        SetTextInternal(text.Substring(0, caretIndex - removedCharacters) + text.Substring(caretIndex));
                        CaretIndex -= removedCharacters;
                        SelectionStart = SelectionEnd = CaretIndex;
                    }
                    handled = true;
                    break;

                case Key.Delete:
                    if (modifiers == InputModifiers.Control && SelectionStart == SelectionEnd)
                    {
                        SetSelectionForControlDelete(modifiers);
                    }

                    if (!DeleteSelection() && caretIndex < text.Length)
                    {
                        var removedCharacters = 1;
                        // handle deleting /r/n
                        // you don't ever want to leave a dangling /r around. So, if deleting /n, check to see if 
                        // a /r should also be deleted.
                        if (CaretIndex < text.Length - 1 &&
                            text[caretIndex + 1] == '\n' &&
                            text[caretIndex] == '\r')
                        {
                            removedCharacters = 2;
                        }

                        SetTextInternal(text.Substring(0, caretIndex) + text.Substring(caretIndex + removedCharacters));
                    }
                    handled = true;
                    break;

                case Key.Enter:
                    if (AcceptsReturn)
                    {
                        HandleTextInput("\r\n");
                        handled = true;
                    }

                    break;

                case Key.Tab:
                    if (AcceptsTab)
                    {
                        HandleTextInput("\t");
                        handled = true;
                    }
                    else
                    {
                        base.OnKeyDown(e);
                    }

                    break;

                default:
                    handled = false;
                    break;
            }

            if (movement && ((modifiers & InputModifiers.Shift) != 0))
            {
                SelectionEnd = CaretIndex;
            }
            else if (movement)
            {
                SelectionStart = SelectionEnd = CaretIndex;
            }

            if (handled || movement)
            {
                e.Handled = true;
            }
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            var point = e.GetPosition(_presenter);
            var index = CaretIndex = _presenter.GetCaretIndex(point);
            var text = Text;

            if (text != null)
            {
                switch (e.ClickCount)
                {
                    case 1:
                        SelectionStart = SelectionEnd = index;
                        break;
                    case 2:
                        if (!StringUtils.IsStartOfWord(text, index))
                        {
                            SelectionStart = StringUtils.PreviousWord(text, index);
                        }

                        SelectionEnd = StringUtils.NextWord(text, index);
                        break;
                    case 3:
                        SelectionStart = 0;
                        SelectionEnd = text.Length;
                        break;
                }
            }

            e.Device.Capture(_presenter);
            e.Handled = true;
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            if (_presenter != null && e.Device.Captured == _presenter)
            {
                var point = e.GetPosition(_presenter);
                CaretIndex = SelectionEnd = _presenter.GetCaretIndex(point);
            }
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            if (_presenter != null && e.Device.Captured == _presenter)
            {
                e.Device.Capture(null);
            }
        }

        protected override void UpdateDataValidation(AvaloniaProperty property, BindingNotification status)
        {
            if (property == TextProperty)
            {
                DataValidationErrors.SetError(this, status.Error);
            }
        }
        
        private int CoerceCaretIndex(int value) => CoerceCaretIndex(value, Text?.Length ?? 0);

        private int CoerceCaretIndex(int value, int length)
        {
            var text = Text;

            if (value < 0)
            {
                return 0;
            }
            else if (value > length)
            {
                return length;
            }
            else if (value > 0 && text[value - 1] == '\r' && text[value] == '\n')
            {
                return value + 1;
            }
            else
            {
                return value;
            }
        }

        private int DeleteCharacter(int index)
        {
            var start = index + 1;
            var text = Text;
            var c = text[index];
            var result = 1;

            if (c == '\n' && index > 0 && text[index - 1] == '\r')
            {
                --index;
                ++result;
            }
            else if (c == '\r' && index < text.Length - 1 && text[index + 1] == '\n')
            {
                ++start;
                ++result;
            }

            Text = text.Substring(0, index) + text.Substring(start);

            return result;
        }

        private void MoveHorizontal(int direction, InputModifiers modifiers)
        {
            var text = Text ?? string.Empty;
            var caretIndex = CaretIndex;

            if ((modifiers & InputModifiers.Control) == 0)
            {
                var index = caretIndex + direction;

                if (index < 0 || index > text.Length)
                {
                    return;
                }
                else if (index == text.Length)
                {
                    CaretIndex = index;
                    return;
                }

                var c = text[index];

                if (direction > 0)
                {
                    CaretIndex += (c == '\r' && index < text.Length - 1 && text[index + 1] == '\n') ? 2 : 1;
                }
                else
                {
                    CaretIndex -= (c == '\n' && index > 0 && text[index - 1] == '\r') ? 2 : 1;
                }
            }
            else
            {
                if (direction > 0)
                {
                    CaretIndex += StringUtils.NextWord(text, caretIndex) - caretIndex;
                }
                else
                {
                    CaretIndex += StringUtils.PreviousWord(text, caretIndex) - caretIndex;
                }
            }
        }

        private bool MoveVertical(int count, InputModifiers modifiers)
        {
            var formattedText = _presenter.FormattedText;
            var lines = formattedText.GetLines().ToList();
            var caretIndex = CaretIndex;
            var lineIndex = GetLine(caretIndex, lines) + count;

            if (lineIndex >= 0 && lineIndex < lines.Count)
            {
                var line = lines[lineIndex];
                var rect = formattedText.HitTestTextPosition(caretIndex);
                var y = count < 0 ? rect.Y : rect.Bottom;
                var point = new Point(rect.X, y + (count * (line.Height / 2)));
                var hit = formattedText.HitTestPoint(point);
                CaretIndex = hit.TextPosition + (hit.IsTrailing ? 1 : 0);
                return true;
            }
            else
            {
                return false;
            }
        }

        private void MoveHome(InputModifiers modifiers)
        {
            var text = Text ?? string.Empty;
            var caretIndex = CaretIndex;

            if ((modifiers & InputModifiers.Control) != 0)
            {
                caretIndex = 0;
            }
            else
            {
                var lines = _presenter.FormattedText.GetLines();
                var pos = 0;

                foreach (var line in lines)
                {
                    if (pos + line.Length > caretIndex || pos + line.Length == text.Length)
                    {
                        break;
                    }

                    pos += line.Length;
                }

                caretIndex = pos;
            }

            CaretIndex = caretIndex;
        }

        private void MoveEnd(InputModifiers modifiers)
        {
            var text = Text ?? string.Empty;
            var caretIndex = CaretIndex;

            if ((modifiers & InputModifiers.Control) != 0)
            {
                caretIndex = text.Length;
            }
            else
            {
                var lines = _presenter.FormattedText.GetLines();
                var pos = 0;

                foreach (var line in lines)
                {
                    pos += line.Length;

                    if (pos > caretIndex)
                    {
                        if (pos < text.Length)
                        {
                            --pos;
                            if (pos > 0 && text[pos - 1] == '\r' && text[pos] == '\n')
                            {
                                --pos;
                            }
                        }

                        break;
                    }
                }

                caretIndex = pos;
            }

            CaretIndex = caretIndex;
        }

        private void SelectAll()
        {
            SelectionStart = 0;
            SelectionEnd = Text?.Length ?? 0;
        }

        private bool DeleteSelection()
        {
            if (!IsReadOnly)
            {
                var selectionStart = SelectionStart;
                var selectionEnd = SelectionEnd;

                if (selectionStart != selectionEnd)
                {
                    var start = Math.Min(selectionStart, selectionEnd);
                    var end = Math.Max(selectionStart, selectionEnd);
                    var text = Text;
                    SetTextInternal(text.Substring(0, start) + text.Substring(end));
                    SelectionStart = SelectionEnd = CaretIndex = start;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return true;
            }
        }

        private string GetSelection()
        {
            var text = Text;
            if (string.IsNullOrEmpty(text))
                return "";
            var selectionStart = SelectionStart;
            var selectionEnd = SelectionEnd;
            var start = Math.Min(selectionStart, selectionEnd);
            var end = Math.Max(selectionStart, selectionEnd);
            if (start == end || (Text?.Length ?? 0) < end)
            {
                return "";
            }
            return text.Substring(start, end - start);
        }

        private int GetLine(int caretIndex, IList<FormattedTextLine> lines)
        {
            int pos = 0;
            int i;

            for (i = 0; i < lines.Count; ++i)
            {
                var line = lines[i];
                pos += line.Length;

                if (pos > caretIndex)
                {
                    break;
                }
            }

            return i;
        }

        private void SetTextInternal(string value)
        {
            try
            {
                _ignoreTextChanges = true;
                SetAndRaise(TextProperty, ref _text, value);
            }
            finally
            {
                _ignoreTextChanges = false;
            }
        }

        private void SetSelectionForControlBackspace(InputModifiers modifiers)
        {
            SelectionStart = CaretIndex;
            MoveHorizontal(-1, modifiers);
            SelectionEnd = CaretIndex;
        }

        private void SetSelectionForControlDelete(InputModifiers modifiers)
        {
            SelectionStart = CaretIndex;
            MoveHorizontal(1, modifiers);
            SelectionEnd = CaretIndex;
        }

        UndoRedoState UndoRedoHelper<UndoRedoState>.IUndoRedoHost.UndoRedoState
        {
            get { return new UndoRedoState(Text, CaretIndex); }
            set
            {
                Text = value.Text;
                SelectionStart = SelectionEnd = CaretIndex = value.CaretPosition;
            }
        }
    }
}
