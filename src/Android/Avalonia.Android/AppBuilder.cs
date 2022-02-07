using Avalonia.Controls;
using Avalonia.PlatformSupport;

namespace Avalonia
{
    public sealed class AppBuilder : AppBuilderBase<AppBuilder>
    {
        public AppBuilder() : base(new StandardRuntimePlatform(),
            builder => StandardRuntimePlatformServices.Register(builder.Instance?.GetType()?.Assembly))
        {

        }
    }
}
