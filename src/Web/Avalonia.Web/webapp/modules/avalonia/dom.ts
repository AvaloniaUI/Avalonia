export class AvaloniaDOM {
    static createAvaloniaHost(host: HTMLElement) {
        // Root element
        host.classList.add("avalonia-container");
        host.tabIndex = 0;
        host.oncontextmenu = function () { return false; };

        // Rendering target canvas
        const canvas = document.createElement("canvas");
        canvas.classList.add('avalonia-canvas');
        canvas.style.backgroundColor = "#ccc";
        canvas.style.width = "100%";
        canvas.style.height = "100%";
        host.appendChild(canvas);
        
        // Native controls host
        const nativeHost = document.createElement("div");
        nativeHost.classList.add('avalonia-native-host');
        nativeHost.style.left = "0px";
        nativeHost.style.top = "0px";
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
        inputElement.style.left = "0px";
        inputElement.style.top = "0px";
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
