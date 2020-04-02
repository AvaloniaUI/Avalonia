using Avalonia.Controls;
using Avalonia.Shared.PlatformSupport;

namespace Avalonia
{
    public class AppBuilder : AppBuilderBase<AppBuilder>
    {
        public AppBuilder() : base(new StandardRuntimePlatform(),
            builder => StandardRuntimePlatformServices.Register(builder.ApplicationType.Assembly))
        {

        }
    }
}
