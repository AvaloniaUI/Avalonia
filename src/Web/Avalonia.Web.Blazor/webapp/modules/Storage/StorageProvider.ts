import { IndexedDbWrapper } from "./IndexedDbWrapper";

declare global {
    type WellKnownDirectory = "desktop" | "documents" | "downloads" | "music" | "pictures" | "videos";
    type StartInDirectory = WellKnownDirectory | FileSystemHandle;
    interface OpenFilePickerOptions {
        startIn?: StartInDirectory
    }
    interface SaveFilePickerOptions {
        startIn?: StartInDirectory
    }
}

const fileBookmarksStore: string = "fileBookmarks";
const avaloniaDb = new IndexedDbWrapper("AvaloniaDb", [
    fileBookmarksStore
]);

class StorageItem {
    constructor(public handle: FileSystemHandle, private bookmarkId?: string) { }

    public getName(): string {
        return this.handle.name
    }

    public getKind(): string {
        return this.handle.kind;
    }

    public async openRead(): Promise<Blob> {
        if (!(this.handle instanceof FileSystemFileHandle)) {
            throw new Error("StorageItem is not a file");
        }

        await this.verityPermissions('read');

        const file = await this.handle.getFile();
        return file;
    }

    public async openWrite(): Promise<FileSystemWritableFileStream> {
        if (!(this.handle instanceof FileSystemFileHandle)) {
            throw new Error("StorageItem is not a file");
        }

        await this.verityPermissions('readwrite');

        return await this.handle.createWritable({ keepExistingData: true });
    }

    public async getProperties(): Promise<{ Size: number, LastModified: number, Type: string } | null> {
        const file = this.handle instanceof FileSystemFileHandle
            && await this.handle.getFile();

        if (!file) {
            return null;
        }

        return {
            Size: file.size,
            LastModified: file.lastModified,
            Type: file.type
        }
    }

    public async getItems(): Promise<StorageItems> {
        if (this.handle.kind !== "directory"){
            return new StorageItems([]);
        }
        
        const items: StorageItem[] = [];
        for await (const [key, value] of (this.handle as any).entries()) {
            items.push(new StorageItem(value));
        }
        return new StorageItems(items);
    }
    
    private async verityPermissions(mode: FileSystemPermissionMode): Promise<void | never> {
        if (await this.handle.queryPermission({ mode }) === 'granted') {
            return;
        }

        if (await this.handle.requestPermission({ mode }) === "denied") {
            throw new Error("Read permissions denied");
        }
    }

    public async saveBookmark(): Promise<string> {
        // If file was previously bookmarked, just return old one.
        if (this.bookmarkId) {
            return this.bookmarkId;
        }
        
        const connection = await avaloniaDb.connect();
        try {
            const key = await connection.put(fileBookmarksStore, this.handle, this.generateBookmarkId());
            return <string>key;
        }
        finally {
            connection.close();
        }
    }

    public async deleteBookmark(): Promise<void> {
        if (!this.bookmarkId) {
            return;
        }

        const connection = await avaloniaDb.connect();
        try {
            const key = await connection.delete(fileBookmarksStore, this.bookmarkId);
        }
        finally {
            connection.close();
        }
    }

    private generateBookmarkId(): string {
        return Date.now().toString(36) + Math.random().toString(36).substring(2);
    }
}

class StorageItems {
    constructor(private items: StorageItem[]) { }

    public count(): number {
        return this.items.length;
    }

    public at(index: number): StorageItem {
        return this.items[index];
    }
}

export class StorageProvider {

    public static canOpen(): boolean {
        return typeof window.showOpenFilePicker !== 'undefined';
    }

    public static canSave(): boolean {
        return typeof window.showSaveFilePicker !== 'undefined';
    }

    public static canPickFolder(): boolean {
        return typeof window.showDirectoryPicker !== 'undefined';
    }

    public static async selectFolderDialog(
        startIn: StorageItem | null)
        : Promise<StorageItem> {

        // 'Picker' API doesn't accept "null" as a parameter, so it should be set to undefined.
        const options: DirectoryPickerOptions = {
            startIn: (startIn?.handle || undefined)
        };

        const handle = await window.showDirectoryPicker(options);
        return new StorageItem(handle);
    }

    public static async openFileDialog(
        startIn: StorageItem | null, multiple: boolean,
        types: FilePickerAcceptType[] | null, excludeAcceptAllOption: boolean)
        : Promise<StorageItems> {

        const options: OpenFilePickerOptions = {
            startIn: (startIn?.handle || undefined),
            multiple,
            excludeAcceptAllOption,
            types: (types || undefined)
        };

        const handles = await window.showOpenFilePicker(options);
        return new StorageItems(handles.map((handle: FileSystemHandle) => new StorageItem(handle)));
    }

    public static async saveFileDialog(
        startIn: StorageItem | null, suggestedName: string | null,
        types: FilePickerAcceptType[] | null, excludeAcceptAllOption: boolean)
        : Promise<StorageItem> {

        const options: SaveFilePickerOptions = {
            startIn: (startIn?.handle || undefined),
            suggestedName: (suggestedName || undefined),
            excludeAcceptAllOption,
            types: (types || undefined)
        };

        const handle = await window.showSaveFilePicker(options);
        return new StorageItem(handle);
    }

    public static async openBookmark(key: string): Promise<StorageItem | null> {
        const connection = await avaloniaDb.connect();
        try {
            const handle = await connection.get(fileBookmarksStore, key);
            return handle && new StorageItem(handle, key);
        }
        finally {
            connection.close();
        }
    }
}
