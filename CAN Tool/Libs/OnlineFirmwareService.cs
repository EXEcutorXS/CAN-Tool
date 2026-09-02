using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CAN_Tool.Libs
{
    // Список прошивок для конкретного типа устройства и сама заливка бинарника с
    // multihot.online (сервер Timberline/modem OTA, host/timberline-web/server.js
    // в C:\source\modem_timberline). Эндпоинты публичные, без авторизации:
    //   GET /firmware/{type}/versions              -> { "versions": ["34.1.3.10", ...] }
    //   GET /firmware/{type}/{version}/firmware.bin -> сырой бинарник (без offset/len - весь файл как есть)
    //   GET /firmware/{type}/{version}/profile      -> "flashBase=0x08020000\n[eraseSectors=...]"
    //
    // {type} - тот же номер типа устройства, что и DeviceId.Type / первый байт версии прошивки
    // (см. Resources/omnidata.json и https://multihot.online/device-types.json).
    //
    // eraseSectors (частичное стирание секторов для некоторых старых сборок) сейчас
    // игнорируется - существующий Erase* в BootloaderDeviceViewModel.*.cs всегда стирает
    // всю область программы целиком, что стирает не меньше нужного (просто чуть дольше).
    public static class OnlineFirmwareService
    {
        private const string BaseUrl = "https://multihot.online";
        private const int TimeoutSeconds = 8;

        public static async Task<List<string>> GetVersionsAsync(int deviceType)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(TimeoutSeconds));
                using var http = new HttpClient();
                var json = await http.GetStringAsync($"{BaseUrl}/firmware/{deviceType}/versions", cts.Token);
                var obj = JObject.Parse(json);
                var result = new List<string>();
                if (obj["versions"] is JArray arr)
                    foreach (var v in arr)
                        result.Add(v.ToString());
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[OnlineFirmwareService] GetVersions({deviceType}) failed: {ex.Message}");
                return new List<string>();
            }
        }

        // В отличие от GetVersionsAsync - не глотает исключения молча: если версия выбрана
        // явно пользователем, а скачать/распарсить не удалось, вызывающий код должен
        // сообщить об этом, а не тихо ничего не залить.
        public static async Task<(byte[] Data, uint FlashBase)> DownloadFirmwareAsync(int deviceType, string version)
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(TimeoutSeconds));
            using var http = new HttpClient();

            var profileText = await http.GetStringAsync($"{BaseUrl}/firmware/{deviceType}/{version}/profile", cts.Token);
            uint? flashBase = null;
            foreach (var line in profileText.Split('\n'))
            {
                var trimmed = line.Trim();
                if (!trimmed.StartsWith("flashBase=", StringComparison.OrdinalIgnoreCase)) continue;
                var hex = trimmed.Substring("flashBase=".Length).Trim();
                if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) hex = hex[2..];
                if (uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var parsed))
                    flashBase = parsed;
            }
            if (flashBase == null)
                throw new InvalidOperationException($"Server did not report flashBase for {deviceType}/{version}");

            var data = await http.GetByteArrayAsync($"{BaseUrl}/firmware/{deviceType}/{version}/firmware.bin");
            return (data, flashBase.Value);
        }
    }
}
