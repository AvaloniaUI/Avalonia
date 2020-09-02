using Avalonia.Controls;
using Avalonia.iOS;
using Avalonia.Shared.PlatformSupport;

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