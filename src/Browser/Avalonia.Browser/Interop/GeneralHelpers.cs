using System.Runtime.InteropServices.JavaScript;

namespace Avalonia.Browser.Interop;

internal static partial class GeneralHelpers
{
    [JSImport("GeneralHelpers.itemsArrayAt", AvaloniaModule.MainModuleName)]
    public static partial JSObject[] ItemsArrayAt(JSObject jsObject, string key);
    public static JSObject[] GetPropertyAsJSObjectArray(this JSObject jsObject, string key) => ItemsArrayAt(jsObject, key);
    
    [JSImport("GeneralHelpers.itemsArrayAt", AvaloniaModule.MainModuleName)]
    public static partial string[] ItemsArrayAtAsStrings(JSObject jsObject, string key);
    public static string[] GetPropertyAsStringArray(this JSObject jsObject, string key) => ItemsArrayAtAsStrings(jsObject, key);
    
    [JSImport("GeneralHelpers.callMethod", AvaloniaModule.MainModuleName)]
    public static partial string IntCallMethodString(JSObject jsObject, string name);
    [JSImport("GeneralHelpers.callMethod", AvaloniaModule.MainModuleName)]
    public static partial string IntCallMethodStringString(JSObject jsObject, string name, string arg1);

    public static string CallMethodString(this JSObject jsObject, string name) => IntCallMethodString(jsObject, name);
    public static string CallMethodString(this JSObject jsObject, string name, string arg1) => IntCallMethodStringString(jsObject, name, arg1);
}
