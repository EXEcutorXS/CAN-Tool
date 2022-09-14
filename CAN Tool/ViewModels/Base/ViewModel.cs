using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CAN_Tool.ViewModels.Base
{

    [AttributeUsage(AttributeTargets.Property)]
    public class AffectsToAttribute : Attribute
    {
        string[] props;

        public string[] Props => props;

        public AffectsToAttribute(params string[] propName)
        {
            props = propName;
        }
    }

    public class PropertyGroupAttribute : Attribute
    {
        string group;

        PropertyGroupAttribute(string groupName) => group = groupName;

    }
    public abstract class ViewModel : INotifyPropertyChanged, IDisposable
    {

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string PropertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
        }

        protected virtual bool Set<T>(ref T field, T value, [CallerMemberName] string PropertyName = null)
        {
            if (!Equals(field, value))
            {
                field = value;
                //1 Вариант
                typeof(T).GetCustomAttributes(typeof(AffectsToAttribute), true).ToList().ForEach(t => { foreach (var s in (t as AffectsToAttribute).Props) OnPropertyChanged(s); });
                //2 Вариант
                foreach (var property in GetType().GetProperties()) OnPropertyChanged(property.Name);
                OnPropertyChanged(PropertyName);
                CommandManager.InvalidateRequerySuggested();  //   Фикc необновления статуса кнопок

                return true;
            }
            else
                return false;
        }

        private bool _Disposed;

        public void Dispose()
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
