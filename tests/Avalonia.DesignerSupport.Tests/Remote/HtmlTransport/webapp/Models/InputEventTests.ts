import {describe} from 'mocha';
import {expect} from 'chai';
import {Mock} from "moq.ts";
import {MouseEvent, WheelEvent} from "react";
import {InputModifiers} from "../../../../../../src/Avalonia.DesignerSupport/Remote/HtmlTransport/webapp/src/Models/Input/InputModifiers";
import {MouseButton} from "../../../../../../src/Avalonia.DesignerSupport/Remote/HtmlTransport/webapp/src/Models/Input/MouseButton";
import {PointerMovedEventMessage} from "../../../../../../src/Avalonia.DesignerSupport/Remote/HtmlTransport/webapp/src/Models/Input/PointerMovedEventMessage";
import {PointerPressedEventMessage} from "../../../../../../src/Avalonia.DesignerSupport/Remote/HtmlTransport/webapp/src/Models/Input/PointerPressedEventMessage";
import {PointerReleasedEventMessage} from "../../../../../../src/Avalonia.DesignerSupport/Remote/HtmlTransport/webapp/src/Models/Input/PointerReleasedEventMessage";
import {ScrollEventMessage} from "../../../../../../src/Avalonia.DesignerSupport/Remote/HtmlTransport/webapp/src/Models/Input/ScrollEventMessage";

const x = .3
const y = .42
const modifiers = [
    InputModifiers.Alt,
    InputModifiers.Control,
    InputModifiers.Shift,
    InputModifiers.Windows,
    InputModifiers.LeftMouseButton,
    InputModifiers.RightMouseButton,
    InputModifiers.MiddleMouseButton
]

const button = MouseButton.Left

const deltaX = -3.
const deltaY = -3. 

const mouseEvent = new Mock<MouseEvent>()
    .setup(x => x.altKey).returns(true)
    .setup(x => x.ctrlKey).returns(true)
    .setup(x => x.shiftKey).returns(true)
    .setup(x => x.metaKey).returns(true)
    .setup(x => x.buttons).returns(7)
    .setup(x => x.button).returns(0)
    .setup(x => x.clientX).returns(x)
    .setup(x => x.clientY).returns(y)
    .object()

const wheelEvent = new Mock<WheelEvent>()
    .setup(x => x.altKey).returns(true)
    .setup(x => x.ctrlKey).returns(true)
    .setup(x => x.shiftKey).returns(true)
    .setup(x => x.metaKey).returns(true)
    .setup(x => x.buttons).returns(7)
    .setup(x => x.clientX).returns(x)
    .setup(x => x.clientY).returns(y)
    .setup(x => x.deltaX).returns(-deltaX)
    .setup(x => x.deltaY).returns(-deltaY)
    .object()

describe("Input event tests", () => {
    it("PointerMovedEventMessage", () => {
        const message = new PointerMovedEventMessage(mouseEvent)
        expect(message.toString())
            .equal(`pointer-moved:${modifiers}:${x}:${y}`)
    })
    it("PointerPressedEventMessage", () => {
        const message = new PointerPressedEventMessage(mouseEvent)
        expect(message.toString())
            .equal(`pointer-pressed:${modifiers}:${x}:${y}:${button}`)
    })
    it("PointerReleasedEventMessage", () => {
        const message = new PointerReleasedEventMessage(mouseEvent)
        expect(message.toString())
            .equal(`pointer-released:${modifiers}:${x}:${y}:${button}`)
    })
    it("ScrollEventMessage", () => {
        const message = new ScrollEventMessage(wheelEvent)
        expect(message.toString())
            .equal(`scroll:${modifiers}:${x}:${y}:${deltaX}:${deltaY}`)
    })
})
