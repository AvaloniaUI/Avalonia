import {PreviewerFrame, PreviewerServerConnection} from "src/PreviewerServerConnection";
import * as React from "react";

interface PreviewerPresenterProps {
    conn: PreviewerServerConnection;
}

export class PreviewerPresenter extends React.Component<PreviewerPresenterProps> {
    private canvasRef: React.RefObject<HTMLCanvasElement>;

    constructor(props: PreviewerPresenterProps) {
        super(props);
        this.state = {width: 1, height: 1};
        this.canvasRef = React.createRef()
        this.componentDidUpdate({
            conn: null!
        }, this.state);
    }

    componentDidMount(): void {
        this.updateCanvas(this.canvasRef.current, this.props.conn.currentFrame);
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
        if (frame == null){
            canvas.width = canvas.height = 1;
            canvas.getContext('2d')!.clearRect(0,0,1,1);
        }
        else {
            canvas.width = frame.data.width;
            canvas.height = frame.data.height;
            const ctx = canvas.getContext('2d')!;
            ctx.putImageData(frame.data, 0,0);
        }
    }

    render() {
        return <canvas ref={this.canvasRef}/>
    }
}
