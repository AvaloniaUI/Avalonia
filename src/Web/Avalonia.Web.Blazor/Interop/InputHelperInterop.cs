using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Avalonia.Web.Blazor.Interop
{
    internal class WebCompositionEventArgs : EventArgs
    {
        public enum WebCompositionEventType
        {
            Start,
            Update,
            End
        }

        public WebCompositionEventArgs(string type, string data)
        {
            Type = type switch
            {
                "compositionstart" => WebCompositionEventType.Start,
                "compositionupdate" => WebCompositionEventType.Update,
                "compositionend" => WebCompositionEventType.End,
                _ => Type
            };

            Data = data;
        }
        
        public WebCompositionEventType Type { get; }
        
        public string Data { get; }
    }

    internal class WebInputEventArgs
    {
        public WebInputEventArgs(string type, string data)
        {
            Type = type;
            Data = data;
        }

        public string Type { get; }

        public string Data { get; }
    }
    
    internal class InputHelperInterop
    {
        private const string ClearSymbol = "InputHelper.clear";
        private const string FocusSymbol = "InputHelper.focus";
        private const string SetCursorSymbol = "InputHelper.setCursor";
        private const string HideSymbol = "InputHelper.hide";
        private const string ShowSymbol = "InputHelper.show";
        private const string StartSymbol = "InputHelper.start";
        private const string SetSurroundingTextSymbol = "InputHelper.setSurroundingText";

        private readonly AvaloniaModule _module;
        private readonly ElementReference _inputElement;
        private readonly ActionHelper<string, string> _compositionAction;
        private readonly ActionHelper<string, string> _inputAction;

        private DotNetObjectReference<ActionHelper<string, string>>? compositionActionReference;
        private DotNetObjectReference<ActionHelper<string, string>>? inputActionReference;

        public InputHelperInterop(AvaloniaModule module, ElementReference inputElement)
        {
            _module = module;
            _inputElement = inputElement;

            _compositionAction = new ActionHelper<string, string>(OnCompositionEvent);
            _inputAction = new ActionHelper<string, string>(OnInputEvent);

            Start();
        }

        public event EventHandler<WebCompositionEventArgs>? CompositionEvent;
        public event EventHandler<WebInputEventArgs>? InputEvent;

        private void OnCompositionEvent(string type, string data)
        {
            Console.WriteLine($"CompositionEvent Handler Helper {CompositionEvent == null} ");
            CompositionEvent?.Invoke(this, new WebCompositionEventArgs(type, data));
        }

        private void OnInputEvent(string type, string data)
        {
            Console.WriteLine($"InputEvent Handler Helper {InputEvent == null} ");
            InputEvent?.Invoke(this, new WebInputEventArgs(type, data));
        }

        public void Clear() => _module.Invoke(ClearSymbol, _inputElement);

        public void Focus() => _module.Invoke(FocusSymbol, _inputElement);

        public void SetCursor(string kind) => _module.Invoke(SetCursorSymbol, _inputElement, kind);

        public void Hide() => _module.Invoke(HideSymbol, _inputElement);

        public void Show() => _module.Invoke(ShowSymbol, _inputElement);

        private void Start()
        {
            if(compositionActionReference != null)
            {
                return;
            }
                          
            compositionActionReference = DotNetObjectReference.Create(_compositionAction);

            inputActionReference = DotNetObjectReference.Create(_inputAction);

            _module.Invoke(StartSymbol, _inputElement, compositionActionReference, inputActionReference);
        }

        public void SetSurroundingText(string text, int start, int end)
        {
            _module.Invoke(SetSurroundingTextSymbol, _inputElement, text, start, end);
        }
    }
}
