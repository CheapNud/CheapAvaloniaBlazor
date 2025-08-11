// cheap-blazor-interop.js
// This file should be embedded as a resource in the package

window.cheapBlazor = {
    initialize: function () {
        // Set up communication with Photino
        if (window.external && window.external.sendMessage) {
            console.log('CheapAvaloniaBlazor: Photino bridge initialized');
        }

        // Handle window controls
        this.setupWindowControls();

        // Handle file drop
        this.setupFileDrop();
    },

    // Window control functions
    sendMessage: function (type, payload) {
        if (window.external && window.external.sendMessage) {
            window.external.sendMessage(JSON.stringify({ type, payload }));
        }
    },

    closeWindow: function () {
        this.sendMessage('close');
    },

    minimizeWindow: function () {
        this.sendMessage('minimize');
    },

    maximizeWindow: function () {
        this.sendMessage('maximize');
    },

    setWindowTitle: function (title) {
        this.sendMessage('setTitle', title);
    },

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

    // File handling
    setupFileDrop: function () {
        document.addEventListener('dragover', (e) => {
            e.preventDefault();
            e.stopPropagation();
        });

        document.addEventListener('drop', async (e) => {
            e.preventDefault();
            e.stopPropagation();

            const files = Array.from(e.dataTransfer.files);
            if (files.length > 0 && window.cheapBlazorInteropService) {
                // Call back to Blazor service instance
                await window.cheapBlazorInteropService.invokeMethodAsync('OnFilesDropped',
                    files.map(f => ({
                        name: f.name,
                        size: f.size,
                        type: f.type,
                        lastModified: f.lastModified
                    }))
                );
            }
        });
    },

    // Window controls setup
    setupWindowControls: function () {
        // Handle double-click on title bar to maximize
        const titlebar = document.querySelector('.titlebar-drag-region');
        if (titlebar) {
            titlebar.addEventListener('dblclick', () => {
                this.sendMessage('toggleMaximize');
            });
        }

        // Prevent context menu in title bar
        document.addEventListener('contextmenu', (e) => {
            if (e.target.closest('.cheap-blazor-titlebar')) {
                e.preventDefault();
            }
        });
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