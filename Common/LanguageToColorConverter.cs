using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace BackupSyncApp.Common
{
    public class LanguageToColorConverter:IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string currentLang = value as string;
            string targetLang = parameter as string;

            if (currentLang == targetLang) return new SolidColorBrush(System.Windows.Media.Color.FromRgb(33, 150, 243));

            return new SolidColorBrush(System.Windows.Media.Color.FromRgb(96, 125, 139));
        }

        public object ConvertBack(object value, Type type, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
