export class InputHelper {
    public static clear(inputElement: HTMLInputElement) {
        inputElement.value = "";
    }

    
    public static isInputElement( element : HTMLInputElement | HTMLElement ) : element is HTMLInputElement {
        return ( element as HTMLInputElement).setSelectionRange !== undefined;
    }

    public static focus(inputElement: HTMLElement) {
        inputElement.focus();
        
        if(this.isInputElement(inputElement))
        {
            (inputElement as HTMLInputElement).setSelectionRange(0,0);
        }
    }

    public static setCursor(inputElement: HTMLInputElement, kind: string) {
        inputElement.style.cursor = kind;
    }

    public static hide(inputElement: HTMLInputElement) {
        inputElement.style.display = 'none';
    }

    public static show(inputElement: HTMLInputElement) {
        inputElement.style.display = 'block';
    }
}
