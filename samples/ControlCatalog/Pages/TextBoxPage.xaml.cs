using ControlCatalog.Controls;

namespace ControlCatalog.Pages
{
    public partial class TextBoxPage : SampleGalleryPage
    {
        internal static readonly SampleInfo[] Demos =
        {
            new(SampleGroups.Overview, "First Look",
                "A single-line TextBox with a placeholder and a live readout of its Text.",
                () => new TextBoxFirstLookPage()),

            new(SampleGroups.Appearance, "Placeholder and Inner Content",
                "PlaceholderText, the floating placeholder, a themed PlaceholderForeground, inner left and right content, and the clearButton and revealPasswordButton classes.",
                () => new TextBoxPlaceholderPage()),

            new(SampleGroups.Appearance, "Text Layout",
                "TextAlignment, TextWrapping, LineHeight and the MinLines and MaxLines height range, all on one box.",
                () => new TextBoxTextLayoutPage()),

            new(SampleGroups.Appearance, "Fonts and Complex Scripts",
                "Embedded font faces, font size and the input method, and CJK and right-to-left paragraphs.",
                () => new TextBoxFontsPage()),

            new(SampleGroups.Features, "Multiline Editing",
                "AcceptsReturn, AcceptsTab, the NewLine string and the Multiline soft-keyboard hint.",
                () => new TextBoxMultilinePage()),

            new(SampleGroups.Features, "Selection and Caret",
                "Setting the selection from XAML and from code, and styling the selection and the caret.",
                () => new TextBoxSelectionPage()),

            new(SampleGroups.Features, "Input Restrictions and Masks",
                "IsReadOnly, MaxLength, password characters, soft-keyboard hints and MaskedTextBox.",
                () => new TextBoxInputPage()),

            new(SampleGroups.Features, "Validation",
                "Errors raised by a model through INotifyDataErrorInfo, and errors set directly through DataValidationErrors.",
                () => new TextBoxValidationPage()),

            new(SampleGroups.Events, "Text, Clipboard and Undo",
                "TextChanging and TextChanged, the three cancellable clipboard events, and the undo stack.",
                () => new TextBoxEditingPage()),
        };

        public TextBoxPage()
        {
            InitializeComponent();
            Samples = Demos;
        }
    }
}
