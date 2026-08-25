ТОЧКА ВХОДА (Program)
======================

Класс: Program (статический)
Файл: Program.cs

Назначение
----------
Точка входа приложения и глобальное хранилище параметров подключения к PostgreSQL.
Параметры заполняются после успешного получения конфига с сервера и хранятся
только в памяти — на диск не записываются.

Статические поля и свойства
-----------------------------
version            — строка вида "Версия 0.1.22". Используется в UI и для извлечения
                     числового номера версии через Regex.
AppDataFolderName  — "PostalApp". Имя папки в AppData; используется Logger (путь к логам)
                     и UpdateManager (при необходимости).

ServerIP           — IP-адрес PostgreSQL-сервера.
ServerPort         — порт (обычно 5432).
ServerDatabase     — имя базы данных.
ServerUser         — имя пользователя.
ServerPassword     — пароль.

Все пять свойств подключения заполняются в StartupForm.ApplyConfigToProgram()
из ServerConfig, полученного с конфигурационного сервера.
До заполнения их значения — null / 0; обращение к БД до входа невозможно.

Main()
------
Точка входа ([STAThread]).
  1. Application.EnableVisualStyles() — нативный рендеринг элементов управления.
  2. Application.SetCompatibleTextRenderingDefault(false) — GDI+ для TextRenderer.
  3. Application.Run(new StartupForm()) — запуск цикла сообщений.
  4. Весь Main обёрнут в try/catch: при необработанном исключении Logger записывает
     критическую ошибку и показывает диалог.

AppExit()
---------
Единственная точка выхода из приложения. Используется вместо Application.Exit()
во всём коде.
Причина: Application.Exit() вызывает цикл завершения форм, что при определённых
условиях (async-методы, обновление) приводит к исключениям. Environment.Exit(0)
завершает процесс немедленно.

StartCustomization(fioLabel, versionLabel)
------------------------------------------
Вспомогательный метод для инициализации UI основных форм.
Устанавливает в fioLabel текст «Роль: ФИО» из CurrentUser.Employee,
в versionLabel — строку Program.version.
Вызывается в OnLoad каждой формы роли (postmanForm, operatorForm и т.д.).
