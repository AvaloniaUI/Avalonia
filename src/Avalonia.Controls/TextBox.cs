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
using Avalonia.Layout;
using Avalonia.Utilities;
using Avalonia.Controls.Metadata;
using Avalonia.Media.TextFormatting;
using Avalonia.Media.TextFormatting.Unicode;
using Avalonia.Automation.Peers;
using Avalonia.Threading;

namespace Avalonia.Controls
{
    /// <summary>
    /// Represents a control that can be used to display or edit unformatted text.
    /// </summary>
    [TemplatePart("PART_TextPresenter", typeof(TextPresenter))]
    [PseudoClasses(":empty")]
    public class TextBox : TemplatedControl, UndoRedoHelper<TextBox.UndoRedoState>.IUndoRedoHost
    {
        public static KeyGesture? CutGesture { get; } = AvaloniaLocator.Current
            .GetService<PlatformHotkeyConfiguration>()?.Cut.FirstOrDefault();

        public static KeyGesture? CopyGesture { get; } = AvaloniaLocator.Current
            .GetService<PlatformHotkeyConfiguration>()?.Copy.FirstOrDefault();

        public static KeyGesture? PasteGesture { get; } = AvaloniaLocator.Current
            .GetService<PlatformHotkeyConfiguration>()?.Paste.FirstOrDefault();

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

        public static readonly StyledProperty<char> PasswordCharProperty =
            AvaloniaProperty.Register<TextBox, char>(nameof(PasswordChar));

        public static readonly StyledProperty<IBrush?> SelectionBrushProperty =
            AvaloniaProperty.Register<TextBox, IBrush?>(nameof(SelectionBrush));

        public static readonly StyledProperty<IBrush?> SelectionForegroundBrushProperty =
            AvaloniaProperty.Register<TextBox, IBrush?>(nameof(SelectionForegroundBrush));

        public static readonly StyledProperty<IBrush?> CaretBrushProperty =
            AvaloniaProperty.Register<TextBox, IBrush?>(nameof(CaretBrush));

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

        public static readonly StyledProperty<int> MaxLengthProperty =
            AvaloniaProperty.Register<TextBox, int>(nameof(MaxLength), defaultValue: 0);

        public static readonly StyledProperty<int> MaxLinesProperty =
            AvaloniaProperty.Register<TextBox, int>(nameof(MaxLines), defaultValue: 0);

        public static readonly DirectProperty<TextBox, string?> TextProperty =
            TextBlock.TextProperty.AddOwnerWithDataValidation<TextBox>(
                o => o.Text,
                (o, v) => o.Text = v,
                defaultBindingMode: BindingMode.TwoWay,
                enableDataValidation: true);

        public static readonly StyledProperty<TextAlignment> TextAlignmentProperty =
            TextBlock.TextAlignmentProperty.AddOwner<TextBox>();

        /// <summary>
        /// Defines the <see cref="HorizontalAlignment"/> property.
        /// </summary>
        public static readonly StyledProperty<HorizontalAlignment> HorizontalContentAlignmentProperty =
            ContentControl.HorizontalContentAlignmentProperty.AddOwner<TextBox>();

        /// <summary>
        /// Defines the <see cref="VerticalAlignment"/> property.
        /// </summary>
        public static readonly StyledProperty<VerticalAlignment> VerticalContentAlignmentProperty =
            ContentControl.VerticalContentAlignmentProperty.AddOwner<TextBox>();

        public static readonly StyledProperty<TextWrapping> TextWrappingProperty =
            TextBlock.TextWrappingProperty.AddOwner<TextBox>();

        /// <summary>
        /// Defines see <see cref="TextPresenter.LineHeight"/> property.
        /// </summary>
        public static readonly StyledProperty<double> LineHeightProperty =
            TextBlock.LineHeightProperty.AddOwner<TextBox>();

        /// <summary>
        /// Defines see <see cref="TextBlock.LetterSpacing"/> property.
        /// </summary>
        public static readonly StyledProperty<double> LetterSpacingProperty =
            TextBlock.LetterSpacingProperty.AddOwner<TextBox>();

        public static readonly StyledProperty<string?> WatermarkProperty =
            AvaloniaProperty.Register<TextBox, string?>(nameof(Watermark));

        public static readonly StyledProperty<bool> UseFloatingWatermarkProperty =
            AvaloniaProperty.Register<TextBox, bool>(nameof(UseFloatingWatermark));

        public static readonly DirectProperty<TextBox, string> NewLineProperty =
            AvaloniaProperty.RegisterDirect<TextBox, string>(nameof(NewLine),
                textbox => textbox.NewLine, (textbox, newline) => textbox.NewLine = newline);

        public static readonly StyledProperty<object> InnerLeftContentProperty =
            AvaloniaProperty.Register<TextBox, object>(nameof(InnerLeftContent));

        public static readonly StyledProperty<object> InnerRightContentProperty =
            AvaloniaProperty.Register<TextBox, object>(nameof(InnerRightContent));

        public static readonly StyledProperty<bool> RevealPasswordProperty =
            AvaloniaProperty.Register<TextBox, bool>(nameof(RevealPassword));

        public static readonly DirectProperty<TextBox, bool> CanCutProperty =
            AvaloniaProperty.RegisterDirect<TextBox, bool>(
                nameof(CanCut),
                o => o.CanCut);

        public static readonly DirectProperty<TextBox, bool> CanCopyProperty =
            AvaloniaProperty.RegisterDirect<TextBox, bool>(
                nameof(CanCopy),
                o => o.CanCopy);

