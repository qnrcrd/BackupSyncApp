using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BackupSyncApp.ViewModels
{
    /// Базовый класс для всех ViewModel.
    /// Реализует INotifyPropertyChanged для привязки данных.

    public abstract class BaseViewModel: INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        /// Вызывает событие PropertyChanged.
        
        // <param name="propertyName">Имя изменившегося свойства.</param>
        
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName=null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        /// Устанавливает значение свойства и вызывает PropertyChanged, если значение изменилось.
        // <typeparam name="T">Тип свойства.</typeparam>
        // <param name="field">Поле свойства.</param>
        // <param name="value">Новое значение.</param>
        // <param name="propertyName">Имя свойства.</param>
        // вернет True, если значение изменилось.
        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if(EqualityComparer<T>.Default.Equals(field,value)) return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
