import { describe } from 'mocha';
import { expect } from 'chai';
import { Mock } from "moq.ts";
import { MouseEvent, WheelEvent } from "react";
import { InputModifiers } from "../../../../../../src/Avalonia.DesignerSupport/Remote/HtmlTransport/webapp/src/Models/Input/InputModifiers";
import { MouseButton } from "../../../../../../src/Avalonia.DesignerSupport/Remote/HtmlTransport/webapp/src/Models/Input/MouseButton";
import { PointerMovedEventMessage } from "../../../../../../src/Avalonia.DesignerSupport/Remote/HtmlTransport/webapp/src/Models/Input/PointerMovedEventMessage";
import { PointerPressedEventMessage } from "../../../../../../src/Avalonia.DesignerSupport/Remote/HtmlTransport/webapp/src/Models/Input/PointerPressedEventMessage";
import { PointerReleasedEventMessage } from "../../../../../../src/Avalonia.DesignerSupport/Remote/HtmlTransport/webapp/src/Models/Input/PointerReleasedEventMessage";
import { ScrollEventMessage } from "../../../../../../src/Avalonia.DesignerSupport/Remote/HtmlTransport/webapp/src/Models/Input/ScrollEventMessage";
import { getModifiers, getMouseButton } from '../../../../../../src/Avalonia.DesignerSupport/Remote/HtmlTransport/webapp/src/Models/Input/MouseEventHelpers';

describe("Input event tests", () => {
    describe("Helpers", () => {
        it("getModifiers", () => {
            const event = new Mock<MouseEvent>()
                .setup(x => x.altKey).returns(false)
                .setup(x => x.ctrlKey).returns(true)
                .setup(x => x.shiftKey).returns(false)
                .setup(x => x.metaKey).returns(false)
                .setup(x => x.buttons).returns(1)
                .object()
            var actual = getModifiers(event)

            expect(actual)
                .eql([InputModifiers.Control, InputModifiers.LeftMouseButton])
        })
        it("getMouseButton", () => {
            const event = new Mock<MouseEvent>()
                .setup(x => x.button).returns(1)
                .object()
            var actual = getMouseButton(event)

            expect(actual)
                .equal(MouseButton.Middle)
        })
    })

    describe("Messages", () => {
        const x = .3
        const y = .42
        const modifiers = "0,1,2,3,4,5,6"

        const button = "1"

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
            const message = new ScrollEventMessage(wheelEvent)

            expect(message.toString())
                .equal(`scroll:${modifiers}:${x}:${y}:${deltaX}:${deltaY}`)
        })
    })
})
