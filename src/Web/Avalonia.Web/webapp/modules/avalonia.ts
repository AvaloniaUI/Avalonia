import { RuntimeAPI } from "../types/dotnet";
import { SizeWatcher, DpiWatcher, Canvas } from "./avalonia/canvas";
import { InputHelper } from "./avalonia/input";
import { AvaloniaDOM } from "./avalonia/dom";
import { Caniuse } from "./avalonia/caniuse";
import { StreamHelper } from "./avalonia/stream";
import { NativeControlHost } from "./avalonia/nativeControlHost";

export async function createAvaloniaRuntime(api: RuntimeAPI): Promise<void> {
    api.setModuleImports("avalonia.ts", {
        Caniuse,
        Canvas,
        InputHelper,
        SizeWatcher,
        DpiWatcher,
        AvaloniaDOM,
        StreamHelper,
        NativeControlHost
    });
}
