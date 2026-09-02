using BackupSyncApp.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace BackupSyncApp.Common
{
    internal class DayOfWeekToStringConverter: IValueConverter
    {
        private readonly ILocalizationService _localizationService;

        public DayOfWeekToStringConverter()
        {
            var settings = Models.AppSettings.Load();
            _localizationService=new LocalizationService(settings);
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DayOfWeek dayOfWeek)
            {
                return dayOfWeek switch
                {
                    DayOfWeek.Monday => _localizationService["DayOfWeek_Monday"],
                    DayOfWeek.Tuesday => _localizationService["DayOfWeek_Tuesday"],
                    DayOfWeek.Wednesday => _localizationService["DayOfWeek_Wednesday"],
                    DayOfWeek.Thursday => _localizationService["DayOfWeek_Thursday"],
                    DayOfWeek.Friday => _localizationService["DayOfWeek_Friday"],
                    DayOfWeek.Saturday => _localizationService["DayOfWeek_Saturday"],
                    DayOfWeek.Sunday => _localizationService["DayOfWeek_Sunday"],
                    _ => dayOfWeek.ToString()
                };
            }
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object type, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
