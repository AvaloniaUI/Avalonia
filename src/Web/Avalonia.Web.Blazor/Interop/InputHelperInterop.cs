using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Avalonia.Web.Blazor.Interop
{
    internal class InputHelperInterop
    {
        private const string ClearSymbol = "InputHelper.clear";
        private const string FocusSymbol = "InputHelper.focus";
        private const string SetCursorSymbol = "InputHelper.setCursor";
        private const string HideSymbol = "InputHelper.hide";
        private const string ShowSymbol = "InputHelper.show";
        private const string StartSymbol = "InputHelper.start";

        private readonly AvaloniaModule _module;
        private readonly ElementReference _inputElement;
        private readonly ActionHelper _actionHelper;
        private DotNetObjectReference<ActionHelper>? callbackReference;

        public InputHelperInterop(AvaloniaModule module, ElementReference inputElement)
        {
            _module = module;
            _inputElement = inputElement;

            _actionHelper = new ActionHelper(() =>
            {

            });
            
            Start();
        }

        public void Clear() => _module.Invoke(ClearSymbol, _inputElement);

        public void Focus() => _module.Invoke(FocusSymbol, _inputElement);

        public void SetCursor(string kind) => _module.Invoke(SetCursorSymbol, _inputElement, kind);

        public void Hide() => _module.Invoke(HideSymbol, _inputElement);

        public void Show() => _module.Invoke(ShowSymbol, _inputElement);

        private void Start()
        {
            if(callbackReference != null)
                return;
            
            callbackReference = DotNetObjectReference.Create(_actionHelper);

            _module.Invoke(StartSymbol, _inputElement, callbackReference);
        }
    }
}
