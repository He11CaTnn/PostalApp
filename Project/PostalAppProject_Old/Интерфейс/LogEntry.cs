using System;

namespace Интерфейс
{
    /// <summary>
    /// Модель записи лога.
    /// Содержит всю информацию об одном событии логирования.
    /// </summary>
    public class LogEntry
    {
        /// <summary>
        /// Время создания записи
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Уровень важности сообщения
        /// </summary>
        public LogLevel Level { get; set; }

        /// <summary>
        /// Текст сообщения
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Stack trace исключения (если есть)
        /// </summary>
        public string StackTrace { get; set; }

        /// <summary>
        /// Имя пользователя (если известно)
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Имя формы, где произошло событие (если известно)
        /// </summary>
        public string FormName { get; set; }

        /// <summary>
        /// Конструктор
        /// </summary>
        public LogEntry()
        {
            Timestamp = DateTime.Now;
        }

        /// <summary>
        /// Конструктор с параметрами
        /// </summary>
        public LogEntry(LogLevel level, string message, string stackTrace = null)
        {
            Timestamp = DateTime.Now;
            Level = level;
            Message = message;
            StackTrace = stackTrace;
        }

        /// <summary>
        /// Преобразование в строку для записи в файл
        /// </summary>
        public override string ToString()
        {
            string result = $"[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Level,-8}] {Message}";

            if (!string.IsNullOrEmpty(UserName))
                result += $" | User: {UserName}";

            if (!string.IsNullOrEmpty(FormName))
                result += $" | Form: {FormName}";

            if (!string.IsNullOrEmpty(StackTrace))
                result += $"\n    StackTrace: {StackTrace}";

            return result;
        }
    }
}
