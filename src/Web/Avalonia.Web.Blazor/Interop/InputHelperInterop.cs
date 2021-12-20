using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SkiaSharp;

namespace Avalonia.Web.Blazor.Interop
{
    internal class InputHelperInterop : JSModuleInterop
    {
        private const string JsFilename = "./_content/Avalonia.Web.Blazor/InputHelper.js";
        private const string ClearSymbol = "InputHelper.clear";
        private const string FocusSymbol = "InputHelper.focus";
        private const string SetCursorSymbol = "InputHelper.setCursor";
        private const string HideSymbol = "InputHelper.hide";
        private const string ShowSymbol = "InputHelper.show";

        private readonly ElementReference inputElement;

        public static async Task<InputHelperInterop> ImportAsync(IJSRuntime js, ElementReference element)
        {
            var interop = new InputHelperInterop(js, element);
            await interop.ImportAsync();
            return interop;
        }

        public InputHelperInterop(IJSRuntime js, ElementReference element)
            : base(js, JsFilename)
        {
            inputElement = element;
        }

        public void Clear() => Invoke(ClearSymbol, inputElement);

        public void Focus() => Invoke(FocusSymbol, inputElement);

        public void SetCursor(string kind) => Invoke(SetCursorSymbol, inputElement, kind);

        public void Hide() => Invoke(HideSymbol, inputElement);

        public void Show() => Invoke(ShowSymbol, inputElement);
    }
}
