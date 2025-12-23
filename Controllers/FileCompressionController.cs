using FileCompressor.Services;
using Microsoft.AspNetCore.Mvc;

namespace FileCompressor.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileCompressionController : ControllerBase
    {
        private readonly ILogger<FileCompressionController> _logger;
        private readonly iFileCompressorService _fileCompressorService;

        public FileCompressionController(ILogger<FileCompressionController> logger, iFileCompressorService fileCompressorService)
        {
            _logger = logger;
            _fileCompressorService = fileCompressorService;
        }

        [HttpPost("compress")]
        [RequestSizeLimit(100_000_000)]
        public async Task<IActionResult> CompressFile([FromForm] IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest("Please select a file!");
                }

                if (file.Length > 100 * 1024 * 1024)
                {
                    return BadRequest("File size must be less than 100MB");
                }

                _logger.LogInformation($"Starting compression for file: {file.FileName}");

                using var fileStream = file.OpenReadStream();
                var compressedFileStream = await _fileCompressorService.CompressorFileAsync(fileStream, file.FileName);

                _logger.LogInformation($"Compression completed successfully for: {file.FileName}");

                return File(compressedFileStream, "application/zip", $"{Path.GetFileNameWithoutExtension(file.FileName)}.zip");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error compressing file");
                return StatusCode(500, $"Error compressing file: {ex.Message}");
            }
        }

        [HttpPost("decompress")]
        [RequestSizeLimit(100_000_000)]
        public async Task<IActionResult> DecompressFile([FromForm] IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest("Please select a zip file to decompress");

                var fileExtension = Path.GetExtension(file.FileName).ToLower();
                if(fileExtension != ".zip")
                {
                    return BadRequest("Please select a valid ZIP file. Only .zip files are supported!");
                }

                if (file.Length > 100 * 1024 * 1024)
                    return BadRequest("File size must be less than 100MB");

                _logger.LogInformation($"Starting decompression for file: {file.FileName}");

                using var fileStream = file.OpenReadStream();
                if (!IsLikelyZipFile(fileStream))
                {
                    return BadRequest("The selected file does not appear to be a valid ZIP archive.");
                }

                var decompressedStream = await _fileCompressorService.DecompressFileAsync(fileStream, null);

                _logger.LogInformation($"Decompression completed successfully for: {file.FileName}");

                var outputFileName = Path.GetExtension(file.FileName).ToLower() == ".zip"
                    ? Path.GetFileNameWithoutExtension(file.FileName)
                    : "decompressed_file";

                return File(decompressedStream, "application/octet-stream", outputFileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decompressing file");
                return StatusCode(500, $"Error decompressing file: {ex.Message}");
            }
        }

        [HttpPost("estimate")]
        [RequestSizeLimit(100_000_000)]
        public async Task<IActionResult> EstimateCompression([FromForm] IFormFile file, [FromQuery] bool fileDownload = false)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest("Please select a file");

                if (file.Length > 100 * 1024 * 1024)
                    return BadRequest("File size must be less than 100MB");

                _logger.LogInformation($"Estimating compression for file: {file.FileName}");

                using var fileStream = file.OpenReadStream();

                if (fileDownload)
                {
                    var compressedFileStream = await _fileCompressorService.CompressorFileAsync(fileStream, file.FileName);
                    return File(compressedFileStream, "application/zip", $"{Path.GetFileNameWithoutExtension(file.FileName)}.zip");
                }
                else
                {
                    var originalSize = file.Length;
                    var compressedSize = _fileCompressorService.GetCompressedFileSize(fileStream);

                    var compressionRatio = Math.Round(((double)(originalSize - compressedSize) / originalSize) * 100, 2);
                    var spaceSaved = originalSize - compressedSize;

                    return Ok(new
                    {
                        OriginalSize = originalSize,
                        EstimatedCompressedSize = compressedSize,
                        EstimatedSpaceSaved = spaceSaved,
                        EstimatedCompressionRatio = compressionRatio,
                        OriginalSizeFormatted = FormatFileSize(originalSize),
                        CompressedSizeFormatted = FormatFileSize(compressedSize),
                        SpaceSavedFormatted = FormatFileSize(spaceSaved)
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error estimating compression");
                return StatusCode(500, $"Error estimating compression: {ex.Message}");
            }
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double len = bytes;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private bool IsLikelyZipFile(Stream fileStream)
        {
            try
            {
                fileStream.Position = 0;
                byte[] header = new byte[4];
                int bytesRead = fileStream.Read(header, 0, 4);
                fileStream.Position = 0;

                return bytesRead >= 4 &&
                       header[0] == 0x50 && header[1] == 0x4B && // PK
                       (header[2] == 0x03 || header[2] == 0x05 || header[2] == 0x07) &&
                       (header[3] == 0x04 || header[3] == 0x06 || header[3] == 0x08);
            }
            catch
            {
                return false;
            }
        }
    }
}