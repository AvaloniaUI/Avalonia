import * as React from "react";
import {InputEventMessageBase} from "./InputEventMessageBase";

export abstract class PointerEventMessageBase extends InputEventMessageBase {
    public readonly x: number;
    public readonly y: number;

    protected constructor(e: React.MouseEvent) {
        super(e);
        this.x = e.clientX;
        this.y = e.clientY;
    }
}
