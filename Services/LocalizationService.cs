using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using System.Resources;
using System.Windows;
using BackupSyncApp.Models;

namespace BackupSyncApp.Services
{
    /// Интерфейс сервиса локализации.
    public interface ILocalizationService
    {
        /// Получает локализованную строку по ключу.
        string this[string key] { get; }

        // current app language
        string CurrentLanguage { get; set; }

        // language change event
        event EventHandler LanguageChanged;

        // available languages
        IEnumerable<string> AvailableLanguages { get; }

        /// Получает локализованную строку с параметрами
        string  GetString(string key, params object[] args);
    }

    public class LocalizationService : ILocalizationService
    {
        private readonly ResourceManager _resourceManager;
        private readonly AppSettings _appSettings;
        private CultureInfo _currentCulture;

        public event EventHandler LanguageChanged;

        public LocalizationService(AppSettings appSettings)
        {
            _appSettings= appSettings?? throw new ArgumentNullException(nameof(appSettings));

            // Инициализация ResourceManager
            // ВАЖНО: Пространство имен должно совпадать с структурой
            // "BackupSyncApp.Resources.Localization.Strings" - это путь к ресурсам
            _resourceManager = new ResourceManager(
                "BackupSyncApp.Resources.Localization.Strings",
                typeof(LocalizationService).Assembly);

            // Загрузка сохраненного языка или использование языка системы
            string languagetoUse = _appSettings.GetLanguageOrDefault();

            if (string.IsNullOrEmpty(languagetoUse))
            {
                var systemCulture = CultureInfo.CurrentUICulture;
                _currentCulture = systemCulture.TwoLetterISOLanguageName == "ru" ? new CultureInfo("ru") : new CultureInfo("en");
            }
            else
            {
                _currentCulture=new CultureInfo(languagetoUse);
            }

            ApplyCulture(_currentCulture);
        }

        public string this[string key]=>GetString(key);

        public string CurrentLanguage
        {
            get => _currentCulture.Name;
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    // Если передана пустая строка, используем автоопределение
                    var systemCulture = CultureInfo.CurrentUICulture;
                    value = systemCulture.TwoLetterISOLanguageName == "ru" ? "ru" : "en";
                }

                if (!IsLanguageAvailable(value)) value = "en";

                if (_currentCulture.Name != value)
                {
                    _currentCulture = new CultureInfo(value);
                    ApplyCulture(_currentCulture);

                    _appSettings.Language= value;
                    _appSettings.Save();

                    LanguageChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public IEnumerable<string> AvailableLanguages => new[]
        {
            "en",
            "en-US",
            "ru",
            "ru-RU"
        };

        public string GetString(string key, params object[] args)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;

            try
            {
                var value = _resourceManager.GetString(key, _currentCulture);

                // Если для текущей культуры нет перевода, пробуем инвариантную культуру
                if (string.IsNullOrEmpty(value)) value = _resourceManager.GetString(key, CultureInfo.InvariantCulture);

                if (string.IsNullOrEmpty(value)) return $"[{key}]";

                return args.Length > 0 ? string.Format(value, args) : value;
            }
            catch
            {
                return $"[{key}]";
            }
        }

        private bool IsLanguageAvailable(string languageCode)
        {
            if (string.IsNullOrEmpty(languageCode)) return false;

            foreach (var availableLang in AvailableLanguages)
            {
                if(string.Equals(availableLang, languageCode, StringComparison.OrdinalIgnoreCase)) return true;

                // Проверяем базовую культуру
                try
                {
                    var culture = new CultureInfo(languageCode);
                    if (string.Equals(availableLang, culture.TwoLetterISOLanguageName, StringComparison.OrdinalIgnoreCase)) return true;

                }
                catch { }
            }

            return false;
        }

        private void ApplyCulture(CultureInfo culture)
        {
            CultureInfo.DefaultThreadCurrentCulture=culture;
            CultureInfo.DefaultThreadCurrentUICulture=culture;
        }
    }
}
