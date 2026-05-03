using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CAN_Tool.Libs
{
    /// <summary>
    /// Downloads selected sheets from a public Google Spreadsheet and patches omnidata.json.
    /// Each sheet tab maps to a top-level JSON array key of the same name (lowercase).
    /// The spreadsheet must be shared as "Anyone with the link can view".
    /// </summary>
    public static class GoogleSheetsUpdater
    {
        // ── Configuration ─────────────────────────────────────────────────────
        // Replace with your actual spreadsheet ID (from the URL):
        //   https://docs.google.com/spreadsheets/d/<ID>/edit
        private const string SpreadsheetId = "TODO_REPLACE_WITH_YOUR_SPREADSHEET_ID";

        // Tab names to download and merge into omnidata.json.
        // Key   = sheet tab name in Google Sheets
        // Value = top-level JSON key in omnidata.json
        private static readonly (string sheetName, string jsonKey)[] Sheets =
        {
            ("devices", "devices"),
        };

        private const int TimeoutSeconds = 5;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Tries to download fresh data from Google Sheets and patch omnidata.json.
        /// Returns without throwing on any failure — the app continues with the local file.
        /// </summary>
        public static async Task TryUpdateAsync()
        {
            if (SpreadsheetId.StartsWith("TODO"))
            {
                Debug.WriteLine("[GoogleSheetsUpdater] Spreadsheet ID is not configured — skipping update.");
                return;
            }

            var localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "omnidata.json");

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(TimeoutSeconds));
                using var http = new HttpClient();

                // Load current local JSON (we'll patch it)
                var localJson = File.Exists(localPath)
                    ? JObject.Parse(await File.ReadAllTextAsync(localPath, cts.Token))
                    : new JObject();

                bool anyChange = false;

                foreach (var (sheetName, jsonKey) in Sheets)
                {
                    try
                    {
                        var csvUrl = $"https://docs.google.com/spreadsheets/d/{SpreadsheetId}" +
                                     $"/gviz/tq?tqx=out:csv&sheet={Uri.EscapeDataString(sheetName)}";

                        var csv = await http.GetStringAsync(csvUrl, cts.Token);
                        var parsed = CsvToJArray(csv, jsonKey);

                        if (parsed != null)
                        {
                            localJson[jsonKey] = parsed;
                            anyChange = true;
                            Debug.WriteLine($"[GoogleSheetsUpdater] '{sheetName}' → {parsed.Count} rows merged.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[GoogleSheetsUpdater] Sheet '{sheetName}' failed: {ex.Message}");
                    }
                }

                if (anyChange)
                {
                    var output = localJson.ToString(Formatting.Indented);
                    await File.WriteAllTextAsync(localPath, output, cts.Token);
                    Debug.WriteLine("[GoogleSheetsUpdater] omnidata.json updated successfully.");
                }
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("[GoogleSheetsUpdater] Timed out — using local omnidata.json.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GoogleSheetsUpdater] Update failed: {ex.Message} — using local omnidata.json.");
            }
        }

        // ── CSV → JArray converters ────────────────────────────────────────────

        private static JArray? CsvToJArray(string csv, string jsonKey) => jsonKey switch
        {
            "devices" => CsvToDevices(csv),
            _         => null
        };

        /// <summary>
        /// Parses the "devices" sheet.
        /// Expected columns (first row = headers, case-insensitive):
        ///   id, devType, imageName, maxBlower, maxFuelPump, bbErrorsLen
        /// </summary>
        private static JArray CsvToDevices(string csv)
        {
            var result = new JArray();
            var lines  = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 2) return result;

            var headers = ParseCsvRow(lines[0]);

            for (int i = 1; i < lines.Length; i++)
            {
                var cols = ParseCsvRow(lines[i]);
                if (cols.Length == 0 || string.IsNullOrWhiteSpace(cols[0]))
                    continue;

                var obj = new JObject();

                for (int c = 0; c < Math.Min(headers.Length, cols.Length); c++)
                {
                    var key = headers[c].Trim().ToLowerInvariant();
                    var val = cols[c].Trim();
                    if (string.IsNullOrEmpty(val)) continue;

                    switch (key)
                    {
                        case "id":
                        case "maxblower":
                        case "bberrorslen":
                            if (int.TryParse(val, out var iv))
                                obj[headers[c].Trim()] = iv;
                            break;
                        case "maxfuelpump":
                            if (double.TryParse(val, System.Globalization.NumberStyles.Any,
                                    System.Globalization.CultureInfo.InvariantCulture, out var dv))
                                obj[headers[c].Trim()] = dv;
                            break;
                        default:
                            obj[headers[c].Trim()] = val;
                            break;
                    }
                }

                if (obj.ContainsKey("id"))
                    result.Add(obj);
            }

            return result;
        }

        /// <summary>RFC-4180-compliant CSV row parser (handles quoted fields with embedded commas/newlines).</summary>
        private static string[] ParseCsvRow(string line)
        {
            var fields = new System.Collections.Generic.List<string>();
            var field  = new System.Text.StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"') { field.Append('"'); i++; }
                        else inQuotes = false;
                    }
                    else field.Append(c);
                }
                else
                {
                    if (c == '"') inQuotes = true;
                    else if (c == ',') { fields.Add(field.ToString()); field.Clear(); }
                    else field.Append(c);
                }
            }
            fields.Add(field.ToString());
            return fields.ToArray();
        }
    }
}
