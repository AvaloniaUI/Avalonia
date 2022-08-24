var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
var __asyncValues = (this && this.__asyncValues) || function (o) {
    if (!Symbol.asyncIterator) throw new TypeError("Symbol.asyncIterator is not defined.");
    var m = o[Symbol.asyncIterator], i;
    return m ? m.call(o) : (o = typeof __values === "function" ? __values(o) : o[Symbol.iterator](), i = {}, verb("next"), verb("throw"), verb("return"), i[Symbol.asyncIterator] = function () { return this; }, i);
    function verb(n) { i[n] = o[n] && function (v) { return new Promise(function (resolve, reject) { v = o[n](v), settle(resolve, reject, v.done, v.value); }); }; }
    function settle(resolve, reject, d, v) { Promise.resolve(v).then(function(v) { resolve({ value: v, done: d }); }, reject); }
};
import { IndexedDbWrapper } from "./IndexedDbWrapper";
const fileBookmarksStore = "fileBookmarks";
const avaloniaDb = new IndexedDbWrapper("AvaloniaDb", [
    fileBookmarksStore
]);
class StorageItem {
    constructor(handle, bookmarkId) {
        this.handle = handle;
        this.bookmarkId = bookmarkId;
    }
    getName() {
        return this.handle.name;
    }
    getKind() {
        return this.handle.kind;
    }
    openRead() {
        return __awaiter(this, void 0, void 0, function* () {
            if (!(this.handle instanceof FileSystemFileHandle)) {
                throw new Error("StorageItem is not a file");
            }
            yield this.verityPermissions('read');
            const file = yield this.handle.getFile();
            return file;
        });
    }
    openWrite() {
        return __awaiter(this, void 0, void 0, function* () {
            if (!(this.handle instanceof FileSystemFileHandle)) {
                throw new Error("StorageItem is not a file");
            }
            yield this.verityPermissions('readwrite');
            return yield this.handle.createWritable({ keepExistingData: true });
        });
    }
    getProperties() {
        return __awaiter(this, void 0, void 0, function* () {
            const file = this.handle instanceof FileSystemFileHandle
                && (yield this.handle.getFile());
            if (!file) {
                return null;
            }
            return {
                Size: file.size,
                LastModified: file.lastModified,
                Type: file.type
            };
        });
    }
    getItems() {
        var e_1, _a;
        return __awaiter(this, void 0, void 0, function* () {
            if (this.handle.kind !== "directory") {
                return new StorageItems([]);
            }
            const items = [];
            try {
                for (var _b = __asyncValues(this.handle.entries()), _c; _c = yield _b.next(), !_c.done;) {
                    const [key, value] = _c.value;
                    items.push(new StorageItem(value));
                }
            }
            catch (e_1_1) { e_1 = { error: e_1_1 }; }
            finally {
                try {
                    if (_c && !_c.done && (_a = _b.return)) yield _a.call(_b);
                }
                finally { if (e_1) throw e_1.error; }
            }
            return new StorageItems(items);
        });
    }
    verityPermissions(mode) {
        return __awaiter(this, void 0, void 0, function* () {
            if ((yield this.handle.queryPermission({ mode })) === 'granted') {
                return;
            }
            if ((yield this.handle.requestPermission({ mode })) === "denied") {
                throw new Error("Read permissions denied");
            }
        });
    }
    saveBookmark() {
        return __awaiter(this, void 0, void 0, function* () {
            if (this.bookmarkId) {
                return this.bookmarkId;
            }
            const connection = yield avaloniaDb.connect();
            try {
                const key = yield connection.put(fileBookmarksStore, this.handle, this.generateBookmarkId());
                return key;
            }
            finally {
                connection.close();
            }
        });
    }
    deleteBookmark() {
        return __awaiter(this, void 0, void 0, function* () {
            if (!this.bookmarkId) {
                return;
            }
            const connection = yield avaloniaDb.connect();
            try {
                const key = yield connection.delete(fileBookmarksStore, this.bookmarkId);
            }
            finally {
                connection.close();
            }
        });
    }
    generateBookmarkId() {
        return Date.now().toString(36) + Math.random().toString(36).substring(2);
    }
}
class StorageItems {
    constructor(items) {
        this.items = items;
    }
    count() {
        return this.items.length;
    }
    at(index) {
        return this.items[index];
    }
}
export class StorageProvider {
    static canOpen() {
        return typeof window.showOpenFilePicker !== 'undefined';
    }
    static canSave() {
        return typeof window.showSaveFilePicker !== 'undefined';
    }
    static canPickFolder() {
        return typeof window.showDirectoryPicker !== 'undefined';
    }
    static selectFolderDialog(startIn) {
        return __awaiter(this, void 0, void 0, function* () {
            const options = {
                startIn: ((startIn === null || startIn === void 0 ? void 0 : startIn.handle) || undefined)
            };
            const handle = yield window.showDirectoryPicker(options);
            return new StorageItem(handle);
        });
    }
    static openFileDialog(startIn, multiple, types, excludeAcceptAllOption) {
        return __awaiter(this, void 0, void 0, function* () {
            const options = {
                startIn: ((startIn === null || startIn === void 0 ? void 0 : startIn.handle) || undefined),
                multiple,
                excludeAcceptAllOption,
                types: (types || undefined)
            };
            const handles = yield window.showOpenFilePicker(options);
            return new StorageItems(handles.map((handle) => new StorageItem(handle)));
        });
    }
    static saveFileDialog(startIn, suggestedName, types, excludeAcceptAllOption) {
        return __awaiter(this, void 0, void 0, function* () {
            const options = {
                startIn: ((startIn === null || startIn === void 0 ? void 0 : startIn.handle) || undefined),
                suggestedName: (suggestedName || undefined),
                excludeAcceptAllOption,
                types: (types || undefined)
            };
            const handle = yield window.showSaveFilePicker(options);
            return new StorageItem(handle);
        });
    }
    static openBookmark(key) {
        return __awaiter(this, void 0, void 0, function* () {
            const connection = yield avaloniaDb.connect();
            try {
                const handle = yield connection.get(fileBookmarksStore, key);
                return handle && new StorageItem(handle, key);
            }
            finally {
                connection.close();
            }
        });
    }
}
//# sourceMappingURL=StorageProvider.js.map