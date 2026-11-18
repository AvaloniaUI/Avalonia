using System;
using System.Collections.Generic;
using Avalonia.Controls.Presenters;
using Avalonia.Input.TextInput;
using Avalonia.Media.TextFormatting;
using Avalonia.Reactive;

namespace Avalonia.Controls
{
    /// <summary>
    /// The input method client of a <see cref="TextBox"/>. Platform input methods compose in the
    /// TextBox's document through <see cref="IStructuredTextInput"/>; legacy backends that only set a
    /// preedit string land in the same composition.
    /// </summary>
    /// <remarks>
    /// Navigation is the TextBox's own <see cref="TextBoxTextNavigation"/> over its real text. Every
    /// edit goes through the TextBox's input rules, and one composition is one undo step.
    /// </remarks>
    internal class TextBoxTextInputMethodClient : TextInputMethodClient, IStructuredTextInput
    {
        private TextBox? _parent;
        private TextPresenter? _presenter;
        private bool _selectionChanged;
        private bool _isInChange;
        private EventHandler? _caretBoundsChangedHandler;
        private TextBoxTextNavigation? _navigation;
        private ITrackedTextRange? _composition;
        private string _replacedByComposition = string.Empty;
        private bool _compositionUndoStepTaken;
        private bool _applyingEdit;
        private IReadOnlyList<TextInputDecoration> _inputDecorations = Array.Empty<TextInputDecoration>();

        public event EventHandler? CaretPositionChanged;
        public event EventHandler? CompositionChanged;
        public event EventHandler? IsReadOnlyChanged;
        public event EventHandler? InputDecorationsChanged;

        public override Visual TextViewVisual => _presenter!;

        public override string SurroundingText
        {
            get
            {
                if (_parent is null)
                {
                    return "";
                }

                if (_presenter is not null && _parent.CaretIndex != _presenter.CaretIndex)
                {
                    _presenter.SetCurrentValue(TextPresenter.CaretIndexProperty, _parent.CaretIndex);
                }

                if (_presenter is not null && _parent.Text != _presenter.Text)
                {
                    _presenter.SetCurrentValue(TextPresenter.TextProperty, _parent.Text);
                }

                return _parent.Text ?? string.Empty;
            }
        }

        public override Rect CursorRectangle
        {
            get
            {
                if (_parent == null || _presenter == null)
                {
                    return default;
                }

                var transform = _presenter.TransformToVisual(_parent);

                if (transform == null)
                {
                    return default;
                }

                return _presenter.CursorRectangle.TransformToAABB(transform.Value);
            }
        }

        public override TextSelection Selection
        {
            get
            {
                if (_parent is null)
                {
                    return default;
                }

                return new TextSelection(_parent.SelectionStart, _parent.SelectionEnd);
            }
            set
            {
                if (_parent is null)
                {
                    return;
                }

                using (BeginChange())
                {
                    _parent.SelectionStart = value.Start;
                    _parent.SelectionEnd = value.End;
                }
            }
        }

        // A backend that only sets a preedit string still composes in the document, so it may keep
        // delivering preedit text.
        public override bool SupportsPreedit => true;

        public override bool SupportsSurroundingText => true;

        internal bool HasActiveComposition => _composition is not null;

        private TextBoxTextNavigation Navigation =>
            _navigation ?? throw new InvalidOperationException("The input method client is not attached to a TextBox.");

        public void SetPresenter(TextPresenter? presenter, TextBox? parent)
        {
            // A composition does not outlive the presenter it was drawn in; its text stays.
            if (_composition is not null && (presenter is null || !ReferenceEquals(parent, _parent)))
            {
                EndComposition();
            }

            if (_parent != null)
            {
                _parent.PropertyChanged -= OnParentPropertyChanged;
                _parent.Tapped -= OnParentTapped;
            }

            _parent = parent;

            if (_parent != null)
            {
                _parent.PropertyChanged += OnParentPropertyChanged;
                _parent.Tapped += OnParentTapped;
                EnsureNavigation(_parent);
            }

            var oldPresenter = _presenter;

            if (oldPresenter != null)
            {
                oldPresenter.CurrentImClient = null;
                oldPresenter.ClearValue(TextPresenter.PreeditTextProperty);
                oldPresenter.SetCompositionRegion(null);

                if (_caretBoundsChangedHandler is not null)
                {
                    oldPresenter.CaretBoundsChanged -= _caretBoundsChangedHandler;
                }
            }

            _presenter = presenter;

            if (_presenter != null)
            {
                _presenter.CurrentImClient = this;
                _caretBoundsChangedHandler ??= OnPresenterCaretBoundsChanged;
                _presenter.CaretBoundsChanged += _caretBoundsChangedHandler;
                PushCompositionToPresenter();
            }

            RaiseTextViewVisualChanged();

            RaiseCursorRectangleChanged();
        }

