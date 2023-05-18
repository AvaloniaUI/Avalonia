using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Styling;

namespace Avalonia.Diagnostics.Controls
{
    //TODO: UpdateSourceTrigger & Binding.ValidationRules could help removing the need for this control.
    internal sealed class CommitTextBox : TextBox
    {
        protected override Type StyleKeyOverride => typeof(TextBox);

        /// <summary>
        ///     Defines the <see cref="CommittedText" /> property.
        /// </summary>
        public static readonly DirectProperty<CommitTextBox, string?> CommittedTextProperty =
            AvaloniaProperty.RegisterDirect<CommitTextBox, string?>(
                nameof(CommittedText), o => o.CommittedText, (o, v) => o.CommittedText = v);

        private string? _committedText;

        public string? CommittedText
        {
            get => _committedText;
            set => SetAndRaise(CommittedTextProperty, ref _committedText, value);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == CommittedTextProperty)
            {
                Text = CommittedText;
            }
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);

            switch (e.Key)
            {
                case Key.Enter:

                    TryCommit();

                    e.Handled = true;

                    break;

                case Key.Escape:

                    Cancel();

                    e.Handled = true;

                    break;
            }
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);

            TryCommit();
        }

        private void Cancel()
        {
            Text = CommittedText;
            DataValidationErrors.ClearErrors(this);
        }

        private void TryCommit()
        {
            if (!DataValidationErrors.GetHasErrors(this))
            {
                CommittedText = Text;
            }
            else
            {
                Text = CommittedText;
                DataValidationErrors.ClearErrors(this);
            }
        }
    }
}
