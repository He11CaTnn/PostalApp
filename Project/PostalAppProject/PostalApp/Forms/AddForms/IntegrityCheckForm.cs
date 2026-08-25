using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PostalApp
{
    /// <summary>
    /// Форма полной проверки целостности файлов приложения.
    /// Проверяет все файлы по version_manifest.json.
    /// Может восстановить файлы через update.zip текущей версии.
    /// </summary>
    public partial class IntegrityCheckForm : Form
    {
        [DllImport("user32.dll")] private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")] private static extern bool ReleaseCapture();
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTCAPTION = 0x2;

        private IntegrityReport _report;
        private UpdateInfo _updateInfo;

        public IntegrityCheckForm()
        {
            InitializeComponent();
            SetupPostInit();
        }

        private void SetupPostInit()
        {
            ApplyRounded(this, 18);
            this.Resize += (s, e) => ApplyRounded(this, 18);

            ApplyRounded(_btnMinimize, 6);
            ApplyRounded(_btnClose, 6);
            ApplyRounded(_btnAction1, 10);
            ApplyRounded(_btnAction2, 10);
            _btnAction1.Resize += (s, e) => ApplyRounded(_btnAction1, 10);
            _btnAction2.Resize += (s, e) => ApplyRounded(_btnAction2, 10);

            ApplyRounded(_resultsPanel, 10);
            _resultsPanel.Resize += (s, e) => ApplyRounded(_resultsPanel, 10);
            ApplyRounded(_progressTrack, 5);

            Program.StartCustomizationVersionLabel(_lblVersion);
            this.MouseDown += Form_MouseDown;
        }

        // ===================================================================
        // OnLoad
        // ===================================================================

        protected override async void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            await RunIntegrityCheck();
        }

        // ===================================================================
        // OnShown
        // ===================================================================
        
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            this.Activate();
        }

        // ===================================================================
        // Главная последовательность
        // ===================================================================

        private async Task RunIntegrityCheck()
        {
            string currentVersion = UpdateManager.GetCurrentVersion();

            SetStatus("Загрузка манифеста версии...", 5);

            bool manifestOk = true;
            try
            {
                await UpdateManager.GetVersionManifest(currentVersion);
            }
            catch (Exception ex)
            {
                Logger.Error("Не удалось загрузить версионный манифест", ex);
                SetStatus("✕  Не удалось загрузить манифест с сервера", 5);
                manifestOk = false;
            }

            if (manifestOk)
            {
                SetStatus("Проверка файлов приложения...", 10);

                var progress = new Progress<IntegrityProgress>(p =>
                {
                    SetStatus($"Проверка: {p.FileName}", 10 + p.Current * 60 / p.Total);
                    SetCounter($"{p.Current} из {p.Total} файлов");
                });

                try
                {
                    _report = await IntegrityChecker.CheckAllFiles(currentVersion, progress);
                    SetStatus(_report.HasErrors
                        ? $"✕  Найдены проблемы: {_report.MissingCount + _report.CorruptedCount} файлов"
                        : "✓  Все файлы в порядке", 70);
                }
                catch (Exception ex)
                {
                    Logger.Error("Ошибка проверки файлов", ex);
                    SetStatus("⚠  Проверка файлов не удалась", 70);
                    manifestOk = false;
                }

                await Task.Delay(400);
            }

            SetStatus("Проверка обновлений...", 80);
            SetCounter("");

            try
            {
                _updateInfo = await UpdateManager.CheckForUpdates();
            }
            catch (Exception ex)
            {
                Logger.Warning("Не удалось проверить обновления: " + ex.Message);
                _updateInfo = null;
            }

            SetStatus("Готово", 100);
            await Task.Delay(300);

            ShowResults(manifestOk);
        }

        // ===================================================================
        // Отображение результатов и кнопок
        // ===================================================================

        private void ShowResults(bool manifestChecked)
        {
            if (InvokeRequired) { Invoke(new Action<bool>(ShowResults), manifestChecked); return; }

            bool hasErrors = manifestChecked && _report != null && _report.HasErrors;
            bool updateAvailable = _updateInfo != null && _updateInfo.IsUpdateAvailable;

            if (manifestChecked && _report != null)
            {
                _lblResultsOk.Text = $"✓  В порядке: {_report.OkCount} из {_report.TotalCount}";
                _lblResultsBad.Text = hasErrors
                    ? $"✕  Проблем: {_report.MissingCount + _report.CorruptedCount}"
                    : "✓  Проблем нет";
                _lblResultsBad.ForeColor = hasErrors
                    ? Color.FromArgb(255, 120, 120)
                    : Color.FromArgb(100, 220, 140);

                if (hasErrors)
                {
                    _lblFilesTitle.Visible = true;
                    _filesListBox.Visible = true;
                    _filesListBox.Items.Clear();
                    foreach (var bad in _report.BadFiles)
                        _filesListBox.Items.Add($"  [{bad.StatusText}]  {bad.Path}");
                }
            }
            else
            {
                _lblResultsOk.Text = "—  Манифест недоступен";
                _lblResultsBad.Text = "Проверка файлов не выполнена";
                _lblResultsBad.ForeColor = Color.FromArgb(200, 180, 100);
            }

            if (updateAvailable)
                _lblResultsBad.Text += $"   |   ↓ v{_updateInfo.LatestVersion} доступна";

            _resultsPanel.Visible = true;

            // Сценарии кнопок:
            //  A: ошибки + есть обновление     → [Обновить] + [Переустановить текущую]
            //  B: ошибки + нет обновления       → [Переустановить текущую]
            //  C: нет ошибок + есть обновление  → [Обновить] + [Закрыть]
            //  D: нет ошибок + нет обновления   → [Закрыть]

            if (hasErrors && updateAvailable)
            {
                ConfigureButton(_btnAction1, $"↓  Обновить до v{_updateInfo.LatestVersion}",
                    Color.FromArgb(25, 55, 200), async () => await DoUpdate());
                ConfigureButton(_btnAction2, "↺  Переустановить текущую",
                    Color.FromArgb(80, 60, 160), async () => await DoReinstallCurrent());
                CenterTwoButtons();
            }
            else if (hasErrors)
            {
                ConfigureButton(_btnAction1, "↺  Восстановить файлы",
                    Color.FromArgb(80, 60, 160), async () => await DoReinstallCurrent());
                CenterOneButton(_btnAction1);
                _btnAction2.Visible = false;
            }
            else if (updateAvailable)
            {
                ConfigureButton(_btnAction1, $"↓  Обновить до v{_updateInfo.LatestVersion}",
                    Color.FromArgb(25, 55, 200), async () => await DoUpdate());
                ConfigureButton(_btnAction2, "✕  Закрыть",
                    Color.FromArgb(60, 80, 140), () => { this.Close(); return Task.CompletedTask; });
                CenterTwoButtons();
            }
            else
            {
                SetStatus("✓  Все файлы в порядке. Обновлений нет.", 100);
                ConfigureButton(_btnAction1, "✓  Закрыть",
                    Color.FromArgb(30, 140, 70), () => { this.Close(); return Task.CompletedTask; });
                CenterOneButton(_btnAction1);
                _btnAction2.Visible = false;
            }
        }

        // ===================================================================
        // Действия кнопок
        // ===================================================================

        /// <summary>Скачивает обновление и применяет через ApplyUpdate.</summary>
        private async Task DoUpdate()
        {
            DisableAllButtons();
            SetStatus($"↓  Скачивание обновления v{_updateInfo.LatestVersion}...", 10);
            _resultsPanel.Visible = false;

            var progress = new Progress<int>(pct =>
                SetStatus($"↓  Скачивание... {pct}%", pct));

            bool downloaded = await UpdateManager.DownloadUpdate(_updateInfo, progress);
            if (!downloaded)
            {
                SetStatus("✕  Не удалось скачать обновление", 0);
                EnableAllButtons();
                return;
            }

            SetStatus("⚙  Применение обновления...", 95);
            bool applied = await UpdateManager.ApplyUpdate();

            if (applied)
            {
                SetStatus($"✓  Обновление v{_updateInfo.LatestVersion} установлено. Перезапустите приложение.", 100);
                ShowRestartButton();
            }
            else
            {
                SetStatus("✕  Не удалось применить обновление", 0);
                EnableAllButtons();
            }
        }

        /// <summary>
        /// Скачивает update.zip текущей версии и восстанавливает файлы через ApplyUpdate.
        /// </summary>
        private async Task DoReinstallCurrent()
        {
            DisableAllButtons();
            SetStatus("↓  Скачивание текущей версии для восстановления...", 5);
            _resultsPanel.Visible = false;

            string version = UpdateManager.GetCurrentVersion();
            string downloadUrl = $"http://81.90.25.60/updates/versions/v{version}/update.zip";

            var tempInfo = new UpdateInfo
            {
                Versions = new List<string> { version },
                DownloadUrl = downloadUrl
            };

            var progress = new Progress<int>(pct =>
                SetStatus($"↓  Скачивание... {pct}%", pct));

            bool downloaded = await UpdateManager.DownloadUpdate(tempInfo, progress);
            if (!downloaded)
            {
                SetStatus("✕  Не удалось скачать файлы для восстановления", 0);
                EnableAllButtons();
                return;
            }

            SetStatus("⚙  Восстановление файлов...", 95);
            bool applied = await UpdateManager.ApplyUpdate();

            if (applied)
            {
                SetStatus("✓  Файлы восстановлены. Перезапустите приложение.", 100);
                ShowRestartButton();
            }
            else
            {
                SetStatus("✕  Не удалось восстановить файлы", 0);
                EnableAllButtons();
            }
        }

        private void ShowRestartButton()
        {
            _btnAction1.Enabled = true;
            ConfigureButton(_btnAction1, "↺  Перезапустить сейчас",
                Color.FromArgb(30, 140, 70), () =>
                {
                    // Закрываем все открытые формы приложения кроме текущей
                    foreach (Form f in Application.OpenForms)
                    {
                        if (f != this)
                            f.Close();
                    }
                    Program.AppExit();
                    return Task.CompletedTask;
                });
            CenterOneButton(_btnAction1);
            _btnAction2.Visible = false;
        }

        // ===================================================================
        // Кнопки управления окном
        // ===================================================================

        private void BtnMinimize_Click(object sender, EventArgs e) =>
            this.WindowState = FormWindowState.Minimized;

        private void BtnClose_Click(object sender, EventArgs e) =>
            this.Close();

        // ===================================================================
        // Утилиты UI
        // ===================================================================

        private void SetStatus(string status, int progressPct)
        {
            if (InvokeRequired) { Invoke(new Action<string, int>(SetStatus), status, progressPct); return; }
            _lblStatus.Text = status;
            int w = (int)(_progressTrack.Width * Math.Min(Math.Max(progressPct, 0), 100) / 100.0);
            _progressBar.Size = new Size(Math.Max(w, 0), _progressTrack.Height);
        }

        private void SetCounter(string text)
        {
            if (InvokeRequired) { Invoke(new Action<string>(SetCounter), text); return; }
            _counterLabel.Text = text;
        }

        private void ConfigureButton(Button btn, string text, Color color, Func<Task> action)
        {
            btn.Tag = action;
            btn.Text = text;
            btn.BackColor = color;
            btn.FlatAppearance.MouseOverBackColor = ControlPaint.Dark(color, 0.15f);
            btn.Click -= UniversalButtonClick;
            btn.Click += UniversalButtonClick;
            btn.Visible = true;
        }

        private async void UniversalButtonClick(object sender, EventArgs e)
        {
            if (((Button)sender).Tag is Func<Task> action)
                await action();
        }

        private void CenterOneButton(Button btn)
        {
            btn.Location = new Point((this.ClientSize.Width - btn.Width) / 2, 418);
            btn.Visible = true;
        }

        private void CenterTwoButtons()
        {
            int total = _btnAction1.Width + 20 + _btnAction2.Width;
            int startX = (this.ClientSize.Width - total) / 2;
            _btnAction1.Location = new Point(startX, 418);
            _btnAction2.Location = new Point(startX + _btnAction1.Width + 20, 418);
            _btnAction1.Visible = true;
            _btnAction2.Visible = true;
        }

        private void DisableAllButtons()
        {
            _btnAction1.Enabled = false;
            _btnAction2.Enabled = false;
        }

        private void EnableAllButtons()
        {
            _btnAction1.Enabled = true;
            _btnAction2.Enabled = true;
        }

        private void Header_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0); }
        }

        private void Form_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0); }
        }

        private static void ApplyRounded(Control ctl, int radius)
        {
            if (ctl.Width == 0 || ctl.Height == 0) return;
            int r = Math.Min(radius * 2, Math.Min(ctl.Width, ctl.Height));
            using (var path = new GraphicsPath())
            {
                path.AddArc(0, 0, r, r, 180, 90);
                path.AddArc(ctl.Width - r, 0, r, r, 270, 90);
                path.AddArc(ctl.Width - r, ctl.Height - r, r, r, 0, 90);
                path.AddArc(0, ctl.Height - r, r, r, 90, 90);
                path.CloseFigure();
                ctl.Region = new Region(path);
            }
        }
    }
}
