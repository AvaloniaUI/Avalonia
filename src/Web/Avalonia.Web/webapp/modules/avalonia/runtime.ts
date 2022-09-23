import { RuntimeAPI } from "../../types/dotnet";

import { Canvas } from "./canvas";

export class AvaloniaRuntime {
    constructor(
        private dotnetAssembly: any,
        api: RuntimeAPI
    ) {
        api.setModuleImports("avalonia.ts", {
            Canvas
        });
    }
}
