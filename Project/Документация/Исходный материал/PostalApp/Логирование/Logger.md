ЛОГИРОВАНИЕ (Logger)
=====================

Классы: Logger (статический), LogEntry, LogLevel (enum)
Файлы: Logger.cs, LogEntry.cs, LogLevel.cs

Назначение
----------
Централизованная система записи событий в файл и показа диалоговых окон.
Заменяет прямые вызовы MessageBox.Show() во всём приложении.

Уровни логирования (LogLevel)
------------------------------
Debug    = 0 — детальная отладочная информация.
Info     = 1 — нормальная работа, успешные операции.
Warning  = 2 — некритичные проблемы.
Error    = 3 — обработанные исключения, неудачные операции.
Critical = 4 — критические ошибки, возможное падение.

Настройки
---------
MinLogLevel      — минимальный уровень для записи (по умолчанию Info; Debug-сообщения в лог не попадают).
MaxLogFileSizeMB — максимальный размер файла в МБ (по умолчанию 10).
LogRetentionDays — срок хранения файлов логов в днях (по умолчанию 30).
IsEnabled        — глобальное включение/выключение логирования.
ShowDialogs      — включение/выключение диалоговых окон (удобно для тестов).

Файлы логов
-----------
Расположение: {AppDomain.CurrentDomain.BaseDirectory}\logs\
Имя файла: log_{yyyy-MM-dd}.log — один файл на день.
При превышении MaxLogFileSizeMB файл переименовывается в log_{yyyy-MM-dd_HHmmss}.log,
записи продолжаются в новый файл с исходным именем.
Файлы старше LogRetentionDays удаляются при запуске (CleanOldLogs в статическом конструкторе).

Методы без UI (только запись в файл)
--------------------------------------
Logger.Debug(message)
Logger.Info(message)
Logger.Warning(message)
Logger.Error(message, ex)    — ex?.ToString() пишется как StackTrace
Logger.Critical(message, ex)

Методы с UI (запись + MessageBox)
-----------------------------------
Logger.ShowInfo(message, title)     — MessageBoxIcon.Information
Logger.ShowWarning(message, title)  — MessageBoxIcon.Warning
Logger.ShowError(message, title)    — MessageBoxIcon.Error
Logger.ShowCritical(message, title) — MessageBoxIcon.Error

Диалоговые методы с возвратом результата
-----------------------------------------
Logger.ShowYesNo(message, title)       → DialogResult
Logger.ShowYesNoCancel(message, title) → DialogResult
Logger.ShowOkCancel(message, title)    → DialogResult

Имя текущего пользователя (CurrentUser.Employee.FIO) автоматически добавляется
в каждую запись лога, если пользователь авторизован.

Потокобезопасность
------------------
Запись в файл производится в Task.Run() (асинхронно от UI-потока) и защищена lock(_lockObject).
Если запись провалилась — исключение поглощается, приложение не падает.

Формат строки лога (LogEntry.ToString())
------------------------------------------
[yyyy-MM-dd HH:mm:ss] [LEVEL   ] Message | User: ФИО | Form: FormName
    StackTrace: ...

Поля User и Form опциональны; StackTrace добавляется только при наличии исключения.

Управление логами
-----------------
Logger.GetLogs(from, to)          — читает текущий файл и возвращает список LogEntry (упрощённый парсинг).
Logger.ExportLogs(destinationPath) — копирует текущий файл лога в указанный путь.
Logger.ClearAllLogs()             — удаляет все файлы log_*.log из папки логов.
Logger.GetLogDirectory()          — возвращает путь к папке логов.
Logger.GetCurrentLogFile()        — возвращает полный путь к текущему файлу лога.
