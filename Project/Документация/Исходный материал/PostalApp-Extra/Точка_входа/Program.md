ТОЧКА ВХОДА (Program)
======================

Класс: Program (статический)
Файл: Program.cs

Назначение
----------
Точка входа и глобальное хранилище параметров подключения к PostgreSQL,
а также начальной позиции карты. Параметры хранятся только в памяти
и заполняются LoginForm после успешной авторизации.

Статические поля
----------------
version        — "Версия 0.25". Отображается в UI главной формы.

ServerIP       — IP-адрес PostgreSQL-сервера.
ServerPort     — порт (по умолчанию 5432).
ServerDatabase — имя базы данных.
ServerUser     — имя пользователя.
ServerPassword — пароль.

StartLat       — широта начальной позиции карты (по умолчанию 55.7522 — Москва).
StartLng       — долгота начальной позиции карты (по умолчанию 37.6156 — Москва).

Все шесть параметров подключения и координаты заполняются в LoginForm.HandleLoginClick()
из ответа сервера. StartLat/StartLng переопределяются координатами из конфига,
если сервер их вернул; иначе остаются значениями по умолчанию.

BuildPgConnectionString()
--------------------------
Формирует строку подключения Npgsql из текущих значений ServerIP, ServerPort,
ServerDatabase, ServerUser, ServerPassword.
Используется при каждом прямом обращении к PostgreSQL:
  - Перенос Access → Сервер (TransferMarkersToServer, TransferRegionsToServer)
  - Перенос Сервер → Карта (LoadFromServerAndDisplayOnMap)
  - Перенос Сервер → Access (LoadFromServerToAcccess)

Main()
------
[STAThread] — однопоточная оконная модель Windows Forms.
Application.Run(new LoginForm()) — цикл сообщений запускается с LoginForm.
После закрытия главной формы (CreateBalancedRegions) LoginForm закрывается,
цикл сообщений завершается.
