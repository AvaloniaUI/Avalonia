class InnerDbConnection {
    constructor(private database: IDBDatabase) { }

    private openStore(store: string, mode: IDBTransactionMode): IDBObjectStore {
        const tx = this.database.transaction(store, mode);
        return tx.objectStore(store);
    }

    public put(store: string, obj: any, key?: IDBValidKey): Promise<IDBValidKey> {
        const os = this.openStore(store, "readwrite");

        return new Promise((resolve, reject) => {
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

    public delete(store: string, key: IDBValidKey): Promise<void> {
        const os = this.openStore(store, "readwrite");

        return new Promise((resolve, reject) => {
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

export class IndexedDbWrapper {
    constructor(private databaseName: string, private objectStores: [string]) {
    }

    public connect(): Promise<InnerDbConnection> {
        const conn = window.indexedDB.open(this.databaseName, 1);

        conn.onupgradeneeded = event => {
            const db = (<IDBRequest<IDBDatabase>>event.target).result;
            this.objectStores.forEach(store => {
                db.createObjectStore(store);
            });
        };

        return new Promise((resolve, reject) => {
            conn.onsuccess = event => {
                resolve(new InnerDbConnection((<IDBRequest<IDBDatabase>>event.target).result));
            };
            conn.onerror = event => {
                reject((<IDBRequest<IDBDatabase>>event.target).error);
            };
        });
    }
}
