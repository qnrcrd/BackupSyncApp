using BackupSyncApp.Common;
using BackupSyncApp.Models;
using BackupSyncApp.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Security.AccessControl;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace BackupSyncApp.ViewModels
{
    /// ViewModel для главного окна приложения.
    /// Содержит всю логику пользовательского интерфейса.
    /// 
    // ВЕРСИЯ 1.0 ( на самом деле куда больше но я начал нумеровать только сейчас)

    // ============================================================================
    // ПАМЯТКА: ПОЛЯ ДЛЯ ХРАНЕНИЯ ПУТЕЙ ( ИБО Я ДОДИК )
    // ============================================================================
    //
    // РУЧНОЕ КОПИРОВАНИЕ:
    // -------------------
    // FolderPath (string)                    - Путь, выбранный пользователем для ручного бэкапа
    // _manualBackupPath (string)             - Сохранённый путь для ручного бэкапа (между сеансами)
    // TargetPathText (string, UI)            - Отображаемый текст "Выбрано: <путь>" для ручного режима
    //
    // АВТОМАТИЧЕСКОЕ КОПИРОВАНИЕ (USB):
    // ---------------------------------
    // _targetDriveId (string)                - Уникальный ID USB-диска (серийный номер)
    // _settings.TargetDrivePath (string)     - Путь к корню USB-диска (например, "D:\")
    // _settings.TargetFolderPath (string)    - Папка ВНУТРИ USB-диска для бэкапа (например, "D:\Backups\")
    // CurrentDriveText (string, UI)          - Отображаемый текст о текущем USB-диске
    //
    // ОБЩЕЕ:
    // ------
    // _settings.SourceFolders (List<string>) - Список папок-источников для копирования
    // ============================================================================


    public class MainViewModel : BaseViewModel
    {
        // Services ( dependencies implementation )
        private readonly IDialogService _dialogService;
        private readonly BackupManager _backupManager;
        private readonly UsbDriveWatcher _usbWatcher;
        private readonly AppSettings _settings;
        private ILocalizationService _localizationService;

        // Commands
        public ICommand AddFolderCommand { get; }
        public ICommand RemoveFolderCommand { get; }
        public ICommand SelectTargetCommand { get; }
        public ICommand StartBackupCommand { get; }
        public ICommand ConfigureAutoBackupCommand { get; }
        public ICommand ClearLogCommand { get; }
        public ICommand SaveLogCommand { get; }
        public ICommand ResetSettingsCommand { get; }
        public RelayCommand SwitchToEnglishCommand { get; }
        public RelayCommand SwitchToRussianCommand { get; }
        public ICommand SwitchToManualModeCommand { get; }
        public ICommand SwitchToAutoModeCommand { get; }
        public ICommand SwitchToSettingsModeCommand {  get; }
        public ICommand SwitchToLogModeCommand { get; }

        // bindings

        // source folders list
        private ObservableCollection<string> _sourceFolders = new ObservableCollection<string>();
        public ObservableCollection<string> SourceFolders
        {
            get => _sourceFolders;
            set => SetField(ref _sourceFolders, value);
        }

        private string _selectedMode = "Manual";
        public string SelectedMode
        {
            get => _selectedMode;
            set
            {
                _selectedMode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsManualModeVisible));
                OnPropertyChanged(nameof(IsAutoModeVisible));
                OnPropertyChanged(nameof(IsLogModeVisible));
                OnPropertyChanged(nameof(IsSettingsModeVisible));
            }
        }
        public Visibility IsManualModeVisible => SelectedMode == "Manual" ? Visibility.Visible : Visibility.Collapsed;
        public Visibility IsAutoModeVisible => SelectedMode == "Auto" ? Visibility.Visible : Visibility.Collapsed;
        public Visibility IsLogModeVisible => SelectedMode == "Log" ? Visibility.Visible : Visibility.Collapsed;
        public Visibility IsSettingsModeVisible => SelectedMode == "Settings" ? Visibility.Visible : Visibility.Collapsed;


        private string _selectedLanguage = "ru";
        public string SelectedLanguage
        {
            get => _selectedLanguage;
            set=> SetField(ref _selectedLanguage, value);
        }

        private string? _selectedFolder;
        public string? SelectedFolder
        {
            get => _selectedFolder;
            set=>SetField(ref _selectedFolder, value);
        }

        // chosen target text
        private string _targetPathText = "no disk chosen";
        public string TargetPathText
        {
            get => _targetPathText;
            set => SetField(ref _targetPathText, value);
        }

        private string _driveInfoText = "";
        public string DriveInfoText
        {
            get => _driveInfoText;
            set=>SetField(ref _driveInfoText, value);
        }

        // drive status
        private string _driveStatusText = "(inactive)";
        public string DriveStatusText
        {
            get => _driveStatusText;
            set => SetField(ref _driveStatusText, value);
        }

        // drive status color
        private System.Windows.Media.Brush _driveStatusColor = System.Windows.Media.Brushes.Gray;
        public System.Windows.Media.Brush DriveStatusColor
        {
            get => _driveStatusColor;
            set => SetField(ref _driveStatusColor, value);
        }
        
        private ObservableCollection<LogEntry> _logEntries = new ObservableCollection<LogEntry>();
        public ObservableCollection<LogEntry> LogEntries
        {
            get => _logEntries;
            set => SetField(ref _logEntries, value);
        }

        // auto backup enabled?
        private bool _isAutoBackupEnabled;
        public bool IsAutoBackupEnabled
        {
            get => _isAutoBackupEnabled;
            set
            {
                if (SetField(ref _isAutoBackupEnabled, value)) OnAutoBackupEnabledChanged();
            }
        }

        // FULL PATH FOR AUTOBACKUP
        private string _autoBackupFullPath = "";
        public string AutoBackupFullPath
        {
            get => _autoBackupFullPath;
            set=> SetField(ref _autoBackupFullPath, value);
        }

        // backup progress
        private int _progressValue;
        public int ProgressValue
        {
            get => _progressValue;
            set => SetField(ref _progressValue, value);
        }

        private string _currentVisibleDrive = "none";
        public string CurrentVisibleDrive
        {
            get => _currentVisibleDrive;
            set => SetField(ref _currentVisibleDrive, value);
        }

        private string _rememberedDriveText = "not configured";
        public string RememberedDriveText
        {
            get => _rememberedDriveText;
            set => SetField(ref _rememberedDriveText, value);
        }

        // compression enabled
        private bool _enableCompression;
        public bool EnableCompression
        {
            get => _enableCompression;
            set
            {
                if (SetField(ref _enableCompression, value))
                {
                    _settings.EnableCompression= value;
                    SaveSettings();

                    _backupManager.UpdateCompressionSettings(value, _selectedCompressMode);
                }
            }
        }

        public CompressMode[] CompressModes { get; } =
        {
            CompressMode.Fast,
            CompressMode.Balanced,
            CompressMode.Maximum
        };

        private CompressMode _selectedCompressMode = CompressMode.Balanced;
        public CompressMode SelectedCompressMode
        {
            get => _selectedCompressMode;
            set
            {
                if (SetField(ref _selectedCompressMode, value))
                {
                    _settings.CompressionMode = value;
                    SaveSettings();

                    _backupManager.UpdateCompressionSettings(_settings.EnableCompression, value);

                    AddLog($"DEBUG: compression mode changed to {value}",LogMessageType.Info);
                }
            }
        }

        // operation status
        private string _statusText = "ready";
        public string StatusText
        {
            get => _statusText;
            set => SetField(ref _statusText, value);
        }

        // CURRENTLY FOR AUTOBACKUP ONLY 
        private string _lastBackupTime = "-";
        public string LastBackupTime
        {
            get => _lastBackupTime;
            set => SetField(ref _lastBackupTime, value);
        }

        private readonly IDpapiService _dpapiService;
        private string _archivePasswordInput = "";

        public bool HasArchivePassword => _settings.EncryptedArchivePassword?.Length > 0;

        public string ArchivePasswordInput
        {
            get => _archivePasswordInput;
            set
            {
                if (SetField(ref _archivePasswordInput, value))
                {
                    if (!string.IsNullOrEmpty(value))
                    {
                        byte[] encrypted=_dpapiService.Encrypt(value);
                        _settings.EncryptedArchivePassword= encrypted;
                    }
                    else
                    {
                        _settings.EncryptedArchivePassword=Array.Empty<byte>();
                    }

                    _settings.Save();

                    _backupManager.UpdateArchivePassword(value);

                    OnPropertyChanged(nameof(HasArchivePassword));

                    AddLog(!string.IsNullOrEmpty(value)
                        ? "🔒 Archive password updated"
                        : "🔓 Archive password removed", LogMessageType.Info);
                }
            }
        }

        // PRIVATE FIELDS
        private bool _isArchiving = false;
        private string _manualBackupPath = "";
        private string _targetDriveId = "";
        private bool _isBackupInProgress = false;
        string FolderPath;


        // MainViewModel Constructor
        public MainViewModel(
            IDialogService dialogService,
            BackupManager backupManager,
            UsbDriveWatcher usbWatcher,
            AppSettings settings)
        {
            // saving services
            _dialogService = dialogService;
            _backupManager = backupManager;
            _dpapiService = new DpapiService();
            _usbWatcher = usbWatcher;
            _settings = settings;
            _localizationService = new LocalizationService(_settings);

            _localizationService.LanguageChanged += (s, e) => UpdateLocalizedProperties();

            // initializing commands
            AddFolderCommand = new RelayCommand(ExecuteAddFolderCommand);
            RemoveFolderCommand = new RelayCommand(ExecuteRemoveFolderCommand);
            SelectTargetCommand = new RelayCommand(ExecuteSelectTargetCommand);
            StartBackupCommand = new RelayCommand(ExecuteStartBackupCommand, CanExecuteStartBackupCommand);
            ConfigureAutoBackupCommand = new RelayCommand(ExecuteConfigureAutoBackupCommand);
            ClearLogCommand = new RelayCommand(ExecuteClearLogCommand);
            SaveLogCommand = new RelayCommand(ExecuteSaveLogCommand);
            ResetSettingsCommand = new RelayCommand(ExecuteResetSettingsCommand);
            SwitchToEnglishCommand = new RelayCommand((param) => SwitchLanguage("en"));
            SwitchToRussianCommand = new RelayCommand((param) => SwitchLanguage("ru"));
            SwitchToManualModeCommand = new RelayCommand(_ => SelectedMode = "Manual");
            SwitchToAutoModeCommand = new RelayCommand(_ => SelectedMode = "Auto");
            SwitchToSettingsModeCommand = new RelayCommand(_ => SelectedMode = "Settings");
            SwitchToLogModeCommand = new RelayCommand(_ => SelectedMode = "Log");


            InitializeEventHandlers();
            StartUsbMonitoring();
            LoadSettings();
            LoadArchivePassword();
        }

        private void InitializeEventHandlers()
        {
            _backupManager.LogMessage += OnBackupManagerLogMessage;
            _backupManager.ProgressChanged += OnBackupManagerProgressChanged;
            _backupManager.StatusChanged += OnBackupManagerStatusChanged;
            _backupManager.StatusChanged += OnBackupManagerStatusChanged;
            _usbWatcher.DriveConnected += OnUsbDriveConnected;
            _usbWatcher.DriveDisconnected += OnUsbDriveDisconnected;
        }

        private void LoadArchivePassword()
        {
            if (_settings.EncryptedArchivePassword?.Length > 0)
            {
                string password = _dpapiService.Decrypt(_settings.EncryptedArchivePassword);
                if (!string.IsNullOrEmpty(password))
                {
                    _backupManager.UpdateArchivePassword(password);
                    AddLog("🔒 Archive password loaded from secure storage", LogMessageType.Info);
                }
            }
        }

        private void LoadSettings()
        {
            try
            {
                TargetPathText = _localizationService["Txt_DiskNotChosen"];
                CurrentVisibleDrive = _localizationService["Txt_CurrentVisibleDriveNone"];
                DriveStatusText = _localizationService["Txt_Status_Inactive"];
                DriveStatusColor = System.Windows.Media.Brushes.Gray;
                StatusText = _localizationService["Txt_Ready"];
                _enableCompression = _settings.EnableCompression;
                _selectedCompressMode = _settings.CompressionMode;
                _selectedLanguage = _settings.GetLanguageOrDefault();
                if (_settings.LastBackupTime.HasValue) LastBackupTime = _settings.LastBackupTime.ToString();

                foreach (var folder in _settings.SourceFolders)
                {
                    if (Directory.Exists(folder)) SourceFolders.Add(folder);
                }

                _targetDriveId = _settings.TargetDriveId;
                _manualBackupPath = _settings.ManualBackupPath;

                if (!string.IsNullOrEmpty(_settings.TargetFolderPath))
                {
                    string driveLabel = UsbDriveWatcher.GetDriveLabel(_settings.TargetDrivePath);
                    RememberedDriveText = $"{_settings.TargetDrivePath} ({driveLabel})";
                    AutoBackupFullPath = !string.IsNullOrEmpty(_settings.TargetFolderPath) ? _settings.TargetFolderPath : _settings.TargetDrivePath;
                }
                else
                {
                    RememberedDriveText = _localizationService["Txt_RememberedDriveNone"];
                    AutoBackupFullPath = " ";
                }
                

                if (!string.IsNullOrEmpty(_manualBackupPath) && SelectedMode == "Manual")
                {
                    TargetPathText = L("Txt_TargetPathChosen", _manualBackupPath);
                    FolderPath = _manualBackupPath;
                }

                if (_settings.EnableAutoBackup) IsAutoBackupEnabled = true;

                if (_settings.LastBackupTime.HasValue) LastBackupTime = _settings.LastBackupTime.Value.ToString("dd.MM.yyyy HH:mm");
                
                ApplyLanguageFromSettings();

                AddLog("Settings loaded", LogMessageType.Info);
            }
            catch (Exception ex)
            {
                AddLog($"Error loading settings: {ex.Message}", LogMessageType.Error);
            }
        }     

        private void SaveSettings()
        {
            try
            {
                _settings.SourceFolders = new System.Collections.Generic.List<string>(SourceFolders);
                _settings.TargetDriveId = _targetDriveId;
                _settings.EnableAutoBackup = IsAutoBackupEnabled;
                _settings.ManualBackupPath = _manualBackupPath;
                DateTime t;
                _settings.LastBackupTime = DateTime.TryParse(LastBackupTime, out t)?  t: null;
                _settings.Save();

                AddLog("Settings saved", LogMessageType.Info);
            }
            catch (System.Exception ex)
            {
                AddLog($"Error loading settings: {ex.Message}", LogMessageType.Error);
            }
        }

        private void AddLog(string message, LogMessageType type)
        {
            try
            {
                var logEntry = new LogEntry(message, type);
                if (System.Windows.Application.Current?.Dispatcher != null)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        LogEntries.Add(logEntry);

                        if (LogEntries.Count > 1000) LogEntries.RemoveAt(0);
                    });
                }
                else LogEntries.Add(logEntry);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Error adding log: {ex.Message}"); }
        }

        /// Обработчик изменения состояния автокопирования.
        private void OnAutoBackupEnabledChanged()
        {
            SaveSettings();

            AddLog(IsAutoBackupEnabled ? "✅ Auto-backup enabled (will start when target drive connected)" :
                "⏸️ Auto-backup disabled (drive detection still active)", LogMessageType.Info);
        }

        /// Запуск мониторинга USB-устройств.
        private void StartUsbMonitoring()
        {
            try
            {
                _usbWatcher.StartWatching();
                DriveStatusText = L("Txt_Status_Waiting");
                DriveStatusColor = System.Windows.Media.Brushes.Orange;
                AddLog("USB monitoring enabled", LogMessageType.Progress);
            }
            catch (System.Exception ex)
            {
                AddLog($"Error starting USB monitoring: {ex.Message}", LogMessageType.Error);
            }
        }

        /// Остановка мониторинга USB-устройств.
        private void StopUsbMonitoring()
        {
            _usbWatcher.StopWatching();
            DriveStatusText = L("Txt_Status_Inactive");
            DriveStatusColor = System.Windows.Media.Brushes.Gray;
            AddLog("USB monitoring disabled", LogMessageType.Info);
        }


        private void UpdateUiProperty(Action updateAction)
        {
            if (System.Windows.Application.Current?.Dispatcher != null) System.Windows.Application.Current.Dispatcher.Invoke(updateAction);
            else updateAction();
        }

        #region EVENT HANDLERS
        // --- EVENT HANDLERS ---

        private void OnBackupManagerLogMessage(string message, LogMessageType type)
        {
            UpdateUiProperty(() => { AddLog(message, type); });
        }



        // === Обновляем обработчик прогресса ===
        private void OnBackupManagerProgressChanged(int progress)
        {
            UpdateUiProperty(() =>
            {
                ProgressValue = progress;

                // === ВАЖНО: Не меняем статус во время архивации ===
                if (!_isArchiving && progress < 100)
                {
                    StatusText = L("Txt_Copying", progress);
                }
                else if (!_isArchiving && progress == 100)
                {
                    StatusText = L("Txt_Ready");
                }
            });
        }

        private void OnBackupManagerStatusChanged(string status)
        {
            UpdateUiProperty(() =>
            {
                if (status == "Archiving")
                {
                    _isArchiving = true;
                    StatusText = L("Txt_Archiving");
                    ProgressValue = 95;  // Фиксируем на 95%
                }
                else if (status == "Ready")
                {
                    _isArchiving = false;
                    StatusText = L("Txt_Ready");
                    ProgressValue = 100;
                }
            });
        }


        private void OnUsbDriveConnected(string drivePath)
        {
            UpdateUiProperty(() =>
            {
                AddLog($"Drive connected: {drivePath}", LogMessageType.Progress);

                string driveLabel = UsbDriveWatcher.GetDriveLabel(drivePath);
                CurrentVisibleDrive = $"{drivePath} ({driveLabel})";

                string driveId = UsbDriveWatcher.GetDriveUniqueID(drivePath);

                if (IsAutoBackupEnabled && !string.IsNullOrEmpty(_targetDriveId) && driveId == _targetDriveId)
                {
                    AddLog($"Target disk detected. Starting backup...", LogMessageType.Success);
                    DriveStatusText = L("Txt_Status_DriveCopying");
                    DriveStatusColor = System.Windows.Media.Brushes.Green;

                    string targetFolder = !string.IsNullOrEmpty(_settings.TargetFolderPath) ? _settings.TargetFolderPath : drivePath;

                    if (!Directory.Exists(targetFolder))
                    {
                        try
                        {
                            Directory.CreateDirectory(targetFolder);
                            AddLog($"Created backup folder: {targetFolder}", LogMessageType.Info);
                        }
                        catch
                        {
                            targetFolder= drivePath;
                            AddLog($"using root folder instead: {targetFolder}", LogMessageType.Warning);
                        }
                    }

                    Task.Run(() => StartAutoBackup(targetFolder));
                }
                else if (!IsAutoBackupEnabled && driveId == _targetDriveId)
                {
                    DriveStatusText = L("Txt_Status_ReadyWaiting");
                    DriveStatusColor = System.Windows.Media.Brushes.Orange;
                }                                
            });
        }

        private void OnUsbDriveDisconnected(string drivePath)
        {
            UpdateUiProperty(() =>
            {
                AddLog($"Drive disconnected: {drivePath}", LogMessageType.Info);

                CurrentVisibleDrive = L("Txt_CurrentVisibleDriveNone");
                if (drivePath + "\\\\" == _settings.TargetDrivePath)
                {
                    DriveStatusText = L("Txt_Status_Inactive");
                    DriveStatusColor = System.Windows.Media.Brushes.Gray;
                }
            });
        }
        #endregion

        #region COMMANDS
        // --- COMMANDS ---

        private void ExecuteAddFolderCommand(object parameter)
        {
            string folderPath = _dialogService.ShowOpenFolderDialog("Dialog_ChooseSourceFolder");
            if (!string.IsNullOrEmpty(folderPath))
            {
                if(!SourceFolders.Contains(folderPath))
                {
                    SourceFolders.Add(folderPath);
                    AddLog($"Folder added: {folderPath}", LogMessageType.Info);
                    SaveSettings();
                }
                else
                {
                    _dialogService.ShowMessageBox("Msg_AlreadyAdded", "Msg_WarningTitle", MessageBoxImage.Warning);
                }
            }
        }

        private void ExecuteRemoveFolderCommand(object parameter)
        {
            string folderToRemove = null;

            if (!string.IsNullOrEmpty(SelectedFolder) && SourceFolders.Contains(SelectedFolder)) folderToRemove = SelectedFolder;
            else if (SourceFolders.Count > 0) folderToRemove = SourceFolders[SourceFolders.Count - 1];

            if (string.IsNullOrEmpty(folderToRemove))
            {
                _dialogService.ShowMessageBox("Msg_NoFolders", "Msg_InfromationTitle", MessageBoxImage.Information);
                return;
            }

            var result = _dialogService.ShowYesNoMessageBox("Msg_ConfirmRemove", "Msg_ConfirmRemoveTitle", folderToRemove);

            if (result != MessageBoxResult.Yes) return;

            if (folderToRemove == SelectedFolder)
            {
                SourceFolders.Remove(SelectedFolder);
                SelectedFolder = null;
            }
            else
            {
                SourceFolders.Remove(folderToRemove);
            }

            AddLog($"Folder removed: {folderToRemove}", LogMessageType.Info);
            SaveSettings();
        }


        
        private void ExecuteSelectTargetCommand(object parameter)
        {
            string folderPath = _dialogService.ShowOpenFolderDialog("Dialog_ChooseDisk");
            FolderPath = folderPath;
            if(!string.IsNullOrEmpty(folderPath))
            {
                TargetPathText = L("Txt_TargetPathChosen", folderPath);
                _manualBackupPath = folderPath;
                _settings.ManualBackupPath = folderPath;
                _settings.Save();
                AddLog($"target disk selected: {folderPath}", LogMessageType.Info);
            }
        }

        private bool CanExecuteStartBackupCommand(object parameter)
        {
            return SourceFolders.Count > 0 &&
                !TargetPathText.Contains("no disk chosen") &&
                !TargetPathText.Contains("диск не выбран") &&
                !_isBackupInProgress;
        }

        private async void ExecuteStartBackupCommand(object parameter)
        {
            try
            {
                _isBackupInProgress = true;

                string targetPath = FolderPath;

                AddLog("=== STARTING BACKUP ===", LogMessageType.Progress);
                AddLog($"Sources: {SourceFolders.Count} folders", LogMessageType.Info);
                AddLog($"Target: {targetPath}", LogMessageType.Info);

                // starting backup
                List<string> foldersList = new List<string>(SourceFolders);
                await _backupManager.CopyFolderAsync(foldersList, targetPath);

                AddLog("=== BACKUP FINISHED ===", LogMessageType.Success);

                StatusText = L("Txt_Ready");
                ProgressValue = 100;

                if (System.Windows.Application.Current is App app)
                {
                    string copie = _localizationService["TrayNotification_FilesCopied"] + $"{foldersList.Count}";
                    app.ShowTrayNotification(copie, isError: false);
                }


                await Task.Delay(5000);
            }
            catch (Exception ex)
            {
                AddLog($"ERROR: {ex.Message}", LogMessageType.Error);

                if (System.Windows.Application.Current is App app)
                {
                    app.ShowTrayNotification(ex.Message, isError:true);
                }

                _dialogService.ShowMessageBox("Msg_Error", "Msg_ErrorTitle", MessageBoxImage.Error, ex.Message);
            }
            finally
            {
                _isBackupInProgress = false;
                ProgressValue = 0;
                StatusText = L("Txt_Ready");
            }
        }

        private void ExecuteConfigureAutoBackupCommand(object parameter)
        {
            string folderPath = _dialogService.ShowOpenFolderDialog("Dialog_ChooseUSB");
            if (!string.IsNullOrEmpty(folderPath))
            {
                var driveInfo = new DriveInfo(folderPath);

                if (driveInfo.DriveType == DriveType.Removable)
                {
                    if (!driveInfo.IsReady)
                    {
                        _dialogService.ShowMessageBox("Msg_DriveNotReady", "Msg_ErrorTitle", MessageBoxImage.Error);
                        return;
                    }

                    string rootPath = driveInfo.RootDirectory.FullName;
                    
                    _targetDriveId = UsbDriveWatcher.GetDriveUniqueID(rootPath);
                    _settings.TargetDriveId = _targetDriveId;

                    _settings.TargetFolderPath = folderPath;
                    _settings.TargetDrivePath = rootPath;
                    
                    SaveSettings();

                    string driveLabel = UsbDriveWatcher.GetDriveLabel(folderPath);
                    
                    RememberedDriveText= $"{rootPath} ({driveLabel})";
                    AutoBackupFullPath = folderPath;

                    //string driveDetails = GetDriveDetails(driveInfo);
                    //DriveInfoText = driveDetails;

                    AddLog($"target disk configured: {folderPath} ({driveLabel})", LogMessageType.Success);

                    _dialogService.ShowMessageBox("Msg_DiskConfigured", "Msg_DiskConfiguredTitle", MessageBoxImage.Information,
                        folderPath, driveLabel, driveInfo.DriveType);
                }
                else
                {
                    
                    _dialogService.ShowMessageBox("Msg_InvalidDiskType", "Msg_InvalidDiskTypeTitle", MessageBoxImage.Warning, driveInfo.DriveType);
                }
            }          
        }

        private string GetDriveDetails(DriveInfo drive)
        {
            if (!drive.IsReady) return "Disk not ready";

            string details = "";

            if (!string.IsNullOrEmpty(drive.VolumeLabel)) details += $"Label: {drive.VolumeLabel}\n";

            details += $"Type: {drive.DriveType}\n";
            details += $"Format: {drive.DriveFormat}";

            if (drive.TotalSize > 0)
            {
                double totalGB = drive.TotalSize / Math.Pow(1024.0, 3);
                double freeGB = drive.AvailableFreeSpace / Math.Pow(1024.0, 3);
                details += $"Size: {totalGB:F1} GB (Free: {freeGB:F1} GB)";
            }

            return details;
        }


        private void ExecuteClearLogCommand(object parameter)
        {
            LogEntries.Clear();
            AddLog("Log cleared", LogMessageType.Info);
        }

        private void ExecuteSaveLogCommand(object parameter)
        {
            string filePath = _dialogService.ShowSaveFileDialog(
                "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                $"backup_log_{System.DateTime.Now:yyyyMMdd_HHmmss}.txt");

            if (!string.IsNullOrEmpty(filePath))
            {
                try
                {
                    using (var writer = new System.IO.StreamWriter(filePath))
                    {
                        foreach (var entry in LogEntries) writer.WriteLine(entry.ToString());
                    }

                    AddLog($"Log saved: {filePath}", LogMessageType.Success);
                }
                catch (Exception ex)
                {
                    AddLog($"Error saving log: {ex.Message}", LogMessageType.Error);
                    _dialogService.ShowMessageBox("Msg_Error", "Msg_ErrorTitle", MessageBoxImage.Error, ex.Message);
                }
            }
        }

        private void ExecuteResetSettingsCommand(object parameter)
        {
            
            var result =_dialogService.ShowYesNoMessageBox("Msg_ResetSettings", "Msg_ResetSettingsTitle");

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    string settingsFile = "settings.json";
                    if (System.IO.File.Exists(settingsFile))
                    {
                        System.IO.File.Delete(settingsFile);
                        AddLog("settings file deleted", LogMessageType.Info);
                    }

                    SourceFolders.Clear();
                   
                    _targetDriveId = "";
                    _manualBackupPath = "";
                    AutoBackupFullPath = "";
                    IsAutoBackupEnabled = false;

                    _enableCompression= false;
                    _selectedCompressMode = CompressMode.Balanced;

                    // reset UI
                    TargetPathText = L("Txt_DiskNotChosen");
                    CurrentVisibleDrive = L("Txt_CurrentVisibleDriveNone");
                    RememberedDriveText = L("Txt_RememberedDriveNone");
                    AutoBackupFullPath = " ";
                    DriveStatusText = L("Txt_Status_Inactive");
                    DriveStatusColor = System.Windows.Media.Brushes.Gray;

                    // new settings
                    _settings.SourceFolders.Clear();
                    _settings.TargetDriveId = "";
                    //_settings.TargetDriveId = "";
                    _settings.TargetFolderPath = "";
                    _settings.TargetDrivePath = "";
                    _settings.EnableAutoBackup = false;
                    _settings.EnableCompression = false;

                    AddLog("All settings reset to default", LogMessageType.Success);     
                    _dialogService.ShowMessageBox("Msg_SettingsReseted", "Msg_ResetSettingsTitle", MessageBoxImage.Information);

                    SaveSettings();
                }
                catch (Exception ex)
                {
                    AddLog($"error resetting settings: {ex.Message}", LogMessageType.Error);                    
                    _dialogService.ShowMessageBox("Msg_Error", "Msg_ErrorTitle", MessageBoxImage.Error, ex.Message);
                }
            }
        }

        private async Task StartAutoBackup(string drivePath)
        {
            try
            {
                if (SourceFolders.Count == 0)
                {
                    UpdateUiProperty(() =>
                    {
                        AddLog("No folders to copy. Please add folders first.", LogMessageType.Warning);
                    });
                    return;
                }

                // creating backup folder
                //string backupFolder = Path.Combine(drivePath, $"Backup_{DateTime.Now:yyyyMMdd_HHmmss}");

                //AddLog($"creating backup in: {backupFolder}", LogMessageType.Progress);
                List<string> foldersList = new List<string>(SourceFolders);

                await _backupManager.CopyFolderAsync(foldersList, drivePath);

                UpdateUiProperty(() =>
                {
                    AddLog($"auto backup completed!", LogMessageType.Success);
                    LastBackupTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
                    SaveSettings();
                    DriveStatusText = L("Txt_Status_Ready");
                    DriveStatusColor = System.Windows.Media.Brushes.Green;
                    if (System.Windows.Application.Current is App app)
                    {
                        app.ShowTrayNotification(_localizationService["TrayNotification_AutoBackupComplete"], isError: false);
                    }
                });                

               

                
                
                await Task.Delay(5000);
            }
            catch (Exception ex)
            {
                UpdateUiProperty(() =>
                {
                    AddLog($"Auto backup error: {ex.Message}", LogMessageType.Error);
                    DriveStatusText = L("Txt_Status_Inactive");
                    DriveStatusColor = System.Windows.Media.Brushes.Red;
                    if (System.Windows.Application.Current is App app)
                    {
                        app.ShowTrayNotification(ex.Message, isError: true);
                    }
                });                            
            }
            finally
            {
                UpdateUiProperty(() =>
                {
                    ProgressValue = 0;
                    StatusText = L("Txt_Ready");
                });                
                //SaveSettings();
            }
        }
        #endregion

        #region LANGUAGE STUFF


        private void SwitchLanguage(object parameter)
        {
            string languageCode = parameter as string;

            if (string.IsNullOrEmpty(languageCode)) return;

            try
            {
                string currentLang = _localizationService.CurrentLanguage;
                string newLang = languageCode;

                if (currentLang == newLang) return;

                _localizationService.CurrentLanguage = newLang;
                _settings.Language = newLang;
                SaveSettings();

                _dialogService.ShowMessageBox("Msg_LanguageChanged", "Msg_LanguageChangedTitle", MessageBoxImage.Information);

                string langName = newLang == "ru" ? "Russian" : "English";
                AddLog($"Language changed to: {langName} (requires restart)", LogMessageType.Info);
            }
            catch (Exception ex)
            {
                AddLog($"Error switching language: {ex.Message}", LogMessageType.Success);
            }
        }

        private void UpdateLocalizedProperties()
        {

        }

        private void ApplyLanguageFromSettings()
        {

            try
            {
                UpdateLocalizedProperties();

                string langName = _localizationService.CurrentLanguage == "ru" ? "Russian" : "English";
                AddLog($"Application language: {langName}", LogMessageType.Info);
            }
            catch (Exception ex)
            {
                AddLog($"Error applying language: {ex.Message}", LogMessageType.Error);
            }
        }

        private string L(string key, params object[] args)
        {
            return _localizationService.GetString(key, args);
        }






        #endregion

    }
}
