using System.IO.Compression;

namespace FileCompressor.Services
{
    public interface iFileCompressorService
    {
        Task<Stream> CompressorFileAsync(Stream fileStream, string fileName);
        Task<Stream> DecompressFileAsync(Stream compressedStream, string fileName);
        long GetOrginalFileSize(Stream stream);
        long GetCompressedFileSize(Stream stream);
    }

    public class FileCompressorService : iFileCompressorService
    {
        public async Task<Stream> CompressorFileAsync(Stream fileStream, string fileName)
        {
            fileStream.Position = 0;
            using var memoryFileStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryFileStream);
            var fileData = memoryFileStream.ToArray();

            var outputFileStream = new MemoryStream();
            using (var archive = new ZipArchive(outputFileStream, ZipArchiveMode.Create, true))
            {
                var entry = archive.CreateEntry(fileName, CompressionLevel.Optimal);

                using (var entryStream = entry.Open())
                using (var dataStream = new MemoryStream(fileData))
                {
                    await dataStream.CopyToAsync(entryStream);
                }
            }

            outputFileStream.Position = 0;
            return outputFileStream;
        }

        public async Task<Stream> DecompressFileAsync(Stream compressedStream, string fileName)
        {
            try
            {
                compressedStream.Position = 0;

                using var memoryFileStream = new MemoryStream();
                await compressedStream.CopyToAsync(memoryFileStream);
                memoryFileStream.Position = 0;

                if (!IsValidZipFile(memoryFileStream))
                {
                    throw new InvalidDataException("The file is not a valid ZIP archive or is corrupted!");
                }

                memoryFileStream.Position = 0;

                using var fileArchive = new ZipArchive(compressedStream, ZipArchiveMode.Read, true);
                if(fileArchive.Entries.Count == 0)
                {
                    throw new InvalidDataException("The ZIP archive is empty!");
                }

                ZipArchiveEntry entry;
                if (!string.IsNullOrEmpty(fileName))
                {
                    entry = fileArchive.GetEntry(fileName);
                    if (entry == null)
                    {
                        throw new FileNotFoundException($"File '{fileName}' not found in archive");
                    }
                }
                else
                {
                    entry = fileArchive.Entries.FirstOrDefault(e => !string.IsNullOrEmpty(e.Name) && e.Length > 0);
                    if (entry == null)
                    {
                        throw new FileNotFoundException("No files found in archive!");
                    }
                }

                var outputFileStream = new MemoryStream();
                using (var fileEntryStream = entry.Open())
                {
                    await fileEntryStream.CopyToAsync(outputFileStream);
                }

                outputFileStream.Position = 0;
                return outputFileStream;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error decompressing file: {ex.Message}", ex);
            }
        }

        private bool IsValidZipFile(Stream fileStream)
        {
            try
            {
                fileStream.Position = 0;

                byte[] signature = new byte[4];
                int bytesRead = fileStream.Read(signature, 0, 4);

                if (bytesRead < 4 || signature[0] != 0x50 || signature[1] != 0x4B)
                {
                    return false;
                }

                fileStream.Position = 0;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public long GetOrginalFileSize(Stream stream)
        {
            return stream.Length;
        }

        public long GetCompressedFileSize(Stream stream)
        {
            try
            {
                var filePosition = stream.Position;
                stream.Position = 0;

                using var memoryFileStream = new MemoryStream();
                stream.CopyTo(memoryFileStream);
                var fileData = memoryFileStream.ToArray();

                using var compressedFileStream = new MemoryStream();

                using (var fileArchive = new ZipArchive(compressedFileStream, ZipArchiveMode.Create, true))
                {
                    var fileEntry = fileArchive.CreateEntry("temp_file", CompressionLevel.Optimal);
                    using (var entryFileStream = fileEntry.Open())
                    {
                        stream.Position = 0;
                        stream.CopyTo(entryFileStream);
                    }
                }

                stream.Position = filePosition;
                return compressedFileStream.Length;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error estimating compressed size: {ex.Message}", ex);
            }
        }
    }
}
