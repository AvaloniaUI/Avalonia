import { CaretHelper } from "./caretHelper";

enum RawInputModifiers {
    None = 0,
    Alt = 1,
    Control = 2,
    Shift = 4,
    Meta = 8,

    LeftMouseButton = 16,
    RightMouseButton = 32,
    MiddleMouseButton = 64,
    XButton1MouseButton = 128,
    XButton2MouseButton = 256,
    KeyboardMask = Alt | Control | Shift | Meta,

    PenInverted = 512,
    PenEraser = 1024,
    PenBarrelButton = 2048
}

/*
* This is a hack to handle Mozilla clipboard events in a more convinient way for framework users.
* In the browser, events go in order KeyDown -> Paste -> KeyUp.
* On KeyDown we trigger Avalonia handlers, which might execute readClipboardText.
* When readClipboardText was executed, we mark ClipboardState as Pending and setup clipboard promise,
* which will un-handle KeyDown event, basically allowing browser to pass a Paste event properly.
* On actual Paste event we execute promise callbacks, resuming async operation, and returning pasted text to the app.
* Note #1, on every KeyUp event we will reset all the state and reject pending promises if any, as this event it expected to come after Paste.
* Note #2, whole this code will be executed only on legacy browsers like Mozilla, where clipboard.readText is not available.
* Note #3, with all of these hacks Clipboard.ReadText will still work only on actual "paste" gesture initiated by user.
* */
enum ClipboardState {
    None,
    Ready,
    Pending
}

export class InputHelper {
    static clipboardState: ClipboardState = ClipboardState.None;
    static resolveClipboard?: any;
    static rejectClipboard?: any;

    public static initializeBackgroundHandlers() {
        if (this.clipboardState !== ClipboardState.None) {
            return;
        }

        globalThis.addEventListener("paste", (args: any) => {
            if (this.clipboardState === ClipboardState.Pending) {
                this.resolveClipboard(args.clipboardData.getData("text"));
            }
        });
        this.clipboardState = ClipboardState.Ready;
    }

    public static async readClipboardText(): Promise<string> {
        if (globalThis.navigator.clipboard.readText) {
            return await globalThis.navigator.clipboard.readText();
        } else {
            try {
                return await new Promise<any>((resolve, reject) => {
                    this.clipboardState = ClipboardState.Pending;
                    this.resolveClipboard = resolve;
                    this.rejectClipboard = reject;
                });
            } finally {
                this.clipboardState = ClipboardState.Ready;
                this.resolveClipboard = null;
                this.rejectClipboard = null;
            }
        }
    }

    public static subscribeKeyEvents(
        element: HTMLInputElement,
        keyDownCallback: (code: string, key: string, modifiers: RawInputModifiers) => boolean,
        keyUpCallback: (code: string, key: string, modifiers: RawInputModifiers) => boolean) {
        const keyDownHandler = (args: KeyboardEvent) => {
            if (keyDownCallback(args.code, args.key, this.getModifiers(args))) {
                if (this.clipboardState !== ClipboardState.Pending) {
                    args.preventDefault();
                }
            }
        };
        element.addEventListener("keydown", keyDownHandler);

        const keyUpHandler = (args: KeyboardEvent) => {
            if (keyUpCallback(args.code, args.key, this.getModifiers(args))) {
                args.preventDefault();
            }
            if (this.rejectClipboard) {
                this.rejectClipboard();
            }
        };

        element.addEventListener("keyup", keyUpHandler);

        return () => {
            element.removeEventListener("keydown", keyDownHandler);
            element.removeEventListener("keyup", keyUpHandler);
        };
    }

    public static subscribeTextEvents(
        element: HTMLInputElement,
        beforeInputCallback: (args: InputEvent, start: number, end: number) => boolean,
        compositionStartCallback: (args: CompositionEvent) => boolean,
        compositionUpdateCallback: (args: CompositionEvent) => boolean,
        compositionEndCallback: (args: CompositionEvent) => boolean) {
        const compositionStartHandler = (args: CompositionEvent) => {
            if (compositionStartCallback(args)) {
                args.preventDefault();
            }
        };
        element.addEventListener("compositionstart", compositionStartHandler);

        const beforeInputHandler = (args: InputEvent) => {
            const ranges = args.getTargetRanges();
            let start = -1;
            let end = -1;
            if (ranges.length > 0) {
                start = ranges[0].startOffset;
                end = ranges[0].endOffset;
            }

            if (args.inputType === "insertCompositionText") {
                start = 2;
                end = start + 2;
            }
            if (beforeInputCallback(args, start, end)) {
                args.preventDefault();
            }
        };
        element.addEventListener("beforeinput", beforeInputHandler);

        const compositionUpdateHandler = (args: CompositionEvent) => {
            if (compositionUpdateCallback(args)) {
                args.preventDefault();
            }
        };
        element.addEventListener("compositionupdate", compositionUpdateHandler);

        const compositionEndHandler = (args: CompositionEvent) => {
            if (compositionEndCallback(args)) {
                args.preventDefault();
            }
        };
        element.addEventListener("compositionend", compositionEndHandler);

        return () => {
            element.removeEventListener("compositionstart", compositionStartHandler);
            element.removeEventListener("compositionupdate", compositionUpdateHandler);
            element.removeEventListener("compositionend", compositionEndHandler);
        };
    }