        // The client belongs to one TextBox for its whole life, so the navigation is made once.
        private void EnsureNavigation(TextBox parent)
        {
            if (_navigation is not null)
            {
                return;
            }

            _navigation = new TextBoxTextNavigation(parent, segmentAsDrawn: false);
            _navigation.TextChanged += OnDocumentTextChanged;
            _navigation.SelectionChanged += (_, _) => CaretPositionChanged?.Invoke(this, EventArgs.Empty);
        }

        // Layout moves the caret's rectangle, not its position, so only the geometry is reported.
        private void OnPresenterCaretBoundsChanged(object? sender, EventArgs e) => RaiseCursorRectangleChanged();

        private void OnParentTapped(object? sender, Input.TappedEventArgs e)
        {
            RaiseInputPaneActivationRequested();
        }

        public override void SetPreeditText(string? preeditText) => SetPreeditText(preeditText, null);

        public override void SetPreeditText(string? preeditText, int? cursorPos)
        {
            // A legacy backend's preedit becomes the in-document composition. An empty preedit cancels it.
            if (string.IsNullOrEmpty(preeditText))
            {
                ((IStructuredTextInput)this).SetCompositionText(null, 0);
                return;
            }

            ((IStructuredTextInput)this).SetCompositionText(preeditText, cursorPos ?? preeditText.Length);
        }

        ITextPointer IStructuredTextInput.CaretPosition => Navigation.CreatePointer(_parent?.CaretIndex ?? 0);

        DirectedTextRange IStructuredTextInput.Selection
        {
            get => Navigation.GetSelection();
            set => Navigation.SetSelection(value);
        }

