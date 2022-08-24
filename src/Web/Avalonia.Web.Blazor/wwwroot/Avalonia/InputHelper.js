export class InputHelper {
    static clear(inputElement) {
        inputElement.value = "";
    }
    static focus(inputElement) {
        inputElement.focus();
        inputElement.setSelectionRange(0, 0);
    }
    static setCursor(inputElement, kind) {
        inputElement.style.cursor = kind;
    }
    static hide(inputElement) {
        inputElement.style.display = 'none';
    }
    static show(inputElement) {
        inputElement.style.display = 'block';
    }
}
//# sourceMappingURL=InputHelper.js.map