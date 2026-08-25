using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PostalApp_Extra
{
    // ===================================================================
    // Запись о файле из версионного манифеста
    // ===================================================================

    public class VersionFileEntry
    {
        public string Path { get; set; } = "";
        public string Md5 { get; set; } = "";
        public long Size { get; set; }
    }

    // ===================================================================
    // Мета-информация версии (для releaseNotes)
    // ===================================================================

    public class VersionInfo
    {
        public string Version { get; set; } = "";
        public string ReleaseDate { get; set; } = "";
        public string ReleaseNotes { get; set; } = "";
    }

    // ===================================================================
    // Модель глобального манифеста
    //
    // Формат manifest.json:
    // {
    //   "versions":    ["0.44", "0.43 beta", "0.43"],  ← первый = самый новый
    //   "downloadUrl": "http://...",
    //   "fileSize":    12345678,
    //   "checksum":    "abc123..."
    // }
    //
    // Формат версии: "X.X" или "X.X слово" (например "0.43 alpha").
    // Версия поддерживается если она есть в массиве versions (точное совпадение).
    // Последняя версия = versions[0].
    //
    // Порядок суффиксов (от старого к новому):
    //   alpha < beta < rc < (без суффикса / release)
    // ===================================================================

    public class UpdateInfo
    {
        public List<string> Versions { get; set; } = new List<string>();
        public string DownloadUrl { get; set; } = "";
        public long FileSize { get; set; }
        public string Checksum { get; set; } = "";

        public string LatestVersion => Versions.Count > 0 ? Versions[0] : "";
        public string ReleaseNotes { get; set; } = "";
        public bool IsUpdateAvailable { get; set; }
        public bool IsCurrentVersionSupported { get; set; } = true;
    }

    // ===================================================================
    // Менеджер обновлений PostalApp-Extra
    // ===================================================================

    public static class UpdateManagerExtra
    {
        private const string ManifestUrl =
            "http://81.90.25.60/extra-updates/manifest.json";

        private const string VersionManifestUrlTemplate =
            "http://81.90.25.60/extra-updates/versions/v{0}/version_manifest.json";

        private static UpdateInfo _cachedManifest;
        private static string _downloadedPath;

        // -------------------------------------------------------------------
        // ТЕКУЩАЯ ВЕРСИЯ И MD5 EXE
        // -------------------------------------------------------------------

        public static string GetCurrentVersion() => Program.version;

        /// <summary>
        /// Считает MD5 текущего .exe файла для отправки на сервер.
        /// </summary>
        public static string GetExeMd5()
        {
            try { return CalculateMD5(Application.ExecutablePath); }
            catch { return ""; }
        }

        // -------------------------------------------------------------------
        // ГЛОБАЛЬНЫЙ МАНИФЕСТ
        // -------------------------------------------------------------------

        public static async Task<UpdateInfo> FetchManifest()
        {
            if (_cachedManifest != null) return _cachedManifest;

            using (var http = new HttpClient())
            {
                http.Timeout = TimeSpan.FromSeconds(10);
                string json = await http.GetStringAsync(ManifestUrl);
                _cachedManifest = ParseManifest(json);
                return _cachedManifest;
            }
        }

        public static void InvalidateCache() => _cachedManifest = null;

        // -------------------------------------------------------------------
        // ПРОВЕРКА ОБНОВЛЕНИЙ
        // -------------------------------------------------------------------

        public static async Task<UpdateInfo> CheckForUpdates()
        {
            var info = await FetchManifest();
            string cur = GetCurrentVersion();

            info.IsUpdateAvailable = info.Versions.Count > 0 &&
                                             IsNewerVersion(cur, info.LatestVersion);
            info.IsCurrentVersionSupported = info.Versions.Contains(cur);

            if (info.IsUpdateAvailable)
            {
                try
                {
                    var vi = await GetVersionInfo(info.LatestVersion);
                    info.ReleaseNotes = vi.ReleaseNotes;
                }
                catch { info.ReleaseNotes = ""; }
            }

            return info;
        }

        // -------------------------------------------------------------------
        // ВЕРСИОННЫЙ МАНИФЕСТ
        // -------------------------------------------------------------------

        public static async Task<VersionInfo> GetVersionInfo(string version)
        {
            string url = string.Format(VersionManifestUrlTemplate, VersionToUrlSegment(version));
            using (var http = new HttpClient())
            {
                http.Timeout = TimeSpan.FromSeconds(15);
                string json = await http.GetStringAsync(url);
                return ParseVersionInfo(json);
            }
        }

        /// <summary>
        /// Скачивает version_manifest.json для указанной версии
        /// и возвращает список файлов с md5 и размером.
        /// Используется IntegrityChecker для проверки целостности.
        /// </summary>
        public static async Task<List<VersionFileEntry>> GetVersionManifest(string version)
        {
            string url = string.Format(VersionManifestUrlTemplate, VersionToUrlSegment(version));
            using (var http = new HttpClient())
            {
                http.Timeout = TimeSpan.FromSeconds(15);
                string json = await http.GetStringAsync(url);
                return ParseVersionFileEntries(json);
            }
        }

        private static List<VersionFileEntry> ParseVersionFileEntries(string json)
        {
            var list = new List<VersionFileEntry>();

            var mArray = Regex.Match(json, "\"files\"\\s*:\\s*\\[(.*?)\\]",
                RegexOptions.Singleline);
            if (!mArray.Success) return list;

            string arrayContent = mArray.Groups[1].Value;

            var objects = Regex.Matches(arrayContent, "\\{([^}]+)\\}",
                RegexOptions.Singleline);

            foreach (Match obj in objects)
            {
                string content = obj.Groups[1].Value;
                string path = ExtractString(content, "path");
                string md5 = ExtractString(content, "md5");
                string sizeStr = ExtractRaw(content, "size");
                long.TryParse(sizeStr, out long size);

                if (!string.IsNullOrEmpty(path))
                    list.Add(new VersionFileEntry { Path = path, Md5 = md5, Size = size });
            }

            return list;
        }

        // -------------------------------------------------------------------
        // СКАЧИВАНИЕ ОБНОВЛЕНИЯ
        // -------------------------------------------------------------------

        public static async Task<bool> DownloadUpdate(UpdateInfo info, IProgress<int> progress)
        {
            try
            {
                _downloadedPath = System.IO.Path.Combine(
                    System.IO.Path.GetTempPath(),
                    $"postalapp_extra_update_{info.LatestVersion}.zip");

                using (var http = new HttpClient())
                {
                    http.Timeout = TimeSpan.FromMinutes(15);
                    using (var response = await http.GetAsync(
                        info.DownloadUrl, HttpCompletionOption.ResponseHeadersRead))
                    {
                        response.EnsureSuccessStatusCode();
                        long totalBytes = response.Content.Headers.ContentLength ?? -1L;
                        long downloaded = 0;
                        byte[] buffer = new byte[8192];

                        using (var src = await response.Content.ReadAsStreamAsync())
                        using (var dest = new FileStream(_downloadedPath, FileMode.Create,
                            FileAccess.Write, FileShare.None, 8192, true))
                        {
                            int read;
                            while ((read = await src.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            {
                                await dest.WriteAsync(buffer, 0, read);
                                downloaded += read;
                                if (totalBytes > 0)
                                    progress?.Report((int)(downloaded * 100 / totalBytes));
                            }
                        }
                    }
                }

                if (!string.IsNullOrEmpty(info.Checksum))
                {
                    string actual = CalculateMD5(_downloadedPath);
                    if (!string.Equals(actual, info.Checksum, StringComparison.OrdinalIgnoreCase))
                    {
                        File.Delete(_downloadedPath);
                        return false;
                    }
                }

                return true;
            }
            catch { return false; }
        }

        // -------------------------------------------------------------------
        // ПРИМЕНЕНИЕ ОБНОВЛЕНИЯ через batch-скрипт
        // -------------------------------------------------------------------

        public static async Task<bool> ApplyUpdate(string zipPath = null)
        {
            string path = zipPath ?? _downloadedPath;
            try
            {
                string tempDir = System.IO.Path.Combine(
                    System.IO.Path.GetTempPath(), "postalapp_extra_update_temp");

                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
                Directory.CreateDirectory(tempDir);

                await Task.Run(() => ZipFile.ExtractToDirectory(path, tempDir));

                string appPath = Application.StartupPath;
                int pid = Process.GetCurrentProcess().Id;
                string exePath = System.IO.Path.Combine(appPath, "PostalApp-Extra.exe");

                var sb = new StringBuilder();
                sb.AppendLine("@echo off");
                sb.AppendLine("");
                sb.AppendLine($"rem Ждём завершения процесса PID={pid}");
                sb.AppendLine(":waitloop");
                sb.AppendLine($"tasklist /FI \"PID eq {pid}\" 2>NUL | find \"{pid}\" > NUL");
                sb.AppendLine("if not errorlevel 1 (");
                sb.AppendLine("    timeout /t 1 /nobreak > nul");
                sb.AppendLine("    goto waitloop");
                sb.AppendLine(")");
                sb.AppendLine("");
                sb.AppendLine("rem Копируем файлы обновления");

                foreach (string file in Directory.GetFiles(tempDir, "*", SearchOption.AllDirectories))
                {
                    string relative = file.Substring(tempDir.Length)
                        .TrimStart(System.IO.Path.DirectorySeparatorChar);

                    if (relative.Equals("config.dat", StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (relative.StartsWith("logs" + System.IO.Path.DirectorySeparatorChar,
                        StringComparison.OrdinalIgnoreCase))
                        continue;

                    string destFile = System.IO.Path.Combine(appPath, relative);
                    string destDir = System.IO.Path.GetDirectoryName(destFile);
                    string safeDestDir = destDir.Replace("%", "%%");
                    string safeSrc = file.Replace("%", "%%");
                    string safeDest = destFile.Replace("%", "%%");

                    sb.AppendLine($"if not exist \"{safeDestDir}\" mkdir \"{safeDestDir}\"");
                    sb.AppendLine($"copy /Y \"{safeSrc}\" \"{safeDest}\" > nul");
                }

                sb.AppendLine("");
                sb.AppendLine("rem Удаляем временную папку");
                sb.AppendLine($"rd /S /Q \"{tempDir}\"");
                sb.AppendLine("");
                sb.AppendLine("rem Перезапускаем приложение");
                sb.AppendLine($"start \"\" \"{exePath}\"");
                sb.AppendLine("");
                sb.AppendLine("rem Самоудаление батника");
                sb.AppendLine("del \"%~f0\"");

                string batchPath = System.IO.Path.Combine(
                    System.IO.Path.GetTempPath(), "postalapp_extra_apply_update.bat");

                File.WriteAllText(batchPath, sb.ToString(), Encoding.GetEncoding(866));

                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/C \"{batchPath}\"",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    UseShellExecute = true
                };
                Process.Start(psi);

                try { if (File.Exists(path)) File.Delete(path); } catch { }

                return true;
            }
            catch { return false; }
        }

        // -------------------------------------------------------------------
        // СРАВНЕНИЕ ВЕРСИЙ
        //
        // Формат: "X.X" или "X.X суффикс"
        // Примеры: "0.43", "0.43 alpha", "0.43 beta", "0.43 rc"
        //
        // Порядок суффиксов (от старого к новому):
        //   unknown < alpha < beta < rc < "" (release)
        //
        // IsNewerVersion(current, candidate) → true если candidate > current
        // -------------------------------------------------------------------

        public static bool IsNewerVersion(string current, string candidate)
        {
            try
            {
                var (cn, cl) = SplitVersion(current);
                var (dn, dl) = SplitVersion(candidate);

                for (int i = 0; i < 2; i++)
                {
                    if (dn[i] > cn[i]) return true;
                    if (dn[i] < cn[i]) return false;
                }

                // Числовые части равны — сравниваем суффиксы
                return LabelRank(dl) > LabelRank(cl);
            }
            catch { return false; }
        }

        // ===================================================================
        // ПАРСЕРЫ
        // ===================================================================

        /// <summary>
        /// Разбивает версию формата "X.X суффикс" на числовую часть и суффикс.
        /// Примеры: "0.43 alpha" → ([0,43], "alpha"), "0.44" → ([0,44], "")
        /// </summary>
        private static (int[] nums, string label) SplitVersion(string v)
        {
            int spIdx = v.IndexOf(' ');
            string numPart = spIdx >= 0 ? v.Substring(0, spIdx) : v;
            string label = spIdx >= 0 ? v.Substring(spIdx + 1).Trim() : "";

            var parts = numPart.Split('.');
            var nums = new int[2];
            for (int i = 0; i < 2 && i < parts.Length; i++)
                int.TryParse(parts[i], out nums[i]);

            return (nums, label);
        }

        /// <summary>
        /// Числовой ранг суффикса для сравнения: чем больше — тем новее.
        /// "" (release) > "rc" > "beta" > "alpha" > прочие
        /// </summary>
        private static int LabelRank(string label)
        {
            switch (label.ToLowerInvariant())
            {
                case "": return 4;
                case "rc": return 3;
                case "beta": return 2;
                case "alpha": return 1;
                default: return 0;
            }
        }

        /// <summary>
        /// Кодирует версию для вставки в URL: пробел → %20.
        /// "0.43 alpha" → "0.43%20alpha"
        /// </summary>
        private static string VersionToUrlSegment(string version)
        {
            return version.Replace(" ", "%20");
        }

        private static UpdateInfo ParseManifest(string json)
        {
            var info = new UpdateInfo
            {
                Versions = ParseVersionsArray(json),
                DownloadUrl = ExtractString(json, "downloadUrl"),
                Checksum = ExtractString(json, "checksum")
            };
            if (long.TryParse(ExtractRaw(json, "fileSize"), out long fs)) info.FileSize = fs;
            return info;
        }

        private static List<string> ParseVersionsArray(string json)
        {
            var list = new List<string>();
            var m = Regex.Match(json, "\"versions\"\\s*:\\s*\\[([^\\]]*)\\]");
            if (!m.Success) return list;
            foreach (Match item in Regex.Matches(m.Groups[1].Value, "\"([^\"]+)\""))
                list.Add(item.Groups[1].Value);
            return list;
        }

        private static VersionInfo ParseVersionInfo(string json)
        {
            return new VersionInfo
            {
                Version = ExtractString(json, "version"),
                ReleaseDate = ExtractString(json, "releaseDate"),
                ReleaseNotes = ExtractString(json, "releaseNotes")
            };
        }

        private static string ExtractString(string json, string key)
        {
            var m = Regex.Match(json, $"\"{Regex.Escape(key)}\"\\s*:\\s*\"([^\"]+)\"");
            return m.Success ? m.Groups[1].Value : "";
        }

        private static string ExtractRaw(string json, string key)
        {
            var m = Regex.Match(json, $"\"{Regex.Escape(key)}\"\\s*:\\s*(\\d+)");
            return m.Success ? m.Groups[1].Value : "";
        }

        // ===================================================================
        // MD5
        // ===================================================================

        internal static string CalculateMD5(string filePath)
        {
            using (var md5 = MD5.Create())
            using (var stream = File.OpenRead(filePath))
            {
                byte[] hash = md5.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}