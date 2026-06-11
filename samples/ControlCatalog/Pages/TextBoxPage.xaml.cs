using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ControlCatalog.Pages
{
    public partial class TextBoxPage : ContentPage
    {
        private static readonly (string Group, string Title, string Description, Func<UserControl> Factory)[] Demos =
        {
            ("Essentials", "Basic Input",
                "Text entry states with read-only, validation, clear button, and custom context flyout.",
                () => new TextBoxBasicInputPage()),
            ("Essentials", "Multiline Text",
                "AcceptsReturn, wrapping, no wrapping, line height, and non-Latin text rendering.",
                () => new TextBoxMultilinePage()),
            ("Essentials", "Masked Input",
                "Mask patterns for constrained text entry.",
                () => new TextBoxMaskedInputPage()),

            ("Input Behavior", "Content Types",
                "Numeric, password, search, suggestions, return key, and sensitivity input options.",
                () => new TextBoxContentTypesPage()),
            ("Input Behavior", "Spell Check",
                "Compare default spell-check, explicit opt-out, search text, custom providers, and multiline misspellings.",
                () => new TextBoxSpellCheckPage()),
            ("Input Behavior", "IME",
                "Input method font sizing and explicit IME disablement.",
                () => new TextBoxImePage()),

            ("Appearance", "Placeholders",
                "Floating placeholders and placeholder foreground colors.",
                () => new TextBoxPlaceholdersPage()),
            ("Appearance", "Selection And Caret",
                "Text alignment, custom selection brushes, and custom caret brush.",
                () => new TextBoxSelectionPage()),
            ("Appearance", "Fonts",
                "Custom font loading with resm and avares paths.",
                () => new TextBoxFontsPage()),
        };

        public TextBoxPage()
        {
            InitializeComponent();
            NavigationPage.SetHasNavigationBar(this, false);
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object? sender, RoutedEventArgs e)
        {
            await SampleNav.PushAsync(NavigationDemoHelper.CreateGalleryHomePage(SampleNav, Demos), null);
        }
    }
}
