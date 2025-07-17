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

window.focusElement = (elementId) => {
    const element = document.getElementById(elementId);
    if (element) {
        element.focus();
    } else {
        console.warn("Element not found:", elementId);
    }
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

window.mobileKeyboardInput = {
    _dotNetRef: null,
    _input: null,

    init: function(dotNetRef) {
        this._dotNetRef = dotNetRef;

        if (!this._input) {
            this._input = document.createElement('input');
            this._input.type = 'text';
            this._input.id = 'mobile-text-input';
            this._input.autocapitalize = 'off';
            this._input.autocomplete = 'off';
            this._input.spellcheck = false;
            
            // Unsichtbar machen, aber sichtbar genug, damit iOS/Android Tastatur aufgeht
            this._input.style.position = 'absolute';
            this._input.style.bottom = '10px';
            this._input.style.left = '10px';
            this._input.style.opacity = '0.01';
            this._input.style.height = '1px';
            this._input.style.width = '1px';
            this._input.style.zIndex = '9999';

            document.body.appendChild(this._input);

            // Eingabe Event abfangen
            this._input.addEventListener('input', (e) => {
                const val = e.target.value;
                if (val.length > 0) {
                    // An Blazor weiterleiten
                    this._dotNetRef.invokeMethodAsync('HandleMobileTextInput', val);
                    e.target.value = '';
                }
            });

            // Optional: Enter oder andere Tasten abfangen, falls benötigt
            this._input.addEventListener('keydown', (e) => {
                // z.B. Eingaben weiterleiten oder ESC etc.
            });
        }
    },

    show: function() {
        if (!this._input) return;
        this._input.style.opacity = '0.01';
        this._input.focus();
    },

    hide: function() {
        if (!this._input) return;
        this._input.blur();
    }
};

// Function to get user clipboard content
window.getClipboardContent = async () => {
    try {
        const text = await navigator.clipboard.readText();
        return text;
    } catch (err) {
        console.error('Failed to read clipboard contents: ', err);
        return '';
    }
};

// Function to write text to the clipboard
window.setClipboardContent = async (text) => {
    try {
        await navigator.clipboard.writeText(text);
    } catch (err) {
        console.error('Failed to write to clipboard: ', err);
    }
};

window.fallbackSetClipboard = async function (text) {
    try {
        await navigator.clipboard.writeText(text);
        console.log("fallbackSetClipboard: Clipboard set to:", text);
    } catch (err) {
        console.error('fallbackSetClipboard failed:', err);
    }
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

