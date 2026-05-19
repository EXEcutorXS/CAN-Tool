using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;

namespace CAN_Tool.Libs
{
    /// <summary>
    /// Reads local.csv (embedded resource) at startup and builds in-memory
    /// ResourceDictionaries — one per language column. Replaces the static
    /// lang.xaml reference from App.xaml so Localizator.exe is no longer needed.
    /// </summary>
    internal static class LocalizationLoader
    {
        // CSV culture header (e.g. "en-En") → ResourceDictionary
        private static readonly Dictionary<string, ResourceDictionary> _dicts = new();
        private static readonly List<string> _cultures = new();   // ordered as in CSV
        private static ResourceDictionary _active;                 // currently injected dict

        public static IReadOnlyList<string> Cultures => _cultures;

        // ── Initialization ────────────────────────────────────────────────────

        public static void Initialize()
        {
            _dicts.Clear();
            _cultures.Clear();
            _active = null;

            var lines = ReadEmbeddedCsv();
            if (lines == null || lines.Length == 0) return;

            // Header row: Key | en-En | ru-Ru | de-De | …
            var headers = lines[0].Split('\t');
            var rdList = new List<ResourceDictionary>();

            for (int col = 1; col < headers.Length; col++)
            {
                string culture = headers[col].Trim();
                if (string.IsNullOrEmpty(culture)) continue;
                var rd = new ResourceDictionary();
                _dicts[culture] = rd;
                _cultures.Add(culture);
                rdList.Add(rd);
            }

            // Data rows
            for (int i = 1; i < lines.Length; i++)
            {
                var parts = lines[i].Split('\t');
                string key = parts[0];
                if (key.Length <= 2) continue;   // skip blank separator rows

                for (int col = 0; col < rdList.Count; col++)
                {
                    int srcCol = col + 1;
                    string value = srcCol < parts.Length ? parts[srcCol]
                                 : parts.Length > 1     ? parts[1]   // fallback to first lang column
                                 : key;
                    rdList[col][key] = value;
                }
            }
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Swaps the active language ResourceDictionary in Application.Resources.
        /// cultureName is a standard CultureInfo.Name (e.g. "ru-RU", "de-DE", "en-US").
        /// Matching is done by 2-char language prefix so "ru-RU" finds "ru-Ru" column.
        /// </summary>
        public static void Apply(string cultureName)
        {
            if (Application.Current == null || _cultures.Count == 0) return;

            var newDict = FindBestMatch(cultureName);
            var merged = Application.Current.Resources.MergedDictionaries;

            if (_active != null && merged.Contains(_active))
            {
                // Replace previously injected dict
                int idx = merged.IndexOf(_active);
                merged.Remove(_active);
                merged.Insert(idx, newDict);
            }
            else
            {
                // First call: find and replace the static lang.xaml entry from App.xaml
                ResourceDictionary oldStatic = null;
                foreach (var d in merged)
                {
                    if (d.Source != null &&
                        d.Source.OriginalString.StartsWith("Resources/lang.", StringComparison.OrdinalIgnoreCase))
                    {
                        oldStatic = d;
                        break;
                    }
                }

                if (oldStatic != null)
                {
                    int idx = merged.IndexOf(oldStatic);
                    merged.Remove(oldStatic);
                    merged.Insert(idx, newDict);
                }
                else
                {
                    merged.Add(newDict);
                }
            }

            _active = newDict;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static ResourceDictionary FindBestMatch(string cultureName)
        {
            if (!string.IsNullOrEmpty(cultureName))
            {
                // Exact match (unlikely given en-En vs en-US, but handle it)
                if (_dicts.TryGetValue(cultureName, out var exact)) return exact;

                // 2-char language prefix: "ru-RU" → "ru", match against "ru-Ru"
                string prefix = cultureName.Length >= 2
                    ? cultureName.Substring(0, 2).ToLowerInvariant()
                    : cultureName.ToLowerInvariant();

                foreach (var kv in _dicts)
                    if (kv.Key.Length >= 2 && kv.Key.Substring(0, 2).ToLowerInvariant() == prefix)
                        return kv.Value;
            }

            // Fallback: first column (English)
            return _dicts.Count > 0 ? _dicts[_cultures[0]] : new ResourceDictionary();
        }

        private static string[] ReadEmbeddedCsv()
        {
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                // Resource name: <RootNamespace>.<folder>.<filename>
                using var stream = asm.GetManifestResourceStream("CAN_Tool.Resources.local.csv");
                if (stream == null) return null;
                using var reader = new StreamReader(stream, Encoding.UTF8);
                return reader.ReadToEnd()
                             .Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            }
            catch { return null; }
        }
    }
}
