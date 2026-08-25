СТАРТОВАЯ ФОРМА (StartupForm)
==============================

Файл: StartupForm.cs

Назначение
----------
Первая форма приложения. Управляет всей последовательностью запуска:
проверка версии → автовход → ручной вход → проверка обновлений → открытие формы роли.
Не закрывается после открытия основной формы — скрывается (Hide).

Визуальное состояние
--------------------
Два взаимоисключающих режима:
  Прогресс-режим: видны _lblStatus и _progressTrack, скрыта _loginPanel.
  Режим входа:    видна _loginPanel, скрыты _lblStatus и _progressTrack.

Прогресс-бар реализован вручную: _progressBar — панель внутри _progressTrack,
ширина вычисляется как progressTrack.Width × pct / 100.

Последовательность запуска (RunStartupSequence)
-------------------------------------------------
Запускается в OnLoad через Task.Run (fire-and-forget с _ = ...).

Этап 0 — Step0_VersionCheck():
  Скачивает глобальный манифест и проверяет поддержку текущей версии.
  Если версия не поддерживается — показывает кнопку «Обновить сейчас» и прерывает.
  Если манифест недоступен — пропускает этап и продолжает (не блокирует запуск).

Автовход:
  GetMotherboardId() → если null → ShowLoginPanel().
  Иначе → HandleDeviceAutoLogin(motherboardId).

HandleDeviceAutoLogin():
  FetchConfigByMotherboardId() → если null → ShowLoginPanel() (устройство не найдено).
  ApplyConfigToProgram() → DataBase.TryConnectAsync() → UserData.LoadUserByMotherboardId()
  → UpdateDeviceInfo() → Step2_CheckUpdates().

Ручной вход:
  ShowLoginPanel() + фокус на _txtEmail.

Ручной вход (HandleLoginClick):
  Описан подробно в Авторизация_и_пользователи/Ручной вход.txt.

Этап 2 — Step2_CheckUpdates():
  InvalidateCache() + CheckForUpdates().
  Если версия не поддерживается — кнопка «Обновить сейчас».
  Если обновлений нет → OpenRoleForm().
  Если обновление доступно → кнопки «Обновить сейчас» + «Позже».
  При ошибке проверки — переходит к OpenRoleForm() с задержкой 800мс.

OpenRoleForm():
  SetStatusProgress("✓  Добро пожаловать!", 100) + UserData.OpenRoleForm(Employee, this).

Скачивание и установка (DownloadAndInstall):
  UpdateManager.DownloadUpdate() → UpdateManager.ApplyUpdate() → Program.AppExit().

Кнопки (управляются через event handlers)
-----------------------------------------
_btnRetry      — повтор подключения (используется при ошибках).
_btnUpdate     — обновить / восстановить файлы.
_btnRemindLater — «Позже» (продолжить без обновления).
HideAllButtons() отписывает обработчики перед скрытием — предотвращает накопление.

Кнопка восстановления (ShowRestoreButton):
  _btnUpdate.Text = "↺  Восстановить файлы".
  Click → открывает IntegrityCheckForm.

Перемещение окна
----------------
P/Invoke SendMessage + ReleaseCapture — перетаскивание за любое место формы.

Скругление углов
----------------
ApplyRounded(control, radius) применяет Region с GraphicsPath.AddArc для
каждого угла. Вызывается при инициализации и при Resize.
