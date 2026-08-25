ВСТРОЕННЫЙ СКРИПТ ПОИСКА АДРЕСОВ (SearchAddresses.py)
=======================================================

Файл: SearchAddresses.py (внешний, рядом с exe; восстанавливается из встроенной копии)
Кнопка запуска: button25
Язык: Python 3.7+

Назначение
----------
Консольный Python-скрипт, который запрашивает у пользователя название населённого пункта,
получает все адресные записи из OpenStreetMap через Overpass API и сохраняет результат
в Data\addresses.txt в формате CSV. Этот файл затем используется для загрузки меток на карту.

Встроенная копия скрипта
--------------------------
Скрипт хранится в C# коде как:
  EmbeddedScriptBase64 — Base64-кодированный контент файла (включая BOM UTF-8).
  EmbeddedScriptHash   — ожидаемый SHA-256 хэш файла:
    e57b2e6ea0ec5828e43623d01e7......8629d4da8db84140c2de707c8527

Логика восстановления (button25_Click)
----------------------------------------
1. Если SearchAddresses.py не существует → needsRestore = true.
2. Если существует → ComputeFileSha256() сравнивается с EmbeddedScriptHash.
   При несовпадении → needsRestore = true.
3. При needsRestore:
   Convert.FromBase64String(EmbeddedScriptBase64) → File.WriteAllBytes(scriptPath, bytes).
   Скрипт восстанавливается молча (только статус обновляется).

Проверка Python
---------------
DetectPython() перебирает команды ["python", "python3"]:
  cmd.exe /c {cmd} --version
  ExitCode == 0 → возвращает имя команды.
Если Python не найден → диалог с предложением открыть python.org/downloads.
При установке обязательно нужна галочка "Add Python to PATH".

Проверка зависимостей
---------------------
IsPipPackageInstalled(pythonCmd, "requests"):
  cmd.exe /c {python} -c "import requests"
  ExitCode == 0 → библиотека установлена.
При отсутствии:
  cmd.exe /c {python} -m pip install requests (с видимым окном для контроля).
  При ExitCode != 0 → показывается ошибка с ручной командой установки.

Запуск скрипта
--------------
cmd.exe /k {python} "{scriptPath}"
UseShellExecute = true, CreateNoWindow = false — скрипт открывается в отдельном
видимом окне консоли. /k (не /c) оставляет консоль открытой после завершения.
WorkingDirectory = Application.StartupPath.

Логика скрипта (SearchAddresses.py)
-------------------------------------
Входные данные: пользователь вводит название населённого пункта (input).

Overpass-запрос:
  [out:json][timeout:60]
  area["name"="{название}"]→.searchArea
  (
    node/way/relation["addr:housenumber"](area.searchArea);
    node/way/relation["building"](area.searchArea);
  )
  out center; > ; out tags;

"out center" — для way/relation возвращает координаты центра вместо всех узлов.

Парсинг каждого элемента:
  Координаты: lat/lon напрямую или из center.lat/center.lon.
  building_type: тег "building".
  street: "addr:street" → "addr:place" → "addr:streetname".
  Номер дома: parse_house_number(addr:housenumber) — разбирает на house, korpus, kvartira:
    Шаблоны квартиры:  кв/кв./квартира + число
    Шаблоны корпуса:   корп/корп./к/к./корпус + число, либо формат 123/1
    Остаток после вырезания → основной номер дома.
  Дополнительно: addr:flat (квартира), addr:unit/flats/block/corpus (корпус).

Выходной файл: Data\addresses.txt
Формат: CSV, кодировка UTF-8, разделитель запятая.
Заголовок: Долгота,Широта,Тип здания,Улица,Дом,Корпус,Квартира
Строки данных: lon,lat,"тип","улица","дом","корпус","квартира"

Парсинг CSV в C# (LoadMarkersFromAddresses)
--------------------------------------------
SplitCsvLine() — собственный CSV-парсер: учитывает кавычки (переключает inQuotes),
разбивает по запятой вне кавычек.
parts[0] = lon (double, InvariantCulture)
parts[1] = lat (double, InvariantCulture)
parts[2] = BuildingType
parts[3] = улица, parts[4] = дом, parts[5] = корпус, parts[6] = квартира
Строки заголовка пропускаются если первые два элемента не парсятся как double.
