using CAN_Tool.Infrastructure.Commands.Base;
using System;

namespace CAN_Tool.Infrastructure.Commands
{
    public class LambdaCommand : Command
    {
        private readonly Action<object> _Execute;
        private readonly Func<object, bool> _CanExecute;

        public LambdaCommand(Action<object> Execute, Func<object, bool> CanExecute = null)
        {
            _Execute = Execute ?? throw new ArgumentException("Execute method can't be null!");
            if (CanExecute != null) _CanExecute = CanExecute;
            else
                CanExecute = (x)=> true;
        }


        public override bool CanExecute(object parameter)
        {
            return _CanExecute?.Invoke(parameter) ?? true;
        }

        public override void Execute(object parameter)
        {
            _Execute(parameter);
        }
    }
}
