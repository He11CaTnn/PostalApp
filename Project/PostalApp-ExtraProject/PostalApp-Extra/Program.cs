using System;
using System.Windows.Forms;

namespace PostalApp_Extra
{
    internal static class Program
    {
        // ── Данные подключения (заполняются LoginForm) ───────────────
        public static string ServerIP       = string.Empty;
        public static int    ServerPort     = 5432;
        public static string ServerDatabase = string.Empty;
        public static string ServerUser     = string.Empty;
        public static string ServerPassword = string.Empty;

        // ── Стартовая позиция карты (заполняется LoginForm) ──────────
        // Значение по умолчанию — Москва
        public static double StartLat = 55.7522;
        public static double StartLng = 37.6156;

        // ── Версия (числовой формат X.XX для UpdateManagerExtra) ─────
        public static string version = "0.52 beta";

        /// <summary>
        /// Строка подключения к серверу PostgreSQL (Npgsql).
        /// </summary>
        public static string BuildPgConnectionString()
        {
            return $"Host={ServerIP};Port={ServerPort};Database={ServerDatabase};" +
                   $"Username={ServerUser};Password={ServerPassword};";
        }

        /// <summary>
        /// Безопасное завершение приложения.
        /// Используется вместо Application.Exit() чтобы избежать исключений
        /// в обработчиках событий при завершении.
        /// </summary>
        public static void AppExit()
        {
            Environment.Exit(0);
        }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new LoginForm());
        }
    }
}
