using BackupSyncApp.Models;
using BackupSyncApp.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Markup;

namespace BackupSyncApp.Common
{
    [MarkupExtensionReturnType(typeof(string))]
    [ContentProperty("Key")]
    internal class LocalizeExtension : MarkupExtension
    {
        private static ILocalizationService _localizationService;

        public string Key { get; set; }

        public LocalizeExtension() { }

        public LocalizeExtension(string key)
        {
            Key = key;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (string.IsNullOrEmpty(Key))
                return $"[{Key}]";

            // 'Ленивая' инициализация сервиса
            if (_localizationService == null)
            {
                InitializeLocalizationService();
            }

            return _localizationService?[Key] ?? $"[{Key}]";
        }

        private static void InitializeLocalizationService()
        {
            try
            {
                // Загружаем настройки
                var settings = AppSettings.Load();

                // Создаем сервис локализации
                _localizationService = new LocalizationService(settings);
            }
            catch (Exception ex)
            {
                // В случае ошибки создаем сервис с настройками по умолчанию
                _localizationService = new LocalizationService(new AppSettings());
                Console.WriteLine($"Error initializing localization: {ex.Message}");
            }
        }

        // Метод для принудительного обновления (при смене языка)
        public static void Refresh()
        {
            _localizationService = null; // Заставит переинициализироваться
        }
    }
}
