
const fileInput = document.getElementById('fileInput');
const uploadArea = document.getElementById('uploadArea');
const fileInfo = document.getElementById('fileInfo');
const fileName = document.getElementById('fileName');
const fileSize = document.getElementById('fileSize');
const result = document.getElementById('result');
const error = document.getElementById('error');
const loading = document.getElementById('loading');
const resultContent = document.getElementById('resultContent');
const errorContent = document.getElementById('errorContent');

uploadArea.addEventListener('click', () => fileInput.click());

uploadArea.addEventListener('dragover', (e) => {
    e.preventDefault();
    uploadArea.classList.add('dragover');
});

uploadArea.addEventListener('dragleave', () => {
    uploadArea.classList.remove('dragover');
});

uploadArea.addEventListener('drop', (e) => {
    e.preventDefault();
    uploadArea.classList.remove('dragover');

    if (e.dataTransfer.files.length) {
        fileInput.files = e.dataTransfer.files;
        handleFileSelection();
    }
});

fileInput.addEventListener('change', handleFileSelection);

function handleFileSelection() {
    if (fileInput.files.length === 0) return;

    const file = fileInput.files[0];
    const fileSizeMB = (file.size / (1024 * 1024)).toFixed(2);

    fileName.textContent = file.name;
    fileSize.textContent = `حجم: ${fileSizeMB} مگابایت`;
    fileInfo.style.display = 'block';

    hideMessages();
}

async function estimateCompression() {
    await processFile('estimate');
}

async function compressFile() {
    await processFile('compress');
}

async function decompressFile() {
    await processFile('decompress');
}

async function processFile(action) {
    if (!fileInput.files[0]) {
        showError('لطفاً ابتدا یک فایل انتخاب کنید');
        return;
    }

    const formData = new FormData();
    formData.append('file', fileInput.files[0]);

    showLoading(true);
    hideMessages();

    try {
        let url;
        switch (action) {
            case 'estimate':
                url = '/api/FileCompression/estimate';
                break;
            case 'compress':
                url = '/api/FileCompression/compress';
                break;
            case 'decompress':
                url = '/api/FileCompression/decompress';
                break;
        }

        const response = await fetch(url, {
            method: 'POST',
            body: formData
        });

        if (action === 'compress' || action === 'decompress') {
            if (response.ok) {
                const blob = await response.blob();
                const downloadUrl = window.URL.createObjectURL(blob);
                const a = document.createElement('a');
                a.href = downloadUrl;

                if (action === 'compress') {
                    a.download = fileInput.files[0].name + '.zip';
                } else {
                    a.download = 'decompressed_file';
                }

                document.body.appendChild(a);
                a.click();
                document.body.removeChild(a);
                window.URL.revokeObjectURL(downloadUrl);

                showResult('فایل با موفقیت پردازش شد! دانلود شروع شد.');
            } else {
                const errorText = await response.text();
                showError('خطا: ' + errorText);
            }
        } else {
            if (response.ok) {
                const resultData = await response.json();
                showResult(`
    <div class="stats">
        <div class="stat-box">
            <div class="stat-value">${resultData.originalSizeFormatted}</div>
            <div class="stat-label">حجم اصلی</div>
        </div>
        <div class="stat-box">
            <div class="stat-value">${resultData.compressedSizeFormatted}</div>
            <div class="stat-label">حجم فشرده</div>
        </div>
        <div class="stat-box">
            <div class="stat-value">${resultData.spaceSavedFormatted}</div>
            <div class="stat-label">صرفه‌جویی شده</div>
        </div>
        <div class="stat-box">
            <div class="stat-value">${resultData.estimatedCompressionRatio.toFixed(2)}%</div>
            <div class="stat-label">نسبت فشرده‌سازی</div>
        </div>
    </div>
    <button class="btn btn-success" style="margin-top: 20px; width: 100%;" onclick="compressFile()">
        <i class="fas fa-download"></i> دانلود فایل فشرده
    </button>
    `);
            } else {
                const errorText = await response.text();
                showError('خطا: ' + errorText);
            }
        }
    } catch (error) {
        console.error('Network error:', error);
        showError('خطای شبکه: ' + error.message);
    } finally {
        showLoading(false);
    }
}

function showLoading(show) {
    loading.style.display = show ? 'block' : 'none';
}

function showResult(content) {
    resultContent.innerHTML = content;
    result.style.display = 'block';
    error.style.display = 'none';
}

function showError(message) {
    errorContent.textContent = message;
    error.style.display = 'block';
    result.style.display = 'none';
}

function hideMessages() {
    result.style.display = 'none';
    error.style.display = 'none';
}