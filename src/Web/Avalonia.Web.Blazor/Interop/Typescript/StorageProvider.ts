// As we don't have proper package managing for Avalonia.Web project, declare types manually
declare global {
    interface FileSystemWritableFileStream {
        write(position: number, data: BufferSource | Blob | string): Promise<void>;
        truncate(size: number): Promise<void>;
        close(): Promise<void>;
    }
    type PermissionsMode = "read" | "readwrite";
    interface FileSystemFileHandle {
        name: string,
        kind: "file" | "directory",
        getFile(): Promise<File>;
        createWritable(options?: { keepExistingData?: boolean }): Promise<FileSystemWritableFileStream>;

        queryPermission(options?: { mode: PermissionsMode }): Promise<"granted" | "denied" | "prompt">;
        requestPermission(options?: { mode: PermissionsMode }): Promise<"granted" | "denied" | "prompt">;
    }
    type WellKnownDirectory = "desktop" | "documents" | "downloads" | "music" | "pictures" | "videos"; 
    type StartInDirectory =  WellKnownDirectory | FileSystemFileHandle;
    interface FilePickerAcceptType {
        description: string,
        // mime -> ext[] array
        accept: { [mime: string]: string | string[] }
    }
    interface FilePickerOptions {
        types?: FilePickerAcceptType[],
        excludeAcceptAllOption: boolean,
        id?: string,
        startIn?: StartInDirectory
    }
    interface OpenFilePickerOptions extends FilePickerOptions {
        multiple: boolean
    }
    interface SaveFilePickerOptions extends FilePickerOptions {
        suggestedName?: string
    }
    interface DirectoryPickerOptions {
        id?: string,
        startIn?: StartInDirectory
    }
    
    interface Window {
        showOpenFilePicker: (options: OpenFilePickerOptions) => Promise<FileSystemFileHandle[]>;
        showSaveFilePicker: (options: SaveFilePickerOptions) => Promise<FileSystemFileHandle>;
        showDirectoryPicker: (options: DirectoryPickerOptions) => Promise<FileSystemFileHandle>;
    }
}

// TODO move to another file and use import
class IndexedDbWrapper {
    constructor(private databaseName: string, private objectStores: [ string ]) {

    }

    public connect(): Promise<InnerDbConnection> {
        var conn = window.indexedDB.open(this.databaseName, 1);

        conn.onupgradeneeded = event => {
            const db = (<IDBRequest<IDBDatabase>>event.target).result;
            this.objectStores.forEach(store => {
                db.createObjectStore(store);
            });
        }

        return new Promise((resolve, reject) => {
            conn.onsuccess = event => {
                resolve(new InnerDbConnection((<IDBRequest<IDBDatabase>>event.target).result));
            }
            conn.onerror = event => {
                reject((<IDBRequest<IDBDatabase>>event.target).error);
            }
        });
    }
}

class InnerDbConnection {
    constructor(private database: IDBDatabase) { }

    private openStore(store: string, mode: IDBTransactionMode): IDBObjectStore {
        const tx = this.database.transaction(store, mode);
        return tx.objectStore(store);
    }

    public put(store: string, obj: any, key?: IDBValidKey): Promise<IDBValidKey> {
        const os = this.openStore(store, "readwrite");

        return new Promise((resolve, reject) => {
            var response = os.put(obj, key);
            response.onsuccess = () => {
                resolve(response.result);
            };
            response.onerror = () => {
                reject(response.error);
            };
        });
    }

    public get(store: string, key: IDBValidKey): any {
        const os = this.openStore(store, "readonly");

        return new Promise((resolve, reject) => {
            var response = os.get(key);
            response.onsuccess = () => {
                resolve(response.result);
            };
            response.onerror = () => {
                reject(response.error);
            };
        });
    }

    public delete(store: string, key: IDBValidKey): Promise<void> {
        const os = this.openStore(store, "readwrite");

        return new Promise((resolve, reject) => {
            var response = os.delete(key);
            response.onsuccess = () => {
                resolve();
            };
            response.onerror = () => {
                reject(response.error);
            };
        });
    }

    public close() {
        this.database.close();
    }
}

const fileBookmarksStore: string = "fileBookmarks";
const avaloniaDb = new IndexedDbWrapper("AvaloniaDb", [
    fileBookmarksStore
])

class StorageItem {
    constructor(private handle: FileSystemFileHandle, private bookmarkId?: string) { }

    public getName(): string {
        return this.handle.name
    }

    public async openRead(): Promise<Blob> {
        await this.verityPermissions('read');

        var file = await this.handle.getFile();
        return file;
    }

    public async openWrite(): Promise<FileSystemWritableFileStream> {
        await this.verityPermissions('readwrite');

        return await this.handle.createWritable({ keepExistingData: true });
    }

    public async getProperties(): Promise<{ Size: number, LastModified: number, Type: string }> {
        var file = this.handle.getFile && await this.handle.getFile();
        
        return file && {
            Size: file.size,
            LastModified: file.lastModified,
            Type: file.type
        }
    }

    private async verityPermissions(mode: PermissionsMode): Promise<void | never> {
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
        startIn: StartInDirectory | null)
        : Promise<StorageItem> {

        // 'Picker' API doesn't accept "null" as a parameter, so it should be set to undefined.
        const options: DirectoryPickerOptions = {
            startIn: (startIn || undefined)
        };

        const handle = await window.showDirectoryPicker(options);
        return new StorageItem(handle);
    }

    public static async openFileDialog(
        startIn: StartInDirectory | null, multiple: boolean,
        types: FilePickerAcceptType[] | null, excludeAcceptAllOption: boolean)
        : Promise<StorageItems> {

        const options: OpenFilePickerOptions = {
            startIn: (startIn || undefined),
            multiple,
            excludeAcceptAllOption,
            types: (types || undefined)
        };

        const handles = await window.showOpenFilePicker(options);
        return new StorageItems(handles.map(handle => new StorageItem(handle)));
    }

    public static async saveFileDialog(
        startIn: StartInDirectory | null, suggestedName: string | null,
        types: FilePickerAcceptType[] | null, excludeAcceptAllOption: boolean)
        : Promise<StorageItem> {

        const options: SaveFilePickerOptions = {
            startIn: (startIn || undefined),
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
