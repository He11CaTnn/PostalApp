ВЫПУСК НОВОЙ ВЕРСИИ POSTALAPP — ПОШАГОВАЯ ИНСТРУКЦИЯ
======================================================

Используемые инструменты: Visual Studio, Inno Setup, manifests.py, WinSCP.

Шаг 1 — Обновить версию в коде приложения
-------------------------------------------
В Program.cs изменить строку:
  public static string version = "Версия X.Y.Z";
где X.Y.Z — новая версия.

Шаг 2 — Собрать Release-сборку
--------------------------------
В Visual Studio: Build → Build Solution (или Ctrl+Shift+B) в конфигурации Release.
Убедиться, что папка bin\Release содержит актуальный PostalApp.exe.

Шаг 3 — Обновить скрипт установщика
--------------------------------------
В PostalApp_Setup.iss:
  AppVersion        = X.Y.Z
  OutputBaseFilename = PostalApp_Setup_vX.Y.Z

Шаг 4 — Скомпилировать установщик
------------------------------------
Открыть PostalApp_Setup.iss в Inno Setup Compiler → Compile (Ctrl+F9).
Установщик появится в Setup\PostalApp\PostalApp_Setup_vX.Y.Z.exe.

Шаг 5 — Запустить manifests.py
--------------------------------
Запустить manifests.py (python manifests.py).

Ввод пути к папке Release:
  D:\Desktop\Project\PostalAppProject\Интерфейс\bin\Release
  (или подтвердить сохранённый из pathProject.txt нажав Enter)

Ввод версии:
  X.Y.Z

Скрипт выполнит четыре шага:
  [1/3] Создаст versions\vX.Y.Z\version_manifest.json
  [2/3] Создаст versions\vX.Y.Z\update.zip
  [2.5/3] Скопирует установщик в versions\vX.Y.Z\ (если настроен pathSetup.txt)
  [3/3] Обновит manifest.json

Шаг 6 — Заполнить releaseNotes
--------------------------------
Открыть versions\vX.Y.Z\version_manifest.json вручную.
Заполнить поле "releaseNotes" — текст изменений для этой версии.
Пример:
  "releaseNotes": "Исправлена ошибка автовхода. Улучшена скорость загрузки карты."

Шаг 7 — Загрузить файлы на сервер через WinSCP
------------------------------------------------
Подключиться к серверу (<ip_адрес>).
Путь назначения на сервере: /var/www/postalapp_updates/

Загрузить в папку versions/vX.Y.Z/:
  update.zip
  version_manifest.json
  PostalApp_Setup_vX.Y.Z.exe  (если нужен на сервере)

Загрузить глобальный манифест:
  manifest.json → /var/www/postalapp_updates/manifest.json
  (перезаписать существующий файл)

Шаг 8 — Проверить результат
-----------------------------
Открыть в браузере:
  http://<ip_адрес>/updates/manifest.json
Убедиться что:
  versions[0] == "X.Y.Z"
  downloadUrl указывает на новую версию
  checksum соответствует MD5 update.zip

Если нужно убрать поддержку старой версии
------------------------------------------
Открыть /var/www/postalapp_updates/manifest.json на сервере.
Удалить вручную нужную версию из массива versions[].
Клиенты с этой версией при следующем запуске получат HTTP 426 и предложение обновиться.

Управление поддерживаемыми версиями
-------------------------------------
versions[] в manifest.json — полный список версий, с которыми сервер разрешает вход.
Все записи кроме versions[0] — это старые версии, которые по-прежнему поддерживаются.
Чтобы принудить всех пользователей обновиться — оставить в versions[] только одну (новую) версию.
