export class Caniuse {
    public static canShowOpenFilePicker(): boolean {
        return typeof window.showOpenFilePicker !== "undefined";
    }

    public static canShowSaveFilePicker(): boolean {
        return typeof window.showSaveFilePicker !== "undefined";
    }

    public static canShowDirectoryPicker(): boolean {
        return typeof window.showDirectoryPicker !== "undefined";
    }
}
