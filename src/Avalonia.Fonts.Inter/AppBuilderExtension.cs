using Avalonia.Fonts.Inter;

namespace Avalonia
{
    public static class AppBuilderExtension
    {
        public static AppBuilder WithInterFont(this AppBuilder appBuilder)
        {
            return appBuilder.ConfigureFonts(fontManager =>
            {
                fontManager.AddFontCollection(new InterFontCollection());
            });
        }
    }
}
