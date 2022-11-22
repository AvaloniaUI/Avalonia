using Avalonia;
using Avalonia.Browser.Blazor;

namespace ControlCatalog.Browser.Blazor;

public partial class App
{
    protected override void OnParametersSet()
    {
        AppBuilder.Configure<ControlCatalog.App>()
            .UseBlazor()
            // .With(new SkiaOptions { CustomGpuFactory = null }) // uncomment to disable GPU/GL rendering
            .SetupWithSingleViewLifetime();

        base.OnParametersSet();
    }
}
