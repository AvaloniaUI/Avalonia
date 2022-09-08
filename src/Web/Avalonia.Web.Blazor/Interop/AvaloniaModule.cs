using Microsoft.JSInterop;

namespace Avalonia.Web.Blazor.Interop
{
    internal class AvaloniaModule : JSModuleInterop
    {
        private AvaloniaModule(IJSRuntime js) : base(js, "./_content/Avalonia.Web.Blazor/avalonia.js")
        {
        }

        public static async Task<AvaloniaModule> ImportAsync(IJSRuntime js)
        {
            var interop = new AvaloniaModule(js);
            await interop.ImportAsync();
            return interop;
        }
    }
}
