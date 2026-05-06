using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using BackupSyncApp.Models;
using BackupSyncApp.Services;
using Microsoft.Win32;
using BackupSyncApp.Common;
using BackupSyncApp.ViewModels;

namespace BackupSyncApp.Views
{
    // Главное окно приложения.
    /// Содержит минимальную логику, вся основная логика в MainViewModel.
    public partial class MainWindow: Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();

            var settings = AppSettings.Load();
            var archiveService=new ArchiveService();
            var usbWatcher = new UsbDriveWatcher();

            var localizationService = new LocalizationService(settings);
            var dialogService = new DialogService(localizationService);
            var backupManager=new BackupManager(
                dialogService,
                archiveService,
                settings.EnableCompression,
                settings.CompressionMode
            );

            _viewModel =new MainViewModel(dialogService, backupManager, usbWatcher, settings);

            DataContext = _viewModel;

            //this.StateChanged += MainWindow_StateChanged;
            this.Closing += MainWindow_Closing;

            //Closed += MainWindow_Closed;
        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                this.Hide();
                if (System.Windows.Application.Current is App app) app.SetTrayIconVisible(true);
            }
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel= true;
            this.WindowState = WindowState.Minimized;
            this.Hide();

            if (System.Windows.Application.Current is App app) app.SetTrayIconVisible(true);
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                //TODO: ADD Dispose() method in ViewModel if needed
            }
        }

        public void ForceClose()
        {
            this.Closing -= MainWindow_Closing;
            this.Close();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                var passwordBox = sender as PasswordBox;
                viewModel.ArchivePasswordInput = passwordBox.Password;
            }
        }
    }
}