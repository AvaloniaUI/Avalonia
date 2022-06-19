using Avalonia.Web.Blazor;

namespace ControlCatalog.Web;

public partial class App
{
    protected override void OnParametersSet()
    {
        WebAppBuilder.Configure<ControlCatalog.App>()
            .AfterSetup(_ =>
            {
                ControlCatalog.Pages.EmbedSample.Implementation = new EmbedSampleWeb();
            })
            .SetupWithSingleViewLifetime();

        base.OnParametersSet();
    }
}
