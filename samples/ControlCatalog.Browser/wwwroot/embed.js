export function addAppButton(parent) {
    var button = globalThis.document.createElement('button');
    button.innerText = 'Hello world';
    var clickCount = 0;
    button.onclick = () => {
        clickCount++;
        button.innerText = 'Click count ' + clickCount;
    };
    parent.appendChild(button);
    return button;
}
