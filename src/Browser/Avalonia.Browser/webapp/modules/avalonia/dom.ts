
export class AvaloniaDOM {
    public static addClass(element: HTMLElement, className: string): void {
        element.classList.add(className);
    }

    static observeDarkMode(observer: (isDarkMode: boolean, isHighContrast: boolean) => boolean) {
        if (globalThis.matchMedia === undefined) {
            return false;
        }

        const colorShemeMedia = globalThis.matchMedia("(prefers-color-scheme: dark)");
        const prefersContrastMedia = globalThis.matchMedia("(prefers-contrast: more)");

        colorShemeMedia.addEventListener("change", (args: MediaQueryListEvent) => {
            observer(args.matches, prefersContrastMedia.matches);
        });
        prefersContrastMedia.addEventListener("change", (args: MediaQueryListEvent) => {
            observer(colorShemeMedia.matches, args.matches);
        });

        return {
            isDarkMode: colorShemeMedia.matches,
            isHighContrast: prefersContrastMedia.matches
        };
    }

    static getFirstElementByClassName(className: string, parent?: HTMLElement): Element | null {
        const elements = (parent ?? globalThis.document).getElementsByClassName(className);
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

    public static isFullscreen(): boolean {
        return document.fullscreenElement != null;
    }

    public static async setFullscreen(isFullscreen: boolean) {
        if (isFullscreen) {
            const doc = document.documentElement;
            await doc.requestFullscreen();
        } else {
            await document.exitFullscreen();
        }
    }

    public static initSafeAreaPadding(): void {
        document.documentElement.style.setProperty("--av-sat", "env(safe-area-inset-top)");
        document.documentElement.style.setProperty("--av-sar", "env(safe-area-inset-right)");
        document.documentElement.style.setProperty("--av-sab", "env(safe-area-inset-bottom)");
        document.documentElement.style.setProperty("--av-sal", "env(safe-area-inset-left)");
    }

    public static getSafeAreaPadding(): number[] {
        const top = parseFloat(getComputedStyle(document.documentElement).getPropertyValue("--av-sat"));
        const bottom = parseFloat(getComputedStyle(document.documentElement).getPropertyValue("--av-sab"));
        const left = parseFloat(getComputedStyle(document.documentElement).getPropertyValue("--av-sal"));
        const right = parseFloat(getComputedStyle(document.documentElement).getPropertyValue("--av-sar"));

        return [left, top, bottom, right];
    }
}
