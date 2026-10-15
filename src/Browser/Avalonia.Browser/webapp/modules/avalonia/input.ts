import { CaretHelper } from "./caretHelper";
import { JsExports } from "./jsExports";
import { IMemoryView } from "../../types/dotnet";
import { StorageItem } from "../storage/storageItem";
import { InputQueue, InputRecordType } from "./inputQueue";

/*
* This is a hack to handle older Firefox (before v127 from June 2024) clipboard events in a more convenient way for framework users.
* In the browser, events go in order KeyDown -> Paste -> KeyUp.
* On KeyDown we trigger Avalonia handlers, which might execute readClipboard.
* When readClipboard was executed, we mark ClipboardState as Pending and setup clipboard promise,
* which will un-handle KeyDown event, basically allowing browser to pass a Paste event properly.
* On actual Paste event we execute promise callbacks, resuming async operation, and returning pasted text to the app.
* Note #1, on every KeyUp event we will reset all the state and reject pending promises if any, as this event it expected to come after Paste.
* Note #2, whole this code will be executed only on older browsers where clipboard.read/readText is not available.
* Note #3, with all of these hacks Clipboard.ReadText will still work only on actual "paste" gesture initiated by user.
* */
enum ClipboardState {
    None,
    Ready,
    Pending
}

interface WriteableClipboardItem {
    data: Record<string, string | Blob>;
}

interface WriteableClipboardSource {
    items: WriteableClipboardItem[];
}

interface ClipboardResult {
    error: string;
    result: ReadableDataItem[]
}

type ReadableDataItem = {
    type: "clipboardItem";
    value: ClipboardItem;
} | {
    type: "dataTransferItem";
    value: DataTransferItem;
} | {
    type: "string";
    value: string;
};

type ReadableDataValue = {
    type: "string";
    value: string;
} | {
    type: "bytes";
    value: Uint8Array;
} | {
    type: "file";
    value: StorageItem;
};

export class InputHelper {
    static clipboardState: ClipboardState = ClipboardState.None;
    static resolveClipboard?: (value: ClipboardResult) => void;
    static rejectClipboard?: (reason?: any) => void;

    public static initializeBackgroundHandlers() {
        if (this.clipboardState !== ClipboardState.None) {
            return;
        }

        globalThis.document.addEventListener("paste", args => {
            if (this.clipboardState !== ClipboardState.Pending || !this.resolveClipboard) {
                return;
            }

            const items = this.getDataTransferItems(args.clipboardData);
            this.resolveClipboard({ result: items.map((item) => ({ type: "dataTransferItem", value: item }))} as ClipboardResult);
        });
        this.clipboardState = ClipboardState.Ready;
    }

    private static getDataTransferItems(dataTransfer?: DataTransfer | null): DataTransferItem[] {
        const dataTransferList = dataTransfer?.items;
        return dataTransferList == null ? [] : Array.from(dataTransferList);
    }

    public static isClipboardFormatSupported(format: string): boolean {
        if (ClipboardItem.supports) {
            return ClipboardItem.supports(format);
        }

        return format === "text/plain" || format === "text/html" || format === "image/png";
    }

    public static createWriteableClipboardSource(): WriteableClipboardSource {
        return { items: [] };
    }

    public static createWriteableClipboardItem(source: WriteableClipboardSource): WriteableClipboardItem {
        const item = { data: {} };
        source.items.push(item);
        return item;
    }

    public static addStringToWriteableClipboardItem(item: WriteableClipboardItem, format: string, value: string) {
        item.data[format] = value;
    }

    public static addBytesToWriteableClipboardItem(item: WriteableClipboardItem, format: string, value: IMemoryView) {
        const bytes = value.slice(0, value.byteLength);
        item.data[format] = new Blob([bytes], { type: format });
    }

