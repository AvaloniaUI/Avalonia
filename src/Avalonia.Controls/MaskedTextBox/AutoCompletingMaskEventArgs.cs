using System.ComponentModel;

namespace Avalonia.Controls.MaskedTextBox
{
    public class AutoCompletingMaskEventArgs : CancelEventArgs
    {
        public AutoCompletingMaskEventArgs(MaskedTextProvider maskedTextProvider, int startPosition, int selectionLength, string input)
        {
            MaskedTextProvider = maskedTextProvider;
            StartPosition = startPosition;
            SelectionLength = selectionLength;
            Input = input;
        }

        public MaskedTextProvider MaskedTextProvider { get; }

        public int StartPosition { get; }

        public int SelectionLength { get; }

        public string Input { get; }

        public int AutoCompleteStartPosition { get; set; } = -1;

        public string AutoCompleteText { get; set; }

    }
}
