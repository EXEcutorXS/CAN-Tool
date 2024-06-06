using CAN_Tool;
using OmniProtocol;
using System;
using System.ComponentModel;
using System.Linq;
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
        public bool TryToAdd(T item)
        {
            var found = Items.FirstOrDefault(i => i.IsSimiliarTo(item));
            if (found == null)
            {
                if (Count > 0)
                {
                    for (var i = 0; i < Count; i++)
                    {
                        if (item.CompareTo(Items[i]) <= 0)
                        {
                            Insert(i, item);
                            return true;
                        }
                    }
                    Add(item);
                    return true;
                }
                else
                {
                    Add(item);
                }
            }
            else
            {
                found.Update(item);
            }
            return false;
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

        public static double ImperialConverter(double val, UnitType type)
        {
            if (!App.Settings.UseImperial) return val;
            if (type == UnitType.Temp)
                return val * 1.8 + 32;
            if (type == UnitType.Pressure)
                return val * 0.14503773773;
            if (type == UnitType.Flow)
                return val / 4.5461;

            return val;
        }
    }
}
