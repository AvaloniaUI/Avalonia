import { avaloniaDb, fileBookmarksStore } from "./indexedDb";
import { FileSystemFileHandle, FileSystemDirectoryHandle, FileSystemWritableFileStream } from "native-file-system-adapter";
import { Caniuse } from "../avalonia";

export class StorageItem {
    constructor(
        public handle?: FileSystemFileHandle | FileSystemDirectoryHandle,
        private readonly bookmarkId?: string,
        public wellKnownType?: WellKnownDirectory
    ) {
    }

    public get name(): string {
        if (this.handle) {
            return this.handle.name;
        }
        return this.wellKnownType ?? "";
    }

    public get kind(): "file" | "directory" {
        if (this.handle) {
            return this.handle.kind;
        }
        return "directory";
    }

    public static createWellKnownDirectory(type: WellKnownDirectory) {
        return new StorageItem(undefined, undefined, type);
    }

    public static async openRead(item: StorageItem): Promise<Blob> {
        if (!item.handle || item.kind !== "file") {
            throw new Error("StorageItem is not a file");
        }

        await item.verityPermissions("read");

        const file = await (item.handle as FileSystemFileHandle).getFile();
        return file;
    }

    public static async openWrite(item: StorageItem): Promise<FileSystemWritableFileStream> {
        if (!item.handle || item.kind !== "file") {
            throw new Error("StorageItem is not a file");
        }

        await item.verityPermissions("readwrite");

        return await (item.handle as FileSystemFileHandle).createWritable({ keepExistingData: true });
    }

    public static async getProperties(item: StorageItem): Promise<{ Size: number; LastModified: number; Type: string } | null> {
        // getFile can fail with an exception depending if we use polyfill with a save file dialog or not.
        try {
            const file = item.handle instanceof FileSystemFileHandle &&
                await item.handle.getFile();

            if (!file) {
                return null;
            }

            return {
                Size: file.size,
                LastModified: file.lastModified,
                Type: file.type
            };
        } catch {
            return null;
        }
    }

    public static async getItems(item: StorageItem): Promise<StorageItems> {
        if (item.kind !== "directory" || !item.handle) {
            return new StorageItems([]);
        }

        const items: StorageItem[] = [];
        for await (const [, value] of (item.handle as any).entries()) {
            items.push(new StorageItem(value));
        }
        return new StorageItems(items);
    }

    private async verityPermissions(mode: "read" | "readwrite"): Promise<void | never> {
        if (!this.handle) {
            return;
        }

        // If we are using polyfill, let it decide permissions by itself, we can't request anything in this case.
        if (!Caniuse.hasNativeFilePicker()) {
            return;
        }

        if (await this.handle.queryPermission({ mode }) === "granted") {
            return;
        }

        if (await this.handle.requestPermission({ mode }) === "denied") {
            throw new Error("Permissions denied");
        }
    }

    public static async saveBookmark(item: StorageItem): Promise<string | null> {
        // If file was previously bookmarked, just return old one.
        if (item.bookmarkId) {
            return item.bookmarkId;
        }

        // Bookmarks are not supported with polyfill.
        if (!item.handle || !Caniuse.hasNativeFilePicker()) {
            return null;
        }

        const connection = await avaloniaDb.connect();
        try {
            const key = await connection.put(fileBookmarksStore, item.handle, item.generateBookmarkId());
            return key as string;
        } finally {
            connection.close();
        }
    }

    public static async deleteBookmark(item: StorageItem): Promise<void> {
        if (!item.bookmarkId || !Caniuse.hasNativeFilePicker()) {
            return;
        }

        const connection = await avaloniaDb.connect();
        try {
            await connection.delete(fileBookmarksStore, item.bookmarkId);
        } finally {
            connection.close();
        }
    }

    private generateBookmarkId(): string {
        return Date.now().toString(36) + Math.random().toString(36).substring(2);
    }
}

export class StorageItems {
    constructor(private readonly items: StorageItem[]) { }

    public static itemsArray(instance: StorageItems): StorageItem[] {
        return instance.items;
    }
}
