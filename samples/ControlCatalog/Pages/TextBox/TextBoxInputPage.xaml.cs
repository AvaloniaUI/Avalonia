using Avalonia;
using Avalonia.Controls;

namespace ControlCatalog.Pages
{
    public partial class TextBoxInputPage : UserControl
    {
        public TextBoxInputPage()
        {
            InitializeComponent();

            LengthBox.PropertyChanged += OnLengthBoxPropertyChanged;
            PasswordCharCombo.SelectionChanged += OnPasswordCharChanged;
            MaskCombo.SelectionChanged += OnMaskChanged;

            UpdateLengthStatus();
        }

        private void OnLengthBoxPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == TextBox.TextProperty || e.Property == TextBox.MaxLengthProperty)
            {
                UpdateLengthStatus();
            }
        }

        private void OnPasswordCharChanged(object? sender, SelectionChangedEventArgs e)
        {
            // The default char, '\0', means the text is drawn as it is typed.
            PasswordBox.PasswordChar = PasswordCharCombo.SelectedIndex switch
            {
                1 => '\u2022',
                2 => '\0',
                _ => '*'
            };
        }

        private void OnMaskChanged(object? sender, SelectionChangedEventArgs e)
        {
            MaskBox.Mask = MaskCombo.SelectedIndex switch
            {
                1 => "0000-00-00",
                2 => "LL-0000",
                _ => "(LLL) 999-0000"
            };
        }

        private void UpdateLengthStatus()
        {
            var length = LengthBox.Text?.Length ?? 0;
            var limit = LengthBox.MaxLength;

            LengthStatus.Text = limit > 0
                ? $"{length} / {limit} characters"
                : $"{length} characters, no limit";
        }
    }
}
