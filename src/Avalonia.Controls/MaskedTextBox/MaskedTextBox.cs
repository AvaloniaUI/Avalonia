using System;
using System.ComponentModel;
using Avalonia.Controls.MaskedTextBox.Enums;
using Avalonia.Controls.MaskedTextBox.Filters;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Styling;

namespace Avalonia.Controls.MaskedTextBox
{
    public class MaskedTextBox : TextBox, IStyleable
    {
        Type IStyleable.StyleKey => typeof(TextBox);
        private MaskedTextProvider _maskedTextProvider;
        #region Properties
        /// <summary>
        /// Gets the MaskTextProvider for the specified Mask
        /// </summary>
        public MaskedTextProvider MaskProvider
        {
            get
            {
                _maskedTextProvider ??= new MaskedTextProvider(Mask) { PromptChar = PromptChar };
                return _maskedTextProvider;
            }
        }

        /// <summary>
        /// Gets or sets the prompt char to apply to the TextBox mask
        /// </summary>
        public char PromptChar
        {
            get => GetValue(PromptCharProperty);
            set => SetValue(PromptCharProperty, value);
        }

        /// <summary>
        /// Dependency property to store the prompt char to apply to the TextBox mask
        /// </summary>
        public static readonly StyledProperty<char> PromptCharProperty =
             AvaloniaProperty.Register<MaskedTextBox, char>(nameof(PromptChar), '_');

        /// <summary>
        /// Gets or sets the mask to apply to the TextBox
        /// </summary>
        public string Mask
        {
            get => GetValue(MaskProperty);
            set => SetValue(MaskProperty, value);
        }

        /// <summary>
        /// Dependency property to store the mask to apply to the TextBox
        /// </summary>
        public static readonly StyledProperty<string> MaskProperty =
            AvaloniaProperty.Register<MaskedTextBox, string>(nameof(Mask));

        /// <summary>
        /// Gets the RegExFilter for the validation Mask.
        /// </summary>
        private DefaultFilter FilterValidator => FilterProvider.Instance.FilterForMaskedType(Filter);

        /// <summary>
        /// Gets a predefined filter for the specified RegExp
        /// </summary>
        public FilterType Filter
        {
            get => GetValue(FilterProperty);
            set => SetValue(FilterProperty, value);
        }

        /// <summary>
        /// Dependency property to store the filter to apply to the TextBox
        /// </summary>
        public static readonly StyledProperty<FilterType> FilterProperty =
             AvaloniaProperty.Register<MaskedTextBox, FilterType>(nameof(Filter), FilterType.Any);

        #endregion

        //force the text of the control to use the mask
        private object ForceText(object value)
        {

            if (!string.IsNullOrEmpty(Mask))
            {
                var provider = MaskProvider;
                if (provider is not null)
                {
                    provider.Set($@"{value}");
                    return provider.ToDisplayString();
                }
            }

            return value;
        }

        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
            if (change.Property == FilterProperty || change.Property == MaskProperty)
            {
                RefreshText(MaskProvider, 0);
            }
            if (change.Property == TextProperty)
            {
                ForceText(change.NewValue.Value);
            }
            base.OnPropertyChanged(change);
        }
        #region Overrides

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
                return;
            }

            var position = CaretIndex;
            var provider = MaskProvider;
            var ifIsPositionInMiddle = position < Text.Length;
            var keymap = AvaloniaLocator.Current.GetService<PlatformHotkeyConfiguration>();
            if (provider is not null)
            {
                if (ifIsPositionInMiddle)
                {
                    position = GetNextCharacterPosition(position);

                    //if (keymap.SelectionModifiers.IsKeyToggled(Key.Insert))
                    //{
                    //    if (provider.Replace(e.Text, position))
                    //    {
                    //        position++;
                    //    }
                    //}
                    //else
                    //{
                    if (provider.InsertAt(e.Text, position))
                    {
                        position++;
                    }
                    //}

                    position = GetNextCharacterPosition(position);
                }

                RefreshText(provider, position);
                e.Handled = true;
            }

            var textToText = ifIsPositionInMiddle ? Text.Insert(position, e.Text) : $@"{Text}{e.Text}";
            if (!FilterValidator.IsTextValid(textToText))
            {
                e.Handled = true;
            }
            base.OnTextInput(e);
        }

        /// <summary>
        /// override the key down to handle delete of a character
        /// </summary>
        /// <param name="e">Arguments for the event</param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            var provider = MaskProvider;
            if (provider is null)
            {
                return;
            }

            var position = CaretIndex;
            switch (e.Key)
            {
                case Key.Delete:
                    if (position < Text.Length)
                    {
                        if (provider.RemoveAt(position))
                        {
                            RefreshText(provider, position);
                        }

                        e.Handled = true;
                    }
                    break;
                case Key.Space:
                    if (provider.InsertAt(@" ", position))
                    {
                        RefreshText(provider, position);
                    }

                    e.Handled = true;
                    break;
                case Key.Back:
                    if (position > 0)
                    { 
                        if (provider.RemoveAt(position))
                        {
                            RefreshText(provider, position);
                        }
                        position--;
                    }
                    e.Handled = true;
                    break;
            }
        }

        #endregion

        #region Helper Methods

        //refreshes the text of the TextBox
        private void RefreshText(MaskedTextProvider provider, int position)
        {
            if (provider is not null)
            {
                Text = provider.ToDisplayString();
                CaretIndex = position;
            }
        }

        //gets the next position in the TextBox to move
        private int GetNextCharacterPosition(int startPosition)
        {
            if (MaskProvider is not null)
            {
                var position = MaskProvider.FindEditPositionFrom(startPosition, true);
                if (position != -1)
                {
                    return position;
                }
            }
            return startPosition;
        }

        #endregion
    }
}
