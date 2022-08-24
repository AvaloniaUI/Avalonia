class InnerDbConnection {
    constructor(database) {
        this.database = database;
    }
    openStore(store, mode) {
        const tx = this.database.transaction(store, mode);
        return tx.objectStore(store);
    }
    put(store, obj, key) {
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
    get(store, key) {
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
    delete(store, key) {
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
    close() {
        this.database.close();
    }
}
export class IndexedDbWrapper {
    constructor(databaseName, objectStores) {
        this.databaseName = databaseName;
        this.objectStores = objectStores;
    }
    connect() {
        const conn = window.indexedDB.open(this.databaseName, 1);
        conn.onupgradeneeded = event => {
            const db = event.target.result;
            this.objectStores.forEach(store => {
                db.createObjectStore(store);
            });
        };
        return new Promise((resolve, reject) => {
            conn.onsuccess = event => {
                resolve(new InnerDbConnection(event.target.result));
            };
            conn.onerror = event => {
                reject(event.target.error);
            };
        });
    }
}
//# sourceMappingURL=IndexedDbWrapper.js.map