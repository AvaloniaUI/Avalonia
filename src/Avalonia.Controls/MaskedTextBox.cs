using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace Avalonia.Controls
{
    public class MaskedTextBox : TextBox
    {
        public static readonly StyledProperty<bool> AsciiOnlyProperty =
             AvaloniaProperty.Register<MaskedTextBox, bool>(nameof(AsciiOnly));

        public static readonly StyledProperty<CultureInfo?> CultureProperty =
             AvaloniaProperty.Register<MaskedTextBox, CultureInfo?>(nameof(Culture), CultureInfo.CurrentCulture);

        public static readonly StyledProperty<bool> HidePromptOnLeaveProperty =
             AvaloniaProperty.Register<MaskedTextBox, bool>(nameof(HidePromptOnLeave));

        public static readonly DirectProperty<MaskedTextBox, bool?> MaskCompletedProperty =
             AvaloniaProperty.RegisterDirect<MaskedTextBox, bool?>(nameof(MaskCompleted), o => o.MaskCompleted);

        public static readonly DirectProperty<MaskedTextBox, bool?> MaskFullProperty =
             AvaloniaProperty.RegisterDirect<MaskedTextBox, bool?>(nameof(MaskFull), o => o.MaskFull);

        public static readonly StyledProperty<string?> MaskProperty =
             AvaloniaProperty.Register<MaskedTextBox, string?>(nameof(Mask), string.Empty);

        public static readonly StyledProperty<char> PromptCharProperty =
             AvaloniaProperty.Register<MaskedTextBox, char>(nameof(PromptChar), '_', coerce: CoercePromptChar);

        public static readonly StyledProperty<bool> ResetOnPromptProperty =
             AvaloniaProperty.Register<MaskedTextBox, bool>(nameof(ResetOnPrompt), true);

        public static readonly StyledProperty<bool> ResetOnSpaceProperty =
             AvaloniaProperty.Register<MaskedTextBox, bool>(nameof(ResetOnSpace), true);

        private bool _ignoreTextChanges;

        static MaskedTextBox()
        {
            PasswordCharProperty.OverrideMetadata<MaskedTextBox>(new('\0', coerce: CoercePasswordChar));
        }

        private static char CoercePasswordChar(AvaloniaObject sender, char baseValue)
        {
            if (!MaskedTextProvider.IsValidPasswordChar(baseValue))
            {
                throw new ArgumentException($"'{baseValue}' is not a valid value for PasswordChar.");
            }
            var textbox = (MaskedTextBox)sender;
            if (textbox.MaskProvider is { } maskProvider && baseValue == maskProvider.PromptChar)
            {
                // Prompt and password chars must be different.
                throw new InvalidOperationException("PasswordChar and PromptChar values cannot be the same.");
            }

            return baseValue;
        }

        private static char CoercePromptChar(AvaloniaObject sender, char baseValue)
        {
            if (!MaskedTextProvider.IsValidInputChar(baseValue))
            {
                throw new ArgumentException($"'{baseValue}' is not a valid value for PromptChar.");
            }
            if (baseValue == sender.GetValue(PasswordCharProperty))
            {
                throw new InvalidOperationException("PasswordChar and PromptChar values cannot be the same.");
            }

            return baseValue;
        }

        public MaskedTextBox() { }

        /// <summary>
        ///  Constructs the MaskedTextBox with the specified MaskedTextProvider object.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("AvaloniaProperty", 
            "AVP1012:An AvaloniaObject should use SetCurrentValue when assigning its own StyledProperty or AttachedProperty values", 
            Justification = "These values are being explicitly provided by a constructor parameter.")]
        public MaskedTextBox(MaskedTextProvider maskedTextProvider)
        {
            if (maskedTextProvider == null)
            {
                throw new ArgumentNullException(nameof(maskedTextProvider));
            }
            AsciiOnly = maskedTextProvider.AsciiOnly;
            Culture = maskedTextProvider.Culture;
            Mask = maskedTextProvider.Mask;
            PasswordChar = maskedTextProvider.PasswordChar;
            PromptChar = maskedTextProvider.PromptChar;
        }

        /// <summary>
        /// Gets or sets a value indicating if the masked text box is restricted to accept only ASCII characters.
        /// Default value is false.
        /// </summary>
        public bool AsciiOnly
        {
            get => GetValue(AsciiOnlyProperty);
            set => SetValue(AsciiOnlyProperty, value);
        }

        /// <summary>
        /// Gets or sets the culture information associated with the masked text box.
        /// </summary>
        public CultureInfo? Culture
        {
            get => GetValue(CultureProperty);
            set => SetValue(CultureProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating if the prompt character is hidden when the masked text box loses focus.
        /// </summary>
        public bool HidePromptOnLeave
        {
            get => GetValue(HidePromptOnLeaveProperty);
            set => SetValue(HidePromptOnLeaveProperty, value);
        }

        /// <summary>
        /// Gets or sets the mask to apply to the TextBox.
        /// </summary>
        public string? Mask
        {
            get => GetValue(MaskProperty);
            set => SetValue(MaskProperty, value);
        }

        /// <summary>
        ///  Specifies whether the test string required input positions, as specified by the mask, have
        ///  all been assigned.
        /// </summary>
        public bool? MaskCompleted
        {
            get => MaskProvider?.MaskCompleted;
        }

        /// <summary>
        ///  Specifies whether all inputs (required and optional) have been provided into the mask successfully.
        /// </summary>
        public bool? MaskFull
        {
            get => MaskProvider?.MaskFull;
        }

        /// <summary>
        /// Gets the MaskTextProvider for the specified Mask.
        /// </summary>
        public MaskedTextProvider? MaskProvider { get; private set; }

        /// <summary>
        /// Gets or sets the character used to represent the absence of user input in MaskedTextBox.
        /// </summary>
        public char PromptChar
        {
            get => GetValue(PromptCharProperty);
            set => SetValue(PromptCharProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating if selected characters should be reset when the prompt character is pressed.
        /// </summary>
        public bool ResetOnPrompt
        {
            get => GetValue(ResetOnPromptProperty);
            set => SetValue(ResetOnPromptProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating if selected characters should be reset when the space character is pressed.
        /// </summary>
        public bool ResetOnSpace
        {
            get => GetValue(ResetOnSpaceProperty);
            set => SetValue(ResetOnSpaceProperty, value);
        }

        protected override Type StyleKeyOverride => typeof(TextBox);

        /// <inheritdoc />
        protected override void OnGotFocus(GotFocusEventArgs e)
        {
            if (HidePromptOnLeave == true && MaskProvider != null)
            {
                SetCurrentValue(TextProperty, MaskProvider.ToDisplayString());
            }
            base.OnGotFocus(e);
        }

        /// <inheritdoc />
        protected override async void OnKeyDown(KeyEventArgs e)
        {
            if (MaskProvider == null)
            {
                base.OnKeyDown(e);
                return;
            }

            var keymap = Application.Current!.PlatformSettings?.HotkeyConfiguration;

            bool Match(List<KeyGesture> gestures) => gestures.Any(g => g.Matches(e));

            if (keymap is not null && Match(keymap.Paste))
            {
                var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;

                if (clipboard is null)
                    return;

                string? text = null;
                try
                {
                    text = await clipboard.GetTextAsync();
                }
                catch (TimeoutException)
                {
                    // Silently ignore.
                }

                if (text == null)
                    return;

                foreach (var item in text)
                {
                    var index = GetNextCharacterPosition(CaretIndex);
                    if (MaskProvider.InsertAt(item, index))
                    {
                        SetCurrentValue(CaretIndexProperty, ++index);
                    }
                }

                SetCurrentValue(TextProperty, MaskProvider.ToDisplayString());
                e.Handled = true;
                return;
            }

            if (e.Key != Key.Back)
            {
                base.OnKeyDown(e);
            }

            switch (e.Key)
            {
                case Key.Delete:
                    if (CaretIndex < Text?.Length)
                    {
                        if (MaskProvider.RemoveAt(CaretIndex))
                        {
                            RefreshText(MaskProvider, CaretIndex);
                        }

                        e.Handled = true;
                    }
                    break;
                case Key.Space:
                    if (!MaskProvider.ResetOnSpace || string.IsNullOrEmpty(SelectedText))
                    {
                        if (MaskProvider.InsertAt(" ", CaretIndex))
                        {
                            RefreshText(MaskProvider, CaretIndex);
                        }
                    }

                    e.Handled = true;
                    break;
                case Key.Back:
                    if (CaretIndex > 0)
                    {
                        MaskProvider.RemoveAt(CaretIndex - 1);
                    }
                    RefreshText(MaskProvider, CaretIndex - 1);
                    e.Handled = true;
                    break;
            }
        }

        /// <inheritdoc />
        protected override void OnLostFocus(RoutedEventArgs e)
        {
            if (HidePromptOnLeave && MaskProvider != null)
            {
                SetCurrentValue(TextProperty, MaskProvider.ToString(!HidePromptOnLeave, true));
            }
            base.OnLostFocus(e);
        }

        /// <inheritdoc />
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            void UpdateMaskProvider()
            {
                MaskProvider = new MaskedTextProvider(Mask!, Culture, true, PromptChar, PasswordChar, AsciiOnly) { ResetOnSpace = ResetOnSpace, ResetOnPrompt = ResetOnPrompt };
                if (Text != null)
                {
                    MaskProvider.Set(Text);
                }
                RefreshText(MaskProvider, 0);
            }
            if (change.Property == TextProperty && MaskProvider != null && _ignoreTextChanges == false)
            {
                if (string.IsNullOrEmpty(Text))
                {
                    MaskProvider.Clear();
                    RefreshText(MaskProvider, CaretIndex);
                    base.OnPropertyChanged(change);
                    return;
                }

                MaskProvider.Set(Text);
                RefreshText(MaskProvider, CaretIndex);
            }
            else if (change.Property == MaskProperty)
            {
                UpdateMaskProvider();

                if (!string.IsNullOrEmpty(Mask))
                {
                    foreach (var c in Mask!)
                    {
                        if (!MaskedTextProvider.IsValidMaskChar(c))
                        {
                            throw new ArgumentException("Specified mask contains characters that are not valid.");
                        }
                    }
                }
            }
            else if (change.Property == PasswordCharProperty)
            {
                if (MaskProvider != null && MaskProvider.PasswordChar != PasswordChar)
                {
                    UpdateMaskProvider();
                }
            }
            else if (change.Property == PromptCharProperty)
            {
                if (MaskProvider != null && MaskProvider.PromptChar != PromptChar)
                {
                    UpdateMaskProvider();
                }
            }
            else if (change.Property == ResetOnPromptProperty)
            {
                if (MaskProvider != null && change.GetNewValue<bool>() is { } newValue)
                {
                    MaskProvider.ResetOnPrompt = newValue;
                }
            }
            else if (change.Property == ResetOnSpaceProperty)
            {
                if (MaskProvider != null && change.GetNewValue<bool>() is { } newValue)
                {
                    MaskProvider.ResetOnSpace = newValue;
                }
            }
            else if (change.Property == AsciiOnlyProperty && MaskProvider != null && MaskProvider.AsciiOnly != AsciiOnly
                 || change.Property == CultureProperty && MaskProvider != null && !MaskProvider.Culture.Equals(Culture))
            {
                UpdateMaskProvider();
            }
            base.OnPropertyChanged(change);
        }

        /// <inheritdoc />
        protected override void OnTextInput(TextInputEventArgs e)
        {
            _ignoreTextChanges = true;
            try
            {
                if (IsReadOnly)
                {
                    e.Handled = true;
                    base.OnTextInput(e);
                    return;
                }
                if (MaskProvider == null)
                {
                    base.OnTextInput(e);
                    return;
                }
                if ((MaskProvider.ResetOnSpace && e.Text == " " || MaskProvider.ResetOnPrompt && e.Text == MaskProvider.PromptChar.ToString()) && !string.IsNullOrEmpty(SelectedText))
                {
                    if (SelectionStart > SelectionEnd ? MaskProvider.RemoveAt(SelectionEnd, SelectionStart - 1) : MaskProvider.RemoveAt(SelectionStart, SelectionEnd - 1))
                    {
                        SelectedText = string.Empty;
                    }
                }

                if (CaretIndex < Text?.Length)
                {
                    SetCurrentValue(CaretIndexProperty, GetNextCharacterPosition(CaretIndex));

                    if (MaskProvider.InsertAt(e.Text!, CaretIndex))
                    {
                        CaretIndex++;
                    }
                    var nextPos = GetNextCharacterPosition(CaretIndex);
                    if (nextPos != 0 && CaretIndex != Text.Length)
                    {
                        SetCurrentValue(CaretIndexProperty, nextPos);
                    }
                }

                RefreshText(MaskProvider, CaretIndex);


                e.Handled = true;

                base.OnTextInput(e);
            }
            finally
            {
                _ignoreTextChanges = false;
            }

        }

        private int GetNextCharacterPosition(int startPosition)
        {
            if (MaskProvider != null)
            {
                var position = MaskProvider.FindEditPositionFrom(startPosition, true);
                if (CaretIndex != -1)
                {
                    return position;
                }
            }
            return startPosition;
        }

        private void RefreshText(MaskedTextProvider? provider, int position)
        {
            if (provider != null)
            {
                SetCurrentValue(TextProperty, provider.ToDisplayString());
                SetCurrentValue(CaretIndexProperty, position);
            }
        }

    }
}
