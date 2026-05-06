using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace BackupSyncApp.Views
{
    // КОНВЕРТЕР ДЛЯ ВЫДЕЛЕНИЯ АКТИВНОЙ КНОПКИ НАВИГАЦИИ
    
    class ModeToSelectedConverter:IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value !=null && parameter != null) return value.ToString()==parameter.ToString() ? "Selected" : null;
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
