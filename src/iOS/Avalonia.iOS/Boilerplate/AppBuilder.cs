using Avalonia.Controls;
using Avalonia.PlatformSupport;

namespace Avalonia
{
    public class AppBuilder : AppBuilderBase<AppBuilder>
    {
        public AppBuilder() : base(new StandardRuntimePlatform(),
            b => StandardRuntimePlatformServices.Register(b.ApplicationType.Assembly))
        {
            this.UseSkia().UseWindowingSubsystem(iOS.Platform.Register);
        }
    }
}
