using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Synapse.Crypto.Patterns
{

    //    public class DelegateCommand : ICommand
    //    {
    //        private readonly Action<object> _execute;
    //        private readonly Func<object, bool> _canExecute;

    //        public event EventHandler CanExecuteChanged; // Событие для оповещения UI

    //        public DelegateCommand(Action<object> execute, Func<object, bool> canExecute = null)
    //        {
    //            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
    //            _canExecute = canExecute;
    //        }

    //        public bool CanExecute(object parameter)
    //        {
    //            return _canExecute == null || _canExecute(parameter);
    //        }

    //        public void Execute(object parameter)
    //        {
    //            _execute(parameter);
    //        }

    //        // Метод для вызова, чтобы обновить состояние CanExecute
    //        public void RaiseCanExecuteChanged()
    //        {
    //            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    //        }
    //    }
    //}


    public class DelegateCommand : ICommand
    {
        private readonly Predicate<object> _canExecute;

        private readonly Action<object> _execute;

        public event EventHandler CanExecuteChanged
        {
            add
            {
                if (_canExecute != null)
                {
                    CommandManager.RequerySuggested += value;
                }
            }
            remove
            {
                if (_canExecute != null)
                {
                    CommandManager.RequerySuggested -= value;
                }
            }
        }

        public DelegateCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            if (execute == null)
            {
                throw new ArgumentNullException("execute");
            }

            _execute = execute;
            _canExecute = canExecute;
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
        }

        public bool CanExecute(object parameter)
        {
            if (_canExecute != null)
            {
                return _canExecute(parameter);
            }

            return true;
        }
    }
}
