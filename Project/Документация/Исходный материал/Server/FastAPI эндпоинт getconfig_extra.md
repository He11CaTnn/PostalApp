СЕРВЕР — ЭНДПОИНТ /api/getconfig_extra
========================================

Файл: main.py (FastAPI)

Назначение
----------
Специальный эндпоинт для PostalApp-Extra. Аутентифицирует пользователя
по email и паролю без проверки версии приложения и целостности exe-файла.

Эндпоинт POST /api/getconfig_extra
-------------------------------------
Модель запроса: LoginExtraRequest
  login    — email пользователя
  password — пароль открытым текстом

Логика:
1. check_login_rate(ip) — проверка rate limit ручного входа (5 попыток за 60 секунд).
   При превышении: HTTP 429 с {"detail":{"reason":"rate_limit","retry_after":N}}.

2. Перебор databases из config.json:
   asyncpg.connect(build_dsn(conn_string)) → SELECT "Пароль" FROM "Логин" WHERE "Почта" = $1.
   verify_pbkdf2(password, row["Пароль"]) — проверка PBKDF2-SHA256.

3. При совпадении: HTTP 200, JSON:
   {"config": "ip|port|db|user|password", "lat": ..., "lng": ...}
   lat/lng добавляются только если не None.

4. Если ни одна БД не дала совпадения: HTTP 401.
   При ошибке подключения к отдельной БД — продолжается перебор.

Отличие от /api/getconfig
--------------------------
  - Не принимает и не проверяет version и exe_md5.
  - Не возвращает HTTP 426 (устаревшая версия).
  - Не возвращает HTTP 403 (нарушение целостности).
  - Rate limit применяется (те же 5 попыток за 60 секунд).

Подробнее о PBKDF2, строке подключения, rate limiting и config.json
смотри в документации Server/FastAPI сервер.txt из PostalApp-проекта.
