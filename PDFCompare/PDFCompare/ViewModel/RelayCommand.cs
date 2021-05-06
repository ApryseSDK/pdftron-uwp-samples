using pdftron.SDF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace PDFViewerUWP_PDFTron.ViewModel
{
    class RelayCommand : ICommand
    {
        Action _execute;
        Predicate<object> _canExecute;

        public event EventHandler CanExecuteChanged;

        /// <summary>
        /// Overloaded contructors
        /// </summary>
        /// <param name="execute"></param>
        public RelayCommand(Action execute) : this(execute, null) { }

        public RelayCommand(Action execute, Predicate<Object> canExecute)
        {
            if (execute == null)
                throw new NullReferenceException("execute");

            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null ? true : _canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            _execute();
        }
    }
}
