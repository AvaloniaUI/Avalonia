import { RuntimeAPI } from "../types/dotnet";
import { SizeWatcher, DpiWatcher, Canvas } from "./avalonia/canvas";
import { InputHelper } from "./avalonia/input";
import { AvaloniaDOM } from "./avalonia/dom";
import { Caniuse } from "./avalonia/caniuse";
import { StreamHelper } from "./avalonia/stream";
import { NativeControlHost } from "./avalonia/nativeControlHost";

async function registerAvaloniaModule(api: RuntimeAPI): Promise<void> {
    api.setModuleImports("avalonia", {
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
export {
    Caniuse,
    Canvas,
    InputHelper,
    SizeWatcher,
    DpiWatcher,
    AvaloniaDOM,
    StreamHelper,
    NativeControlHost,

    registerAvaloniaModule
};
