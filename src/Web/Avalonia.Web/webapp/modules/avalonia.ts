import { RuntimeAPI } from "../types/dotnet";
import { SizeWatcher, DpiWatcher, Canvas } from "./avalonia/canvas";

import { InputHelper } from "./avalonia/input";
import { AvaloniaDOM } from "./avalonia/dom";

export async function createAvaloniaRuntime(api: RuntimeAPI): Promise<void> {
    api.setModuleImports("avalonia.ts", {
        Canvas,
        InputHelper,
        SizeWatcher,
        DpiWatcher,
        AvaloniaDOM
    });
}
