import * as React from "react";
import {PointerEventMessageBase} from "src/Models/Input/PointerEventMessageBase";
import {MouseButton} from "src/Models/Input/MouseButton";
import {getMouseButton} from "src/Models/Input/MouseEventHelpers";

export class PointerPressedEventMessage extends PointerEventMessageBase {
    public readonly button: MouseButton

    constructor(e: React.MouseEvent) {
        super(e)
        this.button = getMouseButton(e);
    }

    public toString = () : string => {
        return `pointer-pressed:${this.modifiers}:${this.button}:${this.x}:${this.y}`;
    }
}
