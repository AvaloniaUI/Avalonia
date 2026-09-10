using System.Globalization;
using Avalonia.Controls;
using Avalonia.Styling;

namespace Avalonia.Themes.Fluent2.UnitTests;

public class LocalizationTests
{
    [AvaloniaFact]
    public void Control_strings_localize_to_current_ui_culture()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        var theme = new Fluent2Theme();
        var app = Application.Current!;
        app.Styles.Add(theme);
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("de-DE");
            Assert.True(app.TryGetResource("StringDatePickerYearText", ThemeVariant.Light, out var year));
            Assert.Equal("Jahr", year);
            Assert.True(app.TryGetResource("StringTextFlyoutPasteText", ThemeVariant.Light, out var paste));
            Assert.Equal("Einfügen", paste);

            // de-AT has no entry of its own; the culture walk lands on "de".
            CultureInfo.CurrentUICulture = new CultureInfo("de-AT");
            Assert.True(app.TryGetResource("StringTextFlyoutCutText", ThemeVariant.Light, out var cut));
            Assert.Equal("Ausschneiden", cut);

            // Untranslated cultures fall back to the invariant English resources.
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
            Assert.True(app.TryGetResource("StringDatePickerYearText", ThemeVariant.Light, out var invariantYear));
            Assert.Equal("year", invariantYear);
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
            app.Styles.Remove(theme);
        }
    }
}
