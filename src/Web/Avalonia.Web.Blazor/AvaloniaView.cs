using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using System;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using BrowserView = Avalonia.Web.AvaloniaView;

namespace Avalonia.Web.Blazor;

[SupportedOSPlatform("browser")]
public class AvaloniaView : ComponentBase
{
    private BrowserView? _browserView;
    private readonly string _containerId;

    public AvaloniaView()
    {
        _containerId = "av_container_" + Guid.NewGuid();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "id", _containerId);
        builder.CloseElement();
    }

    protected override async Task OnInitializedAsync()
    {
        if (OperatingSystem.IsBrowser())
        {
            _ = await JSHost.ImportAsync("avalonia", "/_content/Avalonia.Web.Blazor/avalonia.js");

            _browserView = new BrowserView(_containerId);
            if (Application.Current?.ApplicationLifetime is ISingleViewApplicationLifetime lifetime)
            {
                _browserView.Content = lifetime.MainView;
            }
        }
    }
}