        ITextRange? IStructuredTextInput.CompositionRange
        {
            get => _composition;
            set
            {
                if (value is null)
                {
                    EndComposition();
                    return;
                }

                var (start, end) = Navigation.SpanOf(value);
                BeginComposition(start, end);
                PushCompositionToPresenter();
                CompositionChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        bool IStructuredTextInput.IsReadOnly => _parent?.IsReadOnly ?? true;

        void IStructuredTextInput.ReplaceText(ITextRange range, string text)
        {
            if (_parent is not { IsReadOnly: false } parent)
            {
                return;
            }

            var (start, end) = Navigation.SpanOf(range);

            using var _ = parent.BeginChange();

            if (Apply(start, end, text, startsUndoStep: true) is { } caret)
            {
                MoveCaret(caret);
            }

            if (_composition is not null)
            {
                EndComposition();
            }
        }

        void IStructuredTextInput.SetCompositionText(string? text, int cursorOffset)
        {
            if (_parent is not { } parent)
            {
                return;
            }

            if (text is null)
            {
                CancelComposition();
                return;
            }

            if (parent.IsReadOnly)
            {
                return;
            }

            using var _ = parent.BeginChange();

            var started = false;
            if (_composition is null)
            {
                var selection = Navigation.GetSelection();
                BeginComposition(Navigation.OffsetOf(selection.Start), Navigation.OffsetOf(selection.End));
                started = true;
            }

            var (start, end) = Navigation.SpanOf(_composition!);
            if (Apply(start, end, text, startsUndoStep: !_compositionUndoStepTaken) is not { } compositionEnd)
            {
                return;
            }

            _compositionUndoStepTaken = true;
            TrackComposition(start, compositionEnd);
            MoveCaret(start + Math.Clamp(cursorOffset, 0, compositionEnd - start));
            PushCompositionToPresenter();

            if (started || end != compositionEnd)
            {
                CompositionChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        void IStructuredTextInput.CommitComposition(string? text)
        {
            if (_parent is not { } parent)
            {
                return;
            }

            using var _ = parent.BeginChange();

            if (_composition is null)
            {
                // A result without a composition is committed where the selection is, as typed text.
                if (!string.IsNullOrEmpty(text) && !parent.IsReadOnly)
                {
                    var selection = Navigation.GetSelection();
                    if (Apply(Navigation.OffsetOf(selection.Start), Navigation.OffsetOf(selection.End), text,
                            startsUndoStep: true) is { } caret)
                    {
                        MoveCaret(caret);
                    }
                }

                return;
            }

            if (text is not null && !parent.IsReadOnly)
            {
                var (start, end) = Navigation.SpanOf(_composition);
                if (Apply(start, end, text, startsUndoStep: !_compositionUndoStepTaken) is { } caret)
                {
                    MoveCaret(caret);
                }
            }

            EndComposition();
        }

        IReadOnlyList<TextInputDecoration> IStructuredTextInput.InputDecorations => _inputDecorations;

        void IStructuredTextInput.SetInputDecorations(IReadOnlyList<TextInputDecoration> decorations)
        {
            decorations ??= Array.Empty<TextInputDecoration>();

            // A decoration from another navigator is rejected before anything is painted.
            foreach (var decoration in decorations)
            {
                _ = Navigation.SpanOf(decoration.Range);
            }

            SetInputDecorationsCore(decorations);
        }

        private void SetInputDecorationsCore(IReadOnlyList<TextInputDecoration> decorations)
        {
            if (ReferenceEquals(_inputDecorations, decorations) ||
                (_inputDecorations.Count == 0 && decorations.Count == 0))
            {
                return;
            }

            _inputDecorations = decorations;
            PushCompositionToPresenter();
            InputDecorationsChanged?.Invoke(this, EventArgs.Empty);
        }

        Rect IStructuredTextInput.GetFirstRectForRange(ITextRange range)
        {
            if (_presenter is null)
            {
                return default;
            }

            var (start, end) = Navigation.SpanOf(range);
            if (start == end)
            {
                return ((IStructuredTextInput)this).GetCaretRect(Navigation.CreatePointer(start));
            }

            foreach (var rect in _presenter.TextLayout.HitTestTextRange(start, end - start))
            {
                return TransformPresenterRect(rect);
            }

            return default;
        }

        Rect IStructuredTextInput.GetCaretRect(ITextPointer position)
        {
            if (_presenter is null)
            {
                return default;
            }

            var rect = _presenter.TextLayout.HitTestTextPosition(Navigation.OffsetOf(position));

            return TransformPresenterRect(rect);
        }

        Rect[] IStructuredTextInput.GetSelectionRects(ITextRange range)
        {
            if (_presenter is null)
            {
                return Array.Empty<Rect>();
            }

            var (start, end) = Navigation.SpanOf(range);
            if (start == end)
            {
                return Array.Empty<Rect>();
            }

            var result = new List<Rect>();
            foreach (var rect in _presenter.TextLayout.HitTestTextRange(start, end - start))
            {
                result.Add(TransformPresenterRect(rect));
            }

            return result.ToArray();
        }

        ITextPointer? IStructuredTextInput.GetClosestPosition(Point point)
        {
            if (_presenter is null)
            {
                return null;
            }

            var hit = _presenter.TextLayout.HitTestPoint(TransformPointToPresenter(point));

            return Navigation.CreatePointer(
                hit.TextPosition, hit.IsTrailing ? LogicalDirection.Backward : LogicalDirection.Forward);
        }

        ITextPointer? IStructuredTextInput.GetClosestPosition(Point point, ITextRange withinRange)
        {
            var closest = ((IStructuredTextInput)this).GetClosestPosition(point);
            if (closest is null)
            {
                return null;
            }

            var (start, end) = Navigation.SpanOf(withinRange);
            return Navigation.CreatePointer(Math.Clamp(Navigation.OffsetOf(closest), start, end), closest.Gravity);
        }

        ITextRange? IStructuredTextInput.GetCharacterRangeAtPoint(Point point)
        {
            var closest = ((IStructuredTextInput)this).GetClosestPosition(point);
            if (closest is null)
            {
                return null;
            }

            // The whole user-perceived character, so a surrogate pair or a combining sequence is not split.
            return Navigation.GetRangeEnclosing(Navigation.WithGravity(closest, LogicalDirection.Forward), TextUnit.Character);
        }

        ITextPointer ITextNavigation.DocumentStart => Navigation.DocumentStart;

        ITextPointer ITextNavigation.DocumentEnd => Navigation.DocumentEnd;

        ITextRange ITextNavigation.DocumentRange => Navigation.DocumentRange;

        long ITextNavigation.DocumentVersion => Navigation.DocumentVersion;

        ITextPointer ITextNavigation.GetPosition(ITextPointer origin, TextUnit unit, int count)
            => Navigation.GetPosition(origin, unit, count);

        ITextPointer ITextNavigation.WithGravity(ITextPointer position, LogicalDirection gravity)
            => Navigation.WithGravity(position, gravity);

        ITextRange ITextNavigation.GetRangeEnclosing(ITextPointer position, TextUnit unit)
            => Navigation.GetRangeEnclosing(position, unit);

        ITextRange ITextNavigation.GetRange(ITextPointer a, ITextPointer b) => Navigation.GetRange(a, b);

        string ITextNavigation.GetText(ITextRange range) => Navigation.GetText(range);

        ILinearTextScope ITextNavigation.GetLinearScope(ITextPointer position) => Navigation.GetLinearScope(position);

        ITrackedTextRange ITextNavigation.TrackRange(ITextRange range) => Navigation.TrackRange(range);

        event EventHandler<TextChange>? ITextNavigation.TextChanged
        {
            add => Navigation.TextChanged += value;
            remove => Navigation.TextChanged -= value;
        }

        public override void ExecuteContextMenuAction(ContextMenuAction action)
        {
            base.ExecuteContextMenuAction(action);

            switch (action)
            {
                case ContextMenuAction.Copy:
                    _parent?.Copy();
                    break;
                case ContextMenuAction.Cut:
                    _parent?.Cut();
                    break;
                case ContextMenuAction.Paste:
                    _parent?.Paste();
                    break;
                case ContextMenuAction.SelectAll:
                    _parent?.SelectAll();
                    break;
            }
        }

        private void OnParentPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == TextBox.TextProperty)
            {
                RaiseSurroundingTextChanged();
            }

            if (e.Property == TextBox.IsReadOnlyProperty)
            {
                IsReadOnlyChanged?.Invoke(this, EventArgs.Empty);
            }

            if (e.Property == TextBox.SelectionStartProperty || e.Property == TextBox.SelectionEndProperty)
            {
                if (_isInChange)
                    _selectionChanged = true;
                else
                    RaiseSelectionChanged();
            }
        }

        // An edit that did not come from the input method (a binding, undo, an automation client)
        // invalidates what the input method believes the composition is, so the composition ends with
        // its text as it stands and the input method is asked to start over.
        private void OnDocumentTextChanged(object? sender, TextChange e)
        {
            if (_applyingEdit || _composition is null)
            {
                return;
            }

            EndComposition();
            RequestReset();
        }

        internal IDisposable BeginChange()
        {
            if (_isInChange)
                return Disposable.Empty;

            _isInChange = true;
            return Disposable.Create(RaiseEvents);
        }

        private void RaiseEvents()
        {
            _isInChange = false;

            if (_selectionChanged)
                RaiseSelectionChanged();

            _selectionChanged = false;
        }

        private int? Apply(int start, int end, string text, bool startsUndoStep)
        {
            _applyingEdit = true;
            try
            {
                return _parent?.ReplaceTextFromInput(start, end, text, startsUndoStep);
            }
            finally
            {
                _applyingEdit = false;
            }
        }

        private void MoveCaret(int offset)
        {
            _presenter?.MoveCaretToTextPosition(offset);
            _parent?.SetCurrentValue(TextBox.CaretIndexProperty, offset);

            // CaretIndex collapses the selection only when it changes, and a selected clause can already
            // end at the caret's new position.
            _parent?.SetCurrentValue(TextBox.SelectionStartProperty, offset);
            _parent?.SetCurrentValue(TextBox.SelectionEndProperty, offset);
        }

        private void BeginComposition(int start, int end)
        {
            var text = _parent?.Text ?? string.Empty;
            start = Math.Clamp(start, 0, text.Length);
            end = Math.Clamp(end, start, text.Length);

            _replacedByComposition = text.Substring(start, end - start);
            _compositionUndoStepTaken = false;
            TrackComposition(start, end);
        }

        // The composition is re-tracked after every update rather than left to follow the edit, so an
        // empty composition that just received its first text covers that text instead of collapsing.
        private void TrackComposition(int start, int end)
        {
            var previous = _composition;
            _composition = Navigation.TrackRange(Navigation.CreateRange(start, end));
            previous?.Dispose();
        }

        // Cancelling puts back what the composition replaced: nothing for a fresh composition, the
        // original text for a reconversion.
        private void CancelComposition()
        {
            if (_composition is null || _parent is not { } parent)
            {
                return;
            }

            using var _ = parent.BeginChange();

            if (_compositionUndoStepTaken && !parent.IsReadOnly)
            {
                var (start, end) = Navigation.SpanOf(_composition);
                if (Apply(start, end, _replacedByComposition, startsUndoStep: false) is { } caret)
                {
                    MoveCaret(caret);
                }
            }

            EndComposition();
        }

        private void EndComposition()
        {
            if (_composition is null)
            {
                return;
            }

            _composition.Dispose();
            _composition = null;
            _replacedByComposition = string.Empty;
            _compositionUndoStepTaken = false;

            // Transient decorations belong to the composition session, so they do not survive it.
            SetInputDecorationsCore(Array.Empty<TextInputDecoration>());
            PushCompositionToPresenter();
            CompositionChanged?.Invoke(this, EventArgs.Empty);
        }

        // The composition region always renders decorated: explicit clause decorations
        // when the IME supplied them, the default underline over the whole region
        // otherwise. The presenter draws from resolved offsets so it stays free of the
        // navigation vocabulary.
        private void PushCompositionToPresenter()
        {
            if (_presenter is null)
            {
                return;
            }

            if (_composition is not { } range)
            {
                _presenter.SetCompositionRegion(null);
                return;
            }

            var (start, end) = Navigation.SpanOf(range);
            var highlights = new List<TextPresenter.CompositionHighlight>();

            foreach (var decoration in _inputDecorations)
            {
                var (decorationStart, decorationEnd) = Navigation.SpanOf(decoration.Range);
                if (decorationStart >= decorationEnd)
                {
                    continue;
                }

                highlights.Add(new TextPresenter.CompositionHighlight(
                    decorationStart,
                    decorationEnd,
                    decoration.Background,
                    decoration.Underline,
                    decoration.Kind is TextInputDecorationKind.ConvertedTarget or TextInputDecorationKind.ReconversionTarget));
            }

            if (highlights.Count == 0 && start < end)
            {
                highlights.Add(new TextPresenter.CompositionHighlight(start, end, null, TextInputUnderline.None, false));
            }

            _presenter.SetCompositionRegion(highlights);
        }

        // The structured geometry contract is top-level coordinates, so rects go from the
        // presenter to the visual root and inbound points come back the same way.

        private Rect TransformPresenterRect(Rect rect)
        {
            if (_presenter is null || _presenter.VisualRoot is not Visual root)
            {
                return rect;
            }

            var transform = _presenter.TransformToVisual(root);
            if (transform is null)
            {
                return rect;
            }

            return rect.TransformToAABB(transform.Value);
        }

        private Point TransformPointToPresenter(Point point)
        {
            if (_presenter is null || _presenter.VisualRoot is not Visual root)
            {
                return point;
            }

            var transform = root.TransformToVisual(_presenter);
            if (transform is null)
            {
                return point;
            }

            return point.Transform(transform.Value);
        }
    }
}
