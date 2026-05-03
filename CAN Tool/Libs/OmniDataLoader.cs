using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OmniProtocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static CAN_Tool.Libs.Helper;

namespace CAN_Tool.Libs
{
    public static class OmniDataLoader
    {
        public static void Load(
            Dictionary<int, PgnClass> pgns,
            List<OmniPgnParameter> parameters,
            Dictionary<int, OmniCommand> commands,
            Dictionary<int, DeviceTemplate> devices)
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "omnidata.json");
            var json = File.ReadAllText(path);
            var root = JObject.Parse(json);

            var presets = LoadMeaningPresets(root);
            LoadPgns(root, pgns);
            LoadParameters(root, parameters, presets);
            LoadCommands(root, commands, presets);
            LoadDevices(root, devices);

            // Link each parameter to its PGN's parameter list so ProcessOmniMessage can iterate them
            foreach (var p in parameters)
                if (pgns.TryGetValue(p.Pgn, out var pgn))
                    pgn.parameters.Add(p);
        }

        private static void LoadDevices(JObject root, Dictionary<int, DeviceTemplate> devices)
        {
            if (root["devices"] is not JArray devicesNode) return;

            foreach (var item in devicesNode.Children<JObject>())
            {
                var id = item["id"]!.Value<int>();
                devices[id] = new DeviceTemplate
                {
                    Id          = id,
                    DevType     = item["devType"] != null
                                    ? Enum.Parse<DeviceType_t>(item["devType"]!.Value<string>()!)
                                    : DeviceType_t.None,
                    ImageName   = item["imageName"]?.Value<string>() ?? string.Empty,
                    MaxBlower   = item["maxBlower"]?.Value<int>() ?? 130,
                    MaxFuelPump = item["maxFuelPump"]?.Value<double>() ?? 4,
                    BBErrorsLen = item["bbErrorsLen"]?.Value<int>() ?? 512,
                };
            }
        }

        private static Dictionary<string, Dictionary<int, string>> LoadMeaningPresets(JObject root)
        {
            var presets = new Dictionary<string, Dictionary<int, string>>();
            if (root["meaningPresets"] is not JObject presetsNode) return presets;

            foreach (var prop in presetsNode.Properties())
                presets[prop.Name] = ParseMeaningsObject((JObject)prop.Value);

            return presets;
        }

        private static void LoadPgns(JObject root, Dictionary<int, PgnClass> pgns)
        {
            foreach (var item in root["pgns"]!.Children<JObject>())
            {
                var id = item["id"]!.Value<int>();
                pgns[id] = new PgnClass
                {
                    id = id,
                    name = item["name"]?.Value<string>() ?? "",
                    multiPack = item["multiPack"]?.Value<bool>() ?? false
                };
            }
        }

        private static void LoadParameters(
            JObject root,
            List<OmniPgnParameter> parameters,
            Dictionary<string, Dictionary<int, string>> presets)
        {
            foreach (var item in root["parameters"]!.Children<JObject>())
                parameters.Add(ParseParameter(item, presets));
        }

        private static void LoadCommands(
            JObject root,
            Dictionary<int, OmniCommand> commands,
            Dictionary<string, Dictionary<int, string>> presets)
        {
            foreach (var item in root["commands"]!.Children<JObject>())
            {
                var id = item["id"]!.Value<int>();
                if (!commands.ContainsKey(id))
                    commands[id] = new OmniCommand { Id = id };

                foreach (var paramNode in item["parameters"]!.Children<JObject>())
                    commands[id].Parameters.Add(ParseParameter(paramNode, presets));
            }
        }

        private static OmniPgnParameter ParseParameter(
            JObject node,
            Dictionary<string, Dictionary<int, string>> presets)
        {
            var p = new OmniPgnParameter
            {
                Pgn       = node["pgn"]?.Value<int>() ?? 0,
                Name      = node["name"]?.Value<string>() ?? "",
                StartByte = node["startByte"]?.Value<int>() ?? 0,
                StartBit  = node["startBit"]?.Value<int>() ?? 0,
                BitLength = node["bitLength"]?.Value<int>() ?? 0,
                a         = node["a"]?.Value<double>() ?? 1,
                b         = node["b"]?.Value<double>() ?? 0,
                Signed    = node["signed"]?.Value<bool>() ?? false,
                AnswerOnly = node["answerOnly"]?.Value<bool>() ?? false,
                PackNumber = node["packNumber"]?.Value<int?>(),
                Var       = node["var"]?.Value<int>() ?? 0,
                DefaultValue = node["defaultValue"]?.Value<double>() ?? 0,
                UnitT     = node["unitType"] != null
                    ? Enum.Parse<UnitType_t>(node["unitType"]!.Value<string>()!)
                    : UnitType_t.None,
            };

            if (node["meanings"] is JToken meaningsToken)
            {
                if (meaningsToken.Type == JTokenType.String)
                {
                    var presetName = meaningsToken.Value<string>()!;
                    if (presets.TryGetValue(presetName, out var preset))
                        p.Meanings = new Dictionary<int, string>(preset);
                }
                else if (meaningsToken.Type == JTokenType.Object)
                {
                    p.Meanings = ParseMeaningsObject((JObject)meaningsToken);
                }
            }

            if (node["getMeaning"]?.Value<string>() is string getMeaningKey)
                if (!DecoderRegistry.GetMeaningHandlers.TryGetValue(getMeaningKey, out p.GetMeaning))
                    throw new KeyNotFoundException($"GetMeaning handler '{getMeaningKey}' not found in DecoderRegistry");

            if (node["decoder"]?.Value<string>() is string decoderKey)
                if (!DecoderRegistry.CustomDecoders.TryGetValue(decoderKey, out p.CustomDecoder))
                    throw new KeyNotFoundException($"CustomDecoder '{decoderKey}' not found in DecoderRegistry");

            return p;
        }

        private static Dictionary<int, string> ParseMeaningsObject(JObject obj) =>
            obj.Properties().ToDictionary(p => int.Parse(p.Name), p => p.Value.Value<string>()!);
    }
}
