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

