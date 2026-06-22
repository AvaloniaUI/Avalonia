import * as React from "react";
import {PreviewerFrame, PreviewerServerConnection} from "src/PreviewerServerConnection";
import {PointerPressedEventMessage} from "src/Models/Input/PointerPressedEventMessage";
import {PointerReleasedEventMessage} from "src/Models/Input/PointerReleasedEventMessage";
import {PointerMovedEventMessage} from "src/Models/Input/PointerMovedEventMessage";
import {ScrollEventMessage} from "src/Models/Input/ScrollEventMessage";

interface PreviewerPresenterProps {
    conn: PreviewerServerConnection;
}

export class PreviewerPresenter extends React.Component<PreviewerPresenterProps> {
    private canvasRef: React.RefObject<HTMLCanvasElement>;
    private scale: number = 1;

    constructor(props: PreviewerPresenterProps) {
        super(props);
        this.state = {width: 1, height: 1};
        this.canvasRef = React.createRef()
        this.componentDidUpdate({
            conn: null!
        }, this.state);

        this.handleMouseDown = this.handleMouseDown.bind(this);
        this.handleMouseUp = this.handleMouseUp.bind(this);
        this.handleMouseMove = this.handleMouseMove.bind(this);
        this.handleWheel = this.handleWheel.bind(this);
    }

    componentDidMount(): void {
        this.updateCanvas(this.canvasRef.current, this.props.conn.currentFrame);
        
        if (parent != null && parent !== window)
        {
            window.addEventListener('message', (event) => {
                if (!event.origin.startsWith('vscode-webview://')) {
                    return;
                }

                // Handle previewer dpi changed event
                if (event.data.type === 'scaling') {
                    const scale = Number(event.data.scale);
                    if (Number.isFinite(scale) && scale > 0) {
                        this.scale = event.data.scale;
                    }
                }
            });
        }
    }

    componentDidUpdate(prevProps: Readonly<PreviewerPresenterProps>, prevState: Readonly<{}>, snapshot?: any): void {
        if(prevProps.conn != this.props.conn)
        {
            if(prevProps.conn)
                prevProps.conn.removeFrameListener(this.frameHandler);
            if(this.props.conn)
                this.props.conn.addFrameListener(this.frameHandler);
        }
    }

    private frameHandler = (frame: PreviewerFrame)=>{
        this.updateCanvas(this.canvasRef.current, frame);
    };


    updateCanvas(canvas: HTMLCanvasElement | null, frame: PreviewerFrame | null) {
        if (!canvas)
            return;

        const oldWidth = canvas.style.width;
        const oldHeight = canvas.style.height;

        if (frame == null){
            canvas.width = canvas.height = 1;
            canvas.style.width = canvas.style.height = `1px`;
            canvas.getContext('2d')!.clearRect(0,0,1,1);
        }
        else {
            canvas.width = frame.data.width;
            canvas.height = frame.data.height;

            // To prevent blurry images on high-DPI screens
            canvas.style.width = `${frame.data.width / window.devicePixelRatio}px`;
            canvas.style.height = `${frame.data.height / window.devicePixelRatio}px`;

            const ctx = canvas.getContext('2d')!;
            ctx.putImageData(frame.data, 0,0);
        }

        if (oldWidth !== canvas.style.width || oldHeight !== canvas.style.height)
        {
            // Inform the parent page (a VSCode extention) the canvas size has changed
            parent?.postMessage({
                type: 'resize',
                width: canvas.width / window.devicePixelRatio,
                height: canvas.height / window.devicePixelRatio,
            }, '*');
        }
    }

    handleMouseDown(e: React.MouseEvent) {
        e.preventDefault();
        const pointerPressedEventMessage = new PointerPressedEventMessage(e, this.scale / window.devicePixelRatio);
        this.props.conn.sendMouseEvent(pointerPressedEventMessage);
    }

    handleMouseUp(e: React.MouseEvent) {
        e.preventDefault();
        const pointerReleasedEventMessage = new PointerReleasedEventMessage(e, this.scale / window.devicePixelRatio);
        this.props.conn.sendMouseEvent(pointerReleasedEventMessage);
    }

    handleMouseMove(e: React.MouseEvent) {
        e.preventDefault();
        const pointerMovedEventMessage = new PointerMovedEventMessage(e, this.scale / window.devicePixelRatio);
        this.props.conn.sendMouseEvent(pointerMovedEventMessage);
    }

    handleWheel(e: React.WheelEvent) {
        e.preventDefault();
        const scrollEventMessage = new ScrollEventMessage(e, this.scale / window.devicePixelRatio);
        this.props.conn.sendMouseEvent(scrollEventMessage);
    }

    render() {
        return <canvas ref={this.canvasRef}
                       onMouseDown={this.handleMouseDown}
                       onMouseUp={this.handleMouseUp}
                       onMouseMove={this.handleMouseMove}
                       onWheel={this.handleWheel} />
    }
}
