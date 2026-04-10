// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
document.addEventListener('DOMContentLoaded', function () {
    initializeGlobalUploadProgress();

    // Universal Form Submission State (ISO 9241-210 Predictability Principle)
    const forms = document.querySelectorAll('form');
    
    forms.forEach(form => {
        form.addEventListener('submit', function (event) {
            
            // If the form has jQuery unobtrusive validation, ensure it's valid first
            if (window.jQuery && window.jQuery.validator && window.jQuery(this).data('validator')) {
                if (!window.jQuery(this).valid()) {
                    return; // Validation failed, do not show loading state
                }
            } else if (this.checkValidity && !this.checkValidity()) {
                // Fallback to HTML5 validation
                return; 
            }

            // Find the primary submit button
            const submitBtn = this.querySelector('button[type="submit"]') || this.querySelector('input[type="submit"]');
            
            if (submitBtn && !submitBtn.classList.contains('processing')) {
                
                // Determine layout preservation (width) to avoid jank
                const originalWidth = submitBtn.offsetWidth;
                
                // Add a processing class to prevent double-triggering
                submitBtn.classList.add('processing');
                
                // Small timeout allows the browser to dispatch the actual submission 
                // BEFORE the button is legally disabled in the DOM.
                setTimeout(() => {
                    submitBtn.disabled = true;
                    submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>Processing...';
                    
                    if (originalWidth > 0) {
                        submitBtn.style.minWidth = originalWidth + 'px';
                    }
                }, 10);
            }

            if (isMultipartWithSelectedFile(this)) {
                window.ITUploadProgress?.start({ determinate: false, label: 'Uploading file...' });
            }
        });
    });

    // Universal drag-and-drop enhancement for bulk import file inputs
    initializeDragUploadZones();
});

function initializeDragUploadZones() {
    const fileInputs = document.querySelectorAll('input[type="file"][data-drag-upload]');

    fileInputs.forEach(fileInput => {
        if (fileInput.dataset.dragUploadInitialized === 'true') {
            return;
        }

        const wrapper = document.createElement('div');
        wrapper.className = 'drag-upload-zone';
        wrapper.setAttribute('tabindex', '0');
        wrapper.setAttribute('role', 'button');
        wrapper.setAttribute('aria-label', fileInput.getAttribute('aria-label') || 'Drag and drop file upload area');

        const prompt = document.createElement('div');
        prompt.className = 'drag-upload-prompt';
        prompt.innerHTML = '<i class="bi bi-cloud-arrow-up"></i><span>Drag and drop your file here, or click to browse</span>';

        const fileName = document.createElement('div');
        fileName.className = 'drag-upload-file-name';
        fileName.textContent = 'No file selected';

        const errorMessage = document.createElement('div');
        errorMessage.className = 'drag-upload-error';

        fileInput.parentNode.insertBefore(wrapper, fileInput);
        wrapper.appendChild(fileInput);
        wrapper.appendChild(prompt);
        wrapper.appendChild(fileName);
        wrapper.appendChild(errorMessage);

        fileInput.classList.add('drag-upload-input');

        const maxFileSize = Number(fileInput.dataset.maxFileSize || 5 * 1024 * 1024);
        const acceptTokens = (fileInput.getAttribute('accept') || '')
            .split(',')
            .map(value => value.trim().toLowerCase())
            .filter(Boolean);
        const acceptExtensions = acceptTokens.filter(value => value.startsWith('.'));
        const acceptMimeTypes = acceptTokens.filter(value => value.includes('/'));

        const formatFileSize = (bytes) => {
            if (bytes >= 1024 * 1024) {
                return `${Math.round((bytes / (1024 * 1024)) * 10) / 10}MB`;
            }
            if (bytes >= 1024) {
                return `${Math.round(bytes / 1024)}KB`;
            }
            return `${bytes} bytes`;
        };

        const clearError = () => {
            errorMessage.textContent = '';
            wrapper.classList.remove('has-error');
            fileInput.setCustomValidity('');
        };

        const setError = (message) => {
            errorMessage.textContent = message;
            wrapper.classList.add('has-error');
            fileInput.setCustomValidity(message);
        };

        const isAcceptedFileType = (file) => {
            if (acceptTokens.length === 0) {
                return true;
            }

            const lowerName = (file?.name || '').toLowerCase();
            const mimeType = (file?.type || '').toLowerCase();

            const extensionMatch = acceptExtensions.some(ext => lowerName.endsWith(ext));
            const mimeMatch = acceptMimeTypes.some(mime => {
                if (mime.endsWith('/*')) {
                    const prefix = mime.slice(0, mime.indexOf('/'));
                    return mimeType.startsWith(`${prefix}/`);
                }
                return mimeType === mime;
            });

            return extensionMatch || mimeMatch;
        };

        const validateFiles = (files) => {
            if (!files || files.length === 0) {
                clearError();
                return true;
            }

            if (!fileInput.multiple && files.length > 1) {
                setError('Please upload only one file.');
                return false;
            }

            const file = files[0];

            if (!isAcceptedFileType(file)) {
                const acceptedText = acceptTokens.join(', ');
                setError(`Invalid file type. Allowed: ${acceptedText}.`);
                return false;
            }

            if (file.size > maxFileSize) {
                setError(`File is too large (${formatFileSize(file.size)}). Maximum allowed is ${formatFileSize(maxFileSize)}.`);
                return false;
            }

            clearError();
            return true;
        };

        const updateSelectedFile = () => {
            if (fileInput.files && fileInput.files.length > 0) {
                fileName.textContent = fileInput.files[0].name;
                wrapper.classList.add('has-file');
            } else {
                fileName.textContent = 'No file selected';
                wrapper.classList.remove('has-file');
            }
        };

        const preventDefault = (event) => {
            event.preventDefault();
            event.stopPropagation();
        };

        ['dragenter', 'dragover'].forEach(eventName => {
            wrapper.addEventListener(eventName, event => {
                preventDefault(event);
                wrapper.classList.add('is-dragover');
            });
        });

        ['dragleave', 'dragend'].forEach(eventName => {
            wrapper.addEventListener(eventName, event => {
                preventDefault(event);
                wrapper.classList.remove('is-dragover');
            });
        });

        wrapper.addEventListener('drop', event => {
            preventDefault(event);
            wrapper.classList.remove('is-dragover');

            const droppedFiles = event.dataTransfer && event.dataTransfer.files;
            if (!droppedFiles || droppedFiles.length === 0) {
                return;
            }

            if (!validateFiles(droppedFiles)) {
                fileInput.value = '';
                updateSelectedFile();
                return;
            }

            if (!fileInput.multiple && droppedFiles.length > 1) {
                const transfer = new DataTransfer();
                transfer.items.add(droppedFiles[0]);
                fileInput.files = transfer.files;
            } else {
                fileInput.files = droppedFiles;
            }

            fileInput.dispatchEvent(new Event('change', { bubbles: true }));
        });

        wrapper.addEventListener('keydown', event => {
            if (event.key === 'Enter' || event.key === ' ') {
                event.preventDefault();
                fileInput.click();
            }
        });

        fileInput.addEventListener('change', () => {
            if (!validateFiles(fileInput.files)) {
                fileInput.value = '';
            }
            updateSelectedFile();
        });

        updateSelectedFile();
        fileInput.dataset.dragUploadInitialized = 'true';
    });
}

