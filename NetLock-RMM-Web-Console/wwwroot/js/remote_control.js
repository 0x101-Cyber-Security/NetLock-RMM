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

window.attachWheelListener = function (elementId, dotNetRef) {
    const el = document.getElementById(elementId);
    if (!el) return;

    el.addEventListener('wheel', function (event) {
        event.preventDefault();

        dotNetRef.invokeMethodAsync('HandleScrollFromJs', {
            clientX: event.clientX,
            clientY: event.clientY,
            deltaX: event.deltaX,
            deltaY: event.deltaY
        });
    }, { passive: false });
};

window.toggleFullScreen = function (elementId) {
    const element = document.getElementById(elementId);
    if (!element) return;

    if (!document.fullscreenElement) {
        // Activate full screen and then start remote session
        element.requestFullscreen().then(() => {
            window.startRemoteControlSession(elementId);
        }).catch(err => {
            console.error(`Fehler beim Aktivieren von Fullscreen: ${err.message}`);
        });
    } else {
        // Exit full screen → this also ends Pointer Lock automatically
        document.exitFullscreen().catch(err => {
            console.error(`Fehler beim Beenden von Fullscreen: ${err.message}`);
        });
    }
};

window.startRemoteControlSession = (elementId) => {
    const element = document.getElementById(elementId);
    if (!element) return;

    // Lock pointer
    //element.requestPointerLock();

    // Fokus setzen
    element.setAttribute('tabindex', '0');
    element.focus();

    // Define keyboard handler
    const keyHandler = (e) => {
        e.preventDefault(); // Blocks browser shortcuts such as Ctrl+F
        e.stopPropagation();

        DotNet.invokeMethodAsync('NetLock_RMM_Web_Console', 'HandleKeyDown',
            e.key, e.code, e.ctrlKey, e.shiftKey, e.altKey);
    };

    document.addEventListener('keydown', keyHandler);
    document.addEventListener('contextmenu', e => e.preventDefault());

    // When ESC is pressed → Pointer lock and/or full screen ends
    const cleanup = () => {
        console.log("Remote-Control-Sitzung beendet");
        document.removeEventListener('keydown', keyHandler);
    };

    /*document.addEventListener('pointerlockchange', () => {
        if (document.pointerLockElement !== element) {
            cleanup();
        }
    });*/

    document.addEventListener('fullscreenchange', () => {
        if (!document.fullscreenElement) {
            cleanup();
        }
    });
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

