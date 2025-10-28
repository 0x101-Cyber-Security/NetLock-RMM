(function () {
    'use strict';

    const MAX_FILE_SIZE = 100 * 1024 * 1024;
    const MAX_FILENAME_LENGTH = 255;
    const ALLOWED_MIME_TYPES = {
        spreadsheet: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
        text: 'text/plain',
        json: 'application/json'
    };

    function sanitizeFileName(fileName) {
        if (typeof fileName !== 'string') {
            console.error('Invalid filename type');
            return 'download';
        }
        let sanitized = fileName.replace(/[<>:"/\\|?*\x00-\x1F]/g, '_');
        sanitized = sanitized.substring(0, MAX_FILENAME_LENGTH);
        return sanitized || 'download';
    }

    function validateContentSize(content, maxSize = MAX_FILE_SIZE) {
        const size = typeof content === 'string' ? 
            new Blob([content]).size : 
            (content instanceof ArrayBuffer ? content.byteLength : content.length);
        
        if (size > maxSize) {
            console.error('Content exceeds maximum size');
            return false;
        }
        return true;
    }

    function base64ToArrayBuffer(base64) {
        if (typeof base64 !== 'string') {
            throw new TypeError('base64 must be a string');
        }

        if (!/^[A-Za-z0-9+/]*={0,2}$/.test(base64)) {
            throw new Error('Invalid base64 string');
        }

        try {
            const binaryString = window.atob(base64);
            const len = binaryString.length;
            
            if (len > MAX_FILE_SIZE) {
                throw new Error('Decoded content exceeds maximum size');
            }

            const bytes = new Uint8Array(len);
            for (let i = 0; i < len; i++) {
                bytes[i] = binaryString.charCodeAt(i);
            }
            return bytes.buffer;
        } catch (e) {
            console.error('Error decoding base64:', e);
            throw e;
        }
    }

    function createDownloadLink(blob, fileName, cleanup = true) {
        const link = document.createElement('a');
        
        try {
            if ('createObjectURL' in URL) {
                link.href = URL.createObjectURL(blob);
            } else {
                console.warn('createObjectURL not available, using data URL');
                return null;
            }

            link.download = sanitizeFileName(fileName);
            link.style.display = 'none';
            document.body.appendChild(link);
            link.click();

            if (cleanup) {
                setTimeout(() => {
                    if (link.parentNode) {
                        document.body.removeChild(link);
                    }
                    URL.revokeObjectURL(link.href);
                }, 100);
            }

            return link;
        } catch (e) {
            console.error('Error creating download link:', e);
            if (link.parentNode) {
                document.body.removeChild(link);
            }
            if (link.href && link.href.startsWith('blob:')) {
                URL.revokeObjectURL(link.href);
            }
            return null;
        }
    }

    window.saveAsSpreadSheet = function (fileName, content) {
        try {
            if (!fileName || !content) {
                console.error('Invalid parameters: fileName and content are required');
                return;
            }

            if (typeof content !== 'string') {
                console.error('Content must be a base64 string');
                return;
            }

            if (content.length > MAX_FILE_SIZE * 1.5) {
                console.error('Content exceeds maximum size');
                return;
            }

            const arrayBuffer = base64ToArrayBuffer(content);
            const blob = new Blob([arrayBuffer], { type: ALLOWED_MIME_TYPES.spreadsheet });

            createDownloadLink(blob, fileName);

        } catch (error) {
            console.error('Error in saveAsSpreadSheet:', error);
        }
    };

    window.exportToTxt = function (fileName, content) {
        try {
            if (!fileName || content === undefined || content === null) {
                console.error('Invalid parameters: fileName and content are required');
                return;
            }

            if (typeof content !== 'string') {
                console.error('Content must be a string');
                return;
            }

            if (!validateContentSize(content)) {
                return;
            }

            const existingLinks = document.querySelectorAll('a[id^="exportLink"]');
            existingLinks.forEach(link => {
                if (link.parentNode) {
                    document.body.removeChild(link);
                }
                if (link.href && link.href.startsWith('blob:')) {
                    URL.revokeObjectURL(link.href);
                }
            });

            const blob = new Blob([content], { type: ALLOWED_MIME_TYPES.text + ';charset=utf-8' });
            const link = createDownloadLink(blob, fileName, false);
            
            if (link) {
                link.id = 'exportLink-' + Date.now();
            }

        } catch (error) {
            console.error('Error in exportToTxt:', error);
        }
    };

// javascript (füge in `wwwroot/js/custom.js` hinzu)
    window.exportToHtml = function (fileName, content) {
        try {
            if (!fileName || content === undefined || content === null) {
                console.error('Invalid parameters: fileName and content are required');
                return;
            }

            // Alte Links aufräumen (wie in exportToTxt)
            const existingLinks = document.querySelectorAll('a[id^="exportLink"]');
            existingLinks.forEach(link => {
                if (link.parentNode) document.body.removeChild(link);
                if (link.href && link.href.startsWith('blob:')) URL.revokeObjectURL(link.href);
            });

            const blob = new Blob([content], { type: 'text/html;charset=utf-8' });
            const link = createDownloadLink(blob, fileName, false);

            if (link) {
                link.id = 'exportLink-' + Date.now();
            }
        } catch (error) {
            console.error('Error in exportToHtml:', error);
        }
    };

    window.scrollToEnd = function (textarea) {
        try {
            if (!textarea || !(textarea instanceof HTMLElement)) {
                console.warn('Invalid textarea element');
                return;
            }

            const maxScroll = Math.min(textarea.scrollHeight, Number.MAX_SAFE_INTEGER);
            textarea.scrollTop = maxScroll;

        } catch (error) {
            console.error('Error in scrollToEnd:', error);
        }
    };

    window.downloadFile = function (filename, content) {
        try {
            if (!filename || content === undefined || content === null) {
                console.error('Invalid parameters: filename and content are required');
                return;
            }

            if (typeof content !== 'string') {
                console.error('Content must be a string');
                return;
            }

            if (!validateContentSize(content)) {
                return;
            }

            try {
                JSON.parse(content);
            } catch (e) {
                console.warn('Content is not valid JSON, but proceeding with download');
            }

            const blob = new Blob([content], { type: ALLOWED_MIME_TYPES.json });
            createDownloadLink(blob, filename);

        } catch (error) {
            console.error('Error in downloadFile:', error);
        }
    };

})();

window.isMobileDevice = function () {
    try {
        if (!navigator || !navigator.userAgent) {
            console.warn('Navigator or userAgent not available');
            return false;
        }
        return /Android|iPhone|iPad|iPod|Opera Mini|IEMobile|WPDesktop/i.test(navigator.userAgent);
    } catch (error) {
        console.error('Error in isMobileDevice:', error);
        return false;
    }
};
