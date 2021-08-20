using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Styling;

#nullable enable

namespace Avalonia.Controls
{
    public class MaskedTextBox : TextBox, IStyleable
    {
        public MaskedTextBox() { }

        /// <summary>
        ///  Constructs the MaskedTextBox with the specified MaskedTextProvider object.
        /// </summary>
        public MaskedTextBox(MaskedTextProvider maskedTextProvider)
        {
            if (maskedTextProvider is null)
            {
                throw new ArgumentNullException(nameof(maskedTextProvider));
            }
            AllowPromptAsInput = maskedTextProvider.AllowPromptAsInput;
            AsciiOnly = maskedTextProvider.AsciiOnly;
            Culture = maskedTextProvider.Culture;
            Mask = maskedTextProvider.Mask;
            PasswordChar = maskedTextProvider.PasswordChar;
            PromptChar = maskedTextProvider.PromptChar;
        }
        public static readonly StyledProperty<bool> AllowPromptAsInputProperty =
            AvaloniaProperty.Register<MaskedTextBox, bool>(nameof(AllowPromptAsInput), true);

        public static readonly StyledProperty<bool> AsciiOnlyProperty =
            AvaloniaProperty.Register<MaskedTextBox, bool>(nameof(AsciiOnly));

        public static readonly StyledProperty<CultureInfo?> CultureProperty =
           AvaloniaProperty.Register<MaskedTextBox, CultureInfo?>(nameof(Culture));

        public static readonly StyledProperty<bool> HidePromptOnLeaveProperty =
         AvaloniaProperty.Register<MaskedTextBox, bool>(nameof(HidePromptOnLeave));

        public static readonly StyledProperty<string?> MaskProperty =
            AvaloniaProperty.Register<MaskedTextBox, string?>(nameof(Mask), string.Empty);

        public static new readonly StyledProperty<char> PasswordCharProperty =
            AvaloniaProperty.Register<TextBox, char>(nameof(PasswordChar), '\0');

        public static readonly StyledProperty<char> PromptCharProperty =
             AvaloniaProperty.Register<MaskedTextBox, char>(nameof(PromptChar), '_');


        public bool AllowPromptAsInput
        {
            get => GetValue(AllowPromptAsInputProperty);
            set => SetValue(AllowPromptAsInputProperty, value);
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
            get
            {
                return MaskProvider?.MaskCompleted;
            }
        }

        /// <summary>
        ///  Specifies whether all inputs (required and optional) have been provided into the mask successfully.
        /// </summary>
        public bool? MaskFull
        {
            get
            {
                return MaskProvider?.MaskFull;
            }
        }
        /// <summary>
        /// Gets the MaskTextProvider for the specified Mask.
        /// </summary>
        public MaskedTextProvider? MaskProvider { get; private set; }

        /// <summary>
        /// Gets or sets the character to be displayed in substitute for user input.
        /// </summary>
        public new char PasswordChar
        {
            get => GetValue(PasswordCharProperty);
            set => SetValue(PasswordCharProperty, value);
        }

        /// <summary>
        /// Gets or sets the character used to represent the absence of user input in MaskedTextBox.
        /// </summary>
        public char PromptChar
        {
            get => GetValue(PromptCharProperty);
            set => SetValue(PromptCharProperty, value);
        }

        Type IStyleable.StyleKey => typeof(TextBox);

