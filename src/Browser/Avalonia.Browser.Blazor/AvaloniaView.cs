using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using System;
using Avalonia.Browser.Interop;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using BrowserView = Avalonia.Browser.AvaloniaView;

namespace Avalonia.Browser.Blazor;

[SupportedOSPlatform("browser")]
public class AvaloniaView : ComponentBase
{
    private Browser.AvaloniaView? _browserView;
    private readonly string _containerId;

    public AvaloniaView()
    {
        _containerId = "av_container_" + Guid.NewGuid();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "id", _containerId);
        builder.AddAttribute(2, "style", "width:100vw;height:100vh");
        builder.CloseElement();
    }

    protected override async Task OnInitializedAsync()
    {
        if (OperatingSystem.IsBrowser())
        {
            await AvaloniaModule.ImportMain();

            _browserView = new Browser.AvaloniaView(_containerId);
            if (Application.Current?.ApplicationLifetime is ISingleViewApplicationLifetime lifetime)
            {
                _browserView.Content = lifetime.MainView;
            }
        }
    }
}
