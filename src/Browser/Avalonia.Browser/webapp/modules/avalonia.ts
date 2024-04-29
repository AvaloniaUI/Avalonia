import { InputHelper } from "./avalonia/input";
import { AvaloniaDOM } from "./avalonia/dom";
import { Caniuse } from "./avalonia/caniuse";
import { StreamHelper } from "./avalonia/stream";
import { NativeControlHost } from "./avalonia/nativeControlHost";
import { NavigationHelper } from "./avalonia/navigationHelper";
import { GeneralHelpers } from "./avalonia/generalHelpers";
import { TimerHelper } from "./avalonia/timer";
import { CanvasFactory } from "./avalonia/surfaces/surfaceFactory";

async function registerServiceWorker(path: string, scope: string | undefined) {
    if ("serviceWorker" in navigator) {
        await globalThis.navigator.serviceWorker.register(path, scope ? { scope } : undefined);
    }
}

export {
    Caniuse,
    CanvasFactory,
    InputHelper,
    AvaloniaDOM,
    StreamHelper,
    NativeControlHost,
    NavigationHelper,
    GeneralHelpers,
    TimerHelper,
    registerServiceWorker
};
