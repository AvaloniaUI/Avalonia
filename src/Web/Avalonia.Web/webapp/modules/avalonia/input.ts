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

export class InputHelper {
    public static subscribeKeyEvents(
        element: HTMLInputElement,
        keyDownCallback: (code: string, key: string, modifiers: RawInputModifiers) => boolean,
        keyUpCallback: (code: string, key: string, modifiers: RawInputModifiers) => boolean) {
        const keyDownHandler = (args: KeyboardEvent) => {
            if (keyDownCallback(args.code, args.key, this.getModifiers(args))) {
                args.preventDefault();
            }
        };
        element.addEventListener("keydown", keyDownHandler);

        const keyUpHandler = (args: KeyboardEvent) => {
            if (keyUpCallback(args.code, args.key, this.getModifiers(args))) {
                args.preventDefault();
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
        inputCallback: (type: string, data: string | null) => boolean,
        compositionStartCallback: (args: CompositionEvent) => boolean,
        compositionUpdateCallback: (args: CompositionEvent) => boolean,
        compositionEndCallback: (args: CompositionEvent) => boolean) {
        const inputHandler = (args: Event) => {
            const inputEvent = args as InputEvent;

            // todo check cast
            if (inputCallback(inputEvent.type, inputEvent.data)) {
                args.preventDefault();
            }
        };
        element.addEventListener("input", inputHandler);

        const compositionStartHandler = (args: CompositionEvent) => {
            if (compositionStartCallback(args)) {
                args.preventDefault();
            }
        };
        element.addEventListener("compositionstart", compositionStartHandler);

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
            element.removeEventListener("input", inputHandler);
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

    public static getCoalescedEvents(pointerEvent: PointerEvent): PointerEvent[] {
        return pointerEvent.getCoalescedEvents();
    }

    public static clearInput(inputElement: HTMLInputElement) {
        inputElement.value = "";
    }

    public static focusElement(inputElement: HTMLElement) {
        inputElement.focus();
    }

    public static setCursor(inputElement: HTMLInputElement, kind: string) {
        if (kind === "pointer") {
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
