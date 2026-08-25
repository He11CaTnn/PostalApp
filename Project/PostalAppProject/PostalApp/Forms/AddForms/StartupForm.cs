using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PostalApp
{
    public partial class StartupForm : Form
    {
        [DllImport("user32.dll")] private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")] private static extern bool ReleaseCapture();
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTCAPTION        = 0x2;

        private EventHandler _retryHandler;
        private EventHandler _updateHandler;
        private EventHandler _remindHandler;

        // Кэшируем UpdateInfo с этапа 0 — используется при показе кнопок обновления
        private UpdateInfo _cachedUpdateInfo;

        // Таймер обратного отсчёта при блокировке rate limit
        private Timer _rateLimitTimer;
        private int _rateLimitSecondsLeft;

        public StartupForm()
        {
            InitializeComponent();
            SetupPostInit();
        }

        private void SetupPostInit()
        {
            ApplyRounded(this, 18);
            this.Resize += (s, e) => ApplyRounded(this, 18);
            ApplyRounded(_progressTrack,  5);
            ApplyRounded(_btnRetry,       10);
            ApplyRounded(_btnUpdate,      10);
            ApplyRounded(_btnRemindLater, 10);
            ApplyRounded(_btnMinimize,    6);
            ApplyRounded(_btnClose,       6);
            ApplyRounded(_btnLogin,       8);
            SetHover(_btnUpdate,      Color.FromArgb(25, 55, 255), Color.FromArgb(10, 35, 200));
            SetHover(_btnRemindLater, Color.FromArgb(60, 80, 140), Color.FromArgb(40, 55, 110));
            Program.StartCustomizationVersionLabel(_lblVersion);
            this.MouseDown += Form_MouseDown;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            _ = RunStartupSequence();
        }

        // ===================================================================
        // Главная последовательность запуска
        // ===================================================================

        private async Task RunStartupSequence()
        {
            ShowProgressMode();
            bool versionOk = await Step0_VersionCheck();
            if (!versionOk) return;

            string motherboardId = SecurityConfig.GetMotherboardId();
            if (!string.IsNullOrEmpty(motherboardId))
            {
                SetStatusProgress("Проверка устройства...", 5);
                await HandleDeviceAutoLogin(motherboardId);
                return;
            }

            ShowLoginPanel();
            _txtEmail.Focus();
        }

        // ===================================================================
        // Этап 0 — Проверка версии
        // ===================================================================

        private async Task<bool> Step0_VersionCheck()
        {
            SetStatusProgress("Проверка версии приложения...", 3);

            try
            {
                _cachedUpdateInfo = await UpdateManager.CheckForUpdates();

                if (!_cachedUpdateInfo.IsCurrentVersionSupported)
                {
                    string notes = string.IsNullOrEmpty(_cachedUpdateInfo.ReleaseNotes)
                        ? ""
                        : $"  ({_cachedUpdateInfo.ReleaseNotes})";
                    SetStatusProgress(
                        $"⚠  Версия {UpdateManager.GetCurrentVersion()} устарела. " +
                        $"Требуется обновление до v{_cachedUpdateInfo.LatestVersion}{notes}", 3);
                    ShowUpdateButtonOnly(_cachedUpdateInfo);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Warning("Этап 0: не удалось проверить версию — " + ex.Message);
                return true;
            }
        }

        // ===================================================================
        // Автовход по ID материнской платы
        // ===================================================================

        private async Task HandleDeviceAutoLogin(string motherboardId)
        {
            try
            {
                SetStatusProgress("Получение конфигурации по устройству...", 10);
                SecurityConfig.ServerConfig cfg =
                    await SecurityConfig.FetchConfigByMotherboardId(motherboardId);

                if (cfg == null)
                {
                    ShowLoginPanel();
                    _txtEmail.Focus();
                    return;
                }

                ApplyConfigToProgram(cfg);

                SetStatusProgress("Подключение к базе данных...", 20);
                try
                {
                    await DataBase.TryConnectAsync();
                }
                catch (Exception ex)
                {
                    Logger.Error("Автовход по устройству: ошибка подключения к БД", ex);
                    ShowLoginPanel();
                    ShowLoginError("Не удалось подключиться к базе данных");
                    return;
                }

                SetStatusProgress("Загрузка данных пользователя...", 35);
                bool loaded = await UserData.LoadUserByMotherboardId(motherboardId);

                if (!loaded)
                {
                    Logger.Warning("Автовход: пользователь не найден или PermanentAccess=false");
                    ShowLoginPanel();
                    ShowLoginError("Устройство не разрешено для автовхода. Войдите вручную.");
                    return;
                }

                await UserData.UpdateDeviceInfo(motherboardId);
                await Task.Delay(200);
                await Step2_CheckUpdates();
            }
            catch (RateLimitException ex)
            {
                // IP заблокирован по лимиту ручного входа — показываем таймер
                ShowLoginPanel();
                StartRateLimitCountdown(ex.RetryAfter);
            }
            catch (VersionTooOldException)
            {
                if (_cachedUpdateInfo != null)
                {
                    SetStatusProgress(
                        $"⚠  Версия устарела. Требуется обновление до v{_cachedUpdateInfo.LatestVersion}", 10);
                    ShowUpdateButtonOnly(_cachedUpdateInfo);
                }
                else
                {
                    ShowLoginPanel();
                    ShowLoginError("Версия приложения устарела. Переустановите приложение.");
                }
            }
            catch (IntegrityFailedException)
            {
                SetStatusProgress("✕  Файлы приложения повреждены или модифицированы", 10);
                ShowRestoreButton();
            }
            catch (Exception ex)
            {
                Logger.Error("Автовход по устройству: неожиданная ошибка", ex);
                ShowLoginPanel();
                ShowLoginError("Ошибка автоматического входа. Войдите вручную.");
            }
        }

        // ===================================================================
        // Ручной вход
        // ===================================================================

        private async void BtnLogin_Click(object sender, EventArgs e)
        {
            await HandleLoginClick();
        }

        private async Task HandleLoginClick()
        {
            string email    = _txtEmail.Text.Trim();
            string password = _txtPassword.Text;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ShowLoginError("Введите email и пароль");
                return;
            }

            SetLoginControlsEnabled(false);
            ShowLoginError(null);
            ShowProgressMode();
            SetStatusProgress("Получение конфигурации с сервера...", 5);

            SecurityConfig.ServerConfig cfg;
            try
            {
                cfg = await SecurityConfig.FetchConfigFromServer(email, password);
            }
            catch (RateLimitException ex)
            {
                ShowLoginPanel();
                StartRateLimitCountdown(ex.RetryAfter);
                return;
            }
            catch (VersionTooOldException)
            {
                if (_cachedUpdateInfo != null)
                {
                    SetStatusProgress(
                        $"⚠  Версия устарела. Требуется обновление до v{_cachedUpdateInfo.LatestVersion}", 5);
                    ShowUpdateButtonOnly(_cachedUpdateInfo);
                }
                else
                {
                    ShowLoginPanel();
                    ShowLoginError("Версия приложения устарела. Обновите или переустановите приложение.");
                    SetLoginControlsEnabled(true);
                }
                return;
            }
            catch (IntegrityFailedException)
            {
                SetStatusProgress("✕  Файлы приложения повреждены или модифицированы", 5);
                ShowRestoreButton();
                return;
            }
            catch (UnauthorizedAccessException)
            {
                ShowLoginPanel();
                ShowLoginError("Неверный email или пароль");
                SetLoginControlsEnabled(true);
                return;
            }
            catch (Exception ex)
            {
                ShowLoginPanel();
                ShowLoginError("Ошибка подключения к серверу конфигурации");
                Logger.Error("Ошибка получения конфига", ex);
                SetLoginControlsEnabled(true);
                return;
            }

            try
            {
                ApplyConfigToProgram(cfg);
            }
            catch (Exception ex)
            {
                ShowLoginPanel();
                ShowLoginError("Не удалось разобрать конфигурацию сервера");
                Logger.Error("Ошибка разбора конфига", ex);
                SetLoginControlsEnabled(true);
                return;
            }

            SetStatusProgress("Подключение к базе данных...", 20);
            try
            {
                await DataBase.TryConnectAsync();
            }
            catch (Exception ex)
            {
                ShowLoginPanel();
                ShowLoginError("Не удалось подключиться к базе данных");
                Logger.Error("Ошибка подключения к БД", ex);
                SetLoginControlsEnabled(true);
                return;
            }

            SetStatusProgress("Проверка учётных данных...", 35);
            bool verified = await UserData.VerifyUser(email, password);

            if (!verified)
            {
                ShowLoginPanel();
                ShowLoginError("Неверный email или пароль");
                SetLoginControlsEnabled(true);
                return;
            }

            SetStatusProgress("Обновление данных устройства...", 40);
            string motherboardId = SecurityConfig.GetMotherboardId();
            if (!string.IsNullOrEmpty(motherboardId))
            {
                await UserData.RegisterOrUpdateDevice(
                    motherboardId,
                    permanentAccess: _chkRemember.Checked);
            }

            await Task.Delay(200);
            await Step2_CheckUpdates();
        }

        // ===================================================================
        // Rate Limit — таймер обратного отсчёта
        // ===================================================================

        /// <summary>
        /// Запускает таймер обратного отсчёта на _lblLoginError.
        /// Блокирует кнопку Войти на время retryAfterSeconds.
        /// По истечении — разблокирует кнопку.
        /// </summary>
        private void StartRateLimitCountdown(int retryAfterSeconds)
        {
            // Останавливаем предыдущий таймер если был
            StopRateLimitTimer();

            _rateLimitSecondsLeft = retryAfterSeconds;
            SetLoginControlsEnabled(false);
            UpdateRateLimitLabel();

            _rateLimitTimer          = new System.Windows.Forms.Timer();
            _rateLimitTimer.Interval = 1000;
            _rateLimitTimer.Tick    += RateLimitTimer_Tick;
            _rateLimitTimer.Start();
        }

        private void RateLimitTimer_Tick(object sender, EventArgs e)
        {
            _rateLimitSecondsLeft--;

            if (_rateLimitSecondsLeft <= 0)
            {
                StopRateLimitTimer();
                ShowLoginError("");
                SetLoginControlsEnabled(true);
                _txtEmail.Focus();
            }
            else
            {
                UpdateRateLimitLabel();
            }
        }

        private void UpdateRateLimitLabel()
        {
            ShowLoginError($"Слишком много попыток. Попробуйте через {_rateLimitSecondsLeft} сек.");
        }

        private void StopRateLimitTimer()
        {
            if (_rateLimitTimer != null)
            {
                _rateLimitTimer.Stop();
                _rateLimitTimer.Tick -= RateLimitTimer_Tick;
                _rateLimitTimer.Dispose();
                _rateLimitTimer = null;
            }
        }

        // ===================================================================
        // Этап 2 — Проверка обновлений
        // ===================================================================

        private async Task Step2_CheckUpdates()
        {
            SetStatusProgress("Проверка обновлений...", 65);
            HideAllButtons();

            try
            {
                UpdateManager.InvalidateCache();
                var info = await UpdateManager.CheckForUpdates();
                _cachedUpdateInfo = info;

                if (!info.IsCurrentVersionSupported)
                {
                    string notes = string.IsNullOrEmpty(info.ReleaseNotes) ? "" : $"  ({info.ReleaseNotes})";
                    SetStatusProgress($"⚠  Требуется обновление до v{info.LatestVersion}{notes}", 65);
                    ShowUpdateButtonOnly(info);
                    return;
                }

                if (!info.IsUpdateAvailable)
                {
                    SetStatusProgress("✓  Обновлений нет", 80);
                    await Task.Delay(400);
                    OpenRoleForm();
                    return;
                }

                string text = string.IsNullOrEmpty(info.ReleaseNotes)
                    ? $"↓  Доступно обновление v{info.LatestVersion}"
                    : $"↓  Доступно v{info.LatestVersion} — {info.ReleaseNotes}";
                SetStatusProgress(text, 70);
                ShowUpdateOrLaterButtons(info);
            }
            catch (Exception ex)
            {
                Logger.Warning("Не удалось проверить обновления: " + ex.Message);
                SetStatusProgress("⚠  Проверка обновлений недоступна, продолжение...", 75);
                await Task.Delay(800);
                OpenRoleForm();
            }
        }

        private void OpenRoleForm()
        {
            SetStatusProgress("✓  Добро пожаловать!", 100);
            UserData.OpenRoleForm(UserData.CurrentUser.Employee, this);
        }

        private async Task DownloadAndInstall(UpdateInfo info)
        {
            HideAllButtons();
            var progress = new Progress<int>(pct =>
                SetStatusProgress($"↓  Скачивание... {pct}%", 65 + pct * 25 / 100));
            bool downloaded = await UpdateManager.DownloadUpdate(info, progress);
            if (!downloaded)
            {
                SetStatusProgress("✕  Не удалось скачать обновление. Переустановите приложение.", 65);
                return;
            }
            SetStatusProgress("⚙  Применение обновления...", 92);
            bool applied = await UpdateManager.ApplyUpdate();
            if (applied)
            {
                SetStatusProgress($"✓  Обновление v{info.LatestVersion} установлено. Перезапуск...", 100);
                await Task.Delay(1500);
                Program.AppExit();
            }
            else
            {
                SetStatusProgress("✕  Не удалось применить обновление. Переустановите приложение.", 65);
            }
        }

        // ===================================================================
        // Применение конфига в Program
        // ===================================================================

        private static void ApplyConfigToProgram(SecurityConfig.ServerConfig cfg)
        {
            Program.ServerIP       = cfg.ServerIP;
            Program.ServerPort     = cfg.ServerPort;
            Program.ServerDatabase = cfg.ServerDatabase;
            Program.ServerUser     = cfg.ServerUser;
            Program.ServerPassword = cfg.ServerPassword;

            // Устанавливаем стартовую позицию карты из конфига сервера.
            // Если координаты некорректны — cfg.Lat/Lng уже содержат дефолт (Москва),
            // выставленный в ParseAndApplyCoords.
            Map.startPosition = new GMap.NET.PointLatLng(cfg.Lat, cfg.Lng);
        }

        // ===================================================================
        // UI-методы
        // ===================================================================

        private void ShowLoginPanel()
        {
            _loginPanel.Visible    = true;
            _lblStatus.Visible     = false;
            _progressTrack.Visible = false;
            HideAllButtons();
            ShowLoginError("");
            SetLoginControlsEnabled(true);
        }

        private void ShowProgressMode()
        {
            _loginPanel.Visible    = false;
            _lblStatus.Visible     = true;
            _progressTrack.Visible = true;
        }

        private void SetStatusProgress(string status, int progress)
        {
            if (InvokeRequired) { Invoke(new Action<string, int>(SetStatusProgress), status, progress); return; }
            _lblStatus.Text = status;
            int w = (int)(_progressTrack.Width * Math.Min(Math.Max(progress, 0), 100) / 100.0);
            _progressBar.Size = new Size(Math.Max(w, 0), _progressTrack.Height);
        }

        private void ShowLoginError(string msg)
        {
            if (InvokeRequired) { Invoke(new Action<string>(ShowLoginError), msg); return; }
            _lblLoginError.Text    = msg;
            _lblLoginError.Visible = !string.IsNullOrEmpty(msg);
        }

        private void SetLoginControlsEnabled(bool enabled)
        {
            if (InvokeRequired) { Invoke(new Action<bool>(SetLoginControlsEnabled), enabled); return; }
            _btnLogin.Enabled    = enabled;
            _txtEmail.Enabled    = enabled;
            _txtPassword.Enabled = enabled;
            _chkRemember.Enabled = enabled;
            _btnLogin.Text       = enabled ? "Войти" : "Подождите...";
        }

        private void HideAllButtons()
        {
            if (InvokeRequired) { Invoke(new Action(HideAllButtons)); return; }
            if (_retryHandler  != null) { _btnRetry.Click       -= _retryHandler;  _retryHandler  = null; }
            if (_updateHandler != null) { _btnUpdate.Click      -= _updateHandler; _updateHandler = null; }
            if (_remindHandler != null) { _btnRemindLater.Click -= _remindHandler; _remindHandler = null; }
            _btnRetry.Visible = _btnUpdate.Visible = _btnRemindLater.Visible = false;
        }

        private void ShowRestoreButton()
        {
            _updateHandler = (s, e) => { new IntegrityCheckForm().Show(); };
            _btnUpdate.Click += _updateHandler;
            _btnUpdate.BackColor = Color.FromArgb(170, 40, 40);
            _btnUpdate.Text = "↺  Восстановить файлы";
            _btnUpdate.Location = new Point(130, 232); _btnUpdate.Size = new Size(200, 38);
            _btnUpdate.Visible = true;
            ApplyRounded(_btnUpdate, 10);
        }

        private void ShowUpdateButtonOnly(UpdateInfo info)
        {
            _updateHandler = async (s, e) => await DownloadAndInstall(info);
            _btnUpdate.Click += _updateHandler;
            _btnUpdate.BackColor = Color.FromArgb(25, 55, 255);
            _btnUpdate.Text = "↓  Обновить сейчас";
            _btnUpdate.Location = new Point(130, 232); _btnUpdate.Size = new Size(200, 38);
            _btnUpdate.Visible = true;
            ApplyRounded(_btnUpdate, 10);
        }

        private void ShowUpdateOrLaterButtons(UpdateInfo info)
        {
            _updateHandler = async (s, e) => await DownloadAndInstall(info);
            _remindHandler = async (s, e) => { HideAllButtons(); OpenRoleForm(); };
            _btnUpdate.Click      += _updateHandler;
            _btnRemindLater.Click += _remindHandler;
            _btnUpdate.BackColor       = Color.FromArgb(25, 55, 255); _btnUpdate.Text      = "↓  Обновить сейчас";
            _btnRemindLater.BackColor  = Color.FromArgb(60, 80, 140); _btnRemindLater.Text = "⏱  Позже";
            _btnUpdate.Location      = new Point(60, 232);  _btnUpdate.Size      = new Size(210, 38);
            _btnRemindLater.Location = new Point(286, 232); _btnRemindLater.Size = new Size(114, 38);
            _btnUpdate.Visible = _btnRemindLater.Visible = true;
            ApplyRounded(_btnUpdate, 10); ApplyRounded(_btnRemindLater, 10);
        }

        // ===================================================================
        // Обработчики клавиш и окна
        // ===================================================================

        private void TxtEmail_KeyDown(object sender, KeyEventArgs e)    { if (e.KeyCode == Keys.Enter) _txtPassword.Focus(); }
        private void TxtPassword_KeyDown(object sender, KeyEventArgs e) { if (e.KeyCode == Keys.Enter) _btnLogin.PerformClick(); }
        private void BtnMinimize_Click(object sender, EventArgs e) => this.WindowState = FormWindowState.Minimized;
        private void BtnClose_Click(object sender, EventArgs e)    { StopRateLimitTimer(); Logger.Info("Закрытие на стартовом экране"); Program.AppExit(); }

        private void Form_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0); }
        }

        private static void SetHover(Button btn, Color normal, Color hover)
        {
            btn.MouseEnter += (s, e) => btn.BackColor = hover;
            btn.MouseLeave += (s, e) => btn.BackColor = normal;
        }

        private static void ApplyRounded(Control ctl, int radius)
        {
            if (ctl.Width == 0 || ctl.Height == 0) return;
            int r = Math.Min(radius * 2, Math.Min(ctl.Width, ctl.Height));
            using (var path = new GraphicsPath())
            {
                path.AddArc(0,             0,              r, r, 180, 90);
                path.AddArc(ctl.Width - r, 0,              r, r, 270, 90);
                path.AddArc(ctl.Width - r, ctl.Height - r, r, r,   0, 90);
                path.AddArc(0,             ctl.Height - r, r, r,  90, 90);
                path.CloseFigure();
                ctl.Region = new Region(path);
            }
        }
    }
}
