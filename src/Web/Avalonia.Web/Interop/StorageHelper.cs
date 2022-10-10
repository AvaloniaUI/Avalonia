using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace Avalonia.Web.Interop;

internal static partial class StorageHelper
{
    [JSImport("Caniuse.canShowOpenFilePicker", "avalonia")]
    public static partial bool CanShowOpenFilePicker();

    [JSImport("Caniuse.canShowSaveFilePicker", "avalonia")]
    public static partial bool CanShowSaveFilePicker();

    [JSImport("Caniuse.canShowDirectoryPicker", "avalonia")]
    public static partial bool CanShowDirectoryPicker();

    [JSImport("StorageProvider.selectFolderDialog", "storage")]
    public static partial Task<JSObject?> SelectFolderDialog(JSObject? startIn);

    [JSImport("StorageProvider.openFileDialog", "storage")]
    public static partial Task<JSObject?> OpenFileDialog(JSObject? startIn, bool multiple,
        [JSMarshalAs<JSType.Array<JSType.Any>>] object[]? types, bool excludeAcceptAllOption);

    [JSImport("StorageProvider.saveFileDialog", "storage")]
    public static partial Task<JSObject?> SaveFileDialog(JSObject? startIn, string? suggestedName,
        [JSMarshalAs<JSType.Array<JSType.Any>>] object[]? types, bool excludeAcceptAllOption);

    [JSImport("StorageProvider.openBookmark", "storage")]
    public static partial Task<JSObject?> OpenBookmark(string key);

    [JSImport("StorageItem.saveBookmark", "storage")]
    public static partial Task<string?> SaveBookmark(JSObject item);

    [JSImport("StorageItem.deleteBookmark", "storage")]
    public static partial Task DeleteBookmark(JSObject item);

    [JSImport("StorageItem.getProperties", "storage")]
    public static partial Task<JSObject?> GetProperties(JSObject item);

    [JSImport("StorageItem.openWrite", "storage")]
    public static partial Task<JSObject> OpenWrite(JSObject item);

    [JSImport("StorageItem.openRead", "storage")]
    public static partial Task<JSObject> OpenRead(JSObject item);

    [JSImport("StorageItem.getItems", "storage")]
    [return: JSMarshalAs<JSType.Promise<JSType.Object>>]
    public static partial Task<JSObject> GetItems(JSObject item);

    [JSImport("StorageItems.itemsArray", "storage")]
    public static partial JSObject[] ItemsArray(JSObject item);

    [JSImport("StorageProvider.createAcceptType", "storage")]
    public static partial JSObject CreateAcceptType(string description, string[] mimeTypes);
}
