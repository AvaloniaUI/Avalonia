using Avalonia.Web.Blazor;

namespace ControlCatalog.Web;

public partial class App
{
    protected override void OnParametersSet()
    {
        BlazorSingleViewLifetimeExtensions.UseBlazor<ControlCatalog.App>()
            .SetupWithSingleViewLifetime();

        base.OnParametersSet();
    }
}
