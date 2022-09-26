import { RuntimeAPI } from "../types/dotnet";
import { SizeWatcher } from "./avalonia/canvas";
import { DpiWatcher } from "./avalonia/canvas";
import { Canvas } from "./avalonia/canvas";
import { InputHelper } from "./avalonia/input";
import { AvaloniaDOM } from "./avalonia/dom";
import { CaretHelper } from "./avalonia/CaretHelper"

export async function createAvaloniaRuntime(api: RuntimeAPI): Promise<void> {
    api.setModuleImports("avalonia.ts", {
        Canvas,
        InputHelper,
        SizeWatcher,
        DpiWatcher,
        AvaloniaDOM,
        CaretHelper
    });
}
