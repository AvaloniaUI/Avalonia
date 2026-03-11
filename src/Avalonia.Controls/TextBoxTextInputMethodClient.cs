using System;
using Avalonia.Controls.Presenters;
using Avalonia.Input.TextInput;
using Avalonia.Media.TextFormatting;
using Avalonia.Reactive;
using Avalonia.Utilities;

namespace Avalonia.Controls
{
    internal class TextBoxTextInputMethodClient : TextInputMethodClient
    {
        internal TextBox? Parent { get; set; }

        internal TextPresenter? Presenter { get; set; }
        public override Visual TextViewVisual => Presenter!;
        internal bool IsInChange { get; set; }
        internal bool HasSelectionChanged { get; set; }

        public override Rect CursorRectangle
        {
            get
            {
                if (Parent == null || Presenter == null)
                {
                    return default;
                }

                var transform = Presenter.TransformToVisual(Parent);

                if (transform == null)
                {
                    return default;
                }

                return Presenter.GetCursorRectangle().TransformToAABB(transform.Value);
            }
        }

        public override bool SupportsPreedit => true;

        public override bool SupportsSurroundingText => true;
        public override string SurroundingText
        {
            get
            {
                if (Presenter is null || Parent is null)
                {
                    return "";
                }

                if (Parent.CaretIndex != Presenter.CaretIndex)
                {
                    Presenter.SetCurrentValue(TextPresenter.CaretIndexProperty, Parent.CaretIndex);
                }

                if (Parent.Text != Presenter.Text)
                {
                    Presenter.SetCurrentValue(TextPresenter.TextProperty, Parent.Text);
                }

                var lineIndex = Presenter.TextLayout.GetLineIndexFromCharacterIndex(Presenter.CaretIndex, false);

                var textLine = Presenter.TextLayout.TextLines[lineIndex];

                var lineText = GetTextLineText(textLine);

                return lineText;
            }
        }
        public override TextSelection Selection
        {
            get
            {
                if (Presenter is null || Parent is null)
                {
                    return default;
                }

                var lineIndex = Presenter.TextLayout.GetLineIndexFromCharacterIndex(Parent.CaretIndex, false);

                var textLine = Presenter.TextLayout.TextLines[lineIndex];

                var lineStart = textLine.FirstTextSourceIndex;

                var selectionStart = Math.Max(0, Parent.SelectionStart - lineStart);

                var selectionEnd = Math.Max(0, Parent.SelectionEnd - lineStart);

                return new TextSelection(selectionStart, selectionEnd);
            }
            set
            {
                if (Parent is null || Presenter is null)
                {
                    return;
                }

                var lineIndex = Presenter.TextLayout.GetLineIndexFromCharacterIndex(Parent.CaretIndex, false);

                var textLine = Presenter.TextLayout.TextLines[lineIndex];

                var lineStart = textLine.FirstTextSourceIndex;

                var selectionStart = lineStart + value.Start;
                var selectionEnd = lineStart + value.End;

                Parent.SelectionStart = selectionStart;
                Parent.SelectionEnd = selectionEnd;

                RaiseSelectionChanged();
            }
        }
        private static string GetTextLineText(TextLine textLine)
        {
            if (textLine.Length == 0)
            {
                return string.Empty;
            }

            var builder = StringBuilderCache.Acquire(textLine.Length);

            foreach (var run in textLine.TextRuns)
            {
                if (run.Length > 0)
                {
                    builder.Append(run.Text.Span);
                }
            }

            var lineText = builder.ToString();

            StringBuilderCache.Release(builder);

            return lineText;
        }

        internal IDisposable BeginChange()
        {
            if (IsInChange)
                return Disposable.Empty;

            IsInChange = true;
            return Disposable.Create(RaiseEvents);
        }

        private void RaiseEvents()
        {
            IsInChange = false;

            if (HasSelectionChanged)
                RaiseSelectionChanged();

            HasSelectionChanged = false;
        }

        public override void ExecuteContextMenuAction(ContextMenuAction action)
        {
            base.ExecuteContextMenuAction(action);

            switch (action)
            {
                case ContextMenuAction.Copy:
                    Parent?.Copy();
                    break;
                case ContextMenuAction.Cut:
                    Parent?.Cut();
                    break;
                case ContextMenuAction.Paste:
                    Parent?.Paste();
                    break;
                case ContextMenuAction.SelectAll:
                    Parent?.SelectAll();
                    break;
            }
        }

        public override void SetPreeditText(string? preeditText) => SetPreeditText(preeditText, null);

        public override void SetPreeditText(string? preeditText, int? cursorPos)
        {
            if (Presenter == null || Parent == null)
            {
                return;
            }

            Presenter.SetCurrentValue(TextPresenter.PreeditTextProperty, preeditText);
            Presenter.SetCurrentValue(TextPresenter.PreeditTextCursorPositionProperty, cursorPos);
        }

        public void SetPresenter(TextPresenter? presenter, TextBox? parent)
        {
            if (Parent != null)
            {
                Parent.PropertyChanged -= OnParentPropertyChanged;
                Parent.Tapped -= OnParentTapped;
            }

            Parent = parent;

            if (Parent != null)
            {
                Parent.PropertyChanged += OnParentPropertyChanged;
                Parent.Tapped += OnParentTapped;
            }

            var oldPresenter = Presenter;

            if (oldPresenter != null)
            {
                oldPresenter.ClearValue(TextPresenter.PreeditTextProperty);

                oldPresenter.CaretBoundsChanged -= (s, e) => RaiseCursorRectangleChanged();
            }

            Presenter = presenter;

            if (Presenter != null)
            {
                Presenter.CaretBoundsChanged += (s, e) => RaiseCursorRectangleChanged();
            }

            RaiseTextViewVisualChanged();

            RaiseCursorRectangleChanged();
        }

        private void OnParentPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == TextBox.TextProperty)
            {
                RaiseSurroundingTextChanged();
            }

            if (e.Property == TextBox.SelectionStartProperty || e.Property == TextBox.SelectionEndProperty)
            {
                if (IsInChange)
                    HasSelectionChanged = true;
                else
                    RaiseSelectionChanged();
            }
        }

        private void OnParentTapped(object? sender, Input.TappedEventArgs e)
        {
            RaiseInputPaneActivationRequested();
        }

        public override string Text
        {
            get
            {
                return Presenter?.Text ?? "";
            }
        }

        public override TextSelection SelectionInText
        {
            get
            {
                if (Presenter is null || Parent is null)
                {
                    return default;
                }

                return new TextSelection(Parent.SelectionStart, Parent.SelectionEnd);
            }

            set
            {
                if (Parent is null || Presenter is null)
                {
                    return;
                }

                using var _ = BeginChange();

                Parent.SelectionStart = value.Start;
                Parent.SelectionEnd = value.End;
            }
        }

    }
}
