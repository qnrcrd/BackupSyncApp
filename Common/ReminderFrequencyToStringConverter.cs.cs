using BackupSyncApp.Models;
using BackupSyncApp.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace BackupSyncApp.Common
{
    public class ReminderFrequencyToStringConverter : IValueConverter
    {
        private ILocalizationService _localizationService;

        public ReminderFrequencyToStringConverter()
        {
            var settings = AppSettings.Load();
            _localizationService = new LocalizationService(settings);
        }
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (_localizationService == null)
            {
                var settings = AppSettings.Load();
                _localizationService = new LocalizationService(settings);
            }

            if (value is ReminderFrequency frequency)
            {
                return frequency switch
                {
                    ReminderFrequency.Daily => _localizationService["ReminderMode_Daily"],
                    ReminderFrequency.Weekly => _localizationService["ReminderMode_Weekly"],
                    ReminderFrequency.Monthly => _localizationService["ReminderMode_Monthly"],
                    ReminderFrequency.Yearly => _localizationService["ReminderMode_Yearly"],
                    _ => frequency.ToString()
                };
            }
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
