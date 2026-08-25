using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Интерфейс
{
    /// <summary>
    /// Форма валидации Excel-данных.
    /// Фаза 1 — проверка строк с интерактивным исправлением ошибок.
    /// Фаза 2 — запись в базу данных с прогресс-баром.
    /// </summary>
    public partial class ValidationForm : Form
    {
        [DllImport("user32.dll")] private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")] private static extern bool ReleaseCapture();
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTCAPTION = 0x2;

        public ValidationForm()
        {
            InitializeComponent();
            SetupPostInit();
            this.KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) SafeCancel(); };
        }

        private void SetupPostInit()
        {
            // Скруглённые углы формы
            ApplyRounded(this, 14);
            this.Resize += (s, e) => ApplyRounded(this, 14);

            // Скруглённые пилюли фаз
            ApplyRounded(_phase1Pill, 10);
            ApplyRounded(_phase2Pill, 10);
            _phase1Pill.Resize += (s, e) => ApplyRounded(_phase1Pill, 10);
            _phase2Pill.Resize += (s, e) => ApplyRounded(_phase2Pill, 10);

            // Скруглённый прогресс-трек
            ApplyRounded(_progressTrack, 5);

            // Скруглённая панель ошибок
            ApplyRounded(_errorPanel, 12);
            _errorPanel.Resize += (s, e) => ApplyRounded(_errorPanel, 12);

            // Граница формы (рисуем вручную)
            this.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(210, 215, 235), 1))
                    e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            };
        }

        // ===================================================================
        // Обработчики кнопок (объявлены в Designer через Click +=)
        // ===================================================================

        private void HeaderClose_Click(object sender, EventArgs e) => SafeCancel();
        private void FixButton_Click(object sender, EventArgs e) => this.Close();
        private void SkipButton_Click(object sender, EventArgs e) => this.Close();
        private void CancelButton_Click(object sender, EventArgs e) => this.Close();

        private void Header_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
            }
        }

        // ===================================================================
        // Публичный API — Фаза 1 (проверка строк)
        // ===================================================================

        /// <summary>Обновить текст статуса.</summary>
        public void UpdateStatus(string status)
        {
            if (InvokeRequired) { Invoke(new Action<string>(UpdateStatus), status); return; }
            _statusLabel.Text = status;
            _statusLabel.Refresh();
        }

        /// <summary>Обновить прогресс-бар Фазы 1.</summary>
        public void SetProgress(int current, int total)
        {
            if (InvokeRequired) { Invoke(new Action<int, int>(SetProgress), current, total); return; }
            if (total > 0)
            {
                int w = (int)(_progressTrack.Width * (double)Math.Min(current, total) / total);
                _progressBar.Size = new Size(Math.Max(w, 0), _progressTrack.Height);
                _counterLabel.Text = $"{current} из {total} строк";
            }
        }

        /// <summary>
        /// Показать панель ошибки и ждать решения пользователя.
        /// Возвращает OK (исправить), Ignore (пропустить) или Cancel (отменить всё).
        /// </summary>
        public DialogResult ShowError(string column, int row, string value, string message)
        {
            if (InvokeRequired)
                return (DialogResult)Invoke(
                    new Func<string, int, string, string, DialogResult>(ShowError),
                    column, row, value, message);

            _errorPanel.Visible = true;
            _errorTitle.Text = $"⚠  Строка {row}  |  Столбец: «{column}»";
            _errorMessage.Text = message;
            _originalValueBox.Content = value ?? "";
            _correctedValueBox.Content = value ?? "";
            _correctedValueBox.Focus();
            this.Refresh();

            return this.ShowDialog();
        }

        /// <summary>Вернуть исправленное значение из поля ввода.</summary>
        public string GetCorrectedValue() => _correctedValueBox.Content;

        // ===================================================================
        // Публичный API — Фаза 2 (запись в БД)
        // ===================================================================

        /// <summary>Переключить форму в режим Фазы 2 — запись в БД.</summary>
        public void StartPhase2(int totalRecords)
        {
            if (InvokeRequired) { Invoke(new Action<int>(StartPhase2), totalRecords); return; }

            // Фаза 1 — завершена (зелёная)
            _phase1Pill.BackColor = Color.FromArgb(210, 240, 220);
            _phase1Label.ForeColor = Color.FromArgb(30, 140, 70);
            _phase1Label.Text = "✓  Фаза 1: Проверка данных";

            // Фаза 2 — активная (зелёная)
            _phase2Pill.BackColor = Color.FromArgb(25, 120, 60);
            _phase2Label.ForeColor = Color.White;
            _phase2Label.Text = "⬤  Фаза 2: Импорт в БД";

            // Сбросить прогресс и скрыть панель ошибок
            _errorPanel.Visible = false;
            _statusLabel.Text = "Запись в базу данных...";
            _counterLabel.Text = $"0 из {totalRecords} записей";
            _progressBar.Size = new Size(0, _progressTrack.Height);

            this.Refresh();
        }

        /// <summary>Обновить прогресс Фазы 2.</summary>
        public void SetPhase2Progress(int current, int total)
        {
            if (InvokeRequired) { Invoke(new Action<int, int>(SetPhase2Progress), current, total); return; }
            if (total <= 0) return;

            int w = (int)(_progressTrack.Width * (double)Math.Min(current, total) / total);
            int pct = current * 100 / total;

            _progressBar.Size = new Size(Math.Max(w, 0), _progressTrack.Height);
            _counterLabel.Text = $"{current} из {total} записей";
            _statusLabel.Text = $"Запись в базу данных... {pct}%";

            _progressBar.Refresh();
            _counterLabel.Refresh();
            _statusLabel.Refresh();
        }

        // ===================================================================
        // Утилиты
        // ===================================================================

        private void SafeCancel()
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
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
