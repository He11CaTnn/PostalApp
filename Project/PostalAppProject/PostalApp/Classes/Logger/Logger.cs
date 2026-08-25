using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PostalApp
{
    /// <summary>
    /// Централизованная система логирования и отображения сообщений.
    /// Заменяет все MessageBox.Show() и добавляет запись в файл.
    /// </summary>
    public static class Logger
    {
        // ===================================================================
        // НАСТРОЙКИ
        // ===================================================================

        /// <summary>
        /// Папка для хранения логов (рядом с .exe файлом)
        /// </summary>
        private static string LogDirectory
        {
            get
            {
                string appPath = AppDomain.CurrentDomain.BaseDirectory;
                return Path.Combine(appPath, "logs");
            }
        }

        /// <summary>
        /// Имя файла лога (один файл на день)
        /// </summary>
        private static string LogFileName => $"log_{DateTime.Now:yyyy-MM-dd}.log";

        /// <summary>
        /// Полный путь к текущему файлу лога
        /// </summary>
        private static string LogFilePath => Path.Combine(LogDirectory, LogFileName);

        /// <summary>
        /// Максимальный размер файла лога в МБ (при превышении создается новый файл)
        /// </summary>
        public static int MaxLogFileSizeMB { get; set; } = 10;

        /// <summary>
        /// Количество дней хранения логов (старые удаляются автоматически)
        /// </summary>
        public static int LogRetentionDays { get; set; } = 30;

        /// <summary>
        /// Минимальный уровень логирования (сообщения ниже этого уровня игнорируются)
        /// </summary>
        public static LogLevel MinLogLevel { get; set; } = LogLevel.Info;

        /// <summary>
        /// Включить/выключить логирование
        /// </summary>
        public static bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Показывать ли MessageBox (можно отключить для тестов)
        /// </summary>
        public static bool ShowDialogs { get; set; } = true;

        /// <summary>
        /// Объект для синхронизации записи в файл
        /// </summary>
        private static readonly object _lockObject = new object();

        // ===================================================================
        // ИНИЦИАЛИЗАЦИЯ
        // ===================================================================

        /// <summary>
        /// Статический конструктор - создает папку Logs при первом обращении
        /// </summary>
        static Logger()
        {
            try
            {
                if (!Directory.Exists(LogDirectory))
                    Directory.CreateDirectory(LogDirectory);

                // Очистка старых логов
                CleanOldLogs();
            }
            catch
            {
                // Если не удалось создать папку - логирование будет отключено
            }
        }

        // ===================================================================
        // МЕТОДЫ ЛОГИРОВАНИЯ БЕЗ UI
        // ===================================================================

        /// <summary>
        /// Отладочное сообщение (только для разработки).
        /// Используй для: детальной информации о работе приложения.
        /// Пример: Logger.Debug("Загружено маркеров: 150");
        /// </summary>
        public static void Debug(string message)
        {
            Log(LogLevel.Debug, message);
        }

        /// <summary>
        /// Информационное сообщение о нормальной работе.
        /// Используй для: успешных операций, важных событий.
        /// Пример: Logger.Info("Пользователь вошел в систему");
        /// </summary>
        public static void Info(string message)
        {
            Log(LogLevel.Info, message);
        }

        /// <summary>
        /// Предупреждение о потенциальной проблеме.
        /// Используй для: некритичных проблем, которые не мешают работе.
        /// Пример: Logger.Warning("Некоторые поля не заполнены");
        /// </summary>
        public static void Warning(string message)
        {
            Log(LogLevel.Warning, message);
        }

        /// <summary>
        /// Ошибка, которая не приводит к падению приложения.
        /// Используй для: обработанных исключений, неудачных операций.
        /// Пример: Logger.Error("Не удалось загрузить маркеры", ex);
        /// </summary>
        public static void Error(string message, Exception ex = null)
        {
            string stackTrace = ex?.ToString();
            Log(LogLevel.Error, message, stackTrace);
        }

        /// <summary>
        /// Критическая ошибка, которая может привести к падению приложения.
        /// Используй для: необработанных исключений, потери соединения с БД.
        /// Пример: Logger.Critical("Потеряно соединение с сервером", ex);
        /// </summary>
        public static void Critical(string message, Exception ex = null)
        {
            string stackTrace = ex?.ToString();
            Log(LogLevel.Critical, message, stackTrace);
        }

        // ===================================================================
        // МЕТОДЫ ЛОГИРОВАНИЯ + MESSAGEBOX
        // ===================================================================

        /// <summary>
        /// Показать информационное сообщение + записать в лог.
        /// Используй вместо: MessageBox.Show("Данные сохранены", "Информация", ...)
        /// Пример: Logger.ShowInfo("Данные успешно сохранены");
        /// </summary>
        public static void ShowInfo(string message, string title = "Информация")
        {
            Log(LogLevel.Info, $"[UI] {message}");

            if (ShowDialogs)
            {
                MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// Показать предупреждение + записать в лог.
        /// Используй вместо: MessageBox.Show("Внимание!", "Предупреждение", ...)
        /// Пример: Logger.ShowWarning("Некоторые поля не заполнены");
        /// </summary>
        public static void ShowWarning(string message, string title = "Предупреждение")
        {
            Log(LogLevel.Warning, $"[UI] {message}");

            if (ShowDialogs)
            {
                MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Показать ошибку + записать в лог.
        /// Используй вместо: MessageBox.Show("Ошибка!", "Ошибка", ...)
        /// Пример: Logger.ShowError("Не удалось загрузить данные");
        /// </summary>
        public static void ShowError(string message, string title = "Ошибка")
        {
            Log(LogLevel.Error, $"[UI] {message}");

            if (ShowDialogs)
            {
                MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Показать критическую ошибку + записать в лог.
        /// Используй для: критических ошибок, после которых приложение может закрыться.
        /// Пример: Logger.ShowCritical("Потеряно соединение с сервером");
        /// </summary>
        public static void ShowCritical(string message, string title = "Критическая ошибка")
        {
            Log(LogLevel.Critical, $"[UI] {message}");

            if (ShowDialogs)
            {
                MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ===================================================================
        // ДИАЛОГИ С ВЫБОРОМ
        // ===================================================================

        /// <summary>
        /// Показать вопрос с кнопками Да/Нет + записать в лог.
        /// Используй вместо: MessageBox.Show("Удалить?", "Вопрос", MessageBoxButtons.YesNo, ...)
        /// Пример: if (Logger.ShowYesNo("Удалить выбранный элемент?") == DialogResult.Yes) { ... }
        /// </summary>
        public static DialogResult ShowYesNo(string message, string title = "Подтверждение")
        {
            Log(LogLevel.Info, $"[UI Question] {message}");

            if (ShowDialogs)
            {
                var result = MessageBox.Show(message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                Log(LogLevel.Debug, $"[UI Answer] {result}");
                return result;
            }

            return DialogResult.No;
        }

        /// <summary>
        /// Показать вопрос с кнопками Да/Нет/Отмена + записать в лог.
        /// Пример: var result = Logger.ShowYesNoCancel("Сохранить изменения?");
        /// </summary>
        public static DialogResult ShowYesNoCancel(string message, string title = "Вопрос")
        {
            Log(LogLevel.Info, $"[UI Question] {message}");

            if (ShowDialogs)
            {
                var result = MessageBox.Show(message, title, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                Log(LogLevel.Debug, $"[UI Answer] {result}");
                return result;
            }

            return DialogResult.Cancel;
        }

        /// <summary>
        /// Показать вопрос с кнопками ОК/Отмена + записать в лог.
        /// Пример: if (Logger.ShowOkCancel("Продолжить операцию?") == DialogResult.OK) { ... }
        /// </summary>
        public static DialogResult ShowOkCancel(string message, string title = "Подтверждение")
        {
            Log(LogLevel.Info, $"[UI Question] {message}");

            if (ShowDialogs)
            {
                var result = MessageBox.Show(message, title, MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
                Log(LogLevel.Debug, $"[UI Answer] {result}");
                return result;
            }

            return DialogResult.Cancel;
        }

        // ===================================================================
        // ОСНОВНОЙ МЕТОД ЛОГИРОВАНИЯ
        // ===================================================================

        /// <summary>
        /// Основной метод записи лога
        /// </summary>
        private static void Log(LogLevel level, string message, string stackTrace = null)
        {
            if (!IsEnabled)
                return;

            if (level < MinLogLevel)
                return;

            try
            {
                var entry = new LogEntry(level, message, stackTrace);

                // Добавляем информацию о пользователе (если доступна)
                try
                {
                    if (UserData.CurrentUser.Employee != null)
                        entry.UserName = UserData.CurrentUser.Employee.FIO;
                }
                catch { }

                // Записываем в файл асинхронно
                Task.Run(() => WriteToFile(entry));
            }
            catch
            {
                // Если логирование упало - не роняем приложение
            }
        }

        /// <summary>
        /// Запись в файл (потокобезопасная)
        /// </summary>
        private static void WriteToFile(LogEntry entry)
        {
            try
            {
                lock (_lockObject)
                {
                    // Проверяем размер файла
                    CheckFileSize();

                    // Записываем в файл
                    File.AppendAllText(LogFilePath, entry.ToString() + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch
            {
                // Если не удалось записать - игнорируем
            }
        }

        // ===================================================================
        // УПРАВЛЕНИЕ ФАЙЛАМИ ЛОГОВ
        // ===================================================================

        /// <summary>
        /// Проверка размера файла лога
        /// </summary>
        private static void CheckFileSize()
        {
            try
            {
                if (File.Exists(LogFilePath))
                {
                    var fileInfo = new FileInfo(LogFilePath);
                    long maxSize = MaxLogFileSizeMB * 1024 * 1024;

                    if (fileInfo.Length > maxSize)
                    {
                        // Переименовываем текущий файл
                        string newName = $"log_{DateTime.Now:yyyy-MM-dd_HHmmss}.log";
                        string newPath = Path.Combine(LogDirectory, newName);
                        File.Move(LogFilePath, newPath);
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Очистка старых логов
        /// </summary>
        private static void CleanOldLogs()
        {
            try
            {
                if (!Directory.Exists(LogDirectory))
                    return;

                var files = Directory.GetFiles(LogDirectory, "log_*.log");
                var cutoffDate = DateTime.Now.AddDays(-LogRetentionDays);

                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.LastWriteTime < cutoffDate)
                    {
                        File.Delete(file);
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Получить все записи логов за период
        /// </summary>
        public static List<LogEntry> GetLogs(DateTime? from = null, DateTime? to = null)
        {
            var logs = new List<LogEntry>();

            try
            {
                if (!File.Exists(LogFilePath))
                    return logs;

                var lines = File.ReadAllLines(LogFilePath, Encoding.UTF8);

                // Простой парсинг (можно улучшить)
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    // Парсим строку формата: [2026-03-14 15:30:45] [INFO    ] Message
                    // Это упрощенный парсинг, для полноценного нужен более сложный алгоритм
                    logs.Add(new LogEntry { Message = line });
                }
            }
            catch { }

            return logs;
        }

        /// <summary>
        /// Экспорт логов в файл
        /// </summary>
        public static void ExportLogs(string destinationPath)
        {
            try
            {
                if (File.Exists(LogFilePath))
                {
                    File.Copy(LogFilePath, destinationPath, true);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Не удалось экспортировать логи: {ex.Message}");
            }
        }

        /// <summary>
        /// Очистить все логи
        /// </summary>
        public static void ClearAllLogs()
        {
            try
            {
                if (Directory.Exists(LogDirectory))
                {
                    var files = Directory.GetFiles(LogDirectory, "log_*.log");
                    foreach (var file in files)
                    {
                        File.Delete(file);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Не удалось очистить логи: {ex.Message}");
            }
        }

        /// <summary>
        /// Получить путь к папке с логами
        /// </summary>
        public static string GetLogDirectory()
        {
            return LogDirectory;
        }

        /// <summary>
        /// Получить путь к текущему файлу лога
        /// </summary>
        public static string GetCurrentLogFile()
        {
            return LogFilePath;
        }
    }
}
