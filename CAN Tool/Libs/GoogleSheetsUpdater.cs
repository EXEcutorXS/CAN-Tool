using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CAN_Tool.Libs
{
    /// <summary>
    /// Downloads the Omniprotocol tables from a published Google Spreadsheet and rebuilds omnidata.json.
    /// Each sheet tab maps to a top-level JSON key of omnidata.json (see per-sheet column reference below).
    ///
    /// The spreadsheet must be published via File → Share → "Publish to web" (any sheet/format is fine
    /// for the initial publish — publishing activates public CSV export for every tab by gid).
    /// Plain "Anyone with the link" sharing is NOT enough: Google sometimes serves an anonymous,
    /// cookie-less HttpClient a login/consent redirect instead of raw CSV in that mode, which is what
    /// broke this before. The published "/pub?output=csv" endpoint is built for exactly this and doesn't
    /// have that problem.
    ///
    /// After publishing, copy the id between "/d/e/" and "/pubhtml" (or "/pub") from the dialog's link
    /// into <see cref="PublishedId"/>. Each tab's gid is visible in its edit-mode URL
    /// (".../edit#gid=1234567890") when that tab is selected.
    ///
    /// ── Sheet "pgns" → JSON key "pgns" ──────────────────────────────────────
    ///   id (int, required), name (string), multiPack (bool, optional)
    ///
    /// ── Sheet "parameters" → JSON key "parameters" ──────────────────────────
    /// Fields that sit directly in a PGN's 8-byte payload (outside embedded commands).
    ///   pgn (int, required)         - PGN this field belongs to
    ///   name (string)               - display / localization key
    ///   startByte, startBit, bitLength (int)
    ///   a, b (double)               - value = raw * a + b
    ///   signed (bool)
    ///   answerOnly (bool)
    ///   packNumber (int)            - sub-pack index for multiPack PGNs (e.g. PGN 19)
    ///   var (int)                   - legacy dashboard binding id
    ///   defaultValue (double)
    ///   unitType (string)           - UnitType_t enum name: None/Temp/Volt/Current/Pressure/Flow/Rpm/Rps/Percent/Second/Minute/Hour/Day/Month/Year/Frequency/Power
    ///   meanings (string)           - either a preset name from the "meanings" sheet (e.g. "OnOff"),
    ///                                 or an inline map "code=text;code=text" (e.g. "0=t_off;1=t_on")
    ///   getMeaning (string)         - key into DecoderRegistry.GetMeaningHandlers (e.g. "pgn_name")
    ///   decoder (string)            - key into DecoderRegistry.CustomDecoders (e.g. "bb_pair_number")
    ///
    /// ── Sheet "commands" → JSON key "commands" ──────────────────────────────
    /// Same field set as "parameters", but keyed by commandId instead of pgn; rows sharing the
    /// same commandId are grouped into one commands[].parameters[] array (in sheet row order).
    ///   commandId (int, required), name, startByte, startBit, bitLength, a, b, signed, answerOnly,
    ///   packNumber, var, defaultValue, unitType, meanings, getMeaning, decoder
    ///
    /// ── Sheet "meanings" → JSON key "meaningPresets" ────────────────────────
    /// Named code→text presets reusable across many parameters (e.g. YesNo, OnOff, Allow, Stages).
    ///   presetName (string, required), code (int, required), text (string)
    ///
    /// ── Sheet "devices" → JSON key "devices" ────────────────────────────────
    ///   id (int, required)
    ///   devType (string)     - DeviceType_t enum name
    ///   imageName (string)
    ///   maxBlower (int)
    ///   maxFuelPump (double)
    ///   bbErrorsLen (int)
    ///
    /// ── Sheet "stringIds" → JSON key "stringIds" ────────────────────────────
    ///   id (int, required), name (string)
    /// </summary>
    public static class GoogleSheetsUpdater
    {
        // ── Configuration ─────────────────────────────────────────────────────
        // The id between "/d/e/" and "/pubhtml" in the link shown by
        // File → Share → "Publish to web" (NOT the plain edit-URL spreadsheet id).
        private const string PublishedId = "2PACX-1vRoSPBXvuDypXyYA5eSDqTvcFMZWD1tHJ_pR6HNSiW9X1mNjJLhy654Fa5rNhw2pP_r3Ssy2YtJlv76";

        // Tabs to download and merge into omnidata.json.
        // sheetName = tab name (for logging only, not used in the request)
        // jsonKey   = top-level JSON key in omnidata.json
        // gid       = tab's numeric id, from its edit-mode URL "...edit#gid=<gid>"
        private static readonly (string sheetName, string jsonKey, string gid)[] Sheets =
        {
            ("pgns",       "pgns",           "1784450801"),
            ("parameters", "parameters",     "80585436"),
            ("commands",   "commands",       "175458248"),
            ("meanings",   "meaningPresets", "2127446163"),
            ("devices",    "devices",        "708633351"),
            ("stringIds",  "stringIds",      "1225550681"),
        };

        private const int TimeoutSeconds = 5;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Tries to download fresh data from Google Sheets and patch omnidata.json.
        /// Returns without throwing on any failure — the app continues with the local file.
        /// </summary>
        public static async Task TryUpdateAsync()
        {
            if (PublishedId.StartsWith("TODO"))
            {
                Debug.WriteLine("[GoogleSheetsUpdater] Published spreadsheet id is not configured — skipping update.");
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

                foreach (var (sheetName, jsonKey, gid) in Sheets)
                {
                    try
                    {
                        var csvUrl = $"https://docs.google.com/spreadsheets/d/e/{PublishedId}" +
                                     $"/pub?gid={Uri.EscapeDataString(gid)}&single=true&output=csv";

                        var csv = await http.GetStringAsync(csvUrl, cts.Token);
                        var parsed = CsvToJToken(csv, jsonKey);

                        if (parsed != null)
                        {
                            localJson[jsonKey] = parsed;
                            anyChange = true;
                            var count = parsed is JContainer jc ? jc.Count : 0;
                            Debug.WriteLine($"[GoogleSheetsUpdater] '{sheetName}' → {count} entries merged.");
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

        // ── CSV → JSON dispatch ──────────────────────────────────────────────

        private static JToken? CsvToJToken(string csv, string jsonKey) => jsonKey switch
        {
            "pgns"           => CsvToPgns(csv),
            "parameters"     => CsvToParameters(csv),
            "commands"       => CsvToCommands(csv),
            "meaningPresets" => CsvToMeaningPresets(csv),
            "devices"        => CsvToDevices(csv),
            "stringIds"      => CsvToStringIds(csv),
            _                => null
        };

        // ── Per-sheet builders ───────────────────────────────────────────────

        /// <summary>pgns: id, name, multiPack</summary>
        private static JArray CsvToPgns(string csv)
        {
            var result = new JArray();
            foreach (var row in ParseCsvRows(csv))
            {
                if (!TryGetInt(row, "id", out var id)) continue;

                var obj = new JObject { ["id"] = id };
                if (TryGetString(row, "name", out var name)) obj["name"] = name;
                if (TryGetBool(row, "multipack", out var multiPack) && multiPack) obj["multiPack"] = true;

                result.Add(obj);
            }
            return result;
        }

        /// <summary>parameters: pgn + shared parameter fields</summary>
        private static JArray CsvToParameters(string csv)
        {
            var result = new JArray();
            foreach (var row in ParseCsvRows(csv))
            {
                if (!TryGetInt(row, "pgn", out var pgn)) continue;

                var obj = BuildParameterFields(row, new JObject { ["pgn"] = pgn });
                result.Add(obj);
            }
            return result;
        }

        /// <summary>commands: rows grouped by commandId into commands[].parameters[]</summary>
        private static JArray CsvToCommands(string csv)
        {
            var withId = new List<(int id, Dictionary<string, string> row)>();
            foreach (var row in ParseCsvRows(csv))
                if (TryGetInt(row, "commandid", out var id))
                    withId.Add((id, row));

            var result = new JArray();
            foreach (var group in withId.GroupBy(x => x.id))
            {
                var paramsArr = new JArray();
                foreach (var (_, row) in group)
                    paramsArr.Add(BuildParameterFields(row, new JObject()));

                result.Add(new JObject
                {
                    ["id"] = group.Key,
                    ["parameters"] = paramsArr
                });
            }
            return result;
        }

        /// <summary>meanings: presetName, code, text → grouped into { presetName: { code: text } }</summary>
        private static JObject CsvToMeaningPresets(string csv)
        {
            var result = new JObject();
            foreach (var row in ParseCsvRows(csv))
            {
                if (!TryGetString(row, "presetname", out var preset)) continue;
                if (!TryGetInt(row, "code", out var code)) continue;
                TryGetString(row, "text", out var text);

                if (result[preset] is not JObject presetObj)
                {
                    presetObj = new JObject();
                    result[preset] = presetObj;
                }
                presetObj[code.ToString(CultureInfo.InvariantCulture)] = text ?? "";
            }
            return result;
        }

        /// <summary>devices: id, devType, imageName, maxBlower, maxFuelPump, bbErrorsLen</summary>
        private static JArray CsvToDevices(string csv)
        {
            var result = new JArray();
            foreach (var row in ParseCsvRows(csv))
            {
                if (!TryGetInt(row, "id", out var id)) continue;

                var obj = new JObject { ["id"] = id };
                if (TryGetString(row, "devtype", out var devType)) obj["devType"] = devType;
                if (TryGetString(row, "imagename", out var imageName)) obj["imageName"] = imageName;
                if (TryGetInt(row, "maxblower", out var maxBlower)) obj["maxBlower"] = maxBlower;
                if (TryGetDouble(row, "maxfuelpump", out var maxFuelPump)) obj["maxFuelPump"] = maxFuelPump;
                if (TryGetInt(row, "bberrorslen", out var bbErrorsLen)) obj["bbErrorsLen"] = bbErrorsLen;

                result.Add(obj);
            }
            return result;
        }

        /// <summary>stringIds: id, name</summary>
        private static JArray CsvToStringIds(string csv)
        {
            var result = new JArray();
            foreach (var row in ParseCsvRows(csv))
            {
                if (!TryGetInt(row, "id", out var id)) continue;

                var obj = new JObject { ["id"] = id };
                if (TryGetString(row, "name", out var name)) obj["name"] = name;

                result.Add(obj);
            }
            return result;
        }

        /// <summary>
        /// Fields shared by the "parameters" and "commands" sheets (everything except the
        /// owning key, which the caller seeds into <paramref name="seed"/> beforehand).
        /// </summary>
        private static JObject BuildParameterFields(Dictionary<string, string> row, JObject seed)
        {
            if (TryGetString(row, "name", out var name)) seed["name"] = name;
            if (TryGetInt(row, "startbyte", out var startByte)) seed["startByte"] = startByte;
            if (TryGetInt(row, "startbit", out var startBit)) seed["startBit"] = startBit;
            if (TryGetInt(row, "bitlength", out var bitLength)) seed["bitLength"] = bitLength;
            if (TryGetDouble(row, "a", out var a)) seed["a"] = a;
            if (TryGetDouble(row, "b", out var b)) seed["b"] = b;
            if (TryGetBool(row, "signed", out var signed) && signed) seed["signed"] = true;
            if (TryGetBool(row, "answeronly", out var answerOnly) && answerOnly) seed["answerOnly"] = true;
            if (TryGetInt(row, "packnumber", out var packNumber)) seed["packNumber"] = packNumber;
            if (TryGetInt(row, "var", out var var)) seed["var"] = var;
            if (TryGetDouble(row, "defaultvalue", out var defaultValue)) seed["defaultValue"] = defaultValue;
            if (TryGetString(row, "unittype", out var unitType)) seed["unitType"] = unitType;

            if (TryGetString(row, "meanings", out var meaningsRaw))
            {
                var meaningsToken = ParseMeaningsCell(meaningsRaw);
                if (meaningsToken != null) seed["meanings"] = meaningsToken;
            }

            if (TryGetString(row, "getmeaning", out var getMeaning)) seed["getMeaning"] = getMeaning;
            if (TryGetString(row, "decoder", out var decoder)) seed["decoder"] = decoder;

            return seed;
        }

        /// <summary>
        /// "0=t_off;1=t_on" → inline JObject; anything else (e.g. "OnOff") → reference to a
        /// named preset from the "meanings" sheet, resolved later by OmniDataLoader.
        /// </summary>
        private static JToken? ParseMeaningsCell(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;

            if (raw.Contains('='))
            {
                var obj = new JObject();
                foreach (var pair in raw.Split(';', StringSplitOptions.RemoveEmptyEntries))
                {
                    var kv = pair.Split('=', 2);
                    if (kv.Length != 2) continue;
                    if (int.TryParse(kv[0].Trim(), out var code))
                        obj[code.ToString(CultureInfo.InvariantCulture)] = kv[1].Trim();
                }
                return obj;
            }

            return raw.Trim();
        }

        // ── CSV parsing helpers ──────────────────────────────────────────────

        /// <summary>Parses a sheet's CSV into rows keyed by lower-cased, trimmed header names.</summary>
        private static List<Dictionary<string, string>> ParseCsvRows(string csv)
        {
            var rows = new List<Dictionary<string, string>>();
            var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 2) return rows;

            var headers = ParseCsvRow(lines[0]).Select(h => h.Trim().ToLowerInvariant()).ToArray();

            for (int i = 1; i < lines.Length; i++)
            {
                var cols = ParseCsvRow(lines[i]);
                if (cols.Length == 0 || string.IsNullOrWhiteSpace(cols[0]))
                    continue;

                var row = new Dictionary<string, string>();
                for (int c = 0; c < Math.Min(headers.Length, cols.Length); c++)
                    row[headers[c]] = cols[c].Trim();

                rows.Add(row);
            }
            return rows;
        }

        private static bool TryGetString(Dictionary<string, string> row, string key, out string value)
        {
            if (row.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v))
            {
                value = v;
                return true;
            }
            value = "";
            return false;
        }

        private static bool TryGetInt(Dictionary<string, string> row, string key, out int value)
        {
            value = 0;
            return row.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v)
                   && int.TryParse(v.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryGetDouble(Dictionary<string, string> row, string key, out double value)
        {
            value = 0;
            return row.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v)
                   && double.TryParse(v.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryGetBool(Dictionary<string, string> row, string key, out bool value)
        {
            value = false;
            if (!row.TryGetValue(key, out var v) || string.IsNullOrWhiteSpace(v)) return false;

            v = v.Trim();
            if (v.Equals("true", StringComparison.OrdinalIgnoreCase) || v == "1") { value = true; return true; }
            if (v.Equals("false", StringComparison.OrdinalIgnoreCase) || v == "0") { value = false; return true; }
            return false;
        }

        /// <summary>RFC-4180-compliant CSV row parser (handles quoted fields with embedded commas/newlines).</summary>
        private static string[] ParseCsvRow(string line)
        {
            var fields = new List<string>();
            var field = new System.Text.StringBuilder();
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
