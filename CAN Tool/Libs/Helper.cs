using System.Windows;

namespace CAN_Tool.Libs
{
    internal static class Helper
    {
        public static string GetString(string key)
        {
            string ret = (string)App.Current.TryFindResource(key);
            if (ret != null) 
                return ret;
            else
                return key;
        }
    }
}
