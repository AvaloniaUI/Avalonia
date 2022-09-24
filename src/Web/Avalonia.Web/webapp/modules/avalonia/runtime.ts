import { RuntimeAPI } from "../../types/dotnet";

import { SizeWatcher } from "./canvas";
import { DpiWatcher } from "./canvas";
import { Canvas } from "./canvas";
import { InputHelper } from "./input";

export class AvaloniaRuntime {
    constructor(
        private dotnetAssembly: any,
        api: RuntimeAPI
    ) {
        api.setModuleImports("avalonia.ts", {
            Canvas,
            InputHelper,
            SizeWatcher,
            DpiWatcher
        });
    }
}
