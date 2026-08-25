namespace PostalApp
{
    partial class IntegrityCheckForm
    {
        private System.ComponentModel.IContainer components = null;

        // Шапка
        private System.Windows.Forms.Panel _header;
        private System.Windows.Forms.Label _headerTitle;
        private System.Windows.Forms.Button _btnMinimize;
        private System.Windows.Forms.Button _btnClose;

        // Логотип и название
        private System.Windows.Forms.Label _lblIcon;
        private System.Windows.Forms.Label _lblAppInfo;
        private System.Windows.Forms.Panel _separatorTop;

        // Статус и прогресс
        private System.Windows.Forms.Label _lblStatus;
        private System.Windows.Forms.Panel _progressTrack;
        private System.Windows.Forms.ProgressBar _progressBar;
        private System.Windows.Forms.Label _counterLabel;

        // Панель результатов
        private System.Windows.Forms.Panel _resultsPanel;
        private System.Windows.Forms.Label _lblResultsOk;
        private System.Windows.Forms.Label _lblResultsBad;
        private System.Windows.Forms.Panel _resultsSep;
        private System.Windows.Forms.Label _lblFilesTitle;
        private System.Windows.Forms.ListBox _filesListBox;

        // Кнопки действий
        private System.Windows.Forms.Button _btnAction1;
        private System.Windows.Forms.Button _btnAction2;

        // Нижняя часть
        private System.Windows.Forms.Panel _separatorBottom;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(IntegrityCheckForm));
            this._header = new System.Windows.Forms.Panel();
            this._headerTitle = new System.Windows.Forms.Label();
            this._btnMinimize = new System.Windows.Forms.Button();
            this._btnClose = new System.Windows.Forms.Button();
            this._lblIcon = new System.Windows.Forms.Label();
            this._lblAppInfo = new System.Windows.Forms.Label();
            this._separatorTop = new System.Windows.Forms.Panel();
            this._lblStatus = new System.Windows.Forms.Label();
            this._progressTrack = new System.Windows.Forms.Panel();
            this._progressBar = new System.Windows.Forms.ProgressBar();
            this._counterLabel = new System.Windows.Forms.Label();
            this._resultsPanel = new System.Windows.Forms.Panel();
            this._lblResultsOk = new System.Windows.Forms.Label();
            this._lblResultsBad = new System.Windows.Forms.Label();
            this._resultsSep = new System.Windows.Forms.Panel();
            this._lblFilesTitle = new System.Windows.Forms.Label();
            this._filesListBox = new System.Windows.Forms.ListBox();
            this._btnAction1 = new System.Windows.Forms.Button();
            this._btnAction2 = new System.Windows.Forms.Button();
            this._separatorBottom = new System.Windows.Forms.Panel();
            this._lblVersion = new CuoreUI.Controls.cuiLabel();
            this._header.SuspendLayout();
            this._progressTrack.SuspendLayout();
            this._resultsPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // _header
            // 
            this._header.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(11)))), ((int)(((byte)(24)))), ((int)(((byte)(80)))));
            this._header.Controls.Add(this._headerTitle);
            this._header.Controls.Add(this._btnMinimize);
            this._header.Controls.Add(this._btnClose);
            this._header.Location = new System.Drawing.Point(0, 0);
            this._header.Name = "_header";
            this._header.Size = new System.Drawing.Size(500, 44);
            this._header.TabIndex = 0;
            this._header.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Header_MouseDown);
            // 
            // _headerTitle
            // 
            this._headerTitle.BackColor = System.Drawing.Color.Transparent;
            this._headerTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold);
            this._headerTitle.ForeColor = System.Drawing.Color.White;
            this._headerTitle.Location = new System.Drawing.Point(14, 0);
            this._headerTitle.Name = "_headerTitle";
            this._headerTitle.Size = new System.Drawing.Size(380, 44);
            this._headerTitle.TabIndex = 0;
            this._headerTitle.Text = "🔍  Проверка целостности файлов";
            this._headerTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this._headerTitle.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Header_MouseDown);
            // 
            // _btnMinimize
            // 
            this._btnMinimize.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(58)))), ((int)(((byte)(138)))));
            this._btnMinimize.Cursor = System.Windows.Forms.Cursors.Hand;
            this._btnMinimize.FlatAppearance.BorderSize = 0;
            this._btnMinimize.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(100)))), ((int)(((byte)(215)))));
            this._btnMinimize.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(85)))), ((int)(((byte)(190)))));
            this._btnMinimize.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnMinimize.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this._btnMinimize.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(160)))), ((int)(((byte)(190)))), ((int)(((byte)(255)))));
            this._btnMinimize.Location = new System.Drawing.Point(414, 10);
            this._btnMinimize.Name = "_btnMinimize";
            this._btnMinimize.Size = new System.Drawing.Size(34, 22);
            this._btnMinimize.TabIndex = 1;
            this._btnMinimize.Text = "—";
            this._btnMinimize.UseVisualStyleBackColor = false;
            this._btnMinimize.Click += new System.EventHandler(this.BtnMinimize_Click);
            // 
            // _btnClose
            // 
            this._btnClose.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(58)))), ((int)(((byte)(138)))));
            this._btnClose.Cursor = System.Windows.Forms.Cursors.Hand;
            this._btnClose.FlatAppearance.BorderSize = 0;
            this._btnClose.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(140)))), ((int)(((byte)(15)))), ((int)(((byte)(15)))));
            this._btnClose.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this._btnClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnClose.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this._btnClose.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(160)))), ((int)(((byte)(190)))), ((int)(((byte)(255)))));
            this._btnClose.Location = new System.Drawing.Point(454, 10);
            this._btnClose.Name = "_btnClose";
            this._btnClose.Size = new System.Drawing.Size(34, 22);
            this._btnClose.TabIndex = 2;
            this._btnClose.Text = "✕";
            this._btnClose.UseVisualStyleBackColor = false;
            this._btnClose.Click += new System.EventHandler(this.BtnClose_Click);
            // 
            // _lblIcon
            // 
            this._lblIcon.Font = new System.Drawing.Font("Segoe UI Emoji", 28F);
            this._lblIcon.ForeColor = System.Drawing.Color.White;
            this._lblIcon.Location = new System.Drawing.Point(0, 50);
            this._lblIcon.Name = "_lblIcon";
            this._lblIcon.Size = new System.Drawing.Size(500, 56);
            this._lblIcon.TabIndex = 1;
            this._lblIcon.Text = "🗂";
            this._lblIcon.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this._lblIcon.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Form_MouseDown);
            // 
            // _lblAppInfo
            // 
            this._lblAppInfo.BackColor = System.Drawing.Color.Transparent;
            this._lblAppInfo.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Bold);
            this._lblAppInfo.ForeColor = System.Drawing.Color.White;
            this._lblAppInfo.Location = new System.Drawing.Point(0, 108);
            this._lblAppInfo.Name = "_lblAppInfo";
            this._lblAppInfo.Size = new System.Drawing.Size(500, 26);
            this._lblAppInfo.TabIndex = 2;
            this._lblAppInfo.Text = "Почтовое приложение";
            this._lblAppInfo.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this._lblAppInfo.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Form_MouseDown);
            // 
            // _separatorTop
            // 
            this._separatorTop.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(100)))), ((int)(((byte)(200)))));
            this._separatorTop.Location = new System.Drawing.Point(60, 142);
            this._separatorTop.Name = "_separatorTop";
            this._separatorTop.Size = new System.Drawing.Size(380, 1);
            this._separatorTop.TabIndex = 3;
            // 
            // _lblStatus
            // 
            this._lblStatus.BackColor = System.Drawing.Color.Transparent;
            this._lblStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
            this._lblStatus.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(200)))), ((int)(((byte)(255)))));
            this._lblStatus.Location = new System.Drawing.Point(20, 152);
            this._lblStatus.Name = "_lblStatus";
            this._lblStatus.Size = new System.Drawing.Size(460, 22);
            this._lblStatus.TabIndex = 4;
            this._lblStatus.Text = "Инициализация...";
            this._lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // _progressTrack
            // 
            this._progressTrack.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(70)))), ((int)(((byte)(160)))));
            this._progressTrack.Controls.Add(this._progressBar);
            this._progressTrack.Location = new System.Drawing.Point(60, 182);
            this._progressTrack.Name = "_progressTrack";
            this._progressTrack.Size = new System.Drawing.Size(380, 6);
            this._progressTrack.TabIndex = 5;
            // 
            // _progressBar
            // 
            this._progressBar.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(93)))), ((int)(((byte)(140)))), ((int)(((byte)(255)))));
            this._progressBar.Location = new System.Drawing.Point(0, 0);
            this._progressBar.Name = "_progressBar";
            this._progressBar.Size = new System.Drawing.Size(0, 6);
            this._progressBar.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this._progressBar.TabIndex = 0;
            // 
            // _counterLabel
            // 
            this._counterLabel.BackColor = System.Drawing.Color.Transparent;
            this._counterLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            this._counterLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(110)))), ((int)(((byte)(140)))), ((int)(((byte)(210)))));
            this._counterLabel.Location = new System.Drawing.Point(20, 194);
            this._counterLabel.Name = "_counterLabel";
            this._counterLabel.Size = new System.Drawing.Size(460, 18);
            this._counterLabel.TabIndex = 6;
            this._counterLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // _resultsPanel
            // 
            this._resultsPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(18)))), ((int)(((byte)(38)))), ((int)(((byte)(110)))));
            this._resultsPanel.Controls.Add(this._lblResultsOk);
            this._resultsPanel.Controls.Add(this._lblResultsBad);
            this._resultsPanel.Controls.Add(this._resultsSep);
            this._resultsPanel.Controls.Add(this._lblFilesTitle);
            this._resultsPanel.Controls.Add(this._filesListBox);
            this._resultsPanel.Location = new System.Drawing.Point(20, 218);
            this._resultsPanel.Name = "_resultsPanel";
            this._resultsPanel.Size = new System.Drawing.Size(460, 190);
            this._resultsPanel.TabIndex = 7;
            this._resultsPanel.Visible = false;
            // 
            // _lblResultsOk
            // 
            this._lblResultsOk.BackColor = System.Drawing.Color.Transparent;
            this._lblResultsOk.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold);
            this._lblResultsOk.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(220)))), ((int)(((byte)(140)))));
            this._lblResultsOk.Location = new System.Drawing.Point(12, 10);
            this._lblResultsOk.Name = "_lblResultsOk";
            this._lblResultsOk.Size = new System.Drawing.Size(200, 20);
            this._lblResultsOk.TabIndex = 0;
            this._lblResultsOk.Text = "✓  В порядке: 0";
            // 
            // _lblResultsBad
            // 
            this._lblResultsBad.BackColor = System.Drawing.Color.Transparent;
            this._lblResultsBad.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold);
            this._lblResultsBad.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(120)))), ((int)(((byte)(120)))));
            this._lblResultsBad.Location = new System.Drawing.Point(240, 10);
            this._lblResultsBad.Name = "_lblResultsBad";
            this._lblResultsBad.Size = new System.Drawing.Size(208, 20);
            this._lblResultsBad.TabIndex = 1;
            this._lblResultsBad.Text = "✕  Проблем: 0";
            // 
            // _resultsSep
            // 
            this._resultsSep.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(100)))), ((int)(((byte)(200)))));
            this._resultsSep.Location = new System.Drawing.Point(12, 36);
            this._resultsSep.Name = "_resultsSep";
            this._resultsSep.Size = new System.Drawing.Size(436, 1);
            this._resultsSep.TabIndex = 2;
            // 
            // _lblFilesTitle
            // 
            this._lblFilesTitle.BackColor = System.Drawing.Color.Transparent;
            this._lblFilesTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            this._lblFilesTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(150)))), ((int)(((byte)(170)))), ((int)(((byte)(230)))));
            this._lblFilesTitle.Location = new System.Drawing.Point(12, 44);
            this._lblFilesTitle.Name = "_lblFilesTitle";
            this._lblFilesTitle.Size = new System.Drawing.Size(436, 18);
            this._lblFilesTitle.TabIndex = 3;
            this._lblFilesTitle.Text = "Проблемные файлы:";
            this._lblFilesTitle.Visible = false;
            // 
            // _filesListBox
            // 
            this._filesListBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(11)))), ((int)(((byte)(24)))), ((int)(((byte)(80)))));
            this._filesListBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this._filesListBox.Font = new System.Drawing.Font("Courier New", 8F);
            this._filesListBox.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(160)))), ((int)(((byte)(120)))));
            this._filesListBox.ItemHeight = 14;
            this._filesListBox.Location = new System.Drawing.Point(12, 64);
            this._filesListBox.Name = "_filesListBox";
            this._filesListBox.Size = new System.Drawing.Size(436, 112);
            this._filesListBox.TabIndex = 4;
            this._filesListBox.Visible = false;
            // 
            // _btnAction1
            // 
            this._btnAction1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(55)))), ((int)(((byte)(200)))));
            this._btnAction1.Cursor = System.Windows.Forms.Cursors.Hand;
            this._btnAction1.FlatAppearance.BorderSize = 0;
            this._btnAction1.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(10)))), ((int)(((byte)(35)))), ((int)(((byte)(170)))));
            this._btnAction1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnAction1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold);
            this._btnAction1.ForeColor = System.Drawing.Color.White;
            this._btnAction1.Location = new System.Drawing.Point(60, 418);
            this._btnAction1.Name = "_btnAction1";
            this._btnAction1.Size = new System.Drawing.Size(180, 38);
            this._btnAction1.TabIndex = 8;
            this._btnAction1.Text = "Действие 1";
            this._btnAction1.UseVisualStyleBackColor = false;
            this._btnAction1.Visible = false;
            // 
            // _btnAction2
            // 
            this._btnAction2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(80)))), ((int)(((byte)(140)))));
            this._btnAction2.Cursor = System.Windows.Forms.Cursors.Hand;
            this._btnAction2.FlatAppearance.BorderSize = 0;
            this._btnAction2.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(55)))), ((int)(((byte)(110)))));
            this._btnAction2.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnAction2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold);
            this._btnAction2.ForeColor = System.Drawing.Color.White;
            this._btnAction2.Location = new System.Drawing.Point(260, 418);
            this._btnAction2.Name = "_btnAction2";
            this._btnAction2.Size = new System.Drawing.Size(180, 38);
            this._btnAction2.TabIndex = 9;
            this._btnAction2.Text = "Действие 2";
            this._btnAction2.UseVisualStyleBackColor = false;
            this._btnAction2.Visible = false;
            // 
            // _separatorBottom
            // 
            this._separatorBottom.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(100)))), ((int)(((byte)(200)))));
            this._separatorBottom.Location = new System.Drawing.Point(60, 466);
            this._separatorBottom.Name = "_separatorBottom";
            this._separatorBottom.Size = new System.Drawing.Size(380, 1);
            this._separatorBottom.TabIndex = 10;
            // 
            // _lblVersion
            // 
            this._lblVersion.Content = "Версия";
            this._lblVersion.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(130)))), ((int)(((byte)(200)))));
            this._lblVersion.HorizontalAlignment = System.Drawing.StringAlignment.Far;
            this._lblVersion.Location = new System.Drawing.Point(20, 469);
            this._lblVersion.Name = "_lblVersion";
            this._lblVersion.Size = new System.Drawing.Size(460, 19);
            this._lblVersion.TabIndex = 12;
            this._lblVersion.VerticalAlignment = System.Drawing.StringAlignment.Center;
            // 
            // IntegrityCheckForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(11)))), ((int)(((byte)(24)))), ((int)(((byte)(80)))));
            this.ClientSize = new System.Drawing.Size(500, 500);
            this.Controls.Add(this._lblVersion);
            this.Controls.Add(this._header);
            this.Controls.Add(this._lblIcon);
            this.Controls.Add(this._lblAppInfo);
            this.Controls.Add(this._separatorTop);
            this.Controls.Add(this._lblStatus);
            this.Controls.Add(this._progressTrack);
            this.Controls.Add(this._counterLabel);
            this.Controls.Add(this._resultsPanel);
            this.Controls.Add(this._btnAction1);
            this.Controls.Add(this._btnAction2);
            this.Controls.Add(this._separatorBottom);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "IntegrityCheckForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Проверка целостности файлов";
            this._header.ResumeLayout(false);
            this._progressTrack.ResumeLayout(false);
            this._resultsPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        private CuoreUI.Controls.cuiLabel _lblVersion;
    }
}

