export class NativeControlHost {
    public static CreateDefaultChild(parent: HTMLElement): HTMLElement {
        return document.createElement("div");
    }

    // Used to convert ElementReference to JSObjectReference.
    // Is there a better way?
    public static GetReference(element: Element): Element {
        return element;
    }

    public static CreateAttachment(): NativeControlHostTopLevelAttachment {
        return new NativeControlHostTopLevelAttachment();
    }
}

class NativeControlHostTopLevelAttachment {
    _child?: HTMLElement;
    _host?: HTMLElement;

    InitializeWithChildHandle(child: HTMLElement) {
        this._child = child;
        this._child.style.position = "absolute";
    }

    AttachTo(host: HTMLElement): void {
        if (this._host && this._child) {
            this._host.removeChild(this._child);
        }

        this._host = host;

        if (this._host && this._child) {
            this._host.appendChild(this._child);
        }
    }

    ShowInBounds(x: number, y: number, width: number, height: number): void {
        if (this._child) {
            this._child.style.top = y + "px";
            this._child.style.left = x + "px";
            this._child.style.width = width + "px";
            this._child.style.height = height + "px";
            this._child.style.display = "block";
        }
    }

    HideWithSize(width: number, height: number): void {
        if (this._child) {
            this._child.style.width = width + "px";
            this._child.style.height = height + "px";
            this._child.style.display = "none";
        }
    }

    ReleaseChild(): void {
        if (this._child) {
            this._child = undefined;
        }
    }
}
