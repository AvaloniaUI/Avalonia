import { RuntimeAPI } from "../../types/dotnet";

import { Canvas } from "./canvas";
import { InputHelper } from "./input";

export class AvaloniaRuntime {
    constructor(
        private dotnetAssembly: any,
        api: RuntimeAPI
    ) {
        api.setModuleImports("avalonia.ts", {
            Canvas,
            InputHelper
        });
    }
}
