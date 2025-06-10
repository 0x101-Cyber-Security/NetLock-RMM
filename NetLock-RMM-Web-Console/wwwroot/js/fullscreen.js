window.toggleFullScreen = function (elementId) {
    const element = document.getElementById(elementId);
    if (!element) return;

    if (!document.fullscreenElement) {
        // Vollbild aktivieren und dann Remote-Session starten
        element.requestFullscreen().then(() => {
            window.startRemoteControlSession(elementId);
        }).catch(err => {
            console.error(`Fehler beim Aktivieren von Fullscreen: ${err.message}`);
        });
    } else {
        // Vollbild beenden → damit endet auch Pointer Lock automatisch
        document.exitFullscreen().catch(err => {
            console.error(`Fehler beim Beenden von Fullscreen: ${err.message}`);
        });
    }
};

window.startRemoteControlSession = (elementId) => {
    const element = document.getElementById(elementId);
    if (!element) return;

    // Pointer sperren
    //element.requestPointerLock();

    // Fokus setzen
    element.setAttribute('tabindex', '0');
    element.focus();

    // Tastatur-Handler definieren
    const keyHandler = (e) => {
        e.preventDefault(); // Blockiert Browser-Shortcuts wie Strg+F
        e.stopPropagation();

        DotNet.invokeMethodAsync('NetLock_RMM_Web_Console', 'HandleKeyDown',
            e.key, e.code, e.ctrlKey, e.shiftKey, e.altKey);
    };

    document.addEventListener('keydown', keyHandler);
    document.addEventListener('contextmenu', e => e.preventDefault());

    // Wenn ESC gedrückt wird → Pointer Lock und/oder Fullscreen endet
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
