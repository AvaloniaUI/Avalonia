import * as React from "react";
import {PointerEventMessageBase} from "./PointerEventMessageBase";

export class ScrollEventMessage extends PointerEventMessageBase {
    public readonly deltaX: number;
    public readonly deltaY: number;

    constructor(e: React.WheelEvent) {
        super(e);
        this.deltaX = -e.deltaX;
        this.deltaY = -e.deltaY;
    }

    public toString = () : string => {
        return `scroll:${this.modifiers}:${this.x}:${this.y}:${this.deltaX}:${this.deltaY}`;
    }
}
