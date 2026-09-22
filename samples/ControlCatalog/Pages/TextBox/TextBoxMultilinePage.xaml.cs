using System.Text;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ControlCatalog.Pages
{
    public partial class TextBoxMultilinePage : UserControl
    {
        public TextBoxMultilinePage()
        {
            InitializeComponent();

            NewLineCombo.SelectionChanged += OnNewLineChanged;
            NewLineBox.TextChanged += OnNewLineTextChanged;
            NewLineBox.Loaded += OnNewLineBoxLoaded;
            ApplyNewLine();
        }

        private void OnNewLineBoxLoaded(object? sender, RoutedEventArgs e)
        {
            NewLineBox.Loaded -= OnNewLineBoxLoaded;
            UpdateStatus();
        }

        private void OnNewLineChanged(object? sender, SelectionChangedEventArgs e)
        {
            ApplyNewLine();
        }

        private void OnNewLineTextChanged(object? sender, TextChangedEventArgs e)
        {
            UpdateStatus();
        }

        private void OnAppendLine(object? sender, RoutedEventArgs e)
        {
            NewLineBox.Text += NewLineBox.NewLine + "Appended from code";
            NewLineBox.CaretIndex = NewLineBox.Text?.Length ?? 0;
            NewLineBox.Focus();
        }

        private void ApplyNewLine()
        {
            NewLineBox.NewLine = NewLineCombo.SelectedIndex switch
            {
                1 => "\r\n",
                2 => "\r",
                _ => "\n"
            };

            UpdateStatus();
        }

        private void UpdateStatus()
        {
            // GetLineCount reports the laid out lines, so it is only meaningful once the box has a presenter.
            var lineCount = NewLineBox.GetLineCount();
            var lines = lineCount < 0 ? "not measured yet" : lineCount.ToString();

            NewLineStatus.Text =
                $"NewLine: {Escape(NewLineBox.NewLine)}     Visual lines: {lines}     Text length: {NewLineBox.Text?.Length ?? 0}";
        }

        private static string Escape(string value)
        {
            var builder = new StringBuilder();

            foreach (var character in value)
            {
                builder.Append(character switch
                {
                    '\r' => "\\r",
                    '\n' => "\\n",
                    '\t' => "\\t",
                    _ => character.ToString()
                });
            }

            return builder.ToString();
        }
    }
}
