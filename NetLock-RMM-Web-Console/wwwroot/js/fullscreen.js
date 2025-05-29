window.toggleFullScreen = function (elementId) {
    const elem = document.getElementById(elementId);

    if (!document.fullscreenElement) {
        elem.requestFullscreen().catch(err => {
            console.error(`Error attempting to enable full-screen mode: ${err.message}`);
        });
    } else {
        document.exitFullscreen();
    }
};
