using System;
using System.ComponentModel;
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
        public MaskedTextProvider MaskProvider => _maskedTextProvider ??= new MaskedTextProvider(Mask) { PromptChar = PromptChar };

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

        #endregion

        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
            if (change.Property == MaskProperty)
            {
                RefreshText(MaskProvider, 0);
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
            if (provider is not null)
            {
                if (ifIsPositionInMiddle)
                {
                    position = GetNextCharacterPosition(position);

                    if (provider.InsertAt(e.Text, position))
                    {
                        position++;
                    }

                    position = GetNextCharacterPosition(position);
                }

                RefreshText(provider, position);
            }

            e.Handled = true;

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
