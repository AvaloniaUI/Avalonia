// modules/storage.ts
var StorageProvider = class {
  static isFileApiSupported() {
    return globalThis.showOpenFilePicker !== void 0;
  }
};
export {
  StorageProvider
};
//# sourceMappingURL=storage.js.map
