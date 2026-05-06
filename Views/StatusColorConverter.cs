using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace BackupSyncApp.Views
{
    public class StatusColorConverter: IValueConverter
    {
        /// Конвертер для преобразования цвета статуса.

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value is SolidColorBrush brush) return brush;

            return System.Drawing.Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
