const { buildSync } = require('esbuild');
const { strict: assert } = require('node:assert');
const { test } = require('node:test');
const { join } = require('node:path');
const source = buildSync({
    stdin: {
        contents: 'export { InputHelper } from "./modules/avalonia/input"; export { JsExports } from "./modules/avalonia/jsExports";',
        resolveDir: join(__dirname, '..')
    },
    bundle: true, write: false, platform: 'node', format: 'cjs', target: 'node18',
    external: ['./caretHelper', './inputQueue', '../storage/storageItem', './inputLayout.g']
}).outputFiles[0].text;
globalThis.getDotnetRuntime = () => null;
const loaded = { exports: {} };
new Function('module', 'require', source)(loaded, path => {
    if (path === './inputQueue') return { InputQueue: { flush() {} } };
    return {};
});
const { InputHelper, JsExports } = loaded.exports;
function setup() {
 const calls=[];
 JsExports.resolvedExports={Avalonia:{Browser:{Interop:{InputHelper:{OnBeforeInput(...args){calls.push(args);return Promise.resolve();}}}}}};
 const target=new EventTarget();target.selectionStart=2;target.selectionEnd=4;
 const dispose=InputHelper.subscribeTextEvents(target,7);
 const send=(inputType,data,isComposing=false,cancelable=true)=>{
  const e=new Event('beforeinput',{cancelable});Object.assign(e,{inputType,data,isComposing,getTargetRanges:()=>[]});target.dispatchEvent(e);return e;
 };
 return {calls,dispose,send,target};
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

test('line breaks forward a newline and noncancelable edits do not insert twice',()=>{
 const s=setup();const e=s.send('insertLineBreak',null);
 assert.equal(e.defaultPrevented,true);assert.equal(s.calls[0][4],'\n');
 const native=s.send('insertText','x',false,false);
 assert.equal(native.defaultPrevented,false);assert.equal(s.calls[1][4],null);s.dispose();
});

function synchronize(s, text, start, end) {
 s.target.value = ''; s.target.style = {}; s.target.scrollWidth = 20;
 s.target.setSelectionRange = (a,b) => {
  s.target.selectionEnd = Math.min(b, s.target.value.length);
  s.target.selectionStart = Math.min(a, s.target.selectionEnd);
 };
 InputHelper.setSurroundingText(s.target,text,start,end);
}
for (const [name,start,end] of [['forward',0,5],['backward',5,0],['previous paragraph',0,1]]) {
 test(`unchanged ${name} DOM projection preserves managed selection`,()=>{
  const s=setup(); synchronize(s,'A\n',start,end);
  s.send('insertText','Z');
  assert.equal(s.calls[0][2],-1); assert.equal(s.calls[0][3],-1);
  s.dispose();
 });
}
test('a changed DOM selection is forwarded instead of the synchronized projection',()=>{
 const s=setup(); synchronize(s,'ABC',0,3);
 s.target.selectionStart=1;s.target.selectionEnd=2;
 s.send('insertText','Z');
 assert.equal(s.calls[0][2],1); assert.equal(s.calls[0][3],2); s.dispose();
});
