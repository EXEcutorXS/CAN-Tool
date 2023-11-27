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
        public string[] Props { get; }

        public AffectsToAttribute(params string[] propName)
        {
            Props = propName;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class RefreshCommands : Attribute
    {

    }

    public abstract class ViewModel : INotifyPropertyChanged, IDisposable
    {

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual bool Set<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            if (propertyName != null)
            {
                var attr = GetType().GetProperty(propertyName)?.GetCustomAttribute(typeof(AffectsToAttribute));
                if (attr != null)
                {
                    var at = (AffectsToAttribute)attr;
                    foreach (var p in at.Props)
                        OnPropertyChanged(p);
                }
            }

            OnPropertyChanged(propertyName);

            //if (propertyName != null && GetType().GetProperty(propertyName)?.GetCustomAttributes(typeof(RefreshCommands)) != null)
                CommandManager.InvalidateRequerySuggested();  //   Фикc необновления статуса кнопок

            return true;
        }

        private bool disposed;

        public void Dispose()
        {
            Dispose(true);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing || disposed) return;
            disposed = true;
        }

    }
}
