using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Styling;
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
                         SpellCheck.Languages='en-US, es-ES'
                         TextInputOptions.LocaleHints='fr-FR' />";

        var target = AvaloniaRuntimeXamlLoader.Parse<TextBox>(xaml);

        Assert.True(SpellCheck.GetIsEnabled(target));
        Assert.Equal(new[] { "en-US", "es-ES" }, SpellCheck.GetLanguages(target));
        Assert.Equal(new[] { "fr-FR" }, TextInputOptions.GetLocaleHints(target));
    }

    [Fact]
    public void Spell_Check_Languages_Can_Be_Set_From_A_Style()
    {
        var xaml = @"<Style xmlns='https://github.com/avaloniaui' Selector='TextBox'>
                         <Setter Property='SpellCheck.Languages' Value='en-US, es-ES' />
                     </Style>";

        var style = AvaloniaRuntimeXamlLoader.Parse<Style>(xaml);
        var setter = Assert.IsType<Setter>(Assert.Single(style.Setters));

        Assert.Equal(new[] { "en-US", "es-ES" }, Assert.IsAssignableFrom<IReadOnlyList<string>>(setter.Value));
    }
}
