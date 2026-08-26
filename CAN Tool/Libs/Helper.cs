using CAN_Tool;
using OmniProtocol;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;

namespace CAN_Tool.Libs
{
    public interface IUpdatable<in T>
    {
        public void Update(T item);
        public bool IsSimiliarTo(T item);
    }

    public class Trackable<T> : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private T value;
        public T Value
        {
            get => value;
            set { this.value = value; OnPropertyChanged("Value"); }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString() => Value.ToString();
    }

    public class UpdatableList<T> : BindingList<T> where T : IUpdatable<T>, IComparable
    {
        private readonly object _syncRoot = new object();
        private readonly SynchronizationContext _uiContext;

        public UpdatableList()
        {
            // Захватываем UI контекст (должен вызываться из UI потока)
            _uiContext = SynchronizationContext.Current ?? new SynchronizationContext();
        }

        public bool TryToAdd(T item)
        {
            lock (_syncRoot)
            {
                // Send, не Post: должна выполниться синхронно, пока держим _syncRoot, иначе
                // проверка "уже есть?" и реальная мутация коллекции не атомарны - при быстром
                // потоке вызовов (сообщения теперь диспетчеризуются в UI-поток через Post, не
                // Send) несколько TryToAdd успевают пройти проверку "не найдено" до того, как
                // первый Add/Insert реально попадёт в коллекцию, и получаем дубликаты.
                var found = Items.FirstOrDefault(i => i.IsSimiliarTo(item));
                if (found == null)
                {
                    if (Count > 0)
                    {
                        for (var i = 0; i < Count; i++)
                        {
                            if (item.CompareTo(Items[i]) <= 0)
                            {
                                _uiContext.Send(_ => Insert(i, item), null);
                                return true;
                            }
                        }
                        _uiContext.Send(_ => Add(item), null);
                        return true;
                    }
                    else
                    {
                        _uiContext.Send(_ => Add(item), null);
                    }
                }
                else
                {
                    _uiContext.Send(_ => found.Update(item), null);
                }
                return false;
            }
        }
    }

    public static class Helper
    {
        public static string GetString(string key)
        {
            if (string.IsNullOrEmpty(key) || Application.Current == null) return "";
            var ret = (string)Application.Current.TryFindResource(key);
            if (ret != null)
                return ret;
            else
                return key.Replace('_', ' ');
        }

        public static bool GotResource(string key)
        {
            return Application.Current.TryFindResource(key) != null;
        }

        public static double ImperialConverter(double val, UnitType_t type)
        {
            if (!App.Settings.UseImperial) return val;
            if (type == UnitType_t.Temp)
                return val * 1.8 + 32;
            if (type == UnitType_t.Pressure)
                return val * 0.14503773773;
            if (type == UnitType_t.Flow)
                return val / 4.5461;

            return val;
        }
    }
}
