using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace Avalonia.Web.Interop;

internal static partial class StorageHelper
{
    [JSImport("Caniuse.canShowOpenFilePicker", "avalonia.ts")]
    public static partial bool CanShowOpenFilePicker();

    [JSImport("Caniuse.canShowSaveFilePicker", "avalonia.ts")]
    public static partial bool CanShowSaveFilePicker();

    [JSImport("Caniuse.canShowDirectoryPicker", "avalonia.ts")]
    public static partial bool CanShowDirectoryPicker();

    [JSImport("StorageProvider.selectFolderDialog", "storage.ts")]
    public static partial Task<JSObject?> SelectFolderDialog(JSObject? startIn);

    [JSImport("StorageProvider.openFileDialog", "storage.ts")]
    public static partial Task<JSObject?> OpenFileDialog(JSObject? startIn, bool multiple,
        [JSMarshalAs<JSType.Array<JSType.Any>>] object[]? types, bool excludeAcceptAllOption);

    [JSImport("StorageProvider.saveFileDialog", "storage.ts")]
    public static partial Task<JSObject?> SaveFileDialog(JSObject? startIn, string? suggestedName,
        [JSMarshalAs<JSType.Array<JSType.Any>>] object[]? types, bool excludeAcceptAllOption);

    [JSImport("StorageProvider.openBookmark", "storage.ts")]
    public static partial Task<JSObject?> OpenBookmark(string key);

    [JSImport("StorageItem.saveBookmark", "storage.ts")]
    public static partial Task<string?> SaveBookmark(JSObject item);

    [JSImport("StorageItem.deleteBookmark", "storage.ts")]
    public static partial Task DeleteBookmark(JSObject item);

    [JSImport("StorageItem.getProperties", "storage.ts")]
    public static partial Task<JSObject?> GetProperties(JSObject item);

    [JSImport("StorageItem.openWrite", "storage.ts")]
    public static partial Task<JSObject> OpenWrite(JSObject item);

    [JSImport("StorageItem.openRead", "storage.ts")]
    public static partial Task<JSObject> OpenRead(JSObject item);

    [JSImport("StorageItem.getItems", "storage.ts")]
    [return: JSMarshalAs<JSType.Promise<JSType.Object>>]
    public static partial Task<JSObject> GetItems(JSObject item);

    [JSImport("StorageItems.itemsArray", "storage.ts")]
    public static partial JSObject[] ItemsArray(JSObject item);

    [JSImport("StorageProvider.createAcceptType", "storage.ts")]
    public static partial JSObject CreateAcceptType(string description, string[] mimeTypes);
}
