using Avalonia.Blazor;

namespace ControlCatalog.Web;

public partial class App
{
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        
        using (AvaloniaBlazor.Lock())
        {
            BlazorSingleViewLifetimeExtensions.SetupWithBlazorSingleViewLifetime<ControlCatalog.App>();
        }
    }
}