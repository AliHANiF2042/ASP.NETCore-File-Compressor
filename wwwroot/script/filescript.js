
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
    const fileInput = document.getElementById('fileInput');
    const formData = new FormData();

    if (!fileInput.files[0]) {
        showError('Please select a file first');
        return;
    }

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

        console.log('Sending request to:', url);

        const response = await fetch(url, {
            method: 'POST',
            body: formData
        });

        console.log('Response status:', response.status);

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

                showResult('File processed successfully! Download started.');
            } else {
                const errorText = await response.text();
                showError('Error: ' + errorText);
            }
        } else {
            if (response.ok) {
                const result = await response.json();
                showResult(`
    <p><strong>Original Size:</strong> ${result.originalSizeFormatted}</p>
    <p><strong>Estimated Compressed Size:</strong> ${result.compressedSizeFormatted}</p>
    <p><strong>Space Saved:</strong> ${result.spaceSavedFormatted}</p>
    <p><strong>Compression Ratio:</strong> ${result.estimatedCompressionRatio.toFixed(2)}%</p>
    <div class="mt-3">
        <button class="btn btn-success" onclick="downloadEstimatedFile()">
            <i class="fas fa-download"></i> Download Compressed File
        </button>
    </div>
    `);

                window.lastEstimateData = {
                    file: fileInput.files[0],
                    result: result
                };
            } else {
                const errorText = await response.text();
                showError('Error: ' + errorText);
            }
        }
    } catch (error) {
        console.error('Network error:', error);
        showError('Network error: ' + error.message);
    } finally {
        showLoading(false);
    }
}

async function downloadEstimatedFile() {
    const fileInput = document.getElementById('fileInput');
    const formData = new FormData();

    if (!fileInput.files[0]) {
        showError('Please select a file first');
        return;
    }

    formData.append('file', fileInput.files[0]);

    showLoading(true);
    hideMessages();

    try {
        const response = await fetch('/api/FileCompression/estimate?download=true', {
            method: 'POST',
            body: formData
        });

        if (response.ok) {
            const blob = await response.blob();
            const downloadUrl = window.URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = downloadUrl;
            a.download = fileInput.files[0].name + '.zip';
            document.body.appendChild(a);
            a.click();
            document.body.removeChild(a);
            window.URL.revokeObjectURL(downloadUrl);

            showResult('Compressed file downloaded successfully!');
        } else {
            const errorText = await response.text();
            showError('Download error: ' + errorText);
        }
    } catch (error) {
        console.error('Download error:', error);
        showError('Download error: ' + error.message);
    } finally {
        showLoading(false);
    }
}

function showLoading(show) {
    document.getElementById('loading').style.display = show ? 'block' : 'none';
}

function showResult(content) {
    document.getElementById('resultContent').innerHTML = content;
    document.getElementById('result').style.display = 'block';
    document.getElementById('error').style.display = 'none';
}

function showError(message) {
    document.getElementById('errorContent').innerHTML = message;
    document.getElementById('error').style.display = 'block';
    document.getElementById('result').style.display = 'none';
}

function hideMessages() {
    document.getElementById('result').style.display = 'none';
    document.getElementById('error').style.display = 'none';
}