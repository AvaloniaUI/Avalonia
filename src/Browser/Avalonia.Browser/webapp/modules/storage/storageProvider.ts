import { avaloniaDb, fileBookmarksStore } from "./indexedDb";
import { StorageItem, StorageItems } from "./storageItem";

declare global {
    type WellKnownDirectory = "desktop" | "documents" | "downloads" | "music" | "pictures" | "videos";
    type StartInDirectory = WellKnownDirectory | FileSystemHandle;
    interface OpenFilePickerOptions {
        startIn?: StartInDirectory;
    }
    interface SaveFilePickerOptions {
        startIn?: StartInDirectory;
    }
}

export class StorageProvider {
    public static async selectFolderDialog(
        startIn: StorageItem | null): Promise<StorageItem> {
        // 'Picker' API doesn't accept "null" as a parameter, so it should be set to undefined.
        const options: DirectoryPickerOptions = {
            startIn: (startIn?.wellKnownType ?? startIn?.handle ?? undefined)
        };

        const handle = await window.showDirectoryPicker(options);
        return new StorageItem(handle);
    }

    public static async openFileDialog(
        startIn: StorageItem | null, multiple: boolean,
        types: FilePickerAcceptType[] | null, excludeAcceptAllOption: boolean): Promise<StorageItems> {
        const options: OpenFilePickerOptions = {
            startIn: (startIn?.wellKnownType ?? startIn?.handle ?? undefined),
            multiple,
            excludeAcceptAllOption,
            types: (types ?? undefined)
        };

        const handles = await window.showOpenFilePicker(options);
        return new StorageItems(handles.map((handle: FileSystemHandle) => new StorageItem(handle)));
    }

    public static async saveFileDialog(
        startIn: StorageItem | null, suggestedName: string | null,
        types: FilePickerAcceptType[] | null, excludeAcceptAllOption: boolean): Promise<StorageItem> {
        const options: SaveFilePickerOptions = {
            startIn: (startIn?.wellKnownType ?? startIn?.handle ?? undefined),
            suggestedName: (suggestedName ?? undefined),
            excludeAcceptAllOption,
            types: (types ?? undefined)
        };

        const handle = await window.showSaveFilePicker(options);
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
