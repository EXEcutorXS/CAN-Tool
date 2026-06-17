using OmniProtocol;
using System;
using System.Collections.Generic;
using static CAN_Tool.Libs.Helper;

namespace CAN_Tool.Libs
{
    public static class DecoderRegistry
    {
        public static readonly Dictionary<string, Func<byte[], string>> CustomDecoders = new()
        {
            ["bb_pair_number"]     = d => (d[0] & 0xF) == 3 ? "Номер пары:" + (d[4] * 0x100 + d[5]) + ";" : "",
            ["bb_param_number_req"]= d => (d[0] & 0xF) == 6 ? "Номер параметра:" + (d[4] * 0x100 + d[5]) + ";" : "",
            ["bb_pair_count"]      = d => (d[0] & 0xF) == 6 ? "Запрошено пар:" + (d[6] * 0x100 + d[7]) + ";" : "",
            ["bb_param_number_ans"]= d => d[0] == 4 ? "Параметр:" + (d[2] * 256 + d[3]) + ";" : "",
            ["bb_param_value"]     = d => d[0] == 4 ? "Значение:" + (d[4] * 0x1000000 + d[5] * 0x10000 + d[6] * 0x100 + d[7]) + ";" : "",
            ["firmware_version"]   = i => $"{i[0]}.{i[1]}.{i[2]}.{i[3]}\r\n",
            ["modem_operator_code"]= d =>
            {
                var s = new string(new[] { (char)d[1], (char)d[2], (char)d[3], (char)d[4], (char)d[5] });
                return "Код оператора: " + (d[1] == 0xFF ? GetString("t_no_data") : s) + ";";
            },
            ["modem_lac_cellid"]    = d =>
            {
                if (d[1] == 0xFF && d[2] == 0xFF) return "LAC/CellID: " + GetString("t_no_data") + ";";
                int lac = (d[1] << 8) | d[2];
                long cellId = ((long)d[3] << 24) | ((long)d[4] << 16) | ((long)d[5] << 8) | d[6];
                return $"LAC: {lac}, CellID: {cellId};";
            },
        };

        public static readonly Dictionary<string, Func<int, string>> GetMeaningHandlers;

        static DecoderRegistry()
        {
            // Requires Omni.Pgns to be populated first — lazily referenced via closure
            GetMeaningHandlers = new()
            {
                ["error_code"]    = x => GetString($"e_{x}"),
                ["device_name"]   = i => GetString("t_device") + ": " + GetString($"d_{i}") + ";",
                ["pgn_name"]      = x => Omni.Pgns.TryGetValue(x, out var pgn) ? pgn.name : "Нет такого Pgn",
                ["param_name"]    = x => GetString($"par_{x}"),
                ["release_date"]  = v => $"{v >> 16}.{(v >> 8) & 0xF}.{v & 0xFF}\r\n",
                ["hex_address"]   = r => $"{GetString("t_starting_address")}: 0X{r + 0x8000000:X}",
                ["hex_fragment"]  = r => $"{GetString("t_fragment_address")}: 0X{r:X}",
                ["hex_crc"]       = r => $"CRC: 0X{r:X}",
                ["hex_value"]     = r => $"0X{r:X}",
            };
        }
    }
}
