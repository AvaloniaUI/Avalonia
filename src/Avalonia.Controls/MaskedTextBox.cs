using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Styling;

namespace Avalonia.Controls
{
    public class MaskedTextBox : TextBox, IStyleable
    {

        #region Properties
        /// <summary>
        /// Dependency property to store the mask to apply to the TextBox
        /// </summary>
        public static readonly StyledProperty<bool> AllowPromptAsInputProperty =
            AvaloniaProperty.Register<MaskedTextBox, bool>(nameof(AllowPromptAsInput), true);

        /// <summary>
        /// Dependency property to store the mask to apply to the TextBox
        /// </summary>
        public static readonly StyledProperty<bool> AsciiOnlyProperty =
            AvaloniaProperty.Register<MaskedTextBox, bool>(nameof(AsciiOnly));


        public static readonly StyledProperty<CultureInfo> CultureProperty =
           AvaloniaProperty.Register<MaskedTextBox, CultureInfo>(nameof(Culture));

        public static readonly StyledProperty<bool> HidePromptOnLeaveProperty =
         AvaloniaProperty.Register<MaskedTextBox, bool>(nameof(HidePromptOnLeave));

        /// <summary>
        /// Dependency property to store the mask to apply to the TextBox
        /// </summary>
        public static readonly StyledProperty<string> MaskProperty =
            AvaloniaProperty.Register<MaskedTextBox, string>(nameof(Mask), string.Empty);

        public static new readonly StyledProperty<char> PasswordCharProperty =
            AvaloniaProperty.Register<TextBox, char>(nameof(PasswordChar));
        /// <summary>
        /// Dependency property to store the prompt char to apply to the TextBox mask
        /// </summary>
        public static readonly StyledProperty<char> PromptCharProperty =
             AvaloniaProperty.Register<MaskedTextBox, char>(nameof(PromptChar), '_');


        public bool AllowPromptAsInput
        {
            get => GetValue(AllowPromptAsInputProperty);
            set => SetValue(AllowPromptAsInputProperty, value);
        }

        /// <summary>
        /// Gets or sets the mask to apply to the TextBox
        /// </summary>
        public string Mask
        {
            get => GetValue(MaskProperty);
            set => SetValue(MaskProperty, value);
        }

        Type IStyleable.StyleKey => typeof(TextBox);

        /// <summary>
        /// Gets the MaskTextProvider for the specified Mask
        /// </summary>
        public MaskedTextProvider MaskProvider { get; private set; }

        public new char PasswordChar
        {
            get => GetValue(PasswordCharProperty);
            set => SetValue(PasswordCharProperty, value);
        }
        /// <summary>
        /// Gets or sets the prompt char to apply to the TextBox mask
        /// </summary>
        public char PromptChar
        {
            get => GetValue(PromptCharProperty);
            set => SetValue(PromptCharProperty, value);
        }

        public bool AsciiOnly
        {
            get => GetValue(AsciiOnlyProperty);
            set => SetValue(AsciiOnlyProperty, value);
        }

        public bool HidePromptOnLeave
        {
            get => GetValue(HidePromptOnLeaveProperty);
            set => SetValue(HidePromptOnLeaveProperty, value);
        }

        public CultureInfo Culture
        {
            get => GetValue(CultureProperty);
            set => SetValue(CultureProperty, value);
        }
        #endregion
        protected override void OnGotFocus(GotFocusEventArgs e)
        {
            if (HidePromptOnLeave == true)
            {
                Text = MaskProvider.ToDisplayString();
            }
            base.OnGotFocus(e);
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            if (HidePromptOnLeave == true)
            {
                Text = MaskProvider.ToString(!HidePromptOnLeave, true);
            }
            base.OnLostFocus(e);
        }

        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
            if (change.Property == MaskProperty)
            {
                if (MaskProvider == null && !string.IsNullOrEmpty(Mask))
                {
                    MaskProvider ??= new MaskedTextProvider(Mask, Culture, AllowPromptAsInput, PromptChar, PasswordChar, AsciiOnly);
                }
                RefreshText(MaskProvider, 0);
            }
            else if (change.Property == AllowPromptAsInputProperty && MaskProvider != null && MaskProvider.AllowPromptAsInput != AllowPromptAsInput
                  || change.Property == AsciiOnlyProperty && MaskProvider != null && MaskProvider.AsciiOnly != AsciiOnly
                  || change.Property == CultureProperty && MaskProvider != null && MaskProvider.Culture != Culture)
            {
                MaskProvider = new MaskedTextProvider(Mask, Culture, AllowPromptAsInput, PromptChar, PasswordChar, AsciiOnly);
            }
            base.OnPropertyChanged(change);
        }
        #region Overrides

        /// <summary>
        /// override the key down to handle delete of a character
        /// </summary>
        /// <param name="e">Arguments for the event</param>
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

        /// <summary>
        /// override this method to replace the characters entered with the mask
        /// </summary>
        /// <param name="e">Arguments for event</param>
        protected override void OnTextInput(TextInputEventArgs e)
        {
            //if the text is readonly do not add the text
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
        #endregion

        #region Helper Methods

        //gets the next position in the TextBox to move
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

        //refreshes the text of the TextBox
        private void RefreshText(MaskedTextProvider provider, int position)
        {
            if (provider is not null)
            {
                Text = provider.ToDisplayString();
                CaretIndex = position;
            }
        }
        #endregion
    }
}
