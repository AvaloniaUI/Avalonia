using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Media.Fonts;

namespace Avalonia
{
    public static class SystemFontAppBuilderExtension
    {
        public static AppBuilder WithCustomSystemFonts(this AppBuilder appBuilder, IReadOnlyList<FontFamily> customFontFamilies)
        {
            return appBuilder.ConfigureFonts(fontManager =>
            {
                if(fontManager.SystemFonts is SystemFontCollection systemFontCollection)
                {
                    systemFontCollection.AddCustomFontFamilies(customFontFamilies);
                }
            });
        }
    }
}
