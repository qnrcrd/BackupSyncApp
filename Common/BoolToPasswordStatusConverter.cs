using BackupSyncApp.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace BackupSyncApp.Common
{
    public class BoolToPasswordStatusConverter : IValueConverter
    {
        private readonly ILocalizationService _localizationService;

        public BoolToPasswordStatusConverter()
        {
            var settings = Models.AppSettings.Load();
            _localizationService=new Services.LocalizationService(settings);
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool hasPassword)
            {
                return hasPassword ? "✓ " + _localizationService["Txt_PasswordSet"]
                    : "○ " + _localizationService["Txt_NoPassword"];
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
