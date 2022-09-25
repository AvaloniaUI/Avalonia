export class FocusHelper {
    public static focus(inputElement: HTMLElement) {
        inputElement.focus();
    }

    public static setCursor(inputElement: HTMLInputElement, kind: string) {
        inputElement.style.cursor = kind;
    }
}
