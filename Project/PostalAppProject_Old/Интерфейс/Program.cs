using CuoreUI.Controls;
using System;
using System.Windows.Forms;

namespace Интерфейс
{
    public static class Program
    {
        public static string version = "0.1.32 beta";

        // Данные подключения к БД — заполняются в StartupForm после получения конфига с сервера.
        // Хранятся только в памяти, на диск не пишутся.
        public static string ServerIP { get; set; }
        public static int ServerPort { get; set; }
        public static string ServerDatabase { get; set; }
        public static string ServerUser { get; set; }
        public static string ServerPassword { get; set; }

        [STAThread]
        private static void Main()
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                Application.Run(new StartupForm());
            }
            catch (Exception ex)
            {
                Logger.Critical("Критическая ошибка запуска приложения", ex);
                Logger.ShowCritical("Критическая ошибка запуска приложения");
            }
        }

        /// <summary>
        /// Единая точка выхода из приложения.
        /// </summary>
        public static void AppExit()
        {
            Environment.Exit(0);
        }

        public static void StartCustomizationRoleForm(cuiLabel fioLabel, cuiLabel versionLabel)
        {
            fioLabel.Content = $"{UserData.CurrentUser.Employee.Role}: {UserData.CurrentUser.Employee.FIO}";
            StartCustomizationVersionLabel(versionLabel);
        }

        public static void StartCustomizationVersionLabel(cuiLabel versionLabel)
        {
            versionLabel.Content = $"Версия {version}";
        }
    }
}
