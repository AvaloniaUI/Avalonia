import { InputHelper } from "./avalonia/input";
import { AvaloniaDOM } from "./avalonia/dom";
import { Caniuse } from "./avalonia/caniuse";
import { StreamHelper } from "./avalonia/stream";
import { NativeControlHost } from "./avalonia/nativeControlHost";
import { NavigationHelper } from "./avalonia/navigationHelper";
import { GeneralHelpers } from "./avalonia/generalHelpers";
import { TimerHelper } from "./avalonia/timer";
import { CanvasSurface } from "./avalonia/rendering/canvasSurface";
import { WebRenderTargetRegistry } from "./avalonia/rendering/webRenderTargetRegistry";
import { WebRenderTarget } from "./avalonia/rendering/webRenderTarget";
import { SoftwareRenderTarget } from "./avalonia/rendering/softwareRenderTarget";
import { WebGlRenderTarget } from "./avalonia/rendering/webGlRenderTarget";

async function registerServiceWorker(path: string, scope: string | undefined) {
    if ("serviceWorker" in navigator) {
        await globalThis.navigator.serviceWorker.register(path, scope ? { scope } : undefined);
    }
}

export {
    Caniuse,
    InputHelper,
    AvaloniaDOM,
    StreamHelper,
    NativeControlHost,
    NavigationHelper,
    GeneralHelpers,
    TimerHelper,
    WebRenderTarget,
    CanvasSurface,
    WebRenderTargetRegistry,
    SoftwareRenderTarget,
    WebGlRenderTarget,
    registerServiceWorker
};
