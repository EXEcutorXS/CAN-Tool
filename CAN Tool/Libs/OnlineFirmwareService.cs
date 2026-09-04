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

        // Один HttpClient на всё приложение - конструктор DeviceViewModel запрашивает список
        // версий на каждое появление устройства (в т.ч. на каждый переход в загрузчик/обратно
        // во время прошивки), так что создавать/уничтожать HttpClient на каждый вызов - лишний
        // расход сокетов при частых обновлениях.
        private static readonly HttpClient Http = new();

        // Локальный кэш списков версий по типам устройства - один JSON-файл в папке
        // программы, ключ - номер типа. Не про надёжность (сервер и так публичный и почти
        // всегда доступен), а про то, чтобы при запуске программы список не был пустым, пока
        // идёт сетевой запрос (см. DeviceViewModel.RefreshAvailableServerFirmwaresAsync -
        // сначала читает отсюда синхронно и сразу показывает, потом обновляет с сервера).
        private static readonly string CacheFilePath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "firmware_versions_cache.json");
        private static readonly object CacheLock = new();

        public static List<string> LoadCachedVersions(int deviceType)
        {
            lock (CacheLock)
            {
                try
                {
                    if (!File.Exists(CacheFilePath)) return new List<string>();
                    var obj = JObject.Parse(File.ReadAllText(CacheFilePath));
                    if (obj[deviceType.ToString()] is JArray arr)
                        return arr.Select(v => v.ToString()).ToList();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[OnlineFirmwareService] LoadCachedVersions({deviceType}) failed: {ex.Message}");
                }
                return new List<string>();
            }
        }

        private static void SaveCachedVersions(int deviceType, List<string> versions)
        {
            lock (CacheLock)
            {
                try
                {
                    JObject obj = null;
                    if (File.Exists(CacheFilePath))
                    {
                        try { obj = JObject.Parse(File.ReadAllText(CacheFilePath)); }
                        catch { /* повреждённый кэш - просто перезапишем целиком */ }
                    }
                    obj ??= new JObject();
                    obj[deviceType.ToString()] = new JArray(versions);
                    File.WriteAllText(CacheFilePath, obj.ToString(Formatting.Indented));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[OnlineFirmwareService] SaveCachedVersions({deviceType}) failed: {ex.Message}");
                }
            }
        }

        // Возвращает null при сетевой ошибке (в отличие от пустого списка от сервера) - чтобы
        // вызывающий код мог отличить "сеть недоступна, оставляем то, что уже показали из
        // кэша" от "сервер ответил, но версий для этого типа действительно нет".
        public static async Task<List<string>> GetVersionsAsync(int deviceType)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(TimeoutSeconds));
                var json = await Http.GetStringAsync($"{BaseUrl}/firmware/{deviceType}/versions", cts.Token);
                var obj = JObject.Parse(json);
                var result = new List<string>();
                if (obj["versions"] is JArray arr)
                    foreach (var v in arr)
                        result.Add(v.ToString());
                SaveCachedVersions(deviceType, result);
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[OnlineFirmwareService] GetVersions({deviceType}) failed: {ex.Message}");
                return null;
            }
        }

        // В отличие от GetVersionsAsync - не глотает исключения молча: если версия выбрана
        // явно пользователем, а скачать/распарсить не удалось, вызывающий код должен
        // сообщить об этом, а не тихо ничего не залить.
        public static async Task<(byte[] Data, uint FlashBase)> DownloadFirmwareAsync(int deviceType, string version)
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(TimeoutSeconds));

            var profileText = await Http.GetStringAsync($"{BaseUrl}/firmware/{deviceType}/{version}/profile", cts.Token);
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

            var data = await Http.GetByteArrayAsync($"{BaseUrl}/firmware/{deviceType}/{version}/firmware.bin", cts.Token);
            return (data, flashBase.Value);
        }
    }
}
