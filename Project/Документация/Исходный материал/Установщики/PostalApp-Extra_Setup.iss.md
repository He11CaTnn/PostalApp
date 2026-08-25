УСТАНОВЩИК PostalApp-Extra (PostalApp-Extra_Setup.iss)
=======================================================

Инструмент: Inno Setup (Pascal-совместимый скрипт)
Выходной файл: Setup\PostalApp-Extra\PostalApp-Extra_Setup_v{версия}.exe

Отличия от PostalApp_Setup.iss
-------------------------------
Большинство секций идентичны PostalApp. Ключевые отличия:

  AppName              = PostalApp-Extra
  AppVersion           = 0.26
  AppId                = {9241ADCF-852F-428D-B3B5-78109081406A}
  DefaultDirName       = {autopf32}\PostalApp-Extra
  DefaultGroupName     = PostalApp-Extra
  OutputBaseFilename   = PostalApp-Extra_Setup_v0.26
  OutputDir            = Setup\PostalApp-Extra
  Source               = ...\PostalApp-Extra\bin\Release\...
  Exe                  = PostalApp-Extra.exe

Исключения в [Files]: "*.pdb,*.xml,*.log" — папка logs отдельно не исключается,
т.к. PostalApp-Extra не создаёт папку logs.

Секция [Dirs] пуста — не создаётся папка logs и не задаются специальные права.

Секция [Code] — проверка Python (CheckPython)
----------------------------------------------
Ключевое отличие от установщика PostalApp: наличие проверки Python перед установкой.
PostalApp-Extra требует Python 3.7+ для работы встроенного скрипта SearchAddresses.py.

Функция CheckPython() → Integer:
  Скрипт проверки: python -c "import sys; sys.exit(0 if sys.version_info>=(3,7) else 2)"
  Выполняется через cmd.exe /C для обеих команд: "python" и "python3".
  Флаги: SW_HIDE (скрытое окно), ewWaitUntilTerminated (ждать завершения).

  Коды возврата:
    RC = 0  → Python 3.7+ найден
    RC = 2  → Python найден, но версия ниже 3.7
    Иначе   → Python не найден (типично RC = 9009 «команда не найдена»)

  Логика:
    Сначала пробует "python", при неудаче пробует "python3".
    Возвращает итоговый статус:
      0 — всё OK
      1 — Python не найден совсем
      2 — версия устарела

InitializeSetup() — точка входа:
  Вызывается до показа любой страницы установщика.
  Вызывает CheckPython() и обрабатывает три сценария:

  Статус 0 (Python 3.7+ найден):
    Установка продолжается без вмешательства. Result = True.

  Статус 2 (устаревшая версия):
    MsgBox с вопросом «Открыть страницу загрузки?».
    При Yes:
      ShellExec открывает https://www.python.org/downloads/ в браузере.
      Дополнительный MsgBox с напоминанием выбрать «Add Python to PATH».
    Независимо от ответа: Result = False — установка блокируется.
    Пользователь должен обновить Python и запустить установщик заново.

  Статус 1 (Python не найден):
    MsgBox с вопросом «Открыть страницу загрузки?».
    При Yes: то же поведение — браузер + напоминание про PATH.
    Result = False — установка блокируется.

Почему блокируется установка
------------------------------
SearchAddresses.py необходим для получения адресов из OpenStreetMap.
Без Python скрипт запустить невозможно — кнопка в приложении завершится ошибкой
«Python не найден». Проверка на этапе установщика предотвращает установку
заведомо нерабочей конфигурации и сразу направляет пользователя к решению.

Что менять при выпуске новой версии
-------------------------------------
1. AppVersion = {новая версия}
2. OutputBaseFilename = PostalApp-Extra_Setup_v{новая версия}
3. Source-пути — если изменилась структура проекта.
Остальное не трогать.
