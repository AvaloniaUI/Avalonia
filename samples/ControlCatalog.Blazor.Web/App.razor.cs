using Avalonia;
using Avalonia.Web.Blazor;

namespace ControlCatalog.Blazor.Web;
using Calc;

public partial class App
{
    protected override void OnParametersSet()
    {
        AppBuilder.Configure<Calc.App>()
            .UseBlazor()
            // .With(new SkiaOptions { CustomGpuFactory = null }) // uncomment to disable GPU/GL rendering
            .SetupWithSingleViewLifetime();

        base.OnParametersSet();
    }
}
