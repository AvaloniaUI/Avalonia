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

    createAvaloniaView(element: HTMLDivElement): void {
        const canvas = document.createElement("canvas");
        element.appendChild(canvas);



        this.dotnetAssembly.Avalonia.Web.AvaloniaRuntime.StartAvaloniaView(canvas);
    }
}