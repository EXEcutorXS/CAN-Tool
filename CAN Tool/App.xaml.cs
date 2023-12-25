using System.Collections.Generic;
using System;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Windows;

namespace CAN_Tool
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            m_Languages.Clear();
            m_Languages.Add(new CultureInfo("en-US")); //Нейтральная культура для этого проекта
            m_Languages.Add(new CultureInfo("ru-RU"));
        }

        private static List<CultureInfo> m_Languages = new List<CultureInfo>();

        public static Settings Settings { set; get; } = new();
        public static List<CultureInfo> Languages => m_Languages;

        public static event EventHandler LanguageChanged;

        public static CultureInfo Language
        {
            get
            {
                return System.Threading.Thread.CurrentThread.CurrentUICulture;
            }
            set
            {
                if (value == null) throw new ArgumentNullException("value");
                if (value == System.Threading.Thread.CurrentThread.CurrentUICulture) return;

                //1. Меняем язык приложения:
                System.Threading.Thread.CurrentThread.CurrentUICulture = value;

                //2. Создаём ResourceDictionary для новой культуры
                var dict = new ResourceDictionary();
                switch (value.Name)
                {
                    case "ru-RU":
                        dict.Source = new Uri(String.Format("Resources/lang.{0}.xaml", value.Name), UriKind.Relative);
                        break;
                    default:
                        dict.Source = new Uri("Resources/lang.xaml", UriKind.Relative);
                        break;
                }

                //3. Находим старую ResourceDictionary и удаляем его и добавляем новую ResourceDictionary
                var oldDict = (from d in Application.Current.Resources.MergedDictionaries
                               where d.Source != null && d.Source.OriginalString.StartsWith("Resources/lang.")
                               select d).FirstOrDefault();
                if (oldDict != null)
                {
                    var ind = Application.Current.Resources.MergedDictionaries.IndexOf(oldDict);
                    Application.Current.Resources.MergedDictionaries.Remove(oldDict);
                    Application.Current.Resources.MergedDictionaries.Insert(ind, dict);
                }
                else
                {
                    Application.Current.Resources.MergedDictionaries.Add(dict);
                }

                //4. Вызываем евент для оповещения всех окон.
                LanguageChanged(Application.Current, new EventArgs());
            }
        }
    }

}
