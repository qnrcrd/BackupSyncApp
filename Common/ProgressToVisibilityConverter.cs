using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using System.Windows;

namespace BackupSyncApp.Common
{
    public class ProgressToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int progress)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Converter: {progress} → {(progress > 0 ? "Visible" : "Collapsed")}");
                return progress > 0 ? Visibility.Visible : Visibility.Collapsed;
            }

            System.Diagnostics.Debug.WriteLine($"[DEBUG] Converter: Invalid value → Collapsed");
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
