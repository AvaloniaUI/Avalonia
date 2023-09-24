import { InputEventMessageBase } from "src/Models/Input/InputEventMessageBase";

export interface PreviewerFrame {
    data: ImageData;
    dpiX: number;
    dpiY: number;
}

export class PreviewerServerConnection {
    private nextFrame = {
        width: 0,
        height: 0,
        stride: 0,
        dpiX: 0,
        dpiY: 0,
        sequenceId: "0"
    };

    public currentFrame: PreviewerFrame | null;
    private handlers = new Set<(frame: PreviewerFrame | null) => void>();
    private conn: WebSocket;

    public addFrameListener(listener: (frame: PreviewerFrame | null) => void) {
        this.handlers.add(listener);
        if (this.currentFrame)
            listener(this.currentFrame);
    }

    public removeFrameListener(listener: (frame: PreviewerFrame | null) => void) {
        this.handlers.delete(listener);
    }

    public sendMouseEvent(message: InputEventMessageBase) {
        this.conn.send(message.toString());
    }

    constructor(uri: string) {
        this.currentFrame = null;
        var conn = this.conn = new WebSocket(uri);
        conn.binaryType = 'arraybuffer';

        const onMessage = this.onMessage;
        conn.onmessage = msg => onMessage(msg);
        conn.onopen = open => this.onOpen(open);
        const onClose = () => this.setFrame(null);
        conn.onclose = () => onClose();
        conn.onerror = (err: Event) => {
            onClose();
            console.log("Connection error: " + err);
        }
    }

    private onOpen(open: Event) {
        this.conn.send(window["avaloniaPreviewerSecurityCookie"]);
    }

    private onMessage = (msg: MessageEvent) => {
        if (typeof msg.data == 'string' || msg.data instanceof String) {
            const parts = msg.data.split(':');
            if (parts[0] == 'frame') {
                this.nextFrame = {
                    sequenceId: parts[1],
                    width: parseInt(parts[2]),
                    height: parseInt(parts[3]),
                    stride: parseInt(parts[4]),
                    dpiX: parseInt(parts[5]),
                    dpiY: parseInt(parts[6])
                }
            }
        } else if (msg.data instanceof ArrayBuffer) {
            const arr = new Uint8ClampedArray(msg.data, 0);
            const imageData = new ImageData(arr, this.nextFrame.width, this.nextFrame.height);
            this.conn.send('frame-received:' + this.nextFrame.sequenceId);
            this.setFrame({
                data: imageData,
                dpiX: this.nextFrame.dpiX,
                dpiY: this.nextFrame.dpiY
            });


        }
    };

    private setFrame(frame: PreviewerFrame | null) {
        this.currentFrame = frame;
        this.handlers.forEach(h => h(this.currentFrame));
    }
}
