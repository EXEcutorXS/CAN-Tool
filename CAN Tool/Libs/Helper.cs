using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CAN_Tool.Libs
{
    internal static class Helper
    {
        public static string GetString(string key)
        {
            return (string)App.Current.FindResource(key);
            
        }
    }
}
