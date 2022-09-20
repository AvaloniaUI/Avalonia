export class InputHelper {
    static inputCallback?: DotNet.DotNetObject;
    static compositionCallback?: DotNet.DotNetObject 
    
    public static start(inputElement: HTMLInputElement, compositionCallback: DotNet.DotNetObject, inputCallback: DotNet.DotNetObject)
    {       
        InputHelper.compositionCallback = compositionCallback;

        inputElement.addEventListener('compositionstart', InputHelper.onCompositionEvent);
        inputElement.addEventListener('compositionupdate', InputHelper.onCompositionEvent);
        inputElement.addEventListener('compositionend', InputHelper.onCompositionEvent);    

        InputHelper.inputCallback = inputCallback;

        inputElement.addEventListener('input', InputHelper.onInputEvent);
    }
    
    public static clear(inputElement: HTMLInputElement) {
        inputElement.value = "";
    }
    public static focus(inputElement: HTMLInputElement) {
        inputElement.focus();
        inputElement.setSelectionRange(0,0);
    }

    public static setCursor(inputElement: HTMLInputElement, kind: string) {
        inputElement.style.cursor = kind;
    }
    
    public static setBounds(inputElement: HTMLInputElement, x: number, y: number, width: number, height: number)
    {
        inputElement.style.left = (x - 5).toFixed(0) + "px";
        inputElement.style.top = (y - 5).toFixed(0) + "px";
        inputElement.style.height = "20px";
        inputElement.style.width = "200px";

        getCaretCoordinates(inputElement, inputElement.selectionEnd);
    }

    public static hide(inputElement: HTMLInputElement) {
        inputElement.style.display = 'none';
    }

    public static show(inputElement: HTMLInputElement) {
        inputElement.style.display = 'block';
    }

    public static setSurroundingText(inputElement: HTMLInputElement, text: string, start: number, end: number) {
        if (!inputElement) {
            return;
        }

        inputElement.value = text;
        inputElement.setSelectionRange(start, end);
    }

    private static onCompositionEvent(ev: CompositionEvent)
    {
        if(!InputHelper.compositionCallback)
            return;
        
        switch (ev.type)
        {
            case "compositionstart":
            case "compositionupdate":
            case "compositionend":
                InputHelper.compositionCallback.invokeMethod('Invoke', ev.type, ev.data);
                break;
        }
    }

    private static onInputEvent(ev: Event) {
        if (!InputHelper.inputCallback)
            return;

        var inputEvent = ev as InputEvent;

        InputHelper.inputCallback.invokeMethod('Invoke', ev.type, inputEvent.data);
    }
}