        public static readonly DirectProperty<TextBox, bool> CanPasteProperty =
            AvaloniaProperty.RegisterDirect<TextBox, bool>(
                nameof(CanPaste),
                o => o.CanPaste);

        public static readonly StyledProperty<bool> IsUndoEnabledProperty =
            AvaloniaProperty.Register<TextBox, bool>(
                nameof(IsUndoEnabled),
                defaultValue: true);

        public static readonly DirectProperty<TextBox, int> UndoLimitProperty =
            AvaloniaProperty.RegisterDirect<TextBox, int>(
                nameof(UndoLimit),
                o => o.UndoLimit,
                (o, v) => o.UndoLimit = v,
                unsetValue: -1);

        /// <summary>
        /// Defines the <see cref="CopyingToClipboard"/> event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> CopyingToClipboardEvent =
            RoutedEvent.Register<TextBox, RoutedEventArgs>(
                nameof(CopyingToClipboard), RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="CuttingToClipboard"/> event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> CuttingToClipboardEvent =
            RoutedEvent.Register<TextBox, RoutedEventArgs>(
                nameof(CuttingToClipboard), RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="PastingFromClipboard"/> event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> PastingFromClipboardEvent =
            RoutedEvent.Register<TextBox, RoutedEventArgs>(
                nameof(PastingFromClipboard), RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="TextChanged"/> event.
        /// </summary>
        public static readonly RoutedEvent<TextChangedEventArgs> TextChangedEvent =
            RoutedEvent.Register<TextBox, TextChangedEventArgs>(
                nameof(TextChanged), RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="TextChanging"/> event.
        /// </summary>
        public static readonly RoutedEvent<TextChangingEventArgs> TextChangingEvent =
            RoutedEvent.Register<TextBox, TextChangingEventArgs>(
                nameof(TextChanging), RoutingStrategies.Bubble);

        readonly struct UndoRedoState : IEquatable<UndoRedoState>
        {
            public string? Text { get; }
            public int CaretPosition { get; }

            public UndoRedoState(string? text, int caretPosition)
            {
                Text = text;
                CaretPosition = caretPosition;
            }

            public bool Equals(UndoRedoState other) => ReferenceEquals(Text, other.Text) || Equals(Text, other.Text);

            public override bool Equals(object? obj) => obj is UndoRedoState other && Equals(other);

            public override int GetHashCode() => Text?.GetHashCode() ?? 0;
        }

        private string? _text;
        private int _caretIndex;
        private int _selectionStart;
        private int _selectionEnd;
        private TextPresenter? _presenter;
        private TextBoxTextInputMethodClient _imClient = new TextBoxTextInputMethodClient();
        private UndoRedoHelper<UndoRedoState> _undoRedoHelper;
        private bool _isUndoingRedoing;
        private bool _canCut;
        private bool _canCopy;
        private bool _canPaste;
        private string _newLine = Environment.NewLine;
        private static readonly string[] invalidCharacters = new String[1] { "\u007f" };

        private int _wordSelectionStart = -1;
        private int _selectedTextChangesMadeSinceLastUndoSnapshot;
        private bool _hasDoneSnapshotOnce;
        private const int _maxCharsBeforeUndoSnapshot = 7;

        static TextBox()
        {
            FocusableProperty.OverrideDefaultValue(typeof(TextBox), true);
            TextInputMethodClientRequestedEvent.AddClassHandler<TextBox>((tb, e) =>
            {
                if (!tb.IsReadOnly)
                {
                    e.Client = tb._imClient;
                }
            });
        }

        public TextBox()
        {
            var horizontalScrollBarVisibility = Observable.CombineLatest(
                this.GetObservable(AcceptsReturnProperty),
                this.GetObservable(TextWrappingProperty),
                (acceptsReturn, wrapping) =>
                {
                    if (wrapping != TextWrapping.NoWrap)
                    {
                        return ScrollBarVisibility.Disabled;
                    }

                    return acceptsReturn ? ScrollBarVisibility.Auto : ScrollBarVisibility.Hidden;
                });
            this.Bind(
                ScrollViewer.HorizontalScrollBarVisibilityProperty,
                horizontalScrollBarVisibility,
                BindingPriority.Style);
            _undoRedoHelper = new UndoRedoHelper<UndoRedoState>(this);
            _selectedTextChangesMadeSinceLastUndoSnapshot = 0;
            _hasDoneSnapshotOnce = false;
            UpdatePseudoclasses();
        }

        public bool AcceptsReturn
        {
            get => GetValue(AcceptsReturnProperty);
            set => SetValue(AcceptsReturnProperty, value);
        }

        public bool AcceptsTab
        {
            get => GetValue(AcceptsTabProperty);
            set => SetValue(AcceptsTabProperty, value);
        }

        public int CaretIndex
        {
            get => _caretIndex;
            set
            {
                value = CoerceCaretIndex(value);
                SetAndRaise(CaretIndexProperty, ref _caretIndex, value);

                UndoRedoState state;
                if (IsUndoEnabled && _undoRedoHelper.TryGetLastState(out state) && state.Text == Text)
                    _undoRedoHelper.UpdateLastState();

                SelectionStart = SelectionEnd = value;
            }
        }

        public bool IsReadOnly
        {
            get => GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        public char PasswordChar
        {
            get => GetValue(PasswordCharProperty);
            set => SetValue(PasswordCharProperty, value);
        }

        public IBrush? SelectionBrush
        {
            get => GetValue(SelectionBrushProperty);
            set => SetValue(SelectionBrushProperty, value);
        }

        public IBrush? SelectionForegroundBrush
        {
            get => GetValue(SelectionForegroundBrushProperty);
            set => SetValue(SelectionForegroundBrushProperty, value);
        }

        public IBrush? CaretBrush
        {
            get => GetValue(CaretBrushProperty);
            set => SetValue(CaretBrushProperty, value);
        }

        public int SelectionStart
        {
            get => _selectionStart;
            set
            {
                value = CoerceCaretIndex(value);
                var changed = SetAndRaise(SelectionStartProperty, ref _selectionStart, value);

                if (changed)
                {
                    UpdateCommandStates();
                }

                if (SelectionEnd == value && CaretIndex != value)
                {
                    CaretIndex = value;
                }
            }
        }

        public int SelectionEnd
        {
            get => _selectionEnd;
            set
            {
                value = CoerceCaretIndex(value);
                var changed = SetAndRaise(SelectionEndProperty, ref _selectionEnd, value);

                if (changed)
                {
                    UpdateCommandStates();
                }

                if (SelectionStart == value && CaretIndex != value)
                {
                    CaretIndex = value;
                }
            }
        }

        public int MaxLength
        {
            get => GetValue(MaxLengthProperty);
            set => SetValue(MaxLengthProperty, value);
        }

        public int MaxLines
        {
            get => GetValue(MaxLinesProperty);
            set => SetValue(MaxLinesProperty, value);
        }

        public double LetterSpacing
        {
            get => GetValue(LetterSpacingProperty);
            set => SetValue(LetterSpacingProperty, value);
        }

        /// <summary>
        /// Gets or sets the line height.
        /// </summary>
        public double LineHeight
        {
            get => GetValue(LineHeightProperty);
            set => SetValue(LineHeightProperty, value);
        }

        [Content]
        public string? Text
        {
            get => _text;
            set
            {
                var caretIndex = CaretIndex;
                var selectionStart = SelectionStart;
                var selectionEnd = SelectionEnd;

                CaretIndex = CoerceCaretIndex(caretIndex, value);
                SelectionStart = CoerceCaretIndex(selectionStart, value);
                SelectionEnd = CoerceCaretIndex(selectionEnd, value);

                var textChanged = SetAndRaise(TextProperty, ref _text, value);

                if (textChanged && IsUndoEnabled && !_isUndoingRedoing)
                {
                    _undoRedoHelper.Clear();
                    SnapshotUndoRedo(); // so we always have an initial state
                }

                if (textChanged)
                {
                    RaiseTextChangeEvents();
                }
            }
        }

        public string SelectedText
        {
            get => GetSelection();
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    _selectedTextChangesMadeSinceLastUndoSnapshot++;
                    SnapshotUndoRedo(ignoreChangeCount: false);
                    DeleteSelection();
                }
                else
                {
                    HandleTextInput(value);
                }
            }
        }

        /// <summary>
        /// Gets or sets the horizontal alignment of the content within the control.
        /// </summary>
        public HorizontalAlignment HorizontalContentAlignment
        {
            get => GetValue(HorizontalContentAlignmentProperty);
            set => SetValue(HorizontalContentAlignmentProperty, value);
        }

        /// <summary>
        /// Gets or sets the vertical alignment of the content within the control.
        /// </summary>
        public VerticalAlignment VerticalContentAlignment
        {
            get => GetValue(VerticalContentAlignmentProperty);
            set => SetValue(VerticalContentAlignmentProperty, value);
        }

        public TextAlignment TextAlignment
        {
            get => GetValue(TextAlignmentProperty);
            set => SetValue(TextAlignmentProperty, value);
        }

        /// <summary>
        /// Gets or sets the placeholder or descriptive text that is displayed even if the <see cref="Text"/>
        /// property is not yet set.
        /// </summary>
        public string? Watermark
        {
            get => GetValue(WatermarkProperty);
            set => SetValue(WatermarkProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the <see cref="Watermark"/> will still be shown above the
        /// <see cref="Text"/> even after a text value is set.
        /// </summary>
        public bool UseFloatingWatermark
        {
            get => GetValue(UseFloatingWatermarkProperty);
            set => SetValue(UseFloatingWatermarkProperty, value);
        }

        public object InnerLeftContent
        {
            get => GetValue(InnerLeftContentProperty);
            set => SetValue(InnerLeftContentProperty, value);
        }

        public object InnerRightContent
        {
            get => GetValue(InnerRightContentProperty);
            set => SetValue(InnerRightContentProperty, value);
        }

        public bool RevealPassword
        {
            get => GetValue(RevealPasswordProperty);
            set => SetValue(RevealPasswordProperty, value);
        }

        public TextWrapping TextWrapping
        {
            get => GetValue(TextWrappingProperty);
            set => SetValue(TextWrappingProperty, value);
        }

        /// <summary>
        /// Gets or sets which characters are inserted when Enter is pressed. Default: <see cref="Environment.NewLine"/>
        /// </summary>
        public string NewLine
        {
            get => _newLine;
            set => SetAndRaise(NewLineProperty, ref _newLine, value);
        }

        /// <summary>
        /// Clears the current selection, maintaining the <see cref="CaretIndex"/>
        /// </summary>
        public void ClearSelection()
        {
            CaretIndex = SelectionStart;
        }

        /// <summary>
        /// Property for determining if the Cut command can be executed.
        /// </summary>
        public bool CanCut
        {
            get => _canCut;
            private set => SetAndRaise(CanCutProperty, ref _canCut, value);
        }

        /// <summary>
        /// Property for determining if the Copy command can be executed.
        /// </summary>
        public bool CanCopy
        {
            get => _canCopy;
            private set => SetAndRaise(CanCopyProperty, ref _canCopy, value);
        }

        /// <summary>
        /// Property for determining if the Paste command can be executed.
        /// </summary>
        public bool CanPaste
        {
            get => _canPaste;
            private set => SetAndRaise(CanPasteProperty, ref _canPaste, value);
        }

        /// <summary>
        /// Property for determining whether undo/redo is enabled
        /// </summary>
        public bool IsUndoEnabled
        {
            get => GetValue(IsUndoEnabledProperty);
            set => SetValue(IsUndoEnabledProperty, value);
        }

        public int UndoLimit
        {
            get => _undoRedoHelper.Limit;
            set
            {
                if (_undoRedoHelper.Limit != value)
                {
                    // can't use SetAndRaise due to using _undoRedoHelper.Limit
                    // (can't send a ref of a property to SetAndRaise),
                    // so use RaisePropertyChanged instead.
                    var oldValue = _undoRedoHelper.Limit;
                    _undoRedoHelper.Limit = value;
                    RaisePropertyChanged(UndoLimitProperty, oldValue, value);
                }
                // from docs at
                // https://docs.microsoft.com/en-us/dotnet/api/system.windows.controls.primitives.textboxbase.isundoenabled:
                // "Setting UndoLimit clears the undo queue."
                _undoRedoHelper.Clear();
                _selectedTextChangesMadeSinceLastUndoSnapshot = 0;
                _hasDoneSnapshotOnce = false;
            }
        }

        public event EventHandler<RoutedEventArgs>? CopyingToClipboard
        {
            add => AddHandler(CopyingToClipboardEvent, value);
            remove => RemoveHandler(CopyingToClipboardEvent, value);
        }

        public event EventHandler<RoutedEventArgs>? CuttingToClipboard
        {
            add => AddHandler(CuttingToClipboardEvent, value);
            remove => RemoveHandler(CuttingToClipboardEvent, value);
        }

        public event EventHandler<RoutedEventArgs>? PastingFromClipboard
        {
            add => AddHandler(PastingFromClipboardEvent, value);
            remove => RemoveHandler(PastingFromClipboardEvent, value);
        }

        /// <summary>
        /// Occurs asynchronously after text changes and the new text is rendered.
        /// </summary>
        public event EventHandler<TextChangedEventArgs>? TextChanged
        {
            add => AddHandler(TextChangedEvent, value);
            remove => RemoveHandler(TextChangedEvent, value);
        }

        /// <summary>
        /// Occurs synchronously when text starts to change but before it is rendered.
        /// </summary>
        /// <remarks>
        /// This event occurs just after the <see cref="Text"/> property value has been updated.
        /// </remarks>
        public event EventHandler<TextChangingEventArgs>? TextChanging
        {
            add => AddHandler(TextChangingEvent, value);
            remove => RemoveHandler(TextChangingEvent, value);
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            _presenter = e.NameScope.Get<TextPresenter>("PART_TextPresenter");

            _imClient.SetPresenter(_presenter, this);

            if (IsFocused)
            {
                _presenter?.ShowCaret();
            }
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            if (IsFocused)
            {
                _presenter?.ShowCaret();
            }
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);

            _imClient.SetPresenter(null, null);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == TextProperty)
            {
                UpdatePseudoclasses();
                UpdateCommandStates();
            }
            else if (change.Property == IsUndoEnabledProperty && change.GetNewValue<bool>() == false)
            {
                // from docs at
                // https://docs.microsoft.com/en-us/dotnet/api/system.windows.controls.primitives.textboxbase.isundoenabled:
                // "Setting this property to false clears the undo stack.
                // Therefore, if you disable undo and then re-enable it, undo commands still do not work
                // because the undo stack was emptied when you disabled undo."
                _undoRedoHelper.Clear();
                _selectedTextChangesMadeSinceLastUndoSnapshot = 0;
                _hasDoneSnapshotOnce = false;
            }
        }

        private void UpdateCommandStates()
        {
            var text = GetSelection();
            var isSelectionNullOrEmpty = string.IsNullOrEmpty(text);
            CanCopy = !IsPasswordBox && !isSelectionNullOrEmpty;
            CanCut = !IsPasswordBox && !isSelectionNullOrEmpty && !IsReadOnly;
            CanPaste = !IsReadOnly;
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
                SelectAll();
            }

            UpdateCommandStates();

            _imClient.SetPresenter(_presenter, this);

            _presenter?.ShowCaret();
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);

            if ((ContextFlyout == null || !ContextFlyout.IsOpen) &&
                (ContextMenu == null || !ContextMenu.IsOpen))
            {
                ClearSelection();
                RevealPassword = false;
            }

            UpdateCommandStates();

            _presenter?.HideCaret();

            _imClient.SetPresenter(null, null);
        }

        protected override void OnTextInput(TextInputEventArgs e)
        {
            if (!e.Handled)
            {
                HandleTextInput(e.Text);
                e.Handled = true;
            }
        }

        private void HandleTextInput(string? input)
        {
            if (IsReadOnly)
            {
                return;
            }

            input = RemoveInvalidCharacters(input);

            if (string.IsNullOrEmpty(input))
            {
                return;
            }
            _selectedTextChangesMadeSinceLastUndoSnapshot++;
            SnapshotUndoRedo(ignoreChangeCount: false);

            if (_presenter != null && MaxLines > 0)
            {
                var lineCount = _presenter.TextLayout.TextLines.Count;

                var length = 0;

                var graphemeEnumerator = new GraphemeEnumerator(input.AsMemory());

                while (graphemeEnumerator.MoveNext())
                {
                    var grapheme = graphemeEnumerator.Current;

                    if (grapheme.FirstCodepoint.IsBreakChar)
                    {
                        if (lineCount + 1 > MaxLines)
                        {
                            break;
                        }
                        else
                        {
                            lineCount++;
                        }
                    }

                    length += grapheme.Text.Length;
                }

                if (length < input.Length)
                {
                    input = input.Remove(Math.Max(0, length));
                }
            }

            var text = Text ?? string.Empty;
            var newLength = input.Length + text.Length - Math.Abs(SelectionStart - SelectionEnd);

            if (MaxLength > 0 && newLength > MaxLength)
            {
                input = input.Remove(Math.Max(0, input.Length - (newLength - MaxLength)));
            }

            if (!string.IsNullOrEmpty(input))
            {
                var oldText = _text;

                DeleteSelection(false);
                var caretIndex = CaretIndex;
                text = Text ?? string.Empty;
                SetTextInternal(text.Substring(0, caretIndex) + input + text.Substring(caretIndex));
                ClearSelection();

                if (IsUndoEnabled)
                {
                    _undoRedoHelper.DiscardRedo();
                }

                if (_text != oldText)
                {
                    RaisePropertyChanged(TextProperty, oldText, _text);
                }

                CaretIndex = caretIndex + input.Length;
            }
        }

        public string? RemoveInvalidCharacters(string? text)
        {
            if (text is null)
                return null;

            for (var i = 0; i < invalidCharacters.Length; i++)
            {
                text = text.Replace(invalidCharacters[i], string.Empty);
            }

            return text;
        }

        public async void Cut()
        {
            var text = GetSelection();

            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            var eventArgs = new RoutedEventArgs(CuttingToClipboardEvent);
            RaiseEvent(eventArgs);
            if (!eventArgs.Handled)
            {
                SnapshotUndoRedo();
                await ((IClipboard)AvaloniaLocator.Current.GetRequiredService(typeof(IClipboard)))
                    .SetTextAsync(text);
                DeleteSelection();
            }
        }

        public async void Copy()
        {
            var text = GetSelection();

            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            var eventArgs = new RoutedEventArgs(CopyingToClipboardEvent);
            RaiseEvent(eventArgs);
            if (!eventArgs.Handled)
            {
                await ((IClipboard)AvaloniaLocator.Current.GetRequiredService(typeof(IClipboard)))
                    .SetTextAsync(text);
            }
        }

        public async void Paste()
        {
            var eventArgs = new RoutedEventArgs(PastingFromClipboardEvent);
            RaiseEvent(eventArgs);
            if (eventArgs.Handled)
            {
                return;
            }

            var text = await ((IClipboard)AvaloniaLocator.Current.GetRequiredService(typeof(IClipboard))).GetTextAsync();

            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            SnapshotUndoRedo();
            HandleTextInput(text);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (_presenter == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(_presenter.PreeditText))
            {
                return;
            }

            var text = Text ?? string.Empty;
            var caretIndex = CaretIndex;
            var movement = false;
            var selection = false;
            var handled = false;
            var modifiers = e.KeyModifiers;

            var keymap = AvaloniaLocator.Current.GetRequiredService<PlatformHotkeyConfiguration>();

            bool Match(List<KeyGesture> gestures) => gestures.Any(g => g.Matches(e));
            bool DetectSelection() => e.KeyModifiers.HasAllFlags(keymap.SelectionModifiers);

            if (Match(keymap.SelectAll))
            {
                SelectAll();
                handled = true;
            }
            else if (Match(keymap.Copy))
            {
                if (!IsPasswordBox)
                {
                    Copy();
                }

                handled = true;
            }
            else if (Match(keymap.Cut))
            {
                if (!IsPasswordBox)
                {
                    Cut();
                }

                handled = true;
            }
            else if (Match(keymap.Paste))
            {
                Paste();
                handled = true;
            }
            else if (Match(keymap.Undo) && IsUndoEnabled)
            {
                try
                {
                    SnapshotUndoRedo();
                    _isUndoingRedoing = true;
                    _undoRedoHelper.Undo();
                }
                finally
                {
                    _isUndoingRedoing = false;
                }

                handled = true;
            }
            else if (Match(keymap.Redo) && IsUndoEnabled)
            {
                try
                {
                    _isUndoingRedoing = true;
                    _undoRedoHelper.Redo();
                }
                finally
                {
                    _isUndoingRedoing = false;
                }

                handled = true;
            }
            else if (Match(keymap.MoveCursorToTheStartOfDocument))
            {
                MoveHome(true);
                movement = true;
                selection = false;
                handled = true;
                CaretIndex = _presenter.CaretIndex;
            }
            else if (Match(keymap.MoveCursorToTheEndOfDocument))
            {
                MoveEnd(true);
                movement = true;
                selection = false;
                handled = true;
                CaretIndex = _presenter.CaretIndex;
            }
            else if (Match(keymap.MoveCursorToTheStartOfLine))
            {
                MoveHome(false);
                movement = true;
                selection = false;
                handled = true;
                CaretIndex = _presenter.CaretIndex;
            }
            else if (Match(keymap.MoveCursorToTheEndOfLine))
            {
                MoveEnd(false);
                movement = true;
                selection = false;
                handled = true;
                CaretIndex = _presenter.CaretIndex;
            }
            else if (Match(keymap.MoveCursorToTheStartOfDocumentWithSelection))
            {
                SelectionStart = caretIndex;
                MoveHome(true);
                SelectionEnd = _presenter.CaretIndex;
                movement = true;
                selection = true;
                handled = true;
            }
            else if (Match(keymap.MoveCursorToTheEndOfDocumentWithSelection))
            {
                SelectionStart = caretIndex;
                MoveEnd(true);
                SelectionEnd = _presenter.CaretIndex;
                movement = true;
                selection = true;
                handled = true;
            }
            else if (Match(keymap.MoveCursorToTheStartOfLineWithSelection))
            {
                SelectionStart = caretIndex;
                MoveHome(false);
                SelectionEnd = _presenter.CaretIndex;
                movement = true;
                selection = true;
                handled = true;

            }
            else if (Match(keymap.MoveCursorToTheEndOfLineWithSelection))
            {
                SelectionStart = caretIndex;
                MoveEnd(false);
                SelectionEnd = _presenter.CaretIndex;
                movement = true;
                selection = true;
                handled = true;
            }
            else
            {
                bool hasWholeWordModifiers = modifiers.HasAllFlags(keymap.WholeWordTextActionModifiers);
                switch (e.Key)
                {
                    case Key.Left:
                        selection = DetectSelection();
                        MoveHorizontal(-1, hasWholeWordModifiers, selection);
                        movement = true;
                        break;

                    case Key.Right:
                        selection = DetectSelection();
                        MoveHorizontal(1, hasWholeWordModifiers, selection);
                        movement = true;
                        break;

                    case Key.Up:
                        {
                            selection = DetectSelection();

                            _presenter.MoveCaretVertical(LogicalDirection.Backward);

                            if (caretIndex != _presenter.CaretIndex)
                            {
                                movement = true;
                            }

                            if (selection)
                            {
                                SelectionEnd = _presenter.CaretIndex;
                            }
                            else
                            {
                                CaretIndex = _presenter.CaretIndex;
                            }

                            break;
                        }
                    case Key.Down:
                        {
                            selection = DetectSelection();

                            _presenter.MoveCaretVertical();

                            if (caretIndex != _presenter.CaretIndex)
                            {
                                movement = true;
                            }

                            if (selection)
                            {
                                SelectionEnd = _presenter.CaretIndex;
                            }
                            else
                            {
                                CaretIndex = _presenter.CaretIndex;
                            }

                            break;
                        }
                    case Key.Back:
                        {
                            SnapshotUndoRedo();

                            if (hasWholeWordModifiers && SelectionStart == SelectionEnd)
                            {
                                SetSelectionForControlBackspace();
                            }

                            if (!DeleteSelection())
                            {
                                var characterHit = _presenter.GetNextCharacterHit(LogicalDirection.Backward);

                                var backspacePosition = characterHit.FirstCharacterIndex + characterHit.TrailingLength;

                                if (caretIndex != backspacePosition)
                                {
                                    var start = Math.Min(backspacePosition, caretIndex);
                                    var end = Math.Max(backspacePosition, caretIndex);

                                    var length = end - start;

                                    var editedText = text.Substring(0, start) + text.Substring(Math.Min(end, text.Length));

                                    SetTextInternal(editedText);

                                    CaretIndex = start;
                                }
                            }

                            SnapshotUndoRedo();

                            handled = true;
                            break;
                        }
                    case Key.Delete:
                        SnapshotUndoRedo();

                        if (hasWholeWordModifiers && SelectionStart == SelectionEnd)
                        {
                            SetSelectionForControlDelete();
                        }

                        if (!DeleteSelection())
                        {
                            var characterHit = _presenter.GetNextCharacterHit();

                            var nextPosition = characterHit.FirstCharacterIndex + characterHit.TrailingLength;

                            if (nextPosition != caretIndex)
                            {
                                var start = Math.Min(nextPosition, caretIndex);
                                var end = Math.Max(nextPosition, caretIndex);

                                var editedText = text.Substring(0, start) + text.Substring(Math.Min(end, text.Length));

                                SetTextInternal(editedText);
                            }
                        }

                        SnapshotUndoRedo();

                        handled = true;
                        break;

                    case Key.Enter:
                        if (AcceptsReturn)
                        {
                            SnapshotUndoRedo();
                            HandleTextInput(NewLine);
                            handled = true;
                        }

                        break;

                    case Key.Tab:
                        if (AcceptsTab)
                        {
                            SnapshotUndoRedo();
                            HandleTextInput("\t");
                            handled = true;
                        }
                        else
                        {
                            base.OnKeyDown(e);
                        }

                        break;

                    case Key.Space:
                        SnapshotUndoRedo(); // always snapshot in between words
                        break;

                    default:
                        handled = false;
                        break;
                }
            }

            if (movement && !selection)
            {
                ClearSelection();
            }

            if (handled || movement)
            {
                e.Handled = true;
            }
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            if (_presenter == null || !string.IsNullOrEmpty(_presenter.PreeditText))
            {
                return;
            }

            var text = Text;
            var clickInfo = e.GetCurrentPoint(this);

            if (text != null && clickInfo.Properties.IsLeftButtonPressed &&
                !(clickInfo.Pointer?.Captured is Border))
            {
                var point = e.GetPosition(_presenter);

                var oldIndex = CaretIndex;

                _presenter.MoveCaretToPoint(point);

                var index = _presenter.CaretIndex;

                var clickToSelect = e.KeyModifiers.HasFlag(KeyModifiers.Shift);

                SetAndRaise(CaretIndexProperty, ref _caretIndex, index);

                switch (e.ClickCount)
                {
                    case 1:
                        if (clickToSelect)
                        {
                            if (_wordSelectionStart >= 0)
                            {
                                var previousWord = StringUtils.PreviousWord(text, index);

                                if (index > _wordSelectionStart)
                                {
                                    SelectionEnd = StringUtils.NextWord(text, index);
                                }

                                if (index < _wordSelectionStart || previousWord == _wordSelectionStart)
                                {
                                    SelectionStart = previousWord;
                                }
                            }
                            else
                            {
                                SelectionStart = Math.Min(oldIndex, index);
                                SelectionEnd = Math.Max(oldIndex, index);
                            }
                        }
                        else
                        {
                            if(_wordSelectionStart == -1 || index < SelectionStart || index > SelectionEnd)
                            {
                                SelectionStart = SelectionEnd = index;
                                _wordSelectionStart = -1;
                            }                           
                        }

                        break;
                    case 2:                       

                        if (!StringUtils.IsStartOfWord(text, index))
                        {
                            SelectionStart = StringUtils.PreviousWord(text, index);
                        }

                        _wordSelectionStart = SelectionStart;

                        SelectionEnd = StringUtils.NextWord(text, index);
                        break;
                    case 3:
                        _wordSelectionStart = -1;

                        SelectAll();
                        break;
                }
            }

            e.Pointer.Capture(_presenter);
            e.Handled = true;
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            if (_presenter == null)
            {
                return;
            }

            // selection should not change during pointer move if the user right clicks
            if (e.Pointer.Captured == _presenter && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                var point = e.GetPosition(_presenter);

                point = new Point(
                    MathUtilities.Clamp(point.X, 0, Math.Max(_presenter.Bounds.Width - 1, 0)),
                    MathUtilities.Clamp(point.Y, 0, Math.Max(_presenter.Bounds.Height - 1, 0)));

                _presenter.MoveCaretToPoint(point);  

                var caretIndex = _presenter.CaretIndex;

                var text = Text;

                if (text != null && _wordSelectionStart >= 0)
                {
                    var distance = caretIndex - _wordSelectionStart;

                    if (distance <= 0)
                    {
                        SelectionStart = StringUtils.PreviousWord(text, caretIndex);
                    }

                    if (distance >= 0)
                    {
                        if(SelectionStart != _wordSelectionStart)
                        {
                            SelectionStart = _wordSelectionStart;
                        }

                        SelectionEnd = StringUtils.NextWord(text, caretIndex);
                    }
                }
                else
                {
                    SelectionEnd = caretIndex;
                }
            }
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            if (_presenter == null)
            {
                return;
            }

            if (e.Pointer.Captured != _presenter)
            {
                return;
            }

            if (e.InitialPressMouseButton == MouseButton.Right)
            {
                var point = e.GetPosition(_presenter);

                _presenter.MoveCaretToPoint(point);

                var caretIndex = _presenter.CaretIndex;

                // see if mouse clicked inside current selection
                // if it did not, we change the selection to where the user clicked
                var firstSelection = Math.Min(SelectionStart, SelectionEnd);
                var lastSelection = Math.Max(SelectionStart, SelectionEnd);
                var didClickInSelection = SelectionStart != SelectionEnd &&
                                          caretIndex >= firstSelection && caretIndex <= lastSelection;
                if (!didClickInSelection)
                {
                    CaretIndex = SelectionEnd = SelectionStart = caretIndex;
                }
            }

            e.Pointer.Capture(null);
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new TextBoxAutomationPeer(this);
        }

        protected override void UpdateDataValidation(
            AvaloniaProperty property,
            BindingValueType state,
            Exception? error)
        {
            if (property == TextProperty)
            {
                DataValidationErrors.SetError(this, error);
            }
        }

        private int CoerceCaretIndex(int value) => CoerceCaretIndex(value, Text);

        private static int CoerceCaretIndex(int value, string? text)
        {
            if (text == null)
            {
                return 0;
            }
            var length = text.Length;

            if (value < 0)
            {
                return 0;
            }
            else if (value > length)
            {
                return length;
            }
            else if (value > 0 && text[value - 1] == '\r' && value < length && text[value] == '\n')
            {
                return value + 1;
            }
            else
            {
                return value;
            }
        }

        public void Clear()
        {
            Text = string.Empty;
        }

        private void MoveHorizontal(int direction, bool wholeWord, bool isSelecting)
        {
            if (_presenter == null)
            {
                return;
            }

            var text = Text ?? string.Empty;
            var selectionStart = SelectionStart;
            var selectionEnd = SelectionEnd;

            if (!wholeWord)
            {
                if (isSelecting)
                {
                    _presenter.MoveCaretToTextPosition(selectionEnd);

                    _presenter.MoveCaretHorizontal(direction > 0 ?
                        LogicalDirection.Forward :
                        LogicalDirection.Backward);

                    SelectionEnd = _presenter.CaretIndex;
                }
                else
                {
                    if (selectionStart != selectionEnd)
                    {
                        _presenter.MoveCaretToTextPosition(direction > 0 ?
                            Math.Max(selectionStart, selectionEnd) :
                            Math.Min(selectionStart, selectionEnd));
                    }
                    else
                    {
                        _presenter.MoveCaretHorizontal(direction > 0 ?
                            LogicalDirection.Forward :
                            LogicalDirection.Backward);
                    }

                    CaretIndex = _presenter.CaretIndex;
                }
            }
            else
            {
                int offset;

                if (direction > 0)
                {
                    offset = StringUtils.NextWord(text, selectionEnd) - selectionEnd;
                }
                else
                {
                    offset = StringUtils.PreviousWord(text, selectionEnd) - selectionEnd;
                }

                SelectionEnd += offset;

                _presenter.MoveCaretToTextPosition(SelectionEnd);

                if (!isSelecting)
                {
                    CaretIndex = SelectionEnd;
                }
                else
                {
                    SelectionStart = selectionStart;
                }
            }
        }

        private void MoveHome(bool document)
        {
            if (_presenter is null)
            {
                return;
            }

            var caretIndex = CaretIndex;

            if (document)
            {
                _presenter.MoveCaretToTextPosition(0);
            }
            else
            {
                var textLines = _presenter.TextLayout.TextLines;
                var lineIndex = _presenter.TextLayout.GetLineIndexFromCharacterIndex(caretIndex, false);
                var textLine = textLines[lineIndex];

                _presenter.MoveCaretToTextPosition(textLine.FirstTextSourceIndex);
            }
        }

        private void MoveEnd(bool document)
        {
            if (_presenter is null)
            {
                return;
            }

            var text = Text ?? string.Empty;
            var caretIndex = CaretIndex;

            if (document)
            {
                _presenter.MoveCaretToTextPosition(text.Length, true);
            }
            else
            {
                var textLines = _presenter.TextLayout.TextLines;
                var lineIndex = _presenter.TextLayout.GetLineIndexFromCharacterIndex(caretIndex, false);
                var textLine = textLines[lineIndex];

                var textPosition = textLine.FirstTextSourceIndex + textLine.Length;

                _presenter.MoveCaretToTextPosition(textPosition, true);
            }
        }

        /// <summary>
        /// Select all text in the TextBox
        /// </summary>
        public void SelectAll()
        {
            SelectionStart = 0;
            SelectionEnd = Text?.Length ?? 0;
        }

        internal bool DeleteSelection(bool raiseTextChanged = true)
        {
            if (IsReadOnly)
                return true;

            var selectionStart = SelectionStart;
            var selectionEnd = SelectionEnd;

            if (selectionStart != selectionEnd)
            {
                var start = Math.Min(selectionStart, selectionEnd);
                var end = Math.Max(selectionStart, selectionEnd);
                var text = Text!;

                SetTextInternal(text.Substring(0, start) + text.Substring(end), raiseTextChanged);

                _presenter?.MoveCaretToTextPosition(start);

                CaretIndex = start;

                ClearSelection();

                return true;
            }

            CaretIndex = SelectionStart;

            return false;
        }

        private string GetSelection()
        {
            var text = Text;

            if (string.IsNullOrEmpty(text))
            {
                return "";
            }

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

        /// <summary>
        /// Raises both the <see cref="TextChanging"/> and <see cref="TextChanged"/> events.
        /// </summary>
        /// <remarks>
        /// This must be called after the <see cref="Text"/> property is set.
        /// </remarks>
        private void RaiseTextChangeEvents()
        {
            // Note the following sequence of these events (following WinUI)
            // 1. TextChanging occurs synchronously when text starts to change but before it is rendered.
            //    This occurs after the Text property is set.
            // 2. TextChanged occurs asynchronously after text changes and the new text is rendered.

            var textChangingEventArgs = new TextChangingEventArgs(TextChangingEvent);
            RaiseEvent(textChangingEventArgs);

            Dispatcher.UIThread.Post(() =>
            {
                var textChangedEventArgs = new TextChangedEventArgs(TextChangedEvent);
                RaiseEvent(textChangedEventArgs);
            }, DispatcherPriority.Normal);
        }

        private void SetTextInternal(string value, bool raiseTextChanged = true)
        {
            if (raiseTextChanged)
            {
                bool textChanged = SetAndRaise(TextProperty, ref _text, value);

                if (textChanged)
                {
                    RaiseTextChangeEvents();
                }
            }
            else
            {
                _text = value;
            }
        }

        private void SetSelectionForControlBackspace()
        {
            var selectionStart = CaretIndex;

            MoveHorizontal(-1, true, false);

            SelectionStart = selectionStart;
        }

        private void SetSelectionForControlDelete()
        {
            if (_text == null || _presenter == null)
            {
                return;
            }

            SelectionStart = CaretIndex;

            MoveHorizontal(1, true, true);

            if (SelectionEnd < _text.Length && _text[SelectionEnd] == ' ')
            {
                SelectionEnd++;
            }
        }

        private void UpdatePseudoclasses()
        {
            PseudoClasses.Set(":empty", string.IsNullOrEmpty(Text));
        }

        private bool IsPasswordBox => PasswordChar != default(char);

        UndoRedoState UndoRedoHelper<UndoRedoState>.IUndoRedoHost.UndoRedoState
        {
            get => new UndoRedoState(Text, CaretIndex);
            set
            {
                Text = value.Text;
                CaretIndex = value.CaretPosition;
                ClearSelection();
            }
        }

        private void SnapshotUndoRedo(bool ignoreChangeCount = true)
        {
            if (IsUndoEnabled)
            {
                if (ignoreChangeCount ||
                    !_hasDoneSnapshotOnce ||
                    (!ignoreChangeCount &&
                        _selectedTextChangesMadeSinceLastUndoSnapshot >= _maxCharsBeforeUndoSnapshot))
                {
                    _undoRedoHelper.Snapshot();
                    _selectedTextChangesMadeSinceLastUndoSnapshot = 0;
                    _hasDoneSnapshotOnce = true;
                }
            }
        }
    }
}
