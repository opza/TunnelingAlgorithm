

using System;
using System.Windows.Input;

namespace WPFPrinter
{
    public class ActionCommand : ICommand
    {
        public event EventHandler? CanExecuteChanged;
        Action _action;

        public ActionCommand(Action action)
        {
            _action = action;
        }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => _action?.Invoke();
    }
}
