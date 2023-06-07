class InnerDbConnection {
    constructor(private readonly database: IDBDatabase) { }

    private openStore(store: string, mode: IDBTransactionMode): IDBObjectStore {
        const tx = this.database.transaction(store, mode);
        return tx.objectStore(store);
    }

    public async put(store: string, obj: any, key?: IDBValidKey): Promise<IDBValidKey> {
        const os = this.openStore(store, "readwrite");

        return await new Promise((resolve, reject) => {
            const response = os.put(obj, key);
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
            const response = os.get(key);
            response.onsuccess = () => {
                resolve(response.result);
            };
            response.onerror = () => {
                reject(response.error);
            };
        });
    }

    public async delete(store: string, key: IDBValidKey): Promise<void> {
        const os = this.openStore(store, "readwrite");

        return await new Promise((resolve, reject) => {
            const response = os.delete(key);
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

class IndexedDbWrapper {
    constructor(private readonly databaseName: string, private readonly objectStores: [string]) {
    }

    public async connect(): Promise<InnerDbConnection> {
        const conn = window.indexedDB.open(this.databaseName, 1);

        conn.onupgradeneeded = event => {
            const db = (event.target as IDBRequest<IDBDatabase>).result;
            this.objectStores.forEach(store => {
                db.createObjectStore(store);
            });
        };

        return await new Promise((resolve, reject) => {
            conn.onsuccess = event => {
                resolve(new InnerDbConnection((event.target as IDBRequest<IDBDatabase>).result));
            };
            conn.onerror = event => {
                reject((event.target as IDBRequest<IDBDatabase>).error);
            };
        });
    }
}

export const fileBookmarksStore: string = "fileBookmarks";
export const avaloniaDb = new IndexedDbWrapper("AvaloniaDb", [
    fileBookmarksStore
]);
