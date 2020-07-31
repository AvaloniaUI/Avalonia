import * as React from "react";
import {InputModifiers} from "src/Models/Input/InputModifiers";
import {MouseButton} from "src/Models/Input/MouseButton";

export function getModifiers(e: React.MouseEvent): Array<InputModifiers> {

    let modifiers : Array<InputModifiers> = [];

    if (e.altKey)
        modifiers.push(InputModifiers.Alt);
    if (e.ctrlKey)
        modifiers.push(InputModifiers.Control);
    if (e.shiftKey)
        modifiers.push(InputModifiers.Shift);
    if (e.metaKey)
        modifiers.push(InputModifiers.Windows);
    if ((e.buttons & 1) != 0)
        modifiers.push(InputModifiers.LeftMouseButton);
    if ((e.buttons & 2) != 0)
        modifiers.push(InputModifiers.RightMouseButton);
    if ((e.buttons & 4) != 0)
        modifiers.push(InputModifiers.MiddleMouseButton);

    return modifiers;
}

export function getMouseButton(e: React.MouseEvent) : MouseButton {
    if (e.button == 1) {
        return MouseButton.Left;
    } else if (e.button == 2) {
        return MouseButton.Right;
    } else if (e.button == 4) {
        return MouseButton.Middle
    } else {
        return MouseButton.None;
    }
}
