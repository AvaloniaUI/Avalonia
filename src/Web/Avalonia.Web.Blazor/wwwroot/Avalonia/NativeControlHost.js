export class NativeControlHost {
    static CreateDefaultChild(parent) {
        return document.createElement("div");
    }
    static GetReference(element) {
        return element;
    }
    static CreateAttachment() {
        return new NativeControlHostTopLevelAttachment();
    }
}
class NativeControlHostTopLevelAttachment {
    InitializeWithChildHandle(child) {
        this._child = child;
        this._child.style.position = "absolute";
    }
    AttachTo(host) {
        if (this._host && this._child) {
            this._host.removeChild(this._child);
        }
        this._host = host;
        if (this._host && this._child) {
            this._host.appendChild(this._child);
        }
    }
    ShowInBounds(x, y, width, height) {
        if (this._child) {
            this._child.style.top = y + "px";
            this._child.style.left = x + "px";
            this._child.style.width = width + "px";
            this._child.style.height = height + "px";
            this._child.style.display = "block";
        }
    }
    HideWithSize(width, height) {
        if (this._child) {
            this._child.style.width = width + "px";
            this._child.style.height = height + "px";
            this._child.style.display = "none";
        }
    }
    ReleaseChild() {
        if (this._child) {
            this._child = undefined;
        }
    }
}
//# sourceMappingURL=NativeControlHost.js.map