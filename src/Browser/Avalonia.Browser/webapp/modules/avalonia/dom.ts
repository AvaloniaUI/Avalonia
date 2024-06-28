import { JsExports } from "./jsExports";

export class AvaloniaDOM {
    public static getGlobalThis() {
        return globalThis;
    }

    public static addClass(element: HTMLElement, className: string): void {
        element.classList.add(className);
    }

    static getFirstElementById(className: string, parent: HTMLElement | Window): Element | null {
        const parentNode = parent instanceof Window
            ? parent.document
            : parent.ownerDocument;

        return parentNode.getElementById(className);
    }

    static getFirstElementByClassName(className: string, parent: HTMLElement | Window): Element | null {
        const parentNode = parent instanceof Window
            ? parent.document
            : parent;

        const elements = parentNode.getElementsByClassName(className);
        return elements ? elements[0] : null;
    }

    static createAvaloniaCanvas(host: HTMLElement): HTMLCanvasElement {
        const containerId = host.getAttribute("data-containerId") ?? "0000";

        const canvas = document.createElement("canvas");
        canvas.id = `canvas${containerId}`;
        canvas.classList.add("avalonia-canvas");
        canvas.style.width = "100%";
        canvas.style.height = "100%";
        canvas.style.position = "absolute";

        return canvas;
    }

    static attachCanvas(host: HTMLElement, canvas: HTMLCanvasElement): void {
        host.prepend(canvas);
    }

    static detachCanvas(host: HTMLElement, canvas: HTMLCanvasElement): void {
        host.removeChild(canvas);
    }

    static createAvaloniaHost(host: HTMLElement) {
        const containerId = Math.random().toString(36).replace(/[^a-z]+/g, "").substr(2, 10);

        // Root element
        host.classList.add("avalonia-container");
        host.tabIndex = 0;
        host.setAttribute("data-containerId", containerId);
        host.oncontextmenu = function () { return false; };
        host.style.overflow = "hidden";
        host.style.touchAction = "none";

        // Canvas is lazily created depending on the rendering mode. See createAvaloniaCanvas usage.

        // Native controls host
        const nativeHost = document.createElement("div");
        nativeHost.id = `nativeHost${containerId}`;
        nativeHost.classList.add("avalonia-native-host");
        nativeHost.style.left = "0px";
        nativeHost.style.top = "0px";
        nativeHost.style.width = "100%";
        nativeHost.style.height = "100%";
        nativeHost.style.position = "absolute";

        // IME
        const inputElement = document.createElement("input");
        inputElement.id = `inputElement${containerId}`;
        inputElement.classList.add("avalonia-input-element");
        inputElement.autocapitalize = "none";
        inputElement.type = "text";
        inputElement.spellcheck = false;
        inputElement.style.padding = "0";
        inputElement.style.margin = "0";
        inputElement.style.borderWidth = "0";
        inputElement.style.position = "absolute";
        inputElement.style.overflow = "hidden";
        inputElement.style.borderStyle = "hidden";
        inputElement.style.outline = "none";
        inputElement.style.background = "transparent";
        inputElement.style.color = "transparent";
        inputElement.style.display = "none";
        inputElement.style.height = "20px";
        inputElement.style.zIndex = "-1";
        inputElement.onpaste = function () { return false; };
        inputElement.oncopy = function () { return false; };
        inputElement.oncut = function () { return false; };

        host.prepend(inputElement);
        host.prepend(nativeHost);

        return {
            host,
            nativeHost,
            inputElement
        };
    }

    public static isFullscreen(globalThis: Window): boolean {
        return globalThis.document.fullscreenElement != null;
    }

    public static async setFullscreen(globalThis: Window, isFullscreen: boolean) {
        if (isFullscreen) {
            const doc = globalThis.document.documentElement;
            await doc.requestFullscreen();
        } else {
            await globalThis.document.exitFullscreen();
        }
    }

    public static initGlobalDomEvents(globalThis: Window): void {
        // Init Safe Area properties.
        globalThis.document.documentElement.style.setProperty("--av-sat", "env(safe-area-inset-top)");
        globalThis.document.documentElement.style.setProperty("--av-sar", "env(safe-area-inset-right)");
        globalThis.document.documentElement.style.setProperty("--av-sab", "env(safe-area-inset-bottom)");
        globalThis.document.documentElement.style.setProperty("--av-sal", "env(safe-area-inset-left)");

        // Subscribe on DarkMode changes.
        if (globalThis.matchMedia !== undefined) {
            const colorSchemeMedia = globalThis.matchMedia("(prefers-color-scheme: dark)");
            const prefersContrastMedia = globalThis.matchMedia("(prefers-contrast: more)");

            colorSchemeMedia.addEventListener("change", (args: MediaQueryListEvent) => {
                JsExports.DomHelper.DarkModeChanged(args.matches, prefersContrastMedia.matches);
            });
            prefersContrastMedia.addEventListener("change", (args: MediaQueryListEvent) => {
                JsExports.DomHelper.DarkModeChanged(colorSchemeMedia.matches, args.matches);
            });
        }

        globalThis.document.addEventListener("visibilitychange", () => {
            JsExports.DomHelper.DocumentVisibilityChanged(globalThis.document.visibilityState);
        });

        // Report initial value.
        if (globalThis.document.visibilityState === "visible") {
            globalThis.setTimeout(() => {
                JsExports.DomHelper.DocumentVisibilityChanged(globalThis.document.visibilityState);
            }, 10);
        }
    }

    public static getSafeAreaPadding(globalThis: Window): number[] {
        const top = parseFloat(getComputedStyle(globalThis.document.documentElement).getPropertyValue("--av-sat"));
        const bottom = parseFloat(getComputedStyle(globalThis.document.documentElement).getPropertyValue("--av-sab"));
        const left = parseFloat(getComputedStyle(globalThis.document.documentElement).getPropertyValue("--av-sal"));
        const right = parseFloat(getComputedStyle(globalThis.document.documentElement).getPropertyValue("--av-sar"));

        return [left, top, bottom, right];
    }

    public static getDarkMode(globalThis: Window): number[] {
        if (globalThis.matchMedia === undefined) return [0, 0];

        const colorSchemeMedia = globalThis.matchMedia("(prefers-color-scheme: dark)");
        const prefersContrastMedia = globalThis.matchMedia("(prefers-contrast: more)");
        return [
            colorSchemeMedia.matches ? 1 : 0,
            prefersContrastMedia.matches ? 1 : 0
        ];
    }
}
