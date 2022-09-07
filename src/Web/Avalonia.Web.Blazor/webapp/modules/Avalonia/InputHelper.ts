export class InputHelper {
    public static clear(inputElement: HTMLInputElement) {
        inputElement.value = "";
    }

    public static focus(inputElement: HTMLInputElement) {
        inputElement.focus();
        inputElement.setSelectionRange(0, 0);
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
