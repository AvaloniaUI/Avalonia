using Avalonia.Web.Blazor;

namespace ControlCatalog.Web;

public partial class App
{
    protected override void OnParametersSet()
    {
        WebAppBuilder.Configure<ControlCatalog.App>()
            .SetupWithSingleViewLifetime();

        base.OnParametersSet();
    }
}
