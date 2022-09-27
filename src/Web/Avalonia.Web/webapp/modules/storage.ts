export class StorageProvider {
    static isFileApiSupported(): boolean {
        return (globalThis as any).showOpenFilePicker !== undefined;
    }
}
