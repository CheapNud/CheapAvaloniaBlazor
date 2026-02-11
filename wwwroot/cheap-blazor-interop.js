// cheap-blazor-interop.js
// This file should be embedded as a resource in the package

window.cheapBlazor = {
    // Clipboard functions
    getClipboardText: async function () {
        try {
            return await navigator.clipboard.readText();
        } catch (e) {
            console.error('Failed to read clipboard:', e);
            return null;
        }
    },

    setClipboardText: async function (text) {
        try {
            await navigator.clipboard.writeText(text);
        } catch (e) {
            console.error('Failed to write to clipboard:', e);
        }
    },

    // Notification functions
    showNotification: function (title, message) {
        if ('Notification' in window) {
            if (Notification.permission === 'granted') {
                new Notification(title, { body: message });
            } else if (Notification.permission !== 'denied') {
                Notification.requestPermission().then(permission => {
                    if (permission === 'granted') {
                        new Notification(title, { body: message });
                    }
                });
            }
        }
    },

    // File drag-and-drop handling via Photino message channel.
    // Uses a drag counter to handle spurious dragenter/dragleave from child elements.
    // Capture-phase listeners on window to intercept before WebView2 internal handlers.
    // The postMsg helper is guarded — preventDefault() always runs regardless of messaging.
    setupFileDrop: function () {
        var dragCounter = 0;

        var postMsg = function (type, payload) {
            try {
                if (window.chrome && window.chrome.webview) {
                    window.chrome.webview.postMessage(JSON.stringify({
                        type: type,
                        payload: payload || ''
                    }));
                }
            } catch (e) {
                console.warn('[CheapBlazor] Message post failed:', e);
            }
        };

        // Capture phase (true) on window — fires before any bubbling handlers or WebView2 internals.
        window.addEventListener('dragenter', function (e) {
            e.preventDefault();
            e.stopPropagation();
            dragCounter++;
            if (dragCounter === 1) {
                postMsg('cheapblazor:dragenter');
            }
        }, true);

        window.addEventListener('dragover', function (e) {
            e.preventDefault();
            e.stopPropagation();
            e.dataTransfer.dropEffect = 'copy';
        }, true);

        window.addEventListener('dragleave', function (e) {
            e.preventDefault();
            e.stopPropagation();
            dragCounter = Math.max(0, dragCounter - 1);
            if (dragCounter === 0) {
                postMsg('cheapblazor:dragleave');
            }
        }, true);

        window.addEventListener('drop', function (e) {
            e.preventDefault();
            e.stopPropagation();
            dragCounter = 0;

            var files = Array.from(e.dataTransfer.files);
            if (files.length > 0) {
                postMsg('cheapblazor:filedrop', JSON.stringify(
                    files.map(function (f) {
                        return {
                            name: f.name,
                            size: f.size,
                            type: f.type,
                            lastModified: f.lastModified
                        };
                    })
                ));
            }

            postMsg('cheapblazor:dragleave');
        }, true);

        console.log('[CheapBlazor] File drop handlers registered (capture phase on window)');
    },

    // File system helpers
    readFileAsBase64: async function (file) {
        return new Promise((resolve, reject) => {
            const reader = new FileReader();
            reader.onload = () => resolve(reader.result.split(',')[1]);
            reader.onerror = reject;
            reader.readAsDataURL(file);
        });
    },

    readFileAsText: async function (file) {
        return new Promise((resolve, reject) => {
            const reader = new FileReader();
            reader.onload = () => resolve(reader.result);
            reader.onerror = reject;
            reader.readAsText(file);
        });
    },

    // Download file helper
    downloadFile: function (filename, contentBase64, mimeType) {
        const byteCharacters = atob(contentBase64);
        const byteNumbers = new Array(byteCharacters.length);

        for (let i = 0; i < byteCharacters.length; i++) {
            byteNumbers[i] = byteCharacters.charCodeAt(i);
        }

        const byteArray = new Uint8Array(byteNumbers);
        const blob = new Blob([byteArray], { type: mimeType });

        const link = document.createElement('a');
        link.href = URL.createObjectURL(blob);
        link.download = filename;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(link.href);
    }
};

// Auto-initialize file drop. The preventDefault() calls must always run to prevent
// WebView2 from navigating to dropped files. Messaging is guarded inside postMsg.
window.cheapBlazor.setupFileDrop();
