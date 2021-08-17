import * as React from "react";
import {PointerEventMessageBase} from "./PointerEventMessageBase";

export class PointerMovedEventMessage extends PointerEventMessageBase {
    constructor(e: React.MouseEvent) {
        super(e);
    }

    public toString = () : string => {
        return `pointer-moved:${this.modifiers}:${this.x}:${this.y}`;
    }
}
