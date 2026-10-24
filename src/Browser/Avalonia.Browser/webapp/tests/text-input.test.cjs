const { transformSync } = require('esbuild');
const { strict: assert } = require('node:assert');
const { test } = require('node:test');
const { readFileSync } = require('node:fs');
const { join } = require('node:path');
const JsExports={};
Object.defineProperty(JsExports,'InputHelper',{get:()=>JsExports.resolvedExports.Avalonia.Browser.Interop.InputHelper});
const source=readFileSync(join(__dirname,'../modules/avalonia/input.ts'),'utf8').replace(/^import .*;$/gm,'');
const code=transformSync(source,{loader:'ts',format:'cjs',target:'es2020'}).code;
const loaded={exports:{}};
new Function('module','JsExports','InputQueue',code)(loaded,JsExports,{flush(){}});
const {InputHelper}=loaded.exports;
function setup() {
 const calls=[];
 JsExports.resolvedExports={Avalonia:{Browser:{Interop:{InputHelper:{OnBeforeInput(...args){calls.push(args);return Promise.resolve();}}}}}};
 const target=new EventTarget();target.selectionStart=2;target.selectionEnd=4;
 const dispose=InputHelper.subscribeTextEvents(target,7);
 const send=(inputType,data,isComposing=false)=>{
  const e=new Event('beforeinput',{cancelable:true});Object.assign(e,{inputType,data,isComposing,getTargetRanges:()=>[]});target.dispatchEvent(e);return e;
 };
 return {calls,dispose,send};
}
test('committed text carries data and selection to managed input once',()=>{
 const s=setup();const e=s.send('insertText',' ');
 assert.equal(e.defaultPrevented,true);
 assert.equal(s.calls.length,1);
 assert.equal(s.calls[0][4],' ');
 assert.equal(s.calls[0][2],2);assert.equal(s.calls[0][3],4);
 s.dispose();
});
test('composition updates retain browser default handling',()=>{
 const s=setup();const e=s.send('insertCompositionText','한',true);
 assert.equal(e.defaultPrevented,false);s.dispose();
});
test('disposal removes beforeinput listener',()=>{
 const s=setup();s.dispose();s.send('insertText','x');assert.equal(s.calls.length,0);
});
