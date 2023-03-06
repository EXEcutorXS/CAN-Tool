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

        public static double ImperialConverter(double val, UnitT type)
        {
            if (App.Settings.UseImperial)
                switch (type)
                {
                    case UnitT.temperature: return val * 1.8 + 32;
                    case UnitT.pressure: return val / 14.5;
                    case UnitT.flow: return val /= 4.5461;
                }
            return val;
        }
    }
}