    public static async readClipboard(window: Window): Promise<ClipboardResult> {
        const clipboard = window.navigator.clipboard;

        try {
            if (clipboard.read) {
                const clipboardItems = await clipboard.read();
                return { result: clipboardItems.map((item) => ({ type: "clipboardItem", value: item })) } as ClipboardResult;
            } else if (clipboard.readText) {
                const item: ReadableDataItem = {
                    type: "string",
                    value: await clipboard.readText()
                };
                return { result: [item] } as ClipboardResult;
            } else {
                try {
                    return await new Promise<ClipboardResult>((resolve, reject) => {
                        this.clipboardState = ClipboardState.Pending;
                        this.resolveClipboard = resolve;
                        this.rejectClipboard = reject;
                    });
                } finally {
                    this.clipboardState = ClipboardState.Ready;
                    this.resolveClipboard = undefined;
                    this.rejectClipboard = undefined;
                }
            }
        }
        catch (ex: unknown) {
            if (ex instanceof Error && ex.name == "NotAllowedError") {
                return { error: "denied" } as ClipboardResult;
            }
            throw ex;
        }
    }

    public static async writeClipboard(window: Window, source?: WriteableClipboardSource | null): Promise<string> {
        try {
            const items = source?.items ?? [];
            if (items.length === 0) {
                await window.navigator.clipboard.writeText("");
                return "";
            }

            if (window.navigator.clipboard.write) {
                await window.navigator.clipboard.write(items.map(item => new ClipboardItem(item.data)))
            }
            else {
                await this.writeFirstText(window, items)
            }

            return "";
        }
        catch (error: unknown) {
            if (error instanceof Error && error.name == "NotAllowedError") {
                return "denied";
            }
            throw error;
        }
    }

    private static async writeFirstText(window: Window, items: WriteableClipboardItem[]): Promise<void> {
        for (const item of items) {
            for (const format in item.data) {
                if (!format.startsWith("text/")) {
                    continue;
                }

                let value = item.data[format];
                if (typeof value !== "string") {
                    value = "";
                }

                await window.navigator.clipboard.writeText(value);
                return;
            }
        }
    }

    public static getReadableDataItemFormats(item: ReadableDataItem): readonly string[] {
        /* eslint-disable indent */
        switch (item.type) {
            case "clipboardItem":
                return item.value.types;
            case "dataTransferItem":
                switch (item.value.kind) {
                    case "string":
                        return [item.value.type];
                    case "file":
                        return ["Files"];
                    default:
                        return [];
                }
            case "string":
                return ["text/plain"];
            default:
                return [];
        }
        /* eslint-enable indent */
    }

    // Asynchronous, used to read the clipboard.
    public static async tryGetReadableDataItemValueAsync(item: ReadableDataItem, format: string): Promise<ReadableDataValue | null> {
        const type = item.type;

        /* eslint-disable indent */
        switch (type) {
            case "clipboardItem": {
                const clipboardItem = item.value;
                if (!clipboardItem.types.includes(format)) {
                    return null;
                }

                const blob = await clipboardItem.getType(format);

                return format.startsWith("text/")
                    ? { type: "string", value: await blob.text() }
                    : { type: "bytes", value: await this.getBlobBytes(blob) };
            }

            case "dataTransferItem": {
                const dataTransferItem = item.value;

                switch (dataTransferItem.kind) {
                    case "string": {
                        if (format !== dataTransferItem.type) {
                            return null;
                        }

                        const stringValue = await new Promise<string>((resolve) => dataTransferItem.getAsString((str) => resolve(str)));
                        return { type: "string", value: stringValue };
                    }

                    case "file": {
                        if (format !== "Files") {
                            return null;
                        }

                        const file = dataTransferItem.getAsFile();
                        return file == null ? null : { type: "file", value: StorageItem.createFromFile(file) };
                    }

                    default:
                        return null;
                }
            }

            case "string": {
                return format.startsWith("text/")
                    ? { type: "string", value: item.value }
                    : { type: "bytes", value: await this.getBlobBytes(new Blob([item.value])) };
            }

            default:
                return null;
        }
        /* eslint-enable indent */
    }

    // Synchronous, used only to read a drag-and-drop item.
    public static tryGetReadableDataItemValue(item: ReadableDataItem, format: string): ReadableDataValue | null {
        const type = item.type;

        if (type !== "dataTransferItem") {
            return null;
        }

        const dataTransferItem = item.value;

        /* eslint-disable indent */
        switch (dataTransferItem.kind) {
            case "string": {
                if (format !== dataTransferItem.type) {
                    return null;
                }

                let stringValue = "";
                dataTransferItem.getAsString(function (str) { stringValue = str; });
                return { type: "string", value: stringValue };
            }

            case "file": {
                if (format !== "Files") {
                    return null;
                }

                const file = dataTransferItem.getAsFile();
                return file == null ? null : { type: "file", value: StorageItem.createFromFile(file) };
            }

            default:
                return null;
        }
        /* eslint-enable indent */
    }

