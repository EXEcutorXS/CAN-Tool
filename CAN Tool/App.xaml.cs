using System.Collections.Generic;
using System;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Threading;
using CAN_Tool.Libs;

namespace CAN_Tool
{
    public partial class App : Application
    {
        public App()
        {
            // .NET (Core) не включает кодовые страницы вроде 1251 по умолчанию (только Unicode) -
            // регистрация нужна до первого Encoding.GetEncoding(1251) (см. Omni.cs
            // DecodeStringBytes, PGN61/62 - строки могут прийти в Win1251).
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // Build language list from the CSV columns so adding a new language
            // to local.csv automatically makes it available — no code change needed.
            // CSV culture headers like "en-En", "ru-Ru", "de-De" are mapped to
            // canonical CultureInfo names by taking the first two chars as the
            // language code (e.g. "ru" → "ru-RU").
            LocalizationLoader.Initialize();

            m_Languages.Clear();
            foreach (string csvCulture in LocalizationLoader.Cultures)
            {
                try
                {
                    // "en-En" → "en", look up a canonical culture
                    string lang = csvCulture.Length >= 2 ? csvCulture.Substring(0, 2) : csvCulture;
                    var ci = CultureInfo.GetCultureInfoByIetfLanguageTag(lang);
                    m_Languages.Add(ci);
                }
                catch
                {
                    // Unrecognised tag — add as-is so we don't lose the column
                    try { m_Languages.Add(new CultureInfo(csvCulture)); } catch { /* ignore */ }
                }
            }

            // Fallback: at least English
            if (m_Languages.Count == 0)
                m_Languages.Add(new CultureInfo("en-US"));
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            // Replace the static lang.xaml entry (from App.xaml) with the
            // in-memory English dictionary built from local.csv.
            LocalizationLoader.Apply("en-US");
            base.OnStartup(e);
        }

        private static List<CultureInfo> m_Languages = new List<CultureInfo>();
        public static Settings Settings { set; get; } = new();
        public static List<CultureInfo> Languages => m_Languages;

        public static event EventHandler LanguageChanged;

        public static CultureInfo Language
        {
            get => Thread.CurrentThread.CurrentUICulture;
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                if (value == Thread.CurrentThread.CurrentUICulture) return;

                Thread.CurrentThread.CurrentUICulture = value;

                // Swap the in-memory ResourceDictionary for the requested culture.
                LocalizationLoader.Apply(value.Name);

                LanguageChanged?.Invoke(Application.Current, EventArgs.Empty);
            }
        }
    }
}
