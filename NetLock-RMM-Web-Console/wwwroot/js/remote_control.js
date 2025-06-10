window.getBoundingClientRect = function (element) {
    if (element) {
        return element.getBoundingClientRect();
    }
    return null;
};

window.getImageResolution = (imageElement) => {
    return {
        width: imageElement.naturalWidth,
        height: imageElement.naturalHeight
    };
};

window.preventContextMenu = (clientX, clientY) => {
    document.addEventListener('contextmenu', function (e) {
        e.preventDefault();
    }, { once: true });
};

window.focusElement = (element) => {
    element.focus();
};

window.enableDrag = (elementId) => {
    const el = document.getElementById(elementId);
    let isMouseDown = false;
    let offset = { x: 0, y: 0 };

    if (!el) return;

    const onMouseDown = (e) => {
        isMouseDown = true;
        const rect = el.getBoundingClientRect();
        offset = {
            x: e.clientX - rect.left,
            y: e.clientY - rect.top
        };
        document.addEventListener('mousemove', onMouseMove);
        document.addEventListener('mouseup', onMouseUp);
    };

    const onMouseMove = (e) => {
        if (!isMouseDown) return;
        el.style.position = 'absolute';
        el.style.left = `${e.clientX - offset.x}px`;
        el.style.top = `${e.clientY - offset.y}px`;
    };

    const onMouseUp = () => {
        isMouseDown = false;
        document.removeEventListener('mousemove', onMouseMove);
        document.removeEventListener('mouseup', onMouseUp);
    };

    el.addEventListener('mousedown', onMouseDown);
};

// JavaScript function for capturing the mouse position
function captureMousePosition(elementId) {
    var element = document.getElementById(elementId);
    element.addEventListener('click', function (event) {
        var rect = element.getBoundingClientRect();
        var x = event.clientX - rect.left;
        var y = event.clientY - rect.top;
        DotNet.invokeMethodAsync('YourAssemblyName', 'ProcessMouseClick', x, y);
    });
}

