using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CAN_Tool.ViewModels.Base
{
    internal abstract class ViewModel : INotifyPropertyChanged, IDisposable
    {

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string PropertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
        }

        protected virtual bool Set<T>(ref T field, T value, [CallerMemberName] string PropertyName = null)
        {
            if (field.Equals(value))
            {
                field = value;
                OnPropertyChanged(PropertyName);
                return true;
            }
            else
                return false;
        }

        private bool _Disposed;
<<<<<<< HEAD
        private bool disposedValue;

        public void Dispose()
=======
        protected virtual void Dispose()
>>>>>>> be7bdf243bf257e8e5fc9e93949893652c00208b
        { 
        Dispose(true);
        }
        protected virtual void Dispose(bool Disposing)
        {
            if (!Disposing || _Disposed) return;
            _Disposed = true;
            //Освобождение занятых ресурсов
            return;
        }

    }
}
