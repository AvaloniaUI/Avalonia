using Avalonia.Web.Blazor;

namespace ControlCatalog.Web;

public partial class App
{
    protected override void OnParametersSet()
    {
        BlazorSingleViewLifetimeExtensions.SetupWithBlazorSingleViewLifetime<ControlCatalog.App>();

        base.OnParametersSet();
    }
}
