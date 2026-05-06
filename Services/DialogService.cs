using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32;
using System.Windows;

namespace BackupSyncApp.Services
{
    /// Интерфейс сервиса диалоговых окон.

    public interface IDialogService
    {
        string ShowOpenFolderDialog(string title);
        string ShowSaveFileDialog(string filter, string defaultFileName);
        void ShowMessageBox(string messageKey, string titleKey, MessageBoxImage icon, params object[] args);
        MessageBoxResult ShowYesNoMessageBox(string messageKey, string titleKey, params object[] args);
    }

    /// Реализация сервиса диалоговых окон.
    public class DialogService: IDialogService
    {
        private readonly ILocalizationService _localizationService;

        public DialogService(ILocalizationService localizationService)
        {
            _localizationService = localizationService;
        }

        public string ShowOpenFolderDialog(string titleKey=null)
        {
            string title=string.IsNullOrEmpty(titleKey)
                ? "Select folder"
                : _localizationService[titleKey];

            var dialog = new OpenFolderDialog
            {
                Title = title,
                Multiselect = false
            };

            return dialog.ShowDialog() == true ? dialog.FolderName : null;
        }

        public string ShowSaveFileDialog(string filter, string defaultFileName)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = filter,
                FileName = defaultFileName
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        public void ShowMessageBox(string messageKey, string titleKey, MessageBoxImage icon, params object[] args)
        {
            string message = args.Length > 0
                ? _localizationService.GetString(messageKey, args)
                : _localizationService[messageKey];
            string title=_localizationService[titleKey];

            System.Windows.MessageBox.Show(message, title, MessageBoxButton.OK, icon);
        }

        public MessageBoxResult ShowYesNoMessageBox(string messageKey, string titleKey, params object[] args)
        {
            string message = args.Length > 0
                ? _localizationService.GetString(messageKey, args)
                : _localizationService[messageKey];
            string title = _localizationService[titleKey];
            
            return System.Windows.MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
        }
    }
}
