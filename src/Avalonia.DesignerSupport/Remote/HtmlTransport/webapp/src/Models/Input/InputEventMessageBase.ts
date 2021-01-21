import * as React from "react";
import {InputModifiers} from "./InputModifiers";
import {getModifiers} from "./MouseEventHelpers";

export abstract class InputEventMessageBase {
    public readonly modifiers : Array<InputModifiers>;

    protected constructor(e: React.MouseEvent) {
        this.modifiers = getModifiers(e);
    }
}
