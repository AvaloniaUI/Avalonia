using Avalonia.Controls;
using Avalonia.Platform;

namespace Avalonia.Web.Blazor
{
    public class AvaloniaBlazorAppBuilder : AppBuilderBase<AvaloniaBlazorAppBuilder>
    {
        public AvaloniaBlazorAppBuilder(IRuntimePlatform platform, Action<AvaloniaBlazorAppBuilder> platformServices)
            : base(platform, platformServices)
        {
        }

        public AvaloniaBlazorAppBuilder() : base(BlazorRuntimePlatform.Instance, BlazorRuntimePlatform.RegisterServices)
        {
            UseWindowingSubsystem(BlazorWindowingPlatform.Register);
        }
    }
}
