import { CaretHelper } from "./caretHelper";
import { JsExports } from "./jsExports";

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

    public static async readClipboardText(globalThis: Window): Promise<string> {
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

    public static async writeClipboardText(globalThis: Window, text: string): Promise<void> {
        return await globalThis.navigator.clipboard.writeText(text);
    }

    public static subscribeInputEvents(element: HTMLInputElement, topLevelId: number) {
        const keySub = this.subscribeKeyEvents(element, topLevelId);
        const pointerSub = this.subscribePointerEvents(element, topLevelId);
        const textSub = this.subscribeTextEvents(element, topLevelId);
        const dndSub = this.subscribeDropEvents(element, topLevelId);
        const paneSub = this.subscribeKeyboardGeometryChange(element, topLevelId);

        return () => {
            keySub();
            pointerSub();
            textSub();
            dndSub();
            paneSub();
        };
    }

    public static subscribeKeyEvents(element: HTMLInputElement, topLevelId: number) {
        const keyDownHandler = (args: KeyboardEvent) => {
            JsExports.InputHelper.OnKeyDown(topLevelId, args.code, args.key, this.getModifiers(args))
                .then((handled: boolean) => {
                    if (!handled || this.clipboardState !== ClipboardState.Pending) {
                        args.preventDefault();
                    }
                });
        };
        element.addEventListener("keydown", keyDownHandler);

        const keyUpHandler = (args: KeyboardEvent) => {
            JsExports.InputHelper.OnKeyUp(topLevelId, args.code, args.key, this.getModifiers(args))
                .then((handled: boolean) => {
                    if (!handled) {
                        args.preventDefault();
                    }
                });

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
        topLevelId: number) {
        const compositionStartHandler = (args: CompositionEvent) => {
            JsExports.InputHelper.OnCompositionStart(topLevelId);
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

            JsExports.InputHelper.OnBeforeInput(topLevelId, args.inputType, start, end);
        };
        element.addEventListener("beforeinput", beforeInputHandler);

        const compositionUpdateHandler = (args: CompositionEvent) => {
            JsExports.InputHelper.OnCompositionUpdate(topLevelId, args.data);
        };
        element.addEventListener("compositionupdate", compositionUpdateHandler);

        const compositionEndHandler = (args: CompositionEvent) => {
            JsExports.InputHelper.OnCompositionEnd(topLevelId, args.data);
            args.preventDefault();
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
        topLevelId: number
    ) {
        const pointerMoveHandler = (args: PointerEvent) => {
            JsExports.InputHelper.OnPointerMove(
                topLevelId, args.pointerType, args.pointerId, args.offsetX, args.offsetY,
                args.pressure, args.tiltX, args.tiltY, args.twist, this.getModifiers(args), args);
            args.preventDefault();
        };

        const pointerDownHandler = (args: PointerEvent) => {
            JsExports.InputHelper.OnPointerDown(
                topLevelId, args.pointerType, args.pointerId, args.button, args.offsetX, args.offsetY,
                args.pressure, args.tiltX, args.tiltY, args.twist, this.getModifiers(args));
            args.preventDefault();
        };

        const pointerUpHandler = (args: PointerEvent) => {
            JsExports.InputHelper.OnPointerUp(
                topLevelId, args.pointerType, args.pointerId, args.button, args.offsetX, args.offsetY,
                args.pressure, args.tiltX, args.tiltY, args.twist, this.getModifiers(args));
            args.preventDefault();
        };

        const pointerCancelHandler = (args: PointerEvent) => {
            JsExports.InputHelper.OnPointerCancel(
                topLevelId, args.pointerType, args.pointerId, args.offsetX, args.offsetY,
                args.pressure, args.tiltX, args.tiltY, args.twist, this.getModifiers(args));
        };

        const wheelHandler = (args: WheelEvent) => {
            JsExports.InputHelper.OnWheel(
                topLevelId, args.offsetX, args.offsetY, args.deltaX, args.deltaY, this.getModifiers(args));
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

    public static subscribeDropEvents(
        element: HTMLInputElement,
        topLevelId: number
    ) {
        const handler = (args: DragEvent) => {
            const dataObject = args.dataTransfer;
            JsExports.InputHelper.OnDragDrop(topLevelId, args.type, args.offsetX, args.offsetY, this.getModifiers(args), dataObject?.effectAllowed, dataObject);
        };
        const overAndDropHandler = (args: DragEvent) => {
            args.preventDefault();
            handler(args);
        };
        element.addEventListener("dragover", overAndDropHandler);
        element.addEventListener("dragenter", handler);
        element.addEventListener("dragleave", handler);
        element.addEventListener("drop", overAndDropHandler);

        return () => {
            element.removeEventListener("dragover", overAndDropHandler);
            element.removeEventListener("dragenter", handler);
            element.removeEventListener("dragleave", handler);
            element.removeEventListener("drop", overAndDropHandler);
        };
    }

    public static getCoalescedEvents(pointerEvent: PointerEvent): number[] {
        return pointerEvent.getCoalescedEvents()
            .flatMap(e => [e.offsetX, e.offsetY, e.pressure, e.tiltX, e.tiltY, e.twist]);
    }

    public static subscribeKeyboardGeometryChange(
        element: HTMLInputElement,
        topLevelId: number) {
        if ("virtualKeyboard" in navigator) {
            // (navigator as any).virtualKeyboard.overlaysContent = true;
            const listener = (event: any) => {
                const elementRect = element.getBoundingClientRect();
                const keyboardRect = event.target.boundingRect as DOMRect;

                JsExports.InputHelper.OnKeyboardGeometryChange(
                    topLevelId,
                    keyboardRect.x - elementRect.x,
                    keyboardRect.y - elementRect.y,
                    keyboardRect.width,
                    keyboardRect.height);
            };
            (navigator as any).virtualKeyboard.addEventListener("geometrychange", listener);
            return () => {
                (navigator as any).virtualKeyboard.removeEventListener("geometrychange", listener);
            };
        }

        return () => {};
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

    private static getModifiers(args: KeyboardEvent | PointerEvent | WheelEvent | DragEvent): number {
        let modifiers = RawInputModifiers.None;

        if (args.ctrlKey) { modifiers |= RawInputModifiers.Control; }
        if (args.altKey) { modifiers |= RawInputModifiers.Alt; }
        if (args.shiftKey) { modifiers |= RawInputModifiers.Shift; }
        if (args.metaKey) { modifiers |= RawInputModifiers.Meta; }

        const buttons = (args as PointerEvent).buttons;
        if (buttons) {
            if (buttons & 1) { modifiers |= RawInputModifiers.LeftMouseButton; }
            if (buttons & 2) { modifiers |= (args.type === "pen" ? RawInputModifiers.PenBarrelButton : RawInputModifiers.RightMouseButton); }
            if (buttons & 4) { modifiers |= RawInputModifiers.MiddleMouseButton; }
            if (buttons & 8) { modifiers |= RawInputModifiers.XButton1MouseButton; }
            if (buttons & 16) { modifiers |= RawInputModifiers.XButton2MouseButton; }
            if (buttons & 32) { modifiers |= RawInputModifiers.PenEraser; }
        }

        return modifiers;
    }

    public static setPointerCapture(containerElement: HTMLInputElement, pointerId: number): void {
        containerElement.setPointerCapture(pointerId);
    }

    public static releasePointerCapture(containerElement: HTMLInputElement, pointerId: number): void {
        if (containerElement.hasPointerCapture(pointerId)) {
            containerElement.releasePointerCapture(pointerId);
        }
    }
}
