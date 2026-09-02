using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using BackupSyncApp.Common;

namespace BackupSyncApp.Services
{
    public class BackupManager
    {
        public event Action<string, LogMessageType> LogMessage;
        public event Action<int> ProgressChanged;
        public event Action<string> StatusChanged;

        private readonly IArchiveService _archiveService;
        private bool _enableCompression;
        private string _archivePassword;
        private CompressMode _compressMode;
        private DialogService _dialogService;

        public BackupManager(DialogService dialogService, IArchiveService archiveService = null, bool enableCompression = false, CompressMode compressMode= CompressMode.Balanced, string archivePassword=null)
        {
            _archiveService = archiveService;
            _enableCompression = enableCompression;
            _compressMode = compressMode;
            _archivePassword=archivePassword;
            _dialogService = dialogService;
        }

        public void UpdateCompressionSettings(bool enableCompression, CompressMode compressMode)
        {
            _enableCompression=enableCompression;
            _compressMode=compressMode;
        }

        public void UpdateArchivePassword(string password)
        {
            _archivePassword = password;
        }

        private int GetCompressionLevelFromMode(CompressMode mode)
        {
            return mode switch
            {
                CompressMode.Fast => 3,
                CompressMode.Balanced => 6,
                CompressMode.Maximum => 9,
                _ => 6
            };
        }

        public async Task CopyFolderAsync(List<string> sourceFolders, string targetRootPath)
        {
            if (sourceFolders == null || sourceFolders.Count == 0)
            {
                LogMessage?.Invoke("❌ No folders for backup selected", LogMessageType.Error);
                _dialogService.ShowMessageBox("Error_NoSourceGiven", "Msg_ErrorTitle", MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrEmpty(targetRootPath))
            {
                LogMessage?.Invoke("❌ no target path specified", LogMessageType.Error);
                _dialogService.ShowMessageBox("Error_NoPathChosen", "Msg_ErrorTitle", MessageBoxImage.Error);
                return;
            }

            // === Используем временную папку для копирования ===
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            
            string tempFolder;
            
            string finalArchivePath;

            if (_enableCompression)
            {
                tempFolder = Path.Combine(targetRootPath, $"Backup_{timestamp}_TEMP");
                finalArchivePath = Path.Combine(targetRootPath, $"Backup_{timestamp}.zip");
            }
            else
            {
                // no archive
                tempFolder = Path.Combine(targetRootPath, $"Backup_{timestamp}");
                finalArchivePath = null;
            }

            Directory.CreateDirectory(tempFolder);
            LogMessage?.Invoke($"📁 Target folder: {tempFolder}", LogMessageType.Info);

            int totalFiles = 0;
            int copiedFiles = 0;
            int skippedFiles = 0;
            int errorFiles = 0;

            LogMessage?.Invoke("📊 counting files...", LogMessageType.Progress);
            foreach (var sourceFolder in sourceFolders)
            {
                if (Directory.Exists(sourceFolder))
                {
                    totalFiles += Directory.GetFiles(sourceFolder, "*", SearchOption.AllDirectories).Length;
                }
            }

            LogMessage?.Invoke($"📊 files found: {totalFiles}", LogMessageType.Progress);
            LogMessage?.Invoke($"🔄 Starting backup...", LogMessageType.Progress);
            StatusChanged?.Invoke("Copying");

            foreach (var sourceFolder in sourceFolders)
            {
                if (!Directory.Exists(sourceFolder))
                {
                    LogMessage?.Invoke($"⚠️ Folder not found: {sourceFolder}", LogMessageType.Error);
                    continue;
                }

                string folderName = Path.GetFileName(sourceFolder);
                string targetFolder = Path.Combine(tempFolder, folderName);

                LogMessage?.Invoke($"📂 Copying: {Path.GetFileName(sourceFolder)}", LogMessageType.Progress);

                var result = await CopyDirectoryAsync(sourceFolder, targetFolder, totalFiles, copiedFiles);

                copiedFiles = result.Copied;
                skippedFiles += result.Skipped;
                errorFiles += result.Errors;
            }

            // === АРХИВАЦИЯ ===
            if (_enableCompression && _archiveService != null)
            {
                StatusChanged?.Invoke("Archiving");          
                LogMessage?.Invoke("📦 Starting compression...", LogMessageType.Progress);

                
                int level = GetCompressionLevelFromMode(_compressMode);
                LogMessage?.Invoke($"DEBUG: Compression mode = {_compressMode}, Level = {level}/9", LogMessageType.Info);

                ProgressChanged?.Invoke(95);
                
                var archiveResult = await _archiveService.CreateArchiveAsync(tempFolder, targetRootPath, $"Backup_{timestamp}", level,_archivePassword);

                if (archiveResult.Success)
                {
                    LogMessage?.Invoke($"✅ Archive created: {Path.GetFileName(archiveResult.ArchivePath)}", LogMessageType.Success);
                    if(archiveResult.IsEncrypted) LogMessage?.Invoke($"🔒 Archive is password protected",LogMessageType.Info);
                    LogMessage?.Invoke($"📊 Compression ratio: {archiveResult.CompressionRatio:F1}% (Original: {FormatSize(archiveResult.OriginalSize)} → Compressed: {FormatSize(archiveResult.CompressedSize)})", LogMessageType.Info);

                    // Удаляем временную папку после успешной архивации
                    try
                    {
                        Directory.Delete(tempFolder, true);
                    }
                    catch (Exception ex)
                    {
                        LogMessage?.Invoke($"⚠️ Could not remove temp folder: {ex.Message}", LogMessageType.Warning);
                    }
                }
                else
                {
                    LogMessage?.Invoke($"❌ Archive creation failed: {archiveResult.ErrorMessage}", LogMessageType.Error);
                    _dialogService.ShowMessageBox("Error_ArchiveFailed", "Msg_ErrorTitle", MessageBoxImage.Error, archiveResult.ErrorMessage);
                }
            }

            ProgressChanged?.Invoke(100);
            StatusChanged?.Invoke("Ready");

            LogMessage?.Invoke("✅ Copying finished.", LogMessageType.Success);
            LogMessage?.Invoke($"📊 Result: copied {copiedFiles}, skipped {skippedFiles}, {errorFiles} errors out of {totalFiles} files", LogMessageType.Info);
        }

        private string FormatSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double size = bytes;
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            return $"{size:0.##} {sizes[order]}";
        }

