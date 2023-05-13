using MaterialDesignThemes.Wpf;
using OmniProtocol;
using System.ComponentModel;
using System;
using System.Linq;

namespace CAN_Tool.Libs
{
    public interface IUpdatable<T>
    {
        public void Update(T item);

        public bool IsSimmiliarTo(T item);

    }

    public class UpdatableList<T> : BindingList<T> where T : IUpdatable<T>, IComparable
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="unit">Element to add</param>
        /// <returns>true - element was added, false - updated</returns>
        public bool TryToAdd(T item)
        {
            var found = Items.FirstOrDefault(i => i.IsSimmiliarTo(item));
            if (found == null)
            {
                if (Count > 0)
                {
                    for (int i = 0; i < Count; i++)
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
            if (string.IsNullOrEmpty(key)) return "";
            string ret = (string)App.Current.TryFindResource(key);
            if (ret != null)
                return ret;
            else
                return key.Replace('_',' ');
        }

        public static bool GotResource(string key)
        {
            if (App.Current.TryFindResource(key) != null)
                return true;
            else
                return false;
        }

        public static double ImperialConverter(double val, UnitType type)
        {
            if (App.Settings.UseImperial)
                switch (type)
                {
                    case UnitType.Temp: return val * 1.8 + 32;
                    case UnitType.Pressure: return val * 0.14503773773;
                    case UnitType.Flow: return val /= 4.5461;
                }
            return val;
        }
    }
}
