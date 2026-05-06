using System;
using System.IO;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;

namespace BackupSyncApp.Services
{
    public interface IArchiveService
    {
        Task<ArchiveResult> CreateArchiveAsync(
            string sourceFolder,
            string destinationPath,
            string archiveName,
            int compressionLevel = 6,
            string password = null);
    }

    public class ArchiveResult
    {
        public string ArchivePath { get; set; }
        public long OriginalSize { get; set; }
        public long CompressedSize { get; set; }
        public double CompressionRatio { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public bool IsEncrypted { get; set; }
    }

    public class ArchiveService : IArchiveService
    {
        public async Task<ArchiveResult> CreateArchiveAsync(
            string sourceFolder,
            string destinationPath,
            string archiveName,
            int compressionLevel = 6,
            string password = null)
        {
            return await Task.Run(() =>
            {
                var result = new ArchiveResult();

                try
                {
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string fileName = $"{archiveName}.zip";
                    string fullPath = Path.Combine(destinationPath, fileName);

                    result.OriginalSize = GetDirectorySize(sourceFolder);

                    // === SharpZipLib с поддержкой пароля ===
                    using (var zipStream = new ZipOutputStream(File.Create(fullPath)))
                    {
                        // Уровень сжатия (0-9)
                        zipStream.SetLevel(compressionLevel);

                        // === Устанавливаем пароль если есть ===
                        if (!string.IsNullOrEmpty(password))
                        {
                            zipStream.Password = password;
                            result.IsEncrypted = true;
                        }

                        // Добавляем файлы рекурсивно
                        AddFolderToZip(zipStream, sourceFolder, sourceFolder);

                        zipStream.Finish();
                        zipStream.Close();
                    }

                    result.CompressedSize = new FileInfo(fullPath).Length;
                    result.CompressionRatio = result.OriginalSize > 0
                        ? (1.0 - (double)result.CompressedSize / result.OriginalSize) * 100
                        : 0;

                    result.ArchivePath = fullPath;
                    result.Success = true;
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.ErrorMessage = ex.Message;
                }

                return result;
            });
        }

        /// <summary>
        /// Рекурсивно добавляет папку в ZIP
        /// </summary>
        private void AddFolderToZip(ZipOutputStream zipStream, string folderPath, string rootFolder)
        {
            string[] files = Directory.GetFiles(folderPath);
            string[] subFolders = Directory.GetDirectories(folderPath);

            foreach (string file in files)
            {
                // Создаем entry с относительным путём
                string entryName = file.Substring(rootFolder.Length + 1);
                var zipEntry = new ZipEntry(entryName)
                {
                    DateTime = DateTime.Now,
                    IsUnicodeText = true
                };

                zipStream.PutNextEntry(zipEntry);

                // Копируем содержимое файла
                using (var fileStream = File.OpenRead(file))
                {
                    byte[] buffer = new byte[4096];
                    int bytesRead;
                    while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        zipStream.Write(buffer, 0, bytesRead);
                    }
                }

                zipStream.CloseEntry();
            }

            // Рекурсивно обрабатываем подпапки
            foreach (string subFolder in subFolders)
            {
                AddFolderToZip(zipStream, subFolder, rootFolder);
            }
        }

        private long GetDirectorySize(string path)
        {
            long size = 0;
            try
            {
                foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                {
                    try { size += new FileInfo(file).Length; }
                    catch { }
                }
            }
            catch { }
            return size;
        }
    }
}