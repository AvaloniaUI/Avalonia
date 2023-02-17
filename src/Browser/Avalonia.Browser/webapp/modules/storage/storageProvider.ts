import { avaloniaDb, fileBookmarksStore } from "./indexedDb";
import { StorageItem, StorageItems } from "./storageItem";
import { showOpenFilePicker, showDirectoryPicker, FileSystemFileHandle } from "native-file-system-adapter";

declare global {
    type WellKnownDirectory = "desktop" | "documents" | "downloads" | "music" | "pictures" | "videos";
    interface FilePickerAcceptType {
        description?: string | undefined;
        accept: Record<string, string | string[]>;
    }
}

export class StorageProvider {
    public static async selectFolderDialog(
        startIn: StorageItem | null): Promise<StorageItem> {
        // 'Picker' API doesn't accept "null" as a parameter, so it should be set to undefined.
        const options = {
            startIn: (startIn?.wellKnownType ?? startIn?.handle ?? undefined)
        };

        const handle = await showDirectoryPicker(options as any);
        return new StorageItem(handle);
    }

    public static async openFileDialog(
        startIn: StorageItem | null, multiple: boolean,
        types: FilePickerAcceptType[] | null, excludeAcceptAllOption: boolean): Promise<StorageItems> {
        const options = {
            startIn: (startIn?.wellKnownType ?? startIn?.handle ?? undefined),
            multiple,
            excludeAcceptAllOption,
            types: (types ?? undefined)
        };

        const handles = await showOpenFilePicker(options);
        return new StorageItems(handles.map((handle: FileSystemFileHandle) => new StorageItem(handle)));
    }

    public static async saveFileDialog(
        startIn: StorageItem | null, suggestedName: string | null,
        types: FilePickerAcceptType[] | null, excludeAcceptAllOption: boolean): Promise<StorageItem> {
        const options = {
            startIn: (startIn?.wellKnownType ?? startIn?.handle ?? undefined),
            suggestedName: (suggestedName ?? undefined),
            excludeAcceptAllOption,
            types: (types ?? undefined)
        };

        // Always prefer native save file picker, as polyfill solutions are not reliable.
        const handle = await (globalThis as any).showSaveFilePicker(options);
        return new StorageItem(handle);
    }

    public static async openBookmark(key: string): Promise<StorageItem | null> {
        const connection = await avaloniaDb.connect();
        try {
            const handle = await connection.get(fileBookmarksStore, key);
            return handle && new StorageItem(handle, key);
        } finally {
            connection.close();
        }
    }

    public static createAcceptType(description: string, mimeTypes: string[], extensions: string[] | undefined): FilePickerAcceptType {
        const accept: Record<string, string[]> = {};
        mimeTypes.forEach(a => { accept[a] = extensions ?? []; });
        return { description, accept };
    }
}
