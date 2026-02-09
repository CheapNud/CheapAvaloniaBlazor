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
