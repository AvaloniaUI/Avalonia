import { AvaloniaDOM } from "../dom";
import { SoftwareSurface } from "./softwareSurface";
import { BrowserRenderingMode, CanvasSurface } from "./surfaceBase";
import { WebGlSurface } from "./webGlSurface";

export class CanvasFactory {
    public static create(container: HTMLElement, mode: BrowserRenderingMode): CanvasSurface {
        if (!container) {
            throw new Error("No html container was provided.");
        }

        const canvas = AvaloniaDOM.createAvaloniaCanvas(container);
        AvaloniaDOM.attachCanvas(container, canvas);

        try {
            if (mode === BrowserRenderingMode.Software2D) {
                return new SoftwareSurface(canvas);
            } else if (mode === BrowserRenderingMode.WebGL1 || mode === BrowserRenderingMode.WebGL2) {
                return new WebGlSurface(canvas, mode);
            } else {
                throw new Error(`Unsupported rendering mode: ${BrowserRenderingMode[mode]}`);
            }
        } catch (ex) {
            AvaloniaDOM.detachCanvas(container, canvas);
            throw ex;
        }
    }

    public static destroy(surface: CanvasSurface) {
        surface.destroy();
    }

    public static onSizeChanged(surface: CanvasSurface, sizeChangedCallback: (width: number, height: number, dpr: number) => void) {
        surface.onSizeChanged(sizeChangedCallback);
    }

    public static ensureSize(surface: CanvasSurface): void {
        surface.ensureSize();
    }

    public static putPixelData(surface: SoftwareSurface, span: any /* IMemoryView */, width: number, height: number): void {
        surface.putPixelData(span, width, height);
    }
}
