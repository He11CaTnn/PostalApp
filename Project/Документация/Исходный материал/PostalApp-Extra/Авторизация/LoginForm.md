ФОРМА ВХОДА (LoginForm)
========================

Файл: LoginForm.cs

Назначение
----------
Единственная точка входа в приложение. Осуществляет аутентификацию по email
и паролю через эндпоинт /api/getconfig_extra, получает строку подключения к PostgreSQL
и координаты, затем скрывается и передаёт управление CreateBalancedRegions.

Эндпоинт
--------
POST https://<ip_адрес>/api/getconfig_extra

Отличие от /api/getconfig: не требует полей version и exe_md5.
Сервер не проверяет версию приложения и целостность exe-файла.
Применяется rate limiting ручного входа (5 попыток за 60 секунд).

SSL Pinning
-----------
Идентичен реализации в PostalApp (SecureConfig).
HttpClientHandler.ServerCertificateCustomValidationCallback вычисляет
SHA-256 от cert.RawData и сравнивает с константой:
  ExpectedCertFingerprint = "BC14A0466B54BFB96C9F2B116C519104B9B357374A50DF08FB537C496016008D"
При несовпадении HttpClient бросает HttpRequestException — соединение не устанавливается.

Тело запроса (JSON)
--------------------
{ "login": "email", "password": "пароль" }

Пароль передаётся открытым текстом (сервер проверяет PBKDF2-SHA256 на своей стороне).
Специальные символы JSON экранируются методом EscapeJson() — обратный слеш и кавычки.

Парсинг ответа
--------------
Без JSON-десериализатора, через Regex:
  \"config\"\s*:\s*\"([^\"]+)\"    — строка подключения ip|port|db|user|password
  \"lat\"\s*:\s*([\d\.\-]+)        — широта
  \"lng\"\s*:\s*([\d\.\-]+)        — долгота

Строка config разбивается по '|' на parts[0..4] и записывается в Program.*
Координаты проверяются на валидность диапазона (lat: -90..90, lng: -180..180)
и записываются в Program.StartLat/StartLng. При невалидных или отсутствующих
координатах остаются значения по умолчанию (Москва).

Переход к главной форме
-----------------------
После успешного парсинга создаётся CreateBalancedRegions.
mainForm.FormClosed → Close() : закрытие главной формы закрывает LoginForm,
что завершает Application.Run().
mainForm.Show() + Hide() — LoginForm не уничтожается, а скрывается.

Коды ошибок
-----------
HTTP 429 — rate limit. Тело содержит "retry_after": N.
           Парсится Regex, создаётся RateLimitException(N).
HTTP 401 — неверный email или пароль.
           Бросается UnauthorizedAccessException.
Остальное или HttpRequestException — общая ошибка соединения.

Rate Limit таймер
-----------------
Идентичен PostalApp (StartupForm).
SetControlsEnabled(false) блокирует кнопку, поля email и пароля.
Timer 1000 мс декрементирует счётчик, обновляет метку ошибки.
По достижении нуля: таймер остановлен, интерфейс разблокирован, фокус на email.
StopRateLimitTimer() вызывается при закрытии окна (BtnClose_Click → Application.Exit()).

Закрытие приложения
-------------------
BtnClose_Click вызывает Application.Exit() (не Program.AppExit() как в PostalApp).
Таймер останавливается перед выходом для предотвращения Tick по уже уничтоженному контролу.

Визуальное
----------
Скругление углов: ApplyRounded() через GraphicsPath.AddArc.
Перетаскивание окна: P/Invoke SendMessage + ReleaseCapture по MouseDown на форме.
Enter в txtEmail → фокус на txtPassword.
Enter в txtPassword → _btnLogin.PerformClick().
