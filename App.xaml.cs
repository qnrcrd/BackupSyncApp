using System;
using System.Configuration;
using System.Data;
using System.Windows;
using System.IO;
using System.Drawing;
using BackupSyncApp.Views;
using System.Windows.Forms;
using BackupSyncApp.Services;
using BackupSyncApp.Resources;

namespace BackupSyncApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private static string _logFilePath;

        private NotifyIcon _trayIcon;
        private MainWindow _mainWindow;
        private ILocalizationService _localizationService;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string logDirectory = Path.Combine(appDataPath, "BackupSyncApp", "Logs");
            Directory.CreateDirectory(logDirectory);
            _logFilePath = Path.Combine(logDirectory, $"app_{DateTime.Now:yyyyMMdd}.log");

            var settings = Models.AppSettings.Load();
            _localizationService = new LocalizationService(settings);

            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                LogToFile($"UNHANDLED EXCEPTION: {args.ExceptionObject}");
                System.Windows.MessageBox.Show(
                    $"A critical error occurred:\n{args.ExceptionObject}\n\n" +
                    "the application will close. Check the log file for details.",
                    "Fatal Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            };

            DispatcherUnhandledException += (sender, args) =>
            {
                LogToFile($"DISPATCHER EXCEPTION: {args.Exception}");
                args.Handled = true;
                System.Windows.MessageBox.Show(
                    $"An error occurred:\n{args.Exception.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            };

            TaskScheduler.UnobservedTaskException += (sender, args) =>
            {
                LogToFile($"TASK EXCEPTION: {args.Exception}");
                args.SetObserved();
            };

            InitializeTrayIcon();

            _mainWindow = new MainWindow();

            //var settings = Models.AppSettings.Load();
            if (settings.IsFirstRun)
            {
                var tutorial = new Views.TutorialWindow(_mainWindow);
                tutorial.ShowDialog();
            }

            
            _mainWindow.Show();
        }

        private void InitializeTrayIcon()
        {
            System.Drawing.Icon appIcon = null;

            try
            {
                // === Получаем иконку из ресурсов (byte[]) ===
                byte[] iconBytes = BackupSyncApp.Resources.Images.icon;

                if (iconBytes != null && iconBytes.Length > 0)
                {
                    // === Создаём MemoryStream из байтов ===
                    using (var ms = new MemoryStream(iconBytes))
                    {
                        appIcon = new System.Drawing.Icon(ms);
                    }
                }
            }
            catch (Exception ex)
            {
                App.LogToFile($"Error loading icon from resources: {ex.Message}");

                // === ВАРИАНТ 2: Из файла ===
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico");
                if (File.Exists(iconPath))
                {
                    appIcon = new System.Drawing.Icon(iconPath);
                }
                else
                {
                    // === ВАРИАНТ 3: Стандартная иконка ===
                    appIcon = System.Drawing.SystemIcons.Application;
                }
            }


            _trayIcon = new System.Windows.Forms.NotifyIcon
            {
                Icon = appIcon,
                Text = "BackupSync App",
                Visible = false
            };

            var contextMenu = new ContextMenuStrip();

            var openItem = contextMenu.Items.Add(_localizationService["TrayMenu_Open"]);
            openItem.Click += (s, args) => ShowMainWindow();

            contextMenu.Items.Add(new ToolStripSeparator());

            var exitItem = contextMenu.Items.Add(_localizationService["TrayMenu_Exit"]);
            exitItem.Click += (s, args) => ExitApplication();

            _trayIcon.ContextMenuStrip = contextMenu;

            _trayIcon.MouseClick += (s, args) =>
            {
                if (args.Button == MouseButtons.Left) ShowMainWindow();
            };
        }

        public void ShowMainWindow()
        {
            if (_mainWindow == null)
            {
                _mainWindow= new MainWindow();
                _mainWindow.Show();
            }
            else
            {
                _mainWindow.Show();
                _mainWindow.WindowState = WindowState.Normal;
                _mainWindow.Activate();
            }

            //_trayIcon.Visible=false;
        }

        public void ExitApplication()
        {
            _trayIcon?.Dispose();
            Shutdown();
        }

        public void ShowTrayNotification(string message, bool isError=false)
        {
            if (_trayIcon != null && _trayIcon.Visible)
            {
                string title = _localizationService["TrayNotification_Title"];
                string notificationText = isError ? 
                    _localizationService["TrayNotification_BackupError"] 
                    : _localizationService["TrayNotification_BackupComplete"];
                
                _trayIcon.ShowBalloonTip(3000, title, notificationText + "\n" +message, isError?ToolTipIcon.Error: ToolTipIcon.Info);
            }
        }

        public void SetTrayIconVisible(bool visible)
        {
            if(_trayIcon!= null) _trayIcon.Visible= visible;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _trayIcon?.Dispose();
            base.OnExit(e);
        }

        public static void LogToFile(string message)
        {
            try
            {
                string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}{Environment.NewLine}";
                File.AppendAllText(_logFilePath, logMessage);
            }
            catch
            {

            }
        }
    }

}
