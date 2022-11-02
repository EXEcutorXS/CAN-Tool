using System;
using System.Windows.Input;

namespace CAN_Tool.Infrastructure.Commands.Base
{
    internal abstract class Command : ICommand
    {
        event EventHandler ICommand.CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;

            remove => CommandManager.RequerySuggested -= value;
        }

        public abstract bool CanExecute(object parameter);


        public abstract void Execute(object parameter);

    }
}