        protected override void OnGotFocus(GotFocusEventArgs e)
        {
            if (HidePromptOnLeave == true && MaskProvider != null)
            {
                Text = MaskProvider.ToDisplayString();
            }
            base.OnGotFocus(e);
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            if (HidePromptOnLeave == true && MaskProvider != null)
            {
                Text = MaskProvider.ToString(!HidePromptOnLeave, true);
            }
            base.OnLostFocus(e);
        }

        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
            if (change.Property == MaskProperty)
            {
                MaskProvider = new MaskedTextProvider(Mask, Culture, AllowPromptAsInput, PromptChar, PasswordChar, AsciiOnly);
                RefreshText(MaskProvider, 0);

            }
            else if (change.Property == AllowPromptAsInputProperty && MaskProvider != null && MaskProvider.AllowPromptAsInput != AllowPromptAsInput
                  || change.Property == PasswordCharProperty && MaskProvider != null && MaskProvider.PasswordChar != PasswordChar
                  || change.Property == PromptCharProperty && MaskProvider != null && MaskProvider.PromptChar != PromptChar
                  || change.Property == AsciiOnlyProperty && MaskProvider != null && MaskProvider.AsciiOnly != AsciiOnly
                  || change.Property == CultureProperty && MaskProvider != null && !MaskProvider.Culture.Equals(Culture))
            {
                MaskProvider = new MaskedTextProvider(Mask, Culture, AllowPromptAsInput, PromptChar, PasswordChar, AsciiOnly);
                RefreshText(MaskProvider, 0);
            }
            base.OnPropertyChanged(change);
        }

        protected override async void OnKeyDown(KeyEventArgs e)
        {
            if (MaskProvider is null)
            {
                base.OnKeyDown(e);
                return;
            }
            var keymap = AvaloniaLocator.Current.GetService<PlatformHotkeyConfiguration>();

            bool Match(List<KeyGesture> gestures) => gestures.Any(g => g.Matches(e));

            if (Match(keymap.Paste))
            {
                var text = await ((IClipboard)AvaloniaLocator.Current.GetService(typeof(IClipboard))).GetTextAsync();

                if (text is null)
                    return;

                foreach (var item in text)
                {
                    var index = GetNextCharacterPosition(CaretIndex) - CaretIndex;
                    index = index == 0 ? CaretIndex : CaretIndex + index;
                    if (MaskProvider.InsertAt(item, index))
                    {
                        CaretIndex = ++index;
                    }
                }

                Text = MaskProvider.ToDisplayString();
                e.Handled = true;
                return;
            }
            base.OnKeyDown(e);
            switch (e.Key)
            {
                case Key.Delete:
                    if (CaretIndex < Text.Length)
                    {
                        if (MaskProvider.RemoveAt(CaretIndex))
                        {
                            RefreshText(MaskProvider, CaretIndex);
                        }

                        e.Handled = true;
                    }
                    break;
                case Key.Space:
                    if (MaskProvider.InsertAt(" ", CaretIndex))
                    {
                        RefreshText(MaskProvider, CaretIndex);
                    }

                    e.Handled = true;
                    break;
                case Key.Back:
                    if (CaretIndex > 0)
                    {
                        MaskProvider.RemoveAt(CaretIndex);
                    }
                    RefreshText(MaskProvider, CaretIndex);
                    e.Handled = true;
                    break;
            }
        }

        protected override void OnTextInput(TextInputEventArgs e)
        {

            if (IsReadOnly)
            {
                e.Handled = true;
                base.OnTextInput(e);
                return;
            }
            if (MaskProvider is null)
            {
                base.OnTextInput(e);
                return;
            }


            if (CaretIndex < Text.Length)
            {
                CaretIndex = GetNextCharacterPosition(CaretIndex);

                if (MaskProvider.InsertAt(e.Text, CaretIndex))
                {
                    CaretIndex++;
                }

                CaretIndex = GetNextCharacterPosition(CaretIndex);
            }

            RefreshText(MaskProvider, CaretIndex);


            e.Handled = true;

            base.OnTextInput(e);
        }

        private int GetNextCharacterPosition(int startPosition)
        {
            if (MaskProvider is not null)
            {
                var position = MaskProvider.FindEditPositionFrom(startPosition, true);
                if (CaretIndex != -1)
                {
                    return position;
                }
            }
            return startPosition;
        }

        private void RefreshText(MaskedTextProvider provider, int position)
        {
            if (provider is not null)
            {
                Text = provider.ToDisplayString();
                CaretIndex = position;
            }
        }

    }
}
