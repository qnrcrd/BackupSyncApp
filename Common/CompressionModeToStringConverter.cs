using System;
using System.Globalization;
using System.Windows.Data;
using BackupSyncApp.Models;
using BackupSyncApp.Services;

namespace BackupSyncApp.Common
{
    public class CompressionModeToStringConverter: IValueConverter
    {
        private ILocalizationService _localizationService;

        public CompressionModeToStringConverter()
        {
            var settings = AppSettings.Load();
            _localizationService= new LocalizationService(settings);
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (_localizationService == null)
            {
                var settings=AppSettings.Load();
                _localizationService=new LocalizationService(settings);
            }
            
            if (value is CompressMode mode)
            {
                return mode switch
                {
                    CompressMode.Fast => _localizationService["CompressionMode_Fast"],
                    CompressMode.Balanced=> _localizationService["CompressionMode_Balanced"],
                    CompressMode.Maximum=> _localizationService["CompressionMode_Maximum"],
                    _ => _localizationService["CompressionMode_Balanced"]
                };
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
