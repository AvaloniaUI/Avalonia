import {CaretHelper} from "./CaretHelper";

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
    
    public static setBounds(inputElement: HTMLInputElement, x: number, y: number, width: number, height: number, caret: number)
    {
        if(inputElement.selectionStart) {
            inputElement.style.left = (x).toFixed(0) + "px";
            inputElement.style.top = (y).toFixed(0) + "px";

            let {height, left, top} = CaretHelper.getCaretCoordinates(inputElement, caret);

            inputElement.style.left = (x - left).toFixed(0) + "px";
            inputElement.style.top = (y - top).toFixed(0) + "px";
        }
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



