ВЫПУСК НОВОЙ ВЕРСИИ POSTALAPP-EXTRA — РУЧНАЯ ИНСТРУКЦИЯ
=========================================================

PostalApp-Extra не использует manifests.py и систему update.zip.
Обновление распространяется исключительно через новый установщик (.exe),
который публикуется на сайте. Клиент не имеет механизма автообновления.

Шаг 1 — Обновить версию в коде приложения
-------------------------------------------
В Program.cs изменить:
  public static string version = "Версия X.XX";

Шаг 2 — Собрать Release-сборку
--------------------------------
Visual Studio → Build → Build Solution в конфигурации Release.

Шаг 3 — Обновить скрипт установщика
--------------------------------------
В PostalApp-Extra_Setup.iss:
  AppVersion         = X.XX
  OutputBaseFilename = PostalApp-Extra_Setup_vX.XX

Шаг 4 — Обновить встроенную копию SearchAddresses.py
------------------------------------------------------
Если SearchAddresses.py изменился между версиями — нужно обновить
встроенную копию в CreateBalancedRegions.cs.

Новое содержимое EmbeddedScriptBase64:
  python -c "import base64; print(base64.b64encode(open('SearchAddresses.py','rb').read()).decode())"
  Результат вставить в EmbeddedScriptBase64.

Новый EmbeddedScriptHash:
  python -c "import hashlib; print(hashlib.sha256(open('SearchAddresses.py','rb').read()).hexdigest())"
  Результат вставить в EmbeddedScriptHash.

Если скрипт не менялся — этот шаг пропустить.

Шаг 5 — Скомпилировать установщик
------------------------------------
Открыть PostalApp-Extra_Setup.iss в Inno Setup Compiler → Compile (Ctrl+F9).
Установщик: Setup\PostalApp-Extra\PostalApp-Extra_Setup_vX.XX.exe

Шаг 6 — Обновить manifest.json Extra вручную
----------------------------------------------
Файл: {папка сайта}/assets/postalapp-extra/manifest.json

Текущее содержимое:
  {"versions": ["0.25"]}

Обновить — добавить новую версию первой:
  {"versions": ["X.XX", "0.25"]}

Первый элемент — самая новая версия, именно по нему сайт строит ссылку на скачивание.

Шаг 7 — Разместить файлы
--------------------------
Установщик копируется в папку сайта:
  assets/postalapp-extra/versions/PostalApp-Extra_Setup_vX.XX.exe

Обновлённый manifest.json:
  assets/postalapp-extra/manifest.json

Развернуть на сервере (через WinSCP или git push если сайт в репозитории):
  /var/www/{папка сайта}/assets/postalapp-extra/versions/PostalApp-Extra_Setup_vX.XX.exe
  /var/www/{папка сайта}/assets/postalapp-extra/manifest.json

Шаг 8 — Проверить
-------------------
Открыть сайт → зайти под модератором/администратором → убедиться что
в выпадающем списке версий появилась vX.XX как «последняя».

Почему нет auto-update для Extra
----------------------------------
PostalApp-Extra — инструмент для первичного формирования участков, используется
реже и не требует гарантированной актуальности версии на всех машинах.
Система автообновления с проверкой целостности MD5 и server-side version check
добавляет значительную сложность (manifest на сервере, update.zip, bat-скрипт),
которая для Extra избыточна.
