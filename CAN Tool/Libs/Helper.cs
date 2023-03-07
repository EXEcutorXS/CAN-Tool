using OmniProtocol;

namespace CAN_Tool.Libs
{
    public static class Helper
    {
        public static string GetString(string key)
        {
            string ret = (string)App.Current.TryFindResource(key);
            if (ret != null)
                return ret;
            else
                return key;
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
