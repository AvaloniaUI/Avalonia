using System;
using Avalonia.Media.Fonts;

namespace Avalonia
{
    public static class SystemFontAppBuilderExtension
    {
        public static AppBuilder WithCustomSystemFont(this AppBuilder appBuilder, Uri fontSource)
        {
            return appBuilder.ConfigureFonts(fontManager =>
            {
                if(fontManager.SystemFonts is SystemFontCollection systemFontCollection)
                {
                    systemFontCollection.AddCustomFontSource(fontSource);
                }
            });
        }
    }
}
