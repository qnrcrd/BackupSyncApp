using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace BackupSyncApp.Common
{
    // Реализация команды ICommand для привязки в WPF.

    public class RelayCommand: ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;


        // Событие, уведомляющее об изменении возможности выполнения команды.
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove {  CommandManager.RequerySuggested -= value;}
        }

        /// Создает новую команду        
        // <param name="execute">Действие, выполняемое командой.</param>
        // <param name="canExecute">Функция, определяющая возможность выполнения команды.</param>
        public RelayCommand(Action<object> execute, Func<object, bool> canExecute=null)
        {
            _execute=execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// Определяет, может ли команда выполняться.
        public bool CanExecute(object parameter)
        {
            return _canExecute==null|| _canExecute(parameter);
        }

        /// Выполняет команду.
        public void Execute(object parameter)
        {
            _execute(parameter); 
        }

    }
}
