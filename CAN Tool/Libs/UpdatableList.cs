using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows.Threading;

namespace CAN_Tool.Libs
{

    public interface IUpdatable<T>
    {
        public void Update(T item);

        public bool IsSimmiliarTo(T item);
    }
    public class UpdatableList<T> : BindingList<T> where T : IUpdatable<T>
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
                Add(item);
                return true;
            }
            else
                found.Update(item);
            return false;
        }
    }
}
