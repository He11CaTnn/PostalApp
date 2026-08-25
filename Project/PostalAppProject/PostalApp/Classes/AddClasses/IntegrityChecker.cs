using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PostalApp
{
    // ===================================================================
    // Статус файла после проверки
    // ===================================================================

    public enum FileStatus
    {
        OK,
        Missing,
        Corrupted
    }

    // ===================================================================
    // Результат проверки одного файла
    // ===================================================================

    public class FileIntegrityResult
    {
        public string Path { get; set; }
        public FileStatus Status { get; set; }
        public string ExpectedMd5 { get; set; }
        public string ActualMd5 { get; set; }

        public string StatusText
        {
            get
            {
                switch (Status)
                {
                    case FileStatus.OK: return "OK";
                    case FileStatus.Missing: return "Отсутствует";
                    case FileStatus.Corrupted: return "Повреждён";
                    default: return "Неизвестно";
                }
            }
        }
    }

    // ===================================================================
    // Итоговый отчёт проверки всех файлов
    // ===================================================================

    public class IntegrityReport
    {
        public List<FileIntegrityResult> Results { get; }

        public IntegrityReport(List<FileIntegrityResult> results)
        {
            Results = results ?? new List<FileIntegrityResult>();
        }

        public int TotalCount => Results.Count;
        public int OkCount => Results.Count(r => r.Status == FileStatus.OK);
        public int MissingCount => Results.Count(r => r.Status == FileStatus.Missing);
        public int CorruptedCount => Results.Count(r => r.Status == FileStatus.Corrupted);
        public bool HasErrors => MissingCount > 0 || CorruptedCount > 0;

        public IEnumerable<FileIntegrityResult> BadFiles =>
            Results.Where(r => r.Status != FileStatus.OK);
    }

    // ===================================================================
    // Прогресс-данные для отчёта о текущем файле
    // ===================================================================

    public struct IntegrityProgress
    {
        public int Current { get; set; }
        public int Total { get; set; }
        public string FileName { get; set; }
    }

    // ===================================================================
    // Класс полной проверки целостности файлов
    //
    // ИСПОЛЬЗОВАНИЕ:
    //   var form = new IntegrityCheckForm();
    //   form.Show();
    //   // форма сама запустит проверку через OnLoad
    //
    // Или напрямую:
    //   var report = await IntegrityChecker.CheckAllFiles("2.2.0", progress);
    // ===================================================================

    public static class IntegrityChecker
    {
        /// <summary>
        /// Проверяет все файлы текущей версии приложения по версионному манифесту.
        /// Скачивает version_manifest.json с сервера, затем сверяет каждый файл.
        /// </summary>
        /// <param name="version">Версия приложения, например "2.2.0"</param>
        /// <param name="progress">Прогресс (current, total, fileName)</param>
        public static async Task<IntegrityReport> CheckAllFiles(
            string version,
            IProgress<IntegrityProgress> progress = null)
        {
            // Скачиваем список файлов из версионного манифеста
            List<VersionFileEntry> entries;
            try
            {
                entries = await UpdateManager.GetVersionManifest(version);
            }
            catch (Exception ex)
            {
                Logger.Error($"Не удалось загрузить версионный манифест для v{version}", ex);
                throw; // пробрасываем — форма обработает
            }

            if (entries == null || entries.Count == 0)
            {
                Logger.Warning($"Версионный манифест для v{version} пуст или не содержит файлов");
                return new IntegrityReport(new List<FileIntegrityResult>());
            }

            var results = new List<FileIntegrityResult>();
            int total = entries.Count;

            for (int i = 0; i < total; i++)
            {
                var entry = entries[i];

                // Сообщаем прогресс
                progress?.Report(new IntegrityProgress
                {
                    Current = i + 1,
                    Total = total,
                    FileName = entry.Path
                });

                // Уступаем поток UI каждые 5 файлов
                if (i % 5 == 0) await Task.Delay(1);

                string localPath = Path.Combine(Application.StartupPath, entry.Path);

                if (!File.Exists(localPath))
                {
                    results.Add(new FileIntegrityResult
                    {
                        Path = entry.Path,
                        Status = FileStatus.Missing,
                        ExpectedMd5 = entry.Md5,
                        ActualMd5 = ""
                    });
                    continue;
                }

                // MD5 считаем в фоне чтобы не вешать UI
                string actualMd5 = await Task.Run(() => UpdateManager.CalculateMD5(localPath));
                bool matches = string.Equals(actualMd5, entry.Md5,
                                       StringComparison.OrdinalIgnoreCase);

                results.Add(new FileIntegrityResult
                {
                    Path = entry.Path,
                    Status = matches ? FileStatus.OK : FileStatus.Corrupted,
                    ExpectedMd5 = entry.Md5,
                    ActualMd5 = actualMd5
                });
            }

            var report = new IntegrityReport(results);
            Logger.Info($"Проверка целостности завершена: OK={report.OkCount}, " +
                        $"Отсутствует={report.MissingCount}, Повреждено={report.CorruptedCount}");
            return report;
        }
    }
}