function initializeGlobalUploadProgress() {
    if (document.getElementById('globalUploadProgress')) {
        return;
    }

    const container = document.createElement('div');
    container.id = 'globalUploadProgress';
    container.className = 'global-upload-progress d-none';
    container.innerHTML = `
        <div class="global-upload-progress__bar-wrap">
            <div class="global-upload-progress__bar" style="width: 0%;"></div>
        </div>
        <div class="global-upload-progress__label">Uploading...</div>
    `;
    document.body.appendChild(container);

    const bar = container.querySelector('.global-upload-progress__bar');
    const label = container.querySelector('.global-upload-progress__label');
    let autoTimer = null;
    let autoValue = 0;

    const clearAutoTimer = () => {
        if (autoTimer) {
            clearInterval(autoTimer);
            autoTimer = null;
        }
    };

    const show = () => {
        container.classList.remove('d-none');
        container.classList.add('show');
    };

    const hide = () => {
        clearAutoTimer();
        container.classList.remove('show');
        setTimeout(() => container.classList.add('d-none'), 220);
    };

    window.ITUploadProgress = {
        start: ({ determinate = false, label: text = 'Uploading...' } = {}) => {
            clearAutoTimer();
            show();
            label.textContent = text;
            bar.classList.remove('indeterminate');

            if (determinate) {
                autoValue = 0;
                bar.style.width = '0%';
                return;
            }

            bar.classList.add('indeterminate');
            autoValue = 8;
            bar.style.width = `${autoValue}%`;
            autoTimer = setInterval(() => {
                autoValue = Math.min(autoValue + (autoValue < 60 ? 8 : 2), 90);
                bar.style.width = `${autoValue}%`;
            }, 280);
        },
        set: (percent, text = 'Uploading...') => {
            show();
            label.textContent = text;
            bar.classList.remove('indeterminate');
            const clamped = Math.max(0, Math.min(100, Number(percent) || 0));
            bar.style.width = `${clamped}%`;
        },
        finish: (text = 'Upload complete') => {
            clearAutoTimer();
            label.textContent = text;
            bar.classList.remove('indeterminate');
            bar.style.width = '100%';
            setTimeout(hide, 500);
        },
        fail: (text = 'Upload failed') => {
            clearAutoTimer();
            label.textContent = text;
            bar.classList.remove('indeterminate');
            container.classList.add('error');
            setTimeout(() => {
                container.classList.remove('error');
                hide();
            }, 1200);
        }
    };
}

function isMultipartWithSelectedFile(form) {
    if (!form || form.getAttribute('enctype') !== 'multipart/form-data') {
        return false;
    }

    const fileInputs = form.querySelectorAll('input[type="file"]');
    return Array.from(fileInputs).some(input => input.files && input.files.length > 0);
}
