<div align="center">

[en English](README.md) | [r🇺 Русский](README.ru.md)

</div>

---

# BackupSyncApp

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Platform-Windows-blue)](https://www.microsoft.com/windows)

**BackupSyncApp** is a simple desktop application for automated file backup to USB drives. Designed with the *"set it and forget it"* philosophy for non-technical users.

---

## Key Features

- **Automated USB Backup:** Instant reaction to external drive connection via WMI monitoring.
- **Security:** ZIP archiving with password protection. Passwords and sensitive paths are encrypted using Windows DPAPI.
- **Backup Reminders:** Flexible scheduling (Daily, Weekly, Monthly, Yearly) so you never forget to back up.
- **System Tray:** Runs silently in the background with native Windows notifications.
- **Windows Auto-start:** Launches automatically with Windows (HKCU Registry, no admin rights required).
- **First-Run Tutorial:** Interactive guide for new users.
- **Localization:** Full support for English and Russian.

---

##  Installation

### Via Installer (Recommended)
1. Download the latest version from the [Releases](https://github.com/qnrcrd/BackupSyncApp/releases) section.
2. Run `BackupSyncApp_Setup_1.1.0.exe`.
3. Follow the on-screen instructions.
4. Launch EasyBackup from your Desktop or Start Menu.

### From Source Code
1. run following commands in the console
```bash
git clone https://github.com/qnrcrd/BackupSyncApp.git
cd BackupSyncApp
```
2. Open BackupSyncApp.sln in Visual Studio 2022+
3. Select Release configuration and click Build -> Rebuild Solution

---


##  How to Use

### Quick Start (3 Steps)
1. Add Folders — select the source directories for backup.
2. Configure external drive — specify the target drive and enable auto-backup.
3. Done! — plug in your USB or other external target drive, and the backup will start automatically.
4. 
### Manual Backup
Switch to "Manual Backup" mode, select the target disk, and click "Start Backup".

### Setting up Reminders
In the "Settings" section, enable reminders and choose the frequency. The app will remind you upon launch and before exiting if a backup hasn't been made on the scheduled day.


---

## Architecture & Tech Stack
The application is built using the MVVM (Model-View-ViewModel) pattern with WPF and XAML.

### Tech Stack
- **Language**: C# 12
- **Framework**: .NET 10 / WPF
- **Architecture**: MVVM
- **USB Monitoring**: WMI (Windows Management Instrumentation)
- **Encryption**: Windows DPAPI
- **Archiving**: System.IO.Compression (ZIP)
- **Installer**: Inno Setup

### Project Structure
```text
EasyBackup/
├── App.xaml                    # Entry point, system tray
├── Views/
│   ├── MainWindow.xaml         # Main UI
│   ── TutorialWindow.xaml     # First-run tutorial
── ViewModels/
│   └── MainViewModel.cs        # Business logic and commands
├── Models/
│   └── AppSettings.cs          # Settings serialization
├── Services/
│   ├── BackupManager.cs        # Staging algorithm
│   ├── UsbDriveWatcher.cs      # WMI monitoring
│   └── ArchiveService.cs       # ZIP + encryption
└── Resources/
    └── Localization/           # RU/EN strings
```

---

##  Security
Archive passwords and source folders paths are encrypted via DPAPI and in the settings file, and tied to the Windows local user account. Settings are stored in %localappdata%\EasyBackup\ and are inaccessible to other users.

---

##  System Requirements
- Windows 10/11 (x64)
- .NET 10 Runtime (installed automatically with the app)
- ~30 MB of free disk space
- external drive for backups

---

##  Licence
This project is licensed under the [MIT](https://opensource.org/licenses/MIT) License.
You are free to use, modify, and distribute the code, provided that author attribution is preserved.
