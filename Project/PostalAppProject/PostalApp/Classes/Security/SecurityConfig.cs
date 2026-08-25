using System;
using System.Globalization;
using System.IO;
using System.Management;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PostalApp
{
    // ===================================================================
    // Исключения для специфических ответов сервера
    // ===================================================================

    /// <summary>
    /// Версия приложения устарела — сервер требует обновления (HTTP 426).
    /// </summary>
    public class VersionTooOldException : Exception
    {
        public VersionTooOldException(string message) : base(message) { }
    }

    /// <summary>
    /// Целостность файлов нарушена — сервер отклонил запрос (HTTP 403).
    /// </summary>
    public class IntegrityFailedException : Exception
    {
        public IntegrityFailedException(string message) : base(message) { }
    }

    /// <summary>
    /// Превышен лимит попыток входа — сервер вернул HTTP 429.
    /// RetryAfter — сколько секунд ждать.
    /// </summary>
    public class RateLimitException : Exception
    {
        public int RetryAfter { get; }
        public RateLimitException(int retryAfter)
            : base($"Слишком много попыток. Попробуйте через {retryAfter} сек.")
        {
            RetryAfter = retryAfter;
        }
    }

    // ===================================================================
    // SecureConfig
    // ===================================================================

    public static class SecurityConfig
    {
        // ===================================================================
        // КОНСТАНТЫ
        // ===================================================================

        private const string ExpectedCertFingerprint =
            "BC14A0466B54BFB96C9F2B116C519104B9B357374A50DF08FB537C496016008D";

        private const string ConfigApiUrl      = "https://81.90.25.60/api/getconfig";
        private const string CheckDeviceApiUrl = "https://81.90.25.60/api/checkdevice";

        // ===================================================================
        // МОДЕЛЬ КОНФИГУРАЦИИ
        // ===================================================================

        public class ServerConfig
        {
            public string ServerIP       { get; set; }
            public int    ServerPort     { get; set; }
            public string ServerDatabase { get; set; }
            public string ServerUser     { get; set; }
            public string ServerPassword { get; set; }

            // Координаты населённого пункта (по умолчанию — Москва)
            public double Lat { get; set; } = 55.7522;
            public double Lng { get; set; } = 37.6156;

            public ServerConfig() { }

            public ServerConfig(string ip, int port, string database, string user, string password)
            {
                ServerIP       = ip;
                ServerPort     = port;
                ServerDatabase = database;
                ServerUser     = user;
                ServerPassword = password;
            }

            public static ServerConfig FromConfigString(string s)
            {
                var parts = s.Split('|');
                if (parts.Length < 5)
                    throw new Exception("Неверный формат конфигурации");

                return new ServerConfig
                {
                    ServerIP       = parts[0],
                    ServerPort     = int.Parse(parts[1]),
                    ServerDatabase = parts[2],
                    ServerUser     = parts[3],
                    ServerPassword = parts[4]
                };
            }
        }

        // ===================================================================
        // ИДЕНТИФИКАЦИЯ УСТРОЙСТВА
        // ===================================================================

        public static string GetMotherboardId()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher(
                    "SELECT SerialNumber FROM Win32_BaseBoard"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        string serial = obj["SerialNumber"]?.ToString()?.Trim();
                        if (!string.IsNullOrEmpty(serial) &&
                            serial != "To be filled by O.E.M." &&
                            serial != "Default string")
                        {
                            return serial;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warning("Не удалось получить ID материнской платы: " + ex.Message);
            }
            return null;
        }

        // ===================================================================
        // MD5 EXE-ФАЙЛА
        // ===================================================================

        public static string GetExeMd5()
        {
            try
            {
                string exePath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, "PostalApp.exe");

                if (!File.Exists(exePath))
                {
                    Logger.Warning("GetExeMd5: PostalApp.exe не найден");
                    return "";
                }

                using (var md5 = MD5.Create())
                using (var stream = File.OpenRead(exePath))
                {
                    byte[] hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash)
                        .Replace("-", "").ToLowerInvariant();
                }
            }
            catch (Exception ex)
            {
                Logger.Warning("GetExeMd5: ошибка — " + ex.Message);
                return "";
            }
        }

        // ===================================================================
        // ВСПОМОГАТЕЛЬНЫЙ МЕТОД: разобрать retry_after из тела 429
        // ===================================================================

        /// <summary>
        /// Парсит поле retry_after из JSON тела ответа 429.
        /// Пример тела: {"detail":{"reason":"rate_limit","retry_after":47}}
        /// </summary>
        private static int ParseRetryAfter(string body)
        {
            var m = Regex.Match(body, "\"retry_after\"\\s*:\\s*(\\d+)");
            if (m.Success && int.TryParse(m.Groups[1].Value, out int seconds))
                return seconds;
            return 60; // fallback — 60 секунд если не удалось разобрать
        }

        // ===================================================================
        // API: ВХОД ПО ЛОГИНУ И ПАРОЛЮ
        // ===================================================================

        /// <summary>
        /// Отправляет логин, пароль, версию и MD5 exe на /api/getconfig.
        /// Возвращает ServerConfig с заполненными координатами (если сервер вернул lat/lng).
        ///
        /// Бросает:
        ///   RateLimitException          — превышен лимит попыток (429), RetryAfter = секунды
        ///   UnauthorizedAccessException — неверный логин или пароль (401)
        ///   VersionTooOldException      — версия устарела (426)
        ///   IntegrityFailedException    — файлы изменены (403)
        ///   Exception                   — ошибка соединения
        /// </summary>
        public static async Task<ServerConfig> FetchConfigFromServer(string login, string password)
        {
            string version = UpdateManager.GetCurrentVersion();
            string exeMd5  = GetExeMd5();

            using (var http = new HttpClient(CreateSslHandler()))
            {
                http.Timeout = TimeSpan.FromSeconds(15);

                string json = $"{{" +
                    $"\"login\":\"{EscapeJson(login)}\"," +
                    $"\"password\":\"{EscapeJson(password)}\"," +
                    $"\"version\":\"{EscapeJson(version)}\"," +
                    $"\"exe_md5\":\"{EscapeJson(exeMd5)}\"" +
                    $"}}";
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response;
                try
                {
                    response = await http.PostAsync(ConfigApiUrl, content);
                }
                catch (HttpRequestException ex)
                {
                    Logger.Error("Ошибка подключения к серверу конфигурации", ex);
                    throw new Exception("Не удалось подключиться к серверу. Проверьте интернет-соединение.");
                }

                string body = await response.Content.ReadAsStringAsync();

                // 429 — превышен лимит попыток
                if ((int)response.StatusCode == 429)
                {
                    int retryAfter = ParseRetryAfter(body);
                    Logger.Warning($"Rate limit: ждать {retryAfter} сек.");
                    throw new RateLimitException(retryAfter);
                }

                // 426 — версия устарела
                if ((int)response.StatusCode == 426)
                {
                    Logger.Warning($"Сервер: версия {version} устарела");
                    throw new VersionTooOldException("Версия приложения устарела. Необходимо обновление.");
                }

                // 403 — целостность нарушена
                if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    Logger.Error("Сервер: проверка целостности файлов не пройдена");
                    throw new IntegrityFailedException("Файлы приложения повреждены или модифицированы.");
                }

                // 401 — неверный логин/пароль
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    Logger.Warning("Введён неверный логин или пароль");
                    throw new UnauthorizedAccessException("Неверный логин или пароль");
                }

                response.EnsureSuccessStatusCode();

                var m = Regex.Match(body, "\"config\"\\s*:\\s*\"([^\"]+)\"");
                if (!m.Success)
                    throw new Exception("Сервер вернул неожиданный формат ответа");

                var cfg = ServerConfig.FromConfigString(m.Groups[1].Value);
                ParseAndApplyCoords(body, cfg);

                Logger.Info("Конфигурация успешно получена с сервера");
                return cfg;
            }
        }

        // ===================================================================
        // API: АВТОВХОД ПО ID МАТЕРИНСКОЙ ПЛАТЫ
        // ===================================================================

        /// <summary>
        /// Запрашивает конфигурацию по ID материнской платы.
        ///
        /// Бросает:
        ///   RateLimitException       — IP заблокирован по лимиту ручного входа (429)
        ///   VersionTooOldException   — версия устарела (426)
        ///   IntegrityFailedException — файлы изменены (403)
        /// Возвращает null если устройство не найдено / нет соединения.
        /// </summary>
        public static async Task<ServerConfig> FetchConfigByMotherboardId(string motherboardId)
        {
            if (string.IsNullOrEmpty(motherboardId))
                return null;

            string version = UpdateManager.GetCurrentVersion();
            string exeMd5  = GetExeMd5();

            using (var http = new HttpClient(CreateSslHandler()))
            {
                http.Timeout = TimeSpan.FromSeconds(10);

                string json = $"{{" +
                    $"\"motherboard_id\":\"{EscapeJson(motherboardId)}\"," +
                    $"\"version\":\"{EscapeJson(version)}\"," +
                    $"\"exe_md5\":\"{EscapeJson(exeMd5)}\"" +
                    $"}}";
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response;
                try
                {
                    response = await http.PostAsync(CheckDeviceApiUrl, content);
                }
                catch (HttpRequestException ex)
                {
                    Logger.Warning("Автовход: нет соединения с сервером: " + ex.Message);
                    return null;
                }

                string body = await response.Content.ReadAsStringAsync();

                // 429 — IP заблокирован по лимиту ручного входа
                if ((int)response.StatusCode == 429)
                {
                    int retryAfter = ParseRetryAfter(body);
                    Logger.Warning($"Автовход: IP заблокирован по rate limit, ждать {retryAfter} сек.");
                    throw new RateLimitException(retryAfter);
                }

                // 426 — версия устарела
                if ((int)response.StatusCode == 426)
                {
                    Logger.Warning($"Автовход: версия {version} устарела");
                    throw new VersionTooOldException("Версия приложения устарела. Необходимо обновление.");
                }

                // 403 — целостность нарушена
                if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    Logger.Error("Автовход: проверка целостности файлов не пройдена");
                    throw new IntegrityFailedException("Файлы приложения повреждены или модифицированы.");
                }

                // Устройство не найдено
                if (!response.IsSuccessStatusCode)
                {
                    Logger.Info("Автовход: устройство не зарегистрировано или нет доступа");
                    return null;
                }

                var m = Regex.Match(body, "\"config\"\\s*:\\s*\"([^\"]+)\"");
                if (!m.Success)
                    return null;

                var cfg = ServerConfig.FromConfigString(m.Groups[1].Value);
                ParseAndApplyCoords(body, cfg);

                Logger.Info("Конфигурация по ID устройства успешно получена");
                return cfg;
            }
        }

        // ===================================================================
        // ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ
        // ===================================================================

        /// <summary>
        /// Парсит lat/lng из JSON-тела ответа и записывает в cfg.
        /// Если значения отсутствуют или некорректны — cfg.Lat/Lng остаются дефолтными (Москва).
        /// </summary>
        private static void ParseAndApplyCoords(string body, ServerConfig cfg)
        {
            var mLat = Regex.Match(body, "\"lat\"\\s*:\\s*([\\d\\.\\-]+)");
            var mLng = Regex.Match(body, "\"lng\"\\s*:\\s*([\\d\\.\\-]+)");
            if (mLat.Success && mLng.Success &&
                double.TryParse(mLat.Groups[1].Value, NumberStyles.Float,
                                CultureInfo.InvariantCulture, out double lat) &&
                double.TryParse(mLng.Groups[1].Value, NumberStyles.Float,
                                CultureInfo.InvariantCulture, out double lng) &&
                lat >= -90 && lat <= 90 && lng >= -180 && lng <= 180)
            {
                cfg.Lat = lat;
                cfg.Lng = lng;
            }
        }

        private static HttpClientHandler CreateSslHandler()
        {
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
            {
                string actual = GetCertFingerprint(cert.RawData);
                bool ok = string.Equals(actual, ExpectedCertFingerprint,
                    StringComparison.OrdinalIgnoreCase);
                if (!ok)
                    Logger.Error($"SSL Pinning отклонён. Получен fingerprint: {actual}");
                return ok;
            };
            return handler;
        }

        private static string GetCertFingerprint(byte[] rawData)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(rawData);
                return BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();
            }
        }

        private static string EscapeJson(string s) =>
            s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
