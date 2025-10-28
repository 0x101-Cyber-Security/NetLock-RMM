// Security-Enhanced Remote Control Script
(function() {
    'use strict';

    // Constants for security limits
    const MAX_BLOB_SIZE = 50 * 1024 * 1024; // 50MB max
    const MAX_TEXT_LENGTH = 10 * 1024 * 1024; // 10 MiB max for clipboard
    const ALLOWED_ELEMENT_IDS = new Set(['image-container', 'mobile-text-input']);
    
    // Utility: Validate element ID to prevent DOM injection
    function isValidElementId(elementId) {
        if (typeof elementId !== 'string') return false;
        // Only allow alphanumeric, dash, underscore
        return /^[a-zA-Z0-9_-]+$/.test(elementId);
    }

    // Utility: Sanitize numeric input
    function sanitizeNumber(value, min = -Infinity, max = Infinity) {
        const num = Number(value);
        if (isNaN(num)) return 0;
        return Math.max(min, Math.min(max, num));
    }

    window.getBoundingClientRect = function (element) {
        if (!element || !(element instanceof Element)) {
            console.warn('Invalid element provided to getBoundingClientRect');
            return null;
        }
        try {
            return element.getBoundingClientRect();
        } catch (e) {
            console.error('Error getting bounding rect:', e);
            return null;
        }
    };

    window.updateImageBlob = function(byteArray) {
        try {
            // Validate input
            if (!byteArray || !(byteArray instanceof Uint8Array || Array.isArray(byteArray))) {
                console.error('Invalid byte array provided');
                return;
            }

            // Check size limit
            if (byteArray.length > MAX_BLOB_SIZE) {
                console.error('Image data exceeds maximum size');
                return;
            }

            // Validate JPEG signature
            if (byteArray.length < 2 || byteArray[0] !== 0xFF || byteArray[1] !== 0xD8) {
                console.error('Invalid JPEG signature');
                return;
            }

            const img = document.querySelector('.interactive-image');
            if (!img) {
                console.warn('Image element not found');
                return;
            }
            
            // Revoke old URL to prevent memory leaks
            if (img.src && img.src.startsWith('blob:')) {
                URL.revokeObjectURL(img.src);
            }
            
            const blob = new Blob([byteArray], { type: 'image/jpeg' });
            img.src = URL.createObjectURL(blob);
        } catch (e) {
            console.error('Error updating image blob:', e);
        }
    };

    window.getImageResolution = (imageElement) => {
        if (!imageElement || !(imageElement instanceof HTMLImageElement)) {
            console.warn('Invalid image element');
            return { width: 0, height: 0 };
        }
        return {
            width: sanitizeNumber(imageElement.naturalWidth, 0, 10000),
            height: sanitizeNumber(imageElement.naturalHeight, 0, 10000)
        };
    };

    window.preventContextMenu = (clientX, clientY) => {
        // Validate coordinates
        const x = sanitizeNumber(clientX, 0, window.innerWidth);
        const y = sanitizeNumber(clientY, 0, window.innerHeight);
        
        document.addEventListener('contextmenu', function (e) {
            e.preventDefault();
        }, { once: true, passive: false });
    };

    window.focusElement = (elementId) => {
        if (!isValidElementId(elementId)) {
            console.warn('Invalid element ID format');
            return;
        }
        
        try {
            const element = document.getElementById(elementId);
            if (element) {
                element.focus();
            } else {
                console.warn("Element not found:", elementId);
            }
        } catch (e) {
            console.error('Error focusing element:', e);
        }
    };

    window.enableDrag = (elementId) => {
        if (!isValidElementId(elementId)) {
            console.warn('Invalid element ID format');
            return;
        }

        const el = document.getElementById(elementId);
        if (!el) return;

        let isMouseDown = false;
        let offset = { x: 0, y: 0 };

        const onMouseDown = (e) => {
            isMouseDown = true;
            const rect = el.getBoundingClientRect();
            offset = {
                x: sanitizeNumber(e.clientX - rect.left),
                y: sanitizeNumber(e.clientY - rect.top)
            };
            document.addEventListener('mousemove', onMouseMove);
            document.addEventListener('mouseup', onMouseUp);
        };

        const onMouseMove = (e) => {
            if (!isMouseDown) return;
            const x = sanitizeNumber(e.clientX - offset.x, 0, window.innerWidth);
            const y = sanitizeNumber(e.clientY - offset.y, 0, window.innerHeight);
            
            el.style.position = 'absolute';
            el.style.left = `${x}px`;
            el.style.top = `${y}px`;
        };

        const onMouseUp = () => {
            isMouseDown = false;
            document.removeEventListener('mousemove', onMouseMove);
            document.removeEventListener('mouseup', onMouseUp);
        };

        el.addEventListener('mousedown', onMouseDown);
    };

    window.attachWheelListener = function (elementId, dotNetRef) {
        if (!isValidElementId(elementId)) {
            console.warn('Invalid element ID format');
            return;
        }

        if (!dotNetRef || typeof dotNetRef.invokeMethodAsync !== 'function') {
            console.error('Invalid dotNetRef provided');
            return;
        }

        const el = document.getElementById(elementId);
        if (!el) return;

        el.addEventListener('wheel', function (event) {
            event.preventDefault();

            try {
                dotNetRef.invokeMethodAsync('HandleScrollFromJs', {
                    clientX: sanitizeNumber(event.clientX, 0, window.innerWidth),
                    clientY: sanitizeNumber(event.clientY, 0, window.innerHeight),
                    deltaX: sanitizeNumber(event.deltaX, -1000, 1000),
                    deltaY: sanitizeNumber(event.deltaY, -1000, 1000)
                });
            } catch (e) {
                console.error('Error invoking HandleScrollFromJs:', e);
            }
        }, { passive: false });
    };

    window.toggleFullScreen = function (elementId) {
        if (!isValidElementId(elementId)) {
            console.warn('Invalid element ID format');
            return;
        }

        const element = document.getElementById(elementId);
        if (!element) return;

        if (!document.fullscreenElement) {
            element.requestFullscreen().then(() => {
                window.startRemoteControlSession(elementId);
            }).catch(err => {
                console.error('Error activating fullscreen:', err.message);
            });
        } else {
            document.exitFullscreen().catch(err => {
                console.error('Error exiting fullscreen:', err.message);
            });
        }
    };

    window.startRemoteControlSession = (elementId) => {
        if (!isValidElementId(elementId)) {
            console.warn('Invalid element ID format');
            return;
        }

        const element = document.getElementById(elementId);
        if (!element) return;

        element.setAttribute('tabindex', '0');
        element.focus();

        const keyHandler = (e) => {
            e.preventDefault();
            e.stopPropagation();

            if (typeof DotNet !== 'undefined' && DotNet.invokeMethodAsync) {
                try {
                    DotNet.invokeMethodAsync('NetLock_RMM_Web_Console', 'HandleKeyDown',
                        String(e.key), String(e.code), Boolean(e.ctrlKey), Boolean(e.shiftKey), Boolean(e.altKey));
                } catch (err) {
                    console.error('Error invoking HandleKeyDown:', err);
                }
            }
        };

        document.addEventListener('keydown', keyHandler);
        document.addEventListener('contextmenu', e => e.preventDefault());

        const cleanup = () => {
            console.log('Remote control session ended');
            document.removeEventListener('keydown', keyHandler);
        };

        document.addEventListener('fullscreenchange', () => {
            if (!document.fullscreenElement) {
                cleanup();
            }
        });
    };

    window.mobileKeyboardInput = {
        _dotNetRef: null,
        _input: null,
        _initialized: false,

        init: function(dotNetRef) {
            if (this._initialized) {
                console.warn('Mobile keyboard already initialized');
                return;
            }

            if (!dotNetRef || typeof dotNetRef.invokeMethodAsync !== 'function') {
                console.error('Invalid dotNetRef provided');
                return;
            }

            this._dotNetRef = dotNetRef;
            this._input = document.createElement('input');
            this._input.type = 'text';
            this._input.id = 'mobile-text-input';
            this._input.autocapitalize = 'off';
            this._input.autocomplete = 'off';
            this._input.spellcheck = false;
            this._input.maxLength = 1000; // Limit input length
            
            this._input.style.position = 'absolute';
            this._input.style.bottom = '10px';
            this._input.style.left = '10px';
            this._input.style.opacity = '0.01';
            this._input.style.height = '1px';
            this._input.style.width = '1px';
            this._input.style.zIndex = '9999';

            document.body.appendChild(this._input);

            this._input.addEventListener('input', (e) => {
                const val = e.target.value;
                if (val.length > 0 && val.length <= 100) { // Limit to 100 chars
                    try {
                        this._dotNetRef.invokeMethodAsync('HandleMobileTextInput', val);
                    } catch (err) {
                        console.error('Error handling mobile input:', err);
                    }
                    e.target.value = '';
                }
            });

            this._initialized = true;
        },

        show: function() {
            if (!this._input) return;
            this._input.style.opacity = '0.01';
            this._input.focus();
        },

        hide: function() {
            if (!this._input) return;
            this._input.blur();
        },

        dispose: function() {
            if (this._input && this._input.parentNode) {
                this._input.parentNode.removeChild(this._input);
            }
            this._input = null;
            this._dotNetRef = null;
            this._initialized = false;
        }
    };

    window.getClipboardContent = async () => {
        try {
            if (!navigator.clipboard || !navigator.clipboard.readText) {
                console.warn('Clipboard API not available');
                return '';
            }
            const text = await navigator.clipboard.readText();
            // Limit clipboard size
            return text.substring(0, MAX_TEXT_LENGTH);
        } catch (err) {
            console.error('Failed to read clipboard:', err);
            return '';
        }
    };

    window.setClipboardContent = async (text) => {
        try {
            if (!navigator.clipboard || !navigator.clipboard.writeText) {
                console.warn('Clipboard API not available');
                return;
            }
            if (typeof text !== 'string') {
                console.error('Invalid clipboard content type');
                return;
            }
            // Limit clipboard size
            const sanitized = text.substring(0, MAX_TEXT_LENGTH);
            await navigator.clipboard.writeText(sanitized);
        } catch (err) {
            console.error('Failed to write to clipboard:', err);
        }
    };

    window.fallbackSetClipboard = async function (text) {
        try {
            if (typeof text !== 'string') {
                console.error('Invalid clipboard content type');
                return;
            }
            const sanitized = text.substring(0, MAX_TEXT_LENGTH);
            await navigator.clipboard.writeText(sanitized);
        } catch (err) {
            console.error('fallbackSetClipboard failed:', err);
        }
    };

    window.remoteScreen = {
        isBlankImage: function (dataUrl, options) {
            return new Promise(function (resolve) {
                try {
                    if (!dataUrl || typeof dataUrl !== 'string') {
                        resolve(true);
                        return;
                    }

                    // Validate data URL format
                    if (!dataUrl.startsWith('data:image/') && !dataUrl.startsWith('blob:')) {
                        console.warn('Invalid image URL format');
                        resolve(true);
                        return;
                    }

                    options = options || {};
                    var sampleSize = sanitizeNumber(options.sampleSize || 64, 8, 256);
                    var brightnessThreshold = sanitizeNumber(options.brightnessThreshold || 16, 0, 255);
                    var pctThreshold = sanitizeNumber(options.pctThreshold || 0.99, 0, 1);

                    var img = new Image();
                    img.crossOrigin = 'anonymous';
                    
                    var timeout = setTimeout(() => {
                        console.warn('Image load timeout');
                        resolve(true);
                    }, 5000);

                    img.onload = function () {
                        clearTimeout(timeout);
                        var w = sampleSize, h = sampleSize;
                        var canvas = document.createElement('canvas');
                        canvas.width = w;
                        canvas.height = h;
                        var ctx = canvas.getContext('2d');
                        
                        try {
                            ctx.drawImage(img, 0, 0, w, h);
                            var imgd = ctx.getImageData(0, 0, w, h).data;
                            
                            var dark = 0;
                            var total = w * h;
                            for (var i = 0; i < imgd.length; i += 4) {
                                var r = imgd[i], g = imgd[i + 1], b = imgd[i + 2], a = imgd[i + 3];
                                if (a < 10) { dark++; continue; }
                                var lum = 0.2126 * r + 0.7152 * g + 0.0722 * b;
                                if (lum <= brightnessThreshold) dark++;
                            }
                            resolve((dark / total) >= pctThreshold);
                        } catch (e) {
                            console.error('Error processing image:', e);
                            resolve(false);
                        }
                    };
                    
                    img.onerror = function () {
                        clearTimeout(timeout);
                        console.warn('Image load failed');
                        resolve(true);
                    };
                    
                    img.src = dataUrl;
                } catch (e) {
                    console.error('Error in isBlankImage:', e);
                    resolve(true);
                }
            });
        }
    };

    window.scrollToBottom = (element) => {
        if (!element || !(element instanceof Element)) {
            console.warn('Invalid element for scrollToBottom');
            return;
        }
        try {
            element.scrollTop = element.scrollHeight;
            element.scroll({ top: element.scrollHeight, behavior: 'smooth' });
        } catch (e) {
            console.error('Error scrolling to bottom:', e);
        }
    };

    window.registerBeforeUnload = function(dotNetRef) {
        if (!dotNetRef || typeof dotNetRef.invokeMethodAsync !== 'function') {
            console.error('Invalid dotNetRef provided');
            return;
        }
        
        window.addEventListener('beforeunload', function () {
            try {
                dotNetRef.invokeMethodAsync('DisposeOnUnload');
            } catch (e) {
                console.error('Error in beforeunload handler:', e);
            }
        });
    };

})();
