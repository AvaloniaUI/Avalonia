using Avalonia.Controls;
using Avalonia.Input.TextInput;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Xaml;

public class SpellCheckXamlTests : XamlTestBase
{
    [Fact]
    public void Spell_Check_Properties_Can_Be_Set_From_Xaml()
    {
        var xaml = @"<TextBox xmlns='https://github.com/avaloniaui'
                         SpellCheck.IsEnabled='True'
                         SpellCheck.Language='de-DE'
                         TextInputOptions.LocaleHints='fr-FR' />";

        var target = AvaloniaRuntimeXamlLoader.Parse<TextBox>(xaml);

        Assert.True(SpellCheck.GetIsEnabled(target));
        Assert.Equal("de-DE", SpellCheck.GetLanguage(target));
        Assert.Equal(new[] { "fr-FR" }, TextInputOptions.GetLocaleHints(target));
    }
}