    public static subscribePointerEvents(
        element: HTMLInputElement,
        pointerMoveCallback: (args: PointerEvent) => boolean,
        pointerDownCallback: (args: PointerEvent) => boolean,
        pointerUpCallback: (args: PointerEvent) => boolean,
        pointerCancelCallback: (args: PointerEvent) => boolean,
        wheelCallback: (args: WheelEvent) => boolean
    ) {
        const pointerMoveHandler = (args: PointerEvent) => {
            pointerMoveCallback(args);
            args.preventDefault();
        };

        const pointerDownHandler = (args: PointerEvent) => {
            pointerDownCallback(args);
            args.preventDefault();
        };

        const pointerUpHandler = (args: PointerEvent) => {
            pointerUpCallback(args);
            args.preventDefault();
        };

        const pointerCancelHandler = (args: PointerEvent) => {
            pointerCancelCallback(args);
            args.preventDefault();
        };

        const wheelHandler = (args: WheelEvent) => {
            wheelCallback(args);
            args.preventDefault();
        };

        element.addEventListener("pointermove", pointerMoveHandler);
        element.addEventListener("pointerdown", pointerDownHandler);
        element.addEventListener("pointerup", pointerUpHandler);
        element.addEventListener("wheel", wheelHandler);
        element.addEventListener("pointercancel", pointerCancelHandler);

        return () => {
            element.removeEventListener("pointerover", pointerMoveHandler);
            element.removeEventListener("pointerdown", pointerDownHandler);
            element.removeEventListener("pointerup", pointerUpHandler);
            element.removeEventListener("pointercancel", pointerCancelHandler);
            element.removeEventListener("wheel", wheelHandler);
        };
    }

    public static subscribeInputEvents(
        element: HTMLInputElement,
        inputCallback: (value: string) => boolean
    ) {
        const inputHandler = (args: Event) => {
            if (inputCallback((args as any).value)) {
                args.preventDefault();
            }
        };
        element.addEventListener("input", inputHandler);

        return () => {
            element.removeEventListener("input", inputHandler);
        };
    }

    public static subscribeDropEvents(
        element: HTMLInputElement,
        dragEvent: (args: any) => boolean
    ) {
        const dragHandler = (args: Event) => {
            if (dragEvent(args as any)) {
                args.preventDefault();
            }
        };
        element.addEventListener("dragover", dragHandler);
        element.addEventListener("dragenter", dragHandler);
        element.addEventListener("dragleave", dragHandler);
        element.addEventListener("drop", dragHandler);

        return () => {
            element.removeEventListener("dragover", dragHandler);
            element.removeEventListener("dragenter", dragHandler);
            element.removeEventListener("dragleave", dragHandler);
            element.removeEventListener("drop", dragHandler);
        };
    }

    public static getCoalescedEvents(pointerEvent: PointerEvent): PointerEvent[] {
        return pointerEvent.getCoalescedEvents();
    }

    public static subscribeKeyboardGeometryChange(
        element: HTMLInputElement,
        handler: (args: any) => boolean) {
        if ("virtualKeyboard" in navigator) {
            // (navigator as any).virtualKeyboard.overlaysContent = true;
            (navigator as any).virtualKeyboard.addEventListener("geometrychange", (event: any) => {
                const elementRect = element.getBoundingClientRect();
                const keyboardRect = event.target.boundingRect as DOMRect;
                handler({
                    x: keyboardRect.x - elementRect.x,
                    y: keyboardRect.y - elementRect.y,
                    width: keyboardRect.width,
                    height: keyboardRect.height
                });
            });
        }
    }

    public static subscribeVisibilityChange(
        handler: (state: boolean) => void): boolean {
        document.addEventListener("visibilitychange", () => {
            handler(document.visibilityState === "visible");
        });
        return document.visibilityState === "visible";
    }

    public static clearInput(inputElement: HTMLInputElement) {
        inputElement.value = "";
    }

    public static focusElement(inputElement: HTMLElement) {
        inputElement.focus();
    }

    public static setCursor(inputElement: HTMLInputElement, kind: string) {
        if (kind === "default") {
            inputElement.style.removeProperty("cursor");
        } else {
            inputElement.style.cursor = kind;
        }
    }

    public static setBounds(inputElement: HTMLInputElement, x: number, y: number, caretWidth: number, caretHeight: number, caret: number) {
        inputElement.style.left = (x).toFixed(0) + "px";
        inputElement.style.top = (y).toFixed(0) + "px";

        const { left, top } = CaretHelper.getCaretCoordinates(inputElement, caret);

        inputElement.style.left = (x - left).toFixed(0) + "px";
        inputElement.style.top = (y - top).toFixed(0) + "px";
    }

    public static hide(inputElement: HTMLInputElement) {
        inputElement.style.display = "none";
    }

    public static show(inputElement: HTMLInputElement) {
        inputElement.style.display = "block";
    }

    public static setSurroundingText(inputElement: HTMLInputElement, text: string, start: number, end: number) {
        if (!inputElement) {
            return;
        }

        inputElement.value = text;
        inputElement.setSelectionRange(start, end);
        inputElement.style.width = "20px";
        inputElement.style.width = `${inputElement.scrollWidth}px`;
    }

    private static getModifiers(args: KeyboardEvent): RawInputModifiers {
        let modifiers = RawInputModifiers.None;

        if (args.ctrlKey) { modifiers |= RawInputModifiers.Control; }
        if (args.altKey) { modifiers |= RawInputModifiers.Alt; }
        if (args.shiftKey) { modifiers |= RawInputModifiers.Shift; }
        if (args.metaKey) { modifiers |= RawInputModifiers.Meta; }

        return modifiers;
    }
}
