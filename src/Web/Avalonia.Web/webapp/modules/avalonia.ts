import { RuntimeAPI } from "../types/dotnet";
import { AvaloniaRuntime } from "./avalonia/runtime";

export async function createAvaloniaRuntime(api: RuntimeAPI): Promise<AvaloniaRuntime> {
    const dotnetAssembly = await api.getAssemblyExports("Avalonia.Web.dll");
    return new AvaloniaRuntime(dotnetAssembly, api);
}