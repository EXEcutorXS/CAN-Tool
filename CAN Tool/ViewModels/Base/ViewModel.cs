using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
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
                var attr = GetType().GetProperty(PropertyName)?.GetCustomAttribute(typeof(AffectsToAttribute));
                if (attr != null)
                {
                    AffectsToAttribute at = (AffectsToAttribute)attr;
                    foreach (var p in at.Props)
                        OnPropertyChanged(p);
                }

                //2 Вариант
                //foreach (var property in GetType().GetProperties()) OnPropertyChanged(property.Name);

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
