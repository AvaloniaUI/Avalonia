import * as React from "react";
import {PointerEventMessageBase} from "./PointerEventMessageBase";
import {MouseButton} from "./MouseButton";
import {getMouseButton} from "./MouseEventHelpers";

export class PointerPressedEventMessage extends PointerEventMessageBase {
    public readonly button: MouseButton

    constructor(e: React.MouseEvent) {
        super(e);
        this.button = getMouseButton(e);
    }

    public toString = () : string => {
        return `pointer-pressed:${this.modifiers}:${this.x}:${this.y}:${this.button}`;
    }
}
