СЕРВЕР — FastAPI (main.py)
==========================

Стек: Python, FastAPI, asyncpg, hashlib
Расположение: /var/www/postalapp_api/main.py

Назначение
----------
HTTP API для аутентификации клиентов PostalApp и выдачи строки подключения к PostgreSQL.
Сервер не хранит сессии — каждый запрос самодостаточен.

Конфигурация (config.json)
---------------------------
Файл: /var/www/postalapp_api/config.json
Структура:
{
  "databases": {
    "Название отделения": {
      "conn": "ip|port|database|user|password",
      "lat": 55.534449,
      "lng": 58.246835
    }
  }
}

Поддерживается несколько записей в databases. При входе перебираются все записи
до первого совпадения. Координаты lat/lng передаются клиенту и используются
для начального позиционирования карты. Если conn — строка (старый формат без
вложенного объекта) — совместимость обеспечена через get_conn_string().

Строка подключения conn разбирается build_dsn() в DSN-формат:
  postgresql://user:password@host:port/database

Rate Limiting
-------------
Реализован в памяти процесса (dict, не Redis). При перезапуске сервера лимиты сбрасываются.
Скользящее окно 60 секунд.

_login_attempts  — ручной вход:  5 попыток за 60 секунд на IP.
_device_attempts — автовход:    60 попыток за 60 секунд на IP.

check_login_rate(ip):  проверяет и добавляет метку времени для ручного входа.
check_device_rate(ip): то же для автовхода.
get_login_block_seconds(ip): возвращает секунды блокировки без добавления метки.
  Используется в /api/checkdevice — если IP заблокирован ручным лимитом, автовход тоже блокируется.

При превышении: HTTP 429 с телом {"detail":{"reason":"rate_limit","retry_after":N}}.

Проверка версии и целостности
------------------------------
check_version_and_integrity(version, exe_md5):
  1. Читает /var/www/postalapp_updates/manifest.json.
     versions[] — список поддерживаемых версий.
     version_supported = version in versions[].
  2. Читает /var/www/postalapp_updates/versions/v{version}/version_manifest.json.
     Ищет файл с path == "postalapp.exe" (регистронезависимо).
     Извлекает expected_md5.
  3. integrity_ok = True если expected_md5 не найден (файл недоступен) или хэши совпадают.
  Возвращает (version_supported, integrity_ok).

Эндпоинт POST /api/getconfig
------------------------------
Ручной вход. Тело: login, password, version, exe_md5.

1. check_login_rate(ip) — при превышении 429.
2. check_version_and_integrity() — 426 если версия не поддерживается, 403 если integrity_ok = False.
3. Перебор databases: asyncpg.connect() → SELECT "Пароль" FROM "Логин" WHERE "Почта" = $1.
4. verify_pbkdf2(password, row["Пароль"]):
   - Base64-декодирует stored_hash.
   - Первые 16 байт — соль.
   - hashlib.pbkdf2_hmac('sha256', password.encode('utf-8'), salt, 100000, dklen=32).
   - Побайтовое сравнение.
5. При совпадении: возвращает {"config": conn_string, "lat": ..., "lng": ...}.
   lat/lng включаются только если не None.
6. Если ни одна БД не дала совпадения: HTTP 401.

Эндпоинт POST /api/getconfig_extra
-------------------------------------
Ручной вход без проверки версии и целостности.
Логика идентична /api/getconfig за исключением шага 2.
Предназначен для дополнительных клиентов или отладки.

Эндпоинт POST /api/checkdevice
--------------------------------
Автовход по ID материнской платы. Тело: motherboard_id, version, exe_md5.

1. get_login_block_seconds(ip) — если IP заблокирован по лимиту ручного входа: 429.
2. check_device_rate(ip) — при превышении лимита автовхода: 429.
3. check_version_and_integrity() — 426 / 403 по тем же правилам.
4. Перебор databases: SELECT id FROM "Устройства"
   WHERE "Id материнской платы" = $1 AND "Постоянный доступ" = true.
5. Если запись найдена: возвращает {"config": conn_string, "lat": ..., "lng": ...}.
6. Если не найдена ни в одной БД: HTTP 401.

Обработка ошибок подключения к БД
-----------------------------------
Каждый блок asyncpg.connect() обёрнут в try/except.
При любом исключении (недоступность БД) — продолжается перебор следующей записи.
Это позволяет поддерживать несколько отделений: если одно недоступно, проверяются остальные.
