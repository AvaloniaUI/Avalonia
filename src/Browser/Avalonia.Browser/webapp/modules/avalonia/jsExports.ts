export class JsExports {
    public static resolvedExports?: any;
    public static exportsPromise: Promise<any>;
}
async function resolveExports (): Promise<any> {
    const runtimeApi = await globalThis.getDotnetRuntime(0);
    if (runtimeApi == null) { return; }
    JsExports.resolvedExports = await runtimeApi.getAssemblyExports("Avalonia.Browser.dll");
    return JsExports.resolvedExports;
}

JsExports.exportsPromise = resolveExports();
