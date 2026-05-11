using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Reflection.Metadata;
using System.Globalization;
using System.IO.Compression;
using BackupSyncApp.Common;
namespace BackupSyncApp.Models
{
    public class AppSettings
    {
        public List<string> SourceFolders { get; set;  }=new List<string>();
        
        // AUTOBACKUP ONLY
        public string TargetDriveId { get; set; } = "";
        public string TargetFolderPath { get; set; } = "";
        public string TargetDrivePath { get; set; } = "";
        public DateTime? LastBackupTime { get; set; } = null;
        public string TargetDriveLabel { get; set; } = "";
        /// AUTO BACKUP IS ENABLED BY DEFAULT.

        // MANUAL BACKUP
        public string ManualBackupPath { get; set; } = "";
        
        // OTHER SETTINGS
        public bool EnableAutoBackup { get; set; } = true;
        
        
        /// RESERVED FOR LATER
        public bool CopyOnlyModified { get; set; }=true;
        public bool IsFirstRun { get; set; } = true;
        /// ==================
        
        public bool StartWithWindows { get; set; } = false;
        public string Language { get; set; } = "";

        public bool EnableCompression { get; set; } = false;
        public CompressMode CompressionMode { get; set; } = CompressMode.Balanced;// Fast, Balanced, Maximum

        public byte[] EncryptedArchivePassword { get; set;  } =Array.Empty<byte>();

        private static string GetSettingsPath()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appDirectory = Path.Combine(appDataPath, "BackupSyncApp");

            Directory.CreateDirectory(appDirectory);
            return Path.Combine(appDirectory, "settings.json");
        }
        
        public void Save(string filePath=null)
        {
            try
            {
                string path = filePath ?? GetSettingsPath();

                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });
                File.WriteAllText(path, json);
            }
            catch(Exception ex)
            { System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}"); }
        }

        public static AppSettings Load(string filePath= null)
        {
            try
            {
                string path = filePath ?? GetSettingsPath();
                
                if(File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch(Exception ex)
            { System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}"); }

            return new AppSettings();
        }

        public string GetLanguageOrDefault()
        {
            if(!string.IsNullOrWhiteSpace(Language)) return Language;

            var systemLang = CultureInfo.CurrentUICulture.Name;

            // Проверяем, поддерживается ли язык системы
            // Если система на неподдерживаемом языке, используем английский
            return systemLang.StartsWith("ru", StringComparison.OrdinalIgnoreCase) ? "ru" : "en";
        }


    }
}