    private static async getBlobBytes(blob: Blob): Promise<Uint8Array> {
        return blob.bytes
            ? await blob.bytes()
            : new Uint8Array(await blob.arrayBuffer());
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
        // Key events go through the input queue and are flushed synchronously, so Avalonia's handled
        // state is known before the browser applies the default action.
        const keyDownHandler = (args: KeyboardEvent) => {
            const handled = InputQueue.postKey(InputRecordType.KeyDown, topLevelId, args);
            if (!handled || this.clipboardState !== ClipboardState.Pending) {
                args.preventDefault();
            }
        };
        element.addEventListener("keydown", keyDownHandler);

        const keyUpHandler = (args: KeyboardEvent) => {
            const handled = InputQueue.postKey(InputRecordType.KeyUp, topLevelId, args);
            if (!handled) {
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
        topLevelId: number) {
        // Text events bypass the input queue; flushing first keeps them ordered after queued input.
        const compositionStartHandler = (args: CompositionEvent) => {
            InputQueue.flush();
            JsExports.InputHelper.OnCompositionStart(topLevelId);
        };
        element.addEventListener("compositionstart", compositionStartHandler);

        const beforeInputHandler = (args: InputEvent) => {
            InputQueue.flush();

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
            InputQueue.flush();
            JsExports.InputHelper.OnCompositionUpdate(topLevelId, args.data);
        };
        element.addEventListener("compositionupdate", compositionUpdateHandler);

        const compositionEndHandler = (args: CompositionEvent) => {
            InputQueue.flush();
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
        // Pointer and wheel events are encoded into the input queue and consumed by C# in one batch
        // per burst; pointer presses are flushed synchronously on the single-threaded runtime.
        // The browser default action is suppressed unconditionally.
        const pointerMoveHandler = (args: PointerEvent) => {
            InputQueue.postPointer(InputRecordType.PointerMove, topLevelId, args);
            args.preventDefault();
        };

        const pointerDownHandler = (args: PointerEvent) => {
            InputQueue.postPointer(InputRecordType.PointerDown, topLevelId, args);
            args.preventDefault();
        };

        const pointerUpHandler = (args: PointerEvent) => {
            InputQueue.postPointer(InputRecordType.PointerUp, topLevelId, args);
            args.preventDefault();
        };

        const pointerCancelHandler = (args: PointerEvent) => {
            InputQueue.postPointer(InputRecordType.PointerCancel, topLevelId, args);
        };

        const wheelHandler = (args: WheelEvent) => {
            InputQueue.postWheel(topLevelId, args);
            args.preventDefault();
        };

        element.addEventListener("pointermove", pointerMoveHandler);
        element.addEventListener("pointerdown", pointerDownHandler);
        element.addEventListener("pointerup", pointerUpHandler);
        element.addEventListener("wheel", wheelHandler);
        element.addEventListener("pointercancel", pointerCancelHandler);

        return () => {
            element.removeEventListener("pointermove", pointerMoveHandler);
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
            const dataTransfer = args.dataTransfer;
            if (dataTransfer == null) {
                return;
            }

            const items: ReadableDataItem[] =
                this.getDataTransferItems(dataTransfer).map((item) => ({ type: "dataTransferItem", value: item }));

            // Drag events bypass the input queue; flushing first keeps them ordered after queued input.
            InputQueue.flush();
            JsExports.InputHelper.OnDragDrop(topLevelId, args.type, args.offsetX, args.offsetY, InputQueue.getModifiers(args), dataTransfer, items);
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

        return () => { };
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

    public static setPointerCapture(containerElement: HTMLInputElement, pointerId: number): void {
        containerElement.setPointerCapture(pointerId);
    }

    public static releasePointerCapture(containerElement: HTMLInputElement, pointerId: number): void {
        if (containerElement.hasPointerCapture(pointerId)) {
            containerElement.releasePointerCapture(pointerId);
        }
    }
}
