window.createAppButton = function () {
    var button = document.createElement('button');
    button.innerText = 'Hello world';
    var clickCount = 0;
    button.onclick = () => {
        clickCount++;
        button.innerText = 'Click count ' + clickCount;
    };
    return button;
}
