import * as React from "react";
import {PreviewerPresenter} from './FramePresenter'
import {PreviewerServerConnection} from "src/PreviewerServerConnection";
import * as ReactDOM from "react-dom";

const loc = window.location;
const conn = new PreviewerServerConnection((loc.protocol === "https:" ? "wss" : "ws") + "://" + loc.host + "/ws");

const App = function(){
    return <div style={{width: '100%'}}>
        <PreviewerPresenter conn={conn}/>
    </div>
};

ReactDOM.render(<App/>, document.getElementById("app"));
