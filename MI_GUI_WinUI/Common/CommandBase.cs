using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;

namespace MI_GUI_WinUI.Common
{
    public class CommandBase<T> : ICommand where T : class
    {
        private readonly Action<T?> _execute;
        private readonly Predicate<T?> _canExecute;

        public CommandBase(Action<T?> execute, Predicate<T?> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        // Replace CommandManager with direct event
        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
        {
            return _canExecute?.Invoke(parameter as T) ?? true;
        }

        public void Execute(object? parameter)
        {
            _execute.Invoke(parameter as T);
        }

        public void RaiseCanExecute()
        {
            // Invoke the event directly
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
