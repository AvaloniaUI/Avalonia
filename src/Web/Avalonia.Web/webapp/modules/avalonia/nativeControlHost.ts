class NativeControlHostTopLevelAttachment {
    _child?: HTMLElement;
    _host?: HTMLElement;
}

export class NativeControlHost {
    public static createDefaultChild(parent?: HTMLElement): HTMLElement {
        return document.createElement("div");
    }

    public static createAttachment(): NativeControlHostTopLevelAttachment {
        return new NativeControlHostTopLevelAttachment();
    }

    public static initializeWithChildHandle(element: NativeControlHostTopLevelAttachment, child: HTMLElement): void {
        element._child = child;
        element._child.style.position = "absolute";
    }

    public static attachTo(element: NativeControlHostTopLevelAttachment, host?: HTMLElement): void {
        if (element._host && element._child) {
            element._host.removeChild(element._child);
        }

        element._host = host;

        if (element._host && element._child) {
            element._host.appendChild(element._child);
        }
    }

    public static showInBounds(element: NativeControlHostTopLevelAttachment, x: number, y: number, width: number, height: number): void {
        if (element._child) {
            element._child.style.top = `${y}px`;
            element._child.style.left = `${x}px`;
            element._child.style.width = `${width}px`;
            element._child.style.height = `${height}px`;
            element._child.style.display = "block";
        }
    }

    public static hideWithSize(element: NativeControlHostTopLevelAttachment, width: number, height: number): void {
        if (element._child) {
            element._child.style.width = `${width}px`;
            element._child.style.height = `${height}px`;
            element._child.style.display = "none";
        }
    }

    public static releaseChild(element: NativeControlHostTopLevelAttachment): void {
        if (element._child) {
            element._child = undefined;
        }
    }
}
