using Microsoft.AspNetCore.Components;

namespace Avalonia.Web.Blazor.Interop;

internal class FocusHelperInterop
{
    private const string FocusSymbol = "FocusHelper.focus";
    private const string SetCursorSymbol = "FocusHelper.setCursor";
    
    private readonly AvaloniaModule _module;
    private readonly ElementReference _inputElement;

    public FocusHelperInterop(AvaloniaModule module, ElementReference inputElement)
    {
        _module = module;
        _inputElement = inputElement;
    }
            
    public void Focus() => _module.Invoke(FocusSymbol, _inputElement);
    
    public void SetCursor(string kind) => _module.Invoke(SetCursorSymbol, _inputElement, kind);
}
