// In der wwwroot/js/custom.js Datei

(function () {
    'use strict';

    window.saveAsSpreadSheet = function (fileName, content) {
        try {
            const blob = new Blob([base64ToArrayBuffer(content)], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' });
            const link = document.createElement('a');

            // Verwende createObjectURL nur, wenn es verfügbar ist (für ältere Browser)
            if ('createObjectURL' in URL) {
                link.href = URL.createObjectURL(blob);
            } else {
                link.href = 'data:application/vnd.openxmlformats-officedocument.spreadsheetml.sheet;base64,' + content;
            }

            link.download = fileName;
            document.body.appendChild(link);
            link.click();
        } catch (error) {
            console.error('Error in saveAsSpreadSheet:', error);
        } finally {
            if (link) {
                document.body.removeChild(link);
            }
        }
    };

    function base64ToArrayBuffer(base64) {
        const binaryString = window.atob(base64);
        const len = binaryString.length;
        const bytes = new Uint8Array(len);
        for (let i = 0; i < len; i++) {
            bytes[i] = binaryString.charCodeAt(i);
        }
        return bytes.buffer;
    }

    window.exportToTxt = function (fileName, content) {
        try {
            // Überprüfe, ob der Link bereits existiert, und entferne ihn gegebenenfalls
            const existingLink = document.getElementById('exportLink');
            if (existingLink) {
                document.body.removeChild(existingLink);
            }

            const blob = new Blob([content], { type: 'text/plain' });
            const link = document.createElement('a');
            link.id = 'exportLink';

            // Verwende createObjectURL nur, wenn es verfügbar ist (für ältere Browser)
            if ('createObjectURL' in URL) {
                link.href = URL.createObjectURL(blob);
            } else {
                link.href = 'data:text/plain;charset=utf-8,' + encodeURIComponent(content);
            }

            link.download = fileName;
            document.body.appendChild(link);
            link.click();
        } catch (error) {
            console.error('Error in exportToTxt:', error);
        }
    };

    // Auto scrolling textbox
    window.scrollToEnd = function (textarea) {
        textarea.scrollTop = textarea.scrollHeight;
    };

    // Download file
    window.downloadFile = function (filename, content) {
        const blob = new Blob([content], { type: 'application/json' });
        const link = document.createElement('a');
        link.href = URL.createObjectURL(blob);
        link.download = filename;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    };

})();

window.isMobileDevice = function () {
    return /Android|iPhone|iPad|iPod|Opera Mini|IEMobile|WPDesktop/i.test(navigator.userAgent);
};


