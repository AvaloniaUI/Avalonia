export class AvaloniaDOM {
    static createAvaloniaHost(host: HTMLElement) {
        // Root element
        host.classList.add("avalonia-container");
        host.tabIndex = 0;
        host.style.touchAction = "none";
        // host.style.position = "relative";
        host.style.position = "fixed";
        host.style.width = "100vw";
        host.style.height = "100vh";
        host.oncontextmenu = function () { return false; };

        // Rendering target canvas
        const canvas = document.createElement("canvas");
        canvas.classList.add('avalonia-canvas');
        canvas.style.backgroundColor = "#ccc";
        canvas.style.width = "100%";
        canvas.style.height = "100%";
        //canvas.style.position = "absolute";
        host.appendChild(canvas);
        
        // Native controls host
        const nativeHost = document.createElement("div");
        nativeHost.classList.add('avalonia-native-host');
        nativeHost.style.width = "100%";
        nativeHost.style.height = "100%";
        nativeHost.style.position = "absolute";
        host.appendChild(nativeHost);

        // IME
        const inputElement = document.createElement("input");
        inputElement.autocapitalize = "none";
        inputElement.type = "text";
        inputElement.classList.add('avalonia-input-element');
        inputElement.style.opacity = "0";
        inputElement.style.width = "100%";
        inputElement.style.height = "100%";
        inputElement.style.position = "absolute";
        inputElement.onpaste = function () { return false; };
        inputElement.oncopy = function () { return false; };
        inputElement.oncut = function () { return false; };
        host.appendChild(inputElement);

        return {
            host,
            canvas,
            nativeHost,
            inputElement
        };
    }
}
