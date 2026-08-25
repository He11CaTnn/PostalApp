using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PostalApp_Extra
{
    public partial class LoginForm : Form
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ReleaseCapture();
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTCAPTION        = 0x2;

        // ── API ───────────────────────────────────────────────────────
        private const string ConfigApiUrl = "https://<ip_адрес>/api/getconfig_extra";

        // Fingerprint сертификата сервера (SHA-256 от raw DER).
        private const string ExpectedCertFingerprint =
            "<полученный_хэш>";

        // ── Обработчики кнопок (для безопасной отписки при повторном показе) ──
        private EventHandler _updateHandler;
        private EventHandler _remindHandler;

        // ── Кэш манифеста с этапа 0 ──────────────────────────────────
        private UpdateInfo _cachedUpdateInfo;

        // ── Таймер блокировки (rate limit) ────────────────────────────
        private Timer _rateLimitTimer;
        private int   _rateLimitSecondsLeft;

        // =================================================================
        // КОНСТРУКТОР
        // =================================================================

        public LoginForm()
        {
            InitializeComponent();
            ApplyRoundedCorners();
            _lblVersion.Text = $"Версия {Program.version}";
        }

        // ── Скруглённые углы для формы и кнопок ──────────────────────
        private void ApplyRoundedCorners()
        {
            ApplyRounded(this,            18);
            Resize += (s, e) => ApplyRounded(this, 18);
            ApplyRounded(_btnMinimize,     6);
            ApplyRounded(_btnClose,        6);
            ApplyRounded(_btnLogin,        8);
            ApplyRounded(_btnUpdate,      10);
            ApplyRounded(_btnRemindLater, 10);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            _ = RunStartupSequence();
        }

        // =================================================================
        // ГЛАВНАЯ ПОСЛЕДОВАТЕЛЬНОСТЬ ЗАПУСКА
        // =================================================================

        private async Task RunStartupSequence()
        {
            ShowProgressMode();
            bool versionOk = await Step0_VersionCheck();
            if (!versionOk) return;

            ShowLoginPanel();
            _txtEmail.Focus();
        }

        // =================================================================
        // ЭТАП 0 — Проверка версии
        // =================================================================

        private async Task<bool> Step0_VersionCheck()
        {
            SetStatusProgress("Проверка версии приложения...", 5);
            try
            {
                _cachedUpdateInfo = await UpdateManagerExtra.CheckForUpdates();

                if (!_cachedUpdateInfo.IsCurrentVersionSupported)
                {
                    string notes = string.IsNullOrEmpty(_cachedUpdateInfo.ReleaseNotes)
                        ? ""
                        : $"  ({_cachedUpdateInfo.ReleaseNotes})";

                    SetStatusProgress(
                        $"⚠  Версия {UpdateManagerExtra.GetCurrentVersion()} устарела. " +
                        $"Требуется обновление до v{_cachedUpdateInfo.LatestVersion}{notes}", 3);

                    ShowUpdateButtonOnly(_cachedUpdateInfo);
                    return false;
                }

                return true;
            }
            catch
            {
                // Нет доступа к серверу обновлений — не блокируем вход
                return true;
            }
        }

        // =================================================================
        // РУЧНОЙ ВХОД
        // =================================================================

        private async void BtnLogin_Click(object sender, EventArgs e) =>
            await HandleLoginClick();

        private async Task HandleLoginClick()
        {
            string email    = _txtEmail.Text.Trim();
            string password = _txtPassword.Text;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ShowError("Введите email и пароль");
                return;
            }

            SetControlsEnabled(false);
            ShowError(null);
            ShowProgressMode();
            SetStatusProgress("Подключение к серверу...", 10);

            string body;
            try
            {
                body = await PostLoginRequest(email, password);
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
                        $"⚠  Версия устарела. Требуется обновление до v{_cachedUpdateInfo.LatestVersion}", 10);
                    ShowUpdateButtonOnly(_cachedUpdateInfo);
                }
                else
                {
                    ShowLoginPanel();
                    ShowError("Версия приложения устарела. Обновите или переустановите приложение.");
                    SetControlsEnabled(true);
                }
                return;
            }
            catch (IntegrityFailedException)
            {
                SetStatusProgress("✕  Файлы приложения повреждены или модифицированы", 10);
                ShowRestoreButton();
                return;
            }
            catch (RoleNotAllowedException)
            {
                ShowLoginPanel();
                ShowError("У вас нет доступа к этому приложению");
                SetControlsEnabled(true);
                return;
            }
            catch (UnauthorizedAccessException)
            {
                ShowLoginPanel();
                ShowError("Неверный email или пароль");
                SetControlsEnabled(true);
                return;
            }
            catch (Exception ex)
            {
                ShowLoginPanel();
                ShowError("Ошибка подключения к серверу: " + ex.Message);
                SetControlsEnabled(true);
                return;
            }

            // ── Парсим строку подключения ──────────────────────────────
            SetStatusProgress("Применение конфигурации...", 50);

            var mCfg = Regex.Match(body, "\"config\"\\s*:\\s*\"([^\"]+)\"");
            if (!mCfg.Success)
            {
                ShowLoginPanel();
                ShowError("Сервер вернул неожиданный ответ");
                SetControlsEnabled(true);
                return;
            }

            string[] parts = mCfg.Groups[1].Value.Split('|');
            if (parts.Length < 5 || !int.TryParse(parts[1], out int port))
            {
                ShowLoginPanel();
                ShowError("Некорректный формат конфигурации");
                SetControlsEnabled(true);
                return;
            }

            Program.ServerIP       = parts[0];
            Program.ServerPort     = port;
            Program.ServerDatabase = parts[2];
            Program.ServerUser     = parts[3];
            Program.ServerPassword = parts[4];

            // ── Координаты ────────────────────────────────────────────
            var mLat = Regex.Match(body, "\"lat\"\\s*:\\s*([\\d\\.\\-]+)");
            var mLng = Regex.Match(body, "\"lng\"\\s*:\\s*([\\d\\.\\-]+)");
            if (mLat.Success && mLng.Success &&
                double.TryParse(mLat.Groups[1].Value, NumberStyles.Float,
                    CultureInfo.InvariantCulture, out double lat) &&
                double.TryParse(mLng.Groups[1].Value, NumberStyles.Float,
                    CultureInfo.InvariantCulture, out double lng) &&
                lat >= -90 && lat <= 90 && lng >= -180 && lng <= 180)
            {
                Program.StartLat = lat;
                Program.StartLng = lng;
            }

            await Task.Delay(200);
            await Step2_CheckUpdates();
        }

        // =================================================================
        // ЭТАП 2 — Проверка обновлений после успешного входа
        // =================================================================

        private async Task Step2_CheckUpdates()
        {
            SetStatusProgress("Проверка обновлений...", 65);
            HideAllButtons();

            try
            {
                UpdateManagerExtra.InvalidateCache();
                var info = await UpdateManagerExtra.CheckForUpdates();
                _cachedUpdateInfo = info;

                if (!info.IsCurrentVersionSupported)
                {
                    string notes = string.IsNullOrEmpty(info.ReleaseNotes)
                        ? "" : $"  ({info.ReleaseNotes})";
                    SetStatusProgress(
                        $"⚠  Требуется обновление до v{info.LatestVersion}{notes}", 65);
                    ShowUpdateButtonOnly(info);
                    return;
                }

                if (!info.IsUpdateAvailable)
                {
                    SetStatusProgress("✓  Обновлений нет", 80);
                    await Task.Delay(400);
                    OpenMainForm();
                    return;
                }

                string text = string.IsNullOrEmpty(info.ReleaseNotes)
                    ? $"↓  Доступно обновление v{info.LatestVersion}"
                    : $"↓  Доступно v{info.LatestVersion} — {info.ReleaseNotes}";
                SetStatusProgress(text, 70);
                ShowUpdateOrLaterButtons(info);
            }
            catch
            {
                SetStatusProgress("⚠  Проверка обновлений недоступна, продолжение...", 75);
                await Task.Delay(800);
                OpenMainForm();
            }
        }

        private void OpenMainForm()
        {
            SetStatusProgress("✓  Добро пожаловать!", 100);
            var mainForm = new CreateBalancedRegions();
            mainForm.FormClosed += (s, e) => Close();
            mainForm.Show();
            Hide();
        }

        private async Task DownloadAndInstall(UpdateInfo info)
        {
            HideAllButtons();
            var progress = new Progress<int>(pct =>
                SetStatusProgress($"↓  Скачивание... {pct}%", 65 + pct * 25 / 100));

            bool downloaded = await UpdateManagerExtra.DownloadUpdate(info, progress);
            if (!downloaded)
            {
                SetStatusProgress(
                    "✕  Не удалось скачать обновление. Переустановите приложение.", 65);
                return;
            }

            SetStatusProgress("⚙  Применение обновления...", 92);
            bool applied = await UpdateManagerExtra.ApplyUpdate();
            if (applied)
            {
                SetStatusProgress(
                    $"✓  Обновление v{info.LatestVersion} установлено. Перезапуск...", 100);
                await Task.Delay(1500);
                Program.AppExit();
            }
            else
            {
                SetStatusProgress(
                    "✕  Не удалось применить обновление. Переустановите приложение.", 65);
            }
        }

        // =================================================================
        // HTTP — ЗАПРОС К API СЕРВЕРУ
        // =================================================================

        private async Task<string> PostLoginRequest(string email, string password)
        {
            string version = UpdateManagerExtra.GetCurrentVersion();
            string exeMd5  = UpdateManagerExtra.GetExeMd5();

            using (var http = new HttpClient(CreateSslHandler()))
            {
                http.Timeout = TimeSpan.FromSeconds(15);

                string json = "{" +
                    $"\"login\":\"{EscapeJson(email)}\"," +
                    $"\"password\":\"{EscapeJson(password)}\"," +
                    $"\"version\":\"{EscapeJson(version)}\"," +
                    $"\"exe_md5\":\"{EscapeJson(exeMd5)}\"" +
                    "}";

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response;
                try
                {
                    response = await http.PostAsync(ConfigApiUrl, content);
                }
                catch (HttpRequestException ex)
                {
                    throw new Exception("Нет соединения с сервером", ex);
                }

                string body = await response.Content.ReadAsStringAsync();

                if ((int)response.StatusCode == 429)
                {
                    var m = Regex.Match(body, "\"retry_after\"\\s*:\\s*(\\d+)");
                    int ra = m.Success && int.TryParse(m.Groups[1].Value, out int s) ? s : 60;
                    throw new RateLimitException(ra);
                }

                if ((int)response.StatusCode == 426)
                    throw new VersionTooOldException();

                if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    if (body.IndexOf("Integrity", StringComparison.OrdinalIgnoreCase) >= 0)
                        throw new IntegrityFailedException();
                    throw new RoleNotAllowedException();
                }

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    throw new UnauthorizedAccessException();

                response.EnsureSuccessStatusCode();
                return body;
            }
        }

        // =================================================================
        // SSL PINNING
        // =================================================================

        private static HttpClientHandler CreateSslHandler()
        {
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) =>
            {
                using (var sha256 = SHA256.Create())
                {
                    byte[] hash   = sha256.ComputeHash(cert.RawData);
                    string actual = BitConverter.ToString(hash)
                                        .Replace("-", "").ToUpperInvariant();
                    return string.Equals(actual, ExpectedCertFingerprint,
                                         StringComparison.OrdinalIgnoreCase);
                }
            };
            return handler;
        }

        // =================================================================
        // ТАЙМЕР RATE LIMIT
        // =================================================================

        private void StartRateLimitCountdown(int seconds)
        {
            StopRateLimitTimer();
            _rateLimitSecondsLeft = seconds;
            SetControlsEnabled(false);
            UpdateRateLimitLabel();

            _rateLimitTimer          = new Timer { Interval = 1000 };
            _rateLimitTimer.Tick    += RateLimitTimer_Tick;
            _rateLimitTimer.Start();
        }

        private void RateLimitTimer_Tick(object sender, EventArgs e)
        {
            _rateLimitSecondsLeft--;
            if (_rateLimitSecondsLeft <= 0)
            {
                StopRateLimitTimer();
                ShowError("");
                SetControlsEnabled(true);
                _txtEmail.Focus();
            }
            else
            {
                UpdateRateLimitLabel();
            }
        }

        private void UpdateRateLimitLabel() =>
            ShowError($"Слишком много попыток. Повторите через {_rateLimitSecondsLeft} сек.");

        private void StopRateLimitTimer()
        {
            if (_rateLimitTimer == null) return;
            _rateLimitTimer.Stop();
            _rateLimitTimer.Tick -= RateLimitTimer_Tick;
            _rateLimitTimer.Dispose();
            _rateLimitTimer = null;
        }

        // =================================================================
        // UI — ПЕРЕКЛЮЧЕНИЕ РЕЖИМОВ
        // =================================================================

        private void ShowLoginPanel()
        {
            if (InvokeRequired) { Invoke(new Action(ShowLoginPanel)); return; }
            _loginPanel.Visible    = true;
            _lblStatus.Visible     = false;
            _progressTrack.Visible = false;
            HideAllButtons();
            ShowError("");
            SetControlsEnabled(true);
        }

        private void ShowProgressMode()
        {
            if (InvokeRequired) { Invoke(new Action(ShowProgressMode)); return; }
            _loginPanel.Visible    = false;
            _lblStatus.Visible     = true;
            _progressTrack.Visible = true;
        }

        private void SetStatusProgress(string status, int progress)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string, int>(SetStatusProgress), status, progress);
                return;
            }
            _lblStatus.Text = status;
            int w = (int)(_progressTrack.Width *
                          Math.Min(Math.Max(progress, 0), 100) / 100.0);
            _progressBar.Size = new Size(Math.Max(w, 0), _progressTrack.Height);
        }

        private void ShowError(string msg)
        {
            if (InvokeRequired) { Invoke(new Action<string>(ShowError), msg); return; }
            _lblLoginError.Text    = msg ?? string.Empty;
            _lblLoginError.Visible = !string.IsNullOrEmpty(msg);
        }

        private void SetControlsEnabled(bool enabled)
        {
            if (InvokeRequired) { Invoke(new Action<bool>(SetControlsEnabled), enabled); return; }
            _btnLogin.Enabled    = enabled;
            _txtEmail.Enabled    = enabled;
            _txtPassword.Enabled = enabled;
            _btnLogin.Text       = enabled ? "Войти" : "Подождите...";
        }

        private void HideAllButtons()
        {
            if (InvokeRequired) { Invoke(new Action(HideAllButtons)); return; }
            if (_updateHandler != null) { _btnUpdate.Click      -= _updateHandler; _updateHandler = null; }
            if (_remindHandler != null) { _btnRemindLater.Click -= _remindHandler; _remindHandler = null; }
            _btnUpdate.Visible      = false;
            _btnRemindLater.Visible = false;
        }

        private void ShowRestoreButton()
        {
            _btnUpdate.BackColor = Color.FromArgb(170, 40, 40);
            _btnUpdate.Text      = "↺  Переустановить приложение";
            _btnUpdate.Location  = new Point(130, 285);
            _btnUpdate.Size      = new Size(200, 38);
            _updateHandler       = (s, e) => MessageBox.Show(
                "Файлы приложения повреждены или изменены.\n" +
                "Скачайте и установите приложение заново.",
                "Повреждение файлов",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _btnUpdate.Click  += _updateHandler;
            _btnUpdate.Visible = true;
            ApplyRounded(_btnUpdate, 10);
        }

        private void ShowUpdateButtonOnly(UpdateInfo info)
        {
            _btnUpdate.BackColor = Color.FromArgb(25, 55, 200);
            _btnUpdate.Text      = "↓  Обновить сейчас";
            _btnUpdate.Location  = new Point(130, 285);
            _btnUpdate.Size      = new Size(200, 38);
            _updateHandler       = async (s, e) => await DownloadAndInstall(info);
            _btnUpdate.Click    += _updateHandler;
            _btnUpdate.Visible   = true;
            ApplyRounded(_btnUpdate, 10);
        }

        private void ShowUpdateOrLaterButtons(UpdateInfo info)
        {
            _updateHandler = async (s, e) => await DownloadAndInstall(info);
            _remindHandler = (s, e) => { HideAllButtons(); OpenMainForm(); };

            _btnUpdate.Click      += _updateHandler;
            _btnRemindLater.Click += _remindHandler;

            _btnUpdate.BackColor = Color.FromArgb(25, 55, 200);
            _btnUpdate.Text      = "↓  Обновить сейчас";
            _btnUpdate.Location  = new Point(60, 285);
            _btnUpdate.Size      = new Size(210, 38);

            _btnRemindLater.BackColor = Color.FromArgb(60, 80, 140);
            _btnRemindLater.Text      = "⏱  Позже";
            _btnRemindLater.Location  = new Point(286, 285);
            _btnRemindLater.Size      = new Size(114, 38);

            _btnUpdate.Visible      = true;
            _btnRemindLater.Visible = true;

            ApplyRounded(_btnUpdate,      10);
            ApplyRounded(_btnRemindLater, 10);
        }

        // =================================================================
        // ОБРАБОТЧИКИ КЛАВИШ И ПЕРЕМЕЩЕНИЯ ОКНА
        // =================================================================

        private void TxtEmail_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) _txtPassword.Focus();
        }

        private void TxtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) _btnLogin.PerformClick();
        }

        private void BtnMinimize_Click(object sender, EventArgs e) =>
            WindowState = FormWindowState.Minimized;

        private void BtnClose_Click(object sender, EventArgs e)
        {
            StopRateLimitTimer();
            Program.AppExit();
        }

        private void Form_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
            }
        }

        // =================================================================
        // ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ
        // =================================================================

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

        private static string EscapeJson(string s) =>
            s.Replace("\\", "\\\\").Replace("\"", "\\\"");

        // =================================================================
        // ВНУТРЕННИЕ ИСКЛЮЧЕНИЯ
        // =================================================================

        private class RateLimitException : Exception
        {
            public int RetryAfter { get; }
            public RateLimitException(int retryAfter) { RetryAfter = retryAfter; }
        }

        private class VersionTooOldException : Exception { }

        private class IntegrityFailedException : Exception { }

        private class RoleNotAllowedException : Exception { }
    }
}
