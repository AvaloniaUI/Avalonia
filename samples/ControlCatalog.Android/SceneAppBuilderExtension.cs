using Avalonia;
using Avalonia.Controls;

namespace ControlCatalog.Android
{
    internal static class SceneAppBuilderExtension
    {
        public static AppBuilder UseScenes(this AppBuilder builder)
        {
            return builder.
                AfterSetup(_ =>
                {
                    MainSceneActivity.RegisterInManager();
                    SampleSceneActivity.RegisterInManager();
                });
        }
    }
}
