import QtQuick 2.12
import Lomiri.Components 1.3

// Native Qt control for repeatable clipboard interoperability checks. Use test text only.
MainView {
    objectName: "nativeClipboardProbe"
    applicationName: "avalonia.clipboardprobe"
    width: units.gu(45)
    height: units.gu(80)

    Page {
        header: PageHeader { title: "Native clipboard test" }
        Column {
            anchors { left: parent.left; right: parent.right; top: parent.header.bottom; margins: units.gu(2) }
            spacing: units.gu(2)
            Label { text: "Copy and paste test text between this app and Avalonia."; width: parent.width; wrapMode: Text.Wrap }
            TextArea {
                id: editor
                objectName: "nativeText"
                width: parent.width
                height: units.gu(20)
                placeholderText: "Native Qt text"
                onTextChanged: console.log("NativeClipboardProbe: text length=" + text.length)
            }
            Row {
                width: parent.width
                spacing: units.gu(1)
                Button { width: (parent.width - 3 * parent.spacing) / 4; height: units.gu(6); text: "Test text"; onClicked: { editor.text = "Café 🌙\nsecond line"; editor.forceActiveFocus(); } }
                Button { width: (parent.width - 3 * parent.spacing) / 4; height: units.gu(6); text: "Copy"; onClicked: { editor.forceActiveFocus(); editor.selectAll(); editor.copy(); } }
                Button { width: (parent.width - 3 * parent.spacing) / 4; height: units.gu(6); text: "Paste"; onClicked: { editor.forceActiveFocus(); editor.selectAll(); editor.paste(); } }
                Button { width: (parent.width - 3 * parent.spacing) / 4; height: units.gu(6); text: "Clear"; onClicked: { editor.text = ""; } }
            }
        }
    }
}
