using CAN_Tool.Infrastructure.Commands.Base;
using System;

namespace CAN_Tool.Infrastructure.Commands
{
    public class LambdaCommand : Command
    {
        private readonly Action<object> execute;
        private readonly Func<object, bool> canExecute;

        public LambdaCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            this.execute = execute ?? throw new ArgumentException("Execute method can't be null!");
            if (canExecute != null) this.canExecute = canExecute;
        }


        public override bool CanExecute(object parameter)
        {
            return canExecute?.Invoke(parameter) ?? true;
        }

        public override void Execute(object parameter)
        {
            execute(parameter);
        }
    }
}
