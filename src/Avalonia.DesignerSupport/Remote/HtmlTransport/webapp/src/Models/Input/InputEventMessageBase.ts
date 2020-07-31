import * as React from "react";
import {InputModifiers} from "src/Models/Input/InputModifiers";
import {getModifiers} from "src/Models/Input/MouseEventHelpers";

export abstract class InputEventMessageBase {
    public readonly modifiers : Array<InputModifiers>;

    protected constructor(e: React.MouseEvent) {
        this.modifiers = getModifiers(e);
    }
}