        private struct CopyResult
        {
            public int Copied;
            public int Skipped;
            public int Errors;
        }

        private async Task<CopyResult> CopyDirectoryAsync(string sourceDir, string targetDir, int totalFiles, int alreadyCopied)
        {
            var result = new CopyResult { Copied = alreadyCopied };

            Directory.CreateDirectory(targetDir);

            var files = Directory.GetFiles(sourceDir);
            foreach (var file in files)
            {
                string fileName = Path.GetFileName(file);
                string destFile = Path.Combine(targetDir, fileName);

                try
                {
                    File.Copy(file, destFile, true);
                    result.Copied++;
                    LogMessage?.Invoke($"✅ {fileName}", LogMessageType.Success);                 
                    int progress = totalFiles > 0 ? (int)((double)result.Copied / totalFiles * 100) : 0;
                    ProgressChanged?.Invoke(progress);
                }
                catch (Exception ex)
                {
                    result.Errors++;
                    LogMessage?.Invoke($"❌ {fileName} - {ex.Message}", LogMessageType.Error);
                }

                await Task.Yield();
            }

            var dirs = Directory.GetDirectories(sourceDir);
            foreach (var dir in dirs)
            {
                string dirName = Path.GetFileName(dir);
                string newTargetDir = Path.Combine(targetDir, dirName);

                var subResult = await CopyDirectoryAsync(dir, newTargetDir, totalFiles, result.Copied);

                result.Copied = subResult.Copied;
                result.Skipped += subResult.Skipped;
                result.Errors += subResult.Errors;
            }

            return result;
        }

        public bool ShouldCopyFile(string sourceFile, string targetFile)
        {
            if (!File.Exists(targetFile)) return true;

            FileInfo sourceInfo = new FileInfo(sourceFile);
            FileInfo targetInfo = new FileInfo(targetFile);

            return sourceInfo.LastWriteTime > targetInfo.LastWriteTime || sourceInfo.Length != targetInfo.Length;
        }
    }
}