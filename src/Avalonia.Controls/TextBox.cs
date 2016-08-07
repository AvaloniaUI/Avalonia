// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Input.Platform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
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
            AvaloniaProperty.Register<TextBox, bool>("AcceptsReturn");

        public static readonly StyledProperty<bool> AcceptsTabProperty =
            AvaloniaProperty.Register<TextBox, bool>("AcceptsTab");

        public static readonly DirectProperty<TextBox, bool> CanScrollHorizontallyProperty =
            AvaloniaProperty.RegisterDirect<TextBox, bool>("CanScrollHorizontally", o => o.CanScrollHorizontally);

        public static readonly DirectProperty<TextBox, int> CaretIndexProperty =
            AvaloniaProperty.RegisterDirect<TextBox, int>(
                nameof(CaretIndex),
                o => o.CaretIndex,
                (o, v) => o.CaretIndex = v);

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
                defaultBindingMode: BindingMode.TwoWay);

        public static readonly StyledProperty<TextAlignment> TextAlignmentProperty =
            TextBlock.TextAlignmentProperty.AddOwner<TextBox>();

        public static readonly StyledProperty<TextWrapping> TextWrappingProperty =
            TextBlock.TextWrappingProperty.AddOwner<TextBox>();

        public static readonly StyledProperty<string> WatermarkProperty =
            AvaloniaProperty.Register<TextBox, string>("Watermark");

        public static readonly StyledProperty<bool> UseFloatingWatermarkProperty =
            AvaloniaProperty.Register<TextBox, bool>("UseFloatingWatermark");

        public static readonly StyledProperty<bool> IsReadOnlyProperty =
            AvaloniaProperty.Register<TextBox, bool>(nameof(IsReadOnly));

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
        private bool _canScrollHorizontally;
        private TextPresenter _presenter;
        private UndoRedoHelper<UndoRedoState> _undoRedoHelper;

        static TextBox()
        {
            FocusableProperty.OverrideDefaultValue(typeof(TextBox), true);
        }

        public TextBox()
        {
            var canScrollHorizontally = this.GetObservable(TextWrappingProperty)
                .Select(x => x == TextWrapping.NoWrap)
                .Subscribe(x => CanScrollHorizontally = x);

            var horizontalScrollBarVisibility = this.GetObservable(AcceptsReturnProperty)
                .Select(x => x ? ScrollBarVisibility.Auto : ScrollBarVisibility.Hidden);

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

        public bool CanScrollHorizontally
        {
            get { return _canScrollHorizontally; }
            private set { SetAndRaise(CanScrollHorizontallyProperty, ref _canScrollHorizontally, value); }
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
                if (_undoRedoHelper.IsLastState && _undoRedoHelper.LastState.Text == Text)
                    _undoRedoHelper.UpdateLastState();
            }
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
            }
        }

        [Content]
        public string Text
        {
            get { return _text; }
            set { SetAndRaise(TextProperty, ref _text, value); }
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

        public bool IsReadOnly
        {
            get { return GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
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
        }

        protected override void OnGotFocus(GotFocusEventArgs e)
        {
            base.OnGotFocus(e);
            _presenter.ShowCaret();
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);
            SelectionStart = 0;
            SelectionEnd = 0;
            _presenter.HideCaret();
        }

        protected override void OnTextInput(TextInputEventArgs e)
        {
            HandleTextInput(e.Text);
        }

        protected override void DataValidationChanged(AvaloniaProperty property, BindingNotification status)
        {
            if (property == TextProperty)
            {
                UpdateValidationState(status);
            }
        }

        private void HandleTextInput(string input)
        {
            if (!IsReadOnly)
            {
                string text = Text ?? string.Empty;
                int caretIndex = CaretIndex;
                if (!string.IsNullOrEmpty(input))
                {
                    DeleteSelection();
                    caretIndex = CaretIndex;
                    text = Text ?? string.Empty;
                    Text = text.Substring(0, caretIndex) + input + text.Substring(caretIndex);
                    CaretIndex += input.Length;
                    SelectionStart = SelectionEnd = CaretIndex;
                    _undoRedoHelper.DiscardRedo();
                }
            }
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
            bool handled = true;
            var modifiers = e.Modifiers;

            switch (e.Key)
            {
                case Key.A:
                    if (modifiers == InputModifiers.Control)
                    {
                        SelectAll();
                    }

                    break;
                case Key.C:
                    if (modifiers == InputModifiers.Control)
                    {
                        Copy();
                    }
                    break;

                case Key.X:
                    if(modifiers == InputModifiers.Control)
                    {
                        Copy();
                        DeleteSelection();
                    }
                    break;

                case Key.V:
                    if (modifiers == InputModifiers.Control)
                    {
                        Paste();
                    }

                    break;

                case Key.Z:
                    if (modifiers == InputModifiers.Control)
                        _undoRedoHelper.Undo();

                    break;
                case Key.Y:
                    if (modifiers == InputModifiers.Control)
                        _undoRedoHelper.Redo();

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
                    MoveVertical(-1, modifiers);
                    movement = true;
                    break;

                case Key.Down:
                    MoveVertical(1, modifiers);
                    movement = true;
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
                    if (!DeleteSelection() && CaretIndex > 0)
                    {
                        Text = text.Substring(0, caretIndex - 1) + text.Substring(caretIndex);
                        --CaretIndex;
                    }

                    break;

                case Key.Delete:
                    if (!DeleteSelection() && caretIndex < text.Length)
                    {
                        Text = text.Substring(0, caretIndex) + text.Substring(caretIndex + 1);
                    }

                    break;

                case Key.Enter:
                    if (AcceptsReturn)
                    {
                        HandleTextInput("\r\n");
                    }

                    break;

                case Key.Tab:
                    if (AcceptsTab)
                    {
                        HandleTextInput("\t");
                    }
                    else
                    {
                        base.OnKeyDown(e);
                        handled = false;
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

            if (handled)
            {
                e.Handled = true;
            }
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            if (e.Source == _presenter)
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
                                SelectionStart = StringUtils.PreviousWord(text, index, false);
                            }

                            SelectionEnd = StringUtils.NextWord(text, index, false);
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
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            if (_presenter != null && e.Device.Captured == _presenter)
            {
                var point = e.GetPosition(_presenter);
                CaretIndex = SelectionEnd = _presenter.GetCaretIndex(point);
            }
        }

        protected override void OnPointerReleased(PointerEventArgs e)
        {
            if (_presenter != null && e.Device.Captured == _presenter)
            {
                e.Device.Capture(null);
            }
        }

        private int CoerceCaretIndex(int value)
        {
            var text = Text;
            var length = text?.Length ?? 0;
            return Math.Max(0, Math.Min(length, value));
        }

        private void MoveHorizontal(int count, InputModifiers modifiers)
        {
            var text = Text ?? string.Empty;
            var caretIndex = CaretIndex;

            if ((modifiers & InputModifiers.Control) != 0)
            {
                if (count > 0)
                {
                    count = StringUtils.NextWord(text, caretIndex, false) - caretIndex;
                }
                else
                {
                    count = StringUtils.PreviousWord(text, caretIndex, false) - caretIndex;
                }
            }

            CaretIndex = caretIndex += count;
        }

        private void MoveVertical(int count, InputModifiers modifiers)
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
            SelectionEnd = Text.Length;
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
                    Text = text.Substring(0, start) + text.Substring(end);
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
            var selectionStart = SelectionStart;
            var selectionEnd = SelectionEnd;
            var start = Math.Min(selectionStart, selectionEnd);
            var end = Math.Max(selectionStart, selectionEnd);
            if (start == end || (Text?.Length ?? 0) < end)
            {
                return "";
            }
            return Text.Substring(start, end - start);
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
