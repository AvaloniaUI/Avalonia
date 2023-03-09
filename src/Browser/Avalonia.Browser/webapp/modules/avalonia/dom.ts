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

    static createAvaloniaHost(host: HTMLElement) {
        const randomIdPart = Math.random().toString(36).replace(/[^a-z]+/g, "").substr(2, 10);

        // Root element
        host.classList.add("avalonia-container");
        host.tabIndex = 0;
        host.oncontextmenu = function () { return false; };
        host.style.overflow = "hidden";
        host.style.touchAction = "none";

        // Rendering target canvas
        const canvas = document.createElement("canvas");
        canvas.id = `canvas${randomIdPart}`;
        canvas.classList.add("avalonia-canvas");
        canvas.style.width = "100%";
        canvas.style.position = "absolute";

        // Native controls host
        const nativeHost = document.createElement("div");
        nativeHost.id = `nativeHost${randomIdPart}`;
        nativeHost.classList.add("avalonia-native-host");
        nativeHost.style.left = "0px";
        nativeHost.style.top = "0px";
        nativeHost.style.width = "100%";
        nativeHost.style.height = "100%";
        nativeHost.style.position = "absolute";

        // IME
        const inputElement = document.createElement("input");
        inputElement.id = `inputElement${randomIdPart}`;
        inputElement.classList.add("avalonia-input-element");
        inputElement.autocapitalize = "none";
        inputElement.type = "text";
        inputElement.spellcheck = false;
        inputElement.style.padding = "0";
        inputElement.style.margin = "0";
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
        host.prepend(canvas);

        return {
            host,
            canvas,
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

    public static getSafeAreaPadding(): number[] {
        const top = parseFloat(getComputedStyle(document.documentElement).getPropertyValue("--sat"));
        const bottom = parseFloat(getComputedStyle(document.documentElement).getPropertyValue("--sab"));
        const left = parseFloat(getComputedStyle(document.documentElement).getPropertyValue("--sal"));
        const right = parseFloat(getComputedStyle(document.documentElement).getPropertyValue("--sar"));

        return [left, top, bottom, right];
    }
}
