using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace Avalonia.Browser.Interop;

internal static partial class GeneralHelpers
{
    [JSImport("GeneralHelpers.itemsArrayAt", AvaloniaModule.MainModuleName)]
    public static partial JSObject[] ItemsArrayAt(JSObject jsObject, string key);
    public static JSObject[] GetPropertyAsJSObjectArray(this JSObject jsObject, string key) => ItemsArrayAt(jsObject, key);
    
    [JSImport("GeneralHelpers.itemAt", AvaloniaModule.MainModuleName)]
    public static partial JSObject ItemAtInt(JSObject jsObject, int key);
    public static JSObject GetArrayItem(this JSObject jsObject, int key) => ItemAtInt(jsObject, key);
    
    [JSImport("GeneralHelpers.itemsArrayAt", AvaloniaModule.MainModuleName)]
    public static partial string[] ItemsArrayAtAsStrings(JSObject jsObject, string key);
    public static string[] GetPropertyAsStringArray(this JSObject jsObject, string key) => ItemsArrayAtAsStrings(jsObject, key);
    
    [JSImport("GeneralHelpers.callMethod", AvaloniaModule.MainModuleName)]
    public static partial string IntCallMethodStr(JSObject jsObject, string name);
    [JSImport("GeneralHelpers.callMethod", AvaloniaModule.MainModuleName)]
    public static partial string IntCallMethodStrStr(JSObject jsObject, string name, string arg1);
    [JSImport("GeneralHelpers.callMethod", AvaloniaModule.MainModuleName)]
    public static partial Task<JSObject?> IntCallMethodPromiseObj(JSObject jsObject, string name);

    public static string CallMethodString(this JSObject jsObject, string name) => IntCallMethodStr(jsObject, name);
    public static string CallMethodString(this JSObject jsObject, string name, string arg1) => IntCallMethodStrStr(jsObject, name, arg1);
    public static Task<JSObject?> CallMethodObjectAsync(this JSObject jsObject, string name) => IntCallMethodPromiseObj(jsObject, name);
}
