namespace Интерфейс
{
    partial class StartupForm
    {
        private System.ComponentModel.IContainer components = null;

        // ── Шапка / лого ────────────────────────────────────────────
        private System.Windows.Forms.Label _lblLogo;
        private System.Windows.Forms.Label _lblAppTitle;
        private System.Windows.Forms.Panel _separatorTop;

        // ── Этап 0: панель входа ─────────────────────────────────────
        private System.Windows.Forms.Panel _loginPanel;
        private System.Windows.Forms.Label _lblLoginTitle;
        private System.Windows.Forms.Label _lblEmailHint;
        private System.Windows.Forms.TextBox _txtEmail;
        private System.Windows.Forms.Label _lblPasswordHint;
        private System.Windows.Forms.TextBox _txtPassword;
        private System.Windows.Forms.CheckBox _chkRemember;
        private System.Windows.Forms.Button _btnLogin;
        private System.Windows.Forms.Label _lblLoginError;

        // ── Этапы 1-2: статус и прогресс ────────────────────────────
        private System.Windows.Forms.Label _lblStatus;
        private System.Windows.Forms.Panel _progressTrack;
        private System.Windows.Forms.ProgressBar _progressBar;
        private System.Windows.Forms.Button _btnRetry;
        private System.Windows.Forms.Button _btnUpdate;
        private System.Windows.Forms.Button _btnRemindLater;

        // ── Нижняя часть ─────────────────────────────────────────────
        private System.Windows.Forms.Panel _separatorBottom;
        private System.Windows.Forms.Button _btnMinimize;
        private System.Windows.Forms.Button _btnClose;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(StartupForm));
            this._lblLogo = new System.Windows.Forms.Label();
            this._lblAppTitle = new System.Windows.Forms.Label();
            this._separatorTop = new System.Windows.Forms.Panel();
            this._loginPanel = new System.Windows.Forms.Panel();
            this._lblLoginTitle = new System.Windows.Forms.Label();
            this._lblEmailHint = new System.Windows.Forms.Label();
            this._txtEmail = new System.Windows.Forms.TextBox();
            this._lblPasswordHint = new System.Windows.Forms.Label();
            this._txtPassword = new System.Windows.Forms.TextBox();
            this._chkRemember = new System.Windows.Forms.CheckBox();
            this._btnLogin = new System.Windows.Forms.Button();
            this._lblLoginError = new System.Windows.Forms.Label();
            this._lblStatus = new System.Windows.Forms.Label();
            this._progressTrack = new System.Windows.Forms.Panel();
            this._progressBar = new System.Windows.Forms.ProgressBar();
            this._btnRetry = new System.Windows.Forms.Button();
            this._btnUpdate = new System.Windows.Forms.Button();
            this._btnRemindLater = new System.Windows.Forms.Button();
            this._separatorBottom = new System.Windows.Forms.Panel();
            this._btnMinimize = new System.Windows.Forms.Button();
            this._btnClose = new System.Windows.Forms.Button();
            this._lblVersion = new CuoreUI.Controls.cuiLabel();
            this._loginPanel.SuspendLayout();
            this._progressTrack.SuspendLayout();
            this.SuspendLayout();
            // 
            // _lblLogo
            // 
            this._lblLogo.Font = new System.Drawing.Font("Segoe UI Emoji", 42F);
            this._lblLogo.ForeColor = System.Drawing.Color.White;
            this._lblLogo.Location = new System.Drawing.Point(0, 30);
            this._lblLogo.Name = "_lblLogo";
            this._lblLogo.Size = new System.Drawing.Size(460, 80);
            this._lblLogo.TabIndex = 0;
            this._lblLogo.Text = "📬";
            this._lblLogo.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this._lblLogo.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Form_MouseDown);
            // 
            // _lblAppTitle
            // 
            this._lblAppTitle.BackColor = System.Drawing.Color.Transparent;
            this._lblAppTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 16F, System.Drawing.FontStyle.Bold);
            this._lblAppTitle.ForeColor = System.Drawing.Color.White;
            this._lblAppTitle.Location = new System.Drawing.Point(0, 116);
            this._lblAppTitle.Name = "_lblAppTitle";
            this._lblAppTitle.Size = new System.Drawing.Size(460, 32);
            this._lblAppTitle.TabIndex = 1;
            this._lblAppTitle.Text = "Почтовое приложение";
            this._lblAppTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this._lblAppTitle.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Form_MouseDown);
            // 
            // _separatorTop
            // 
            this._separatorTop.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(100)))), ((int)(((byte)(200)))));
            this._separatorTop.Location = new System.Drawing.Point(60, 158);
            this._separatorTop.Name = "_separatorTop";
            this._separatorTop.Size = new System.Drawing.Size(340, 1);
            this._separatorTop.TabIndex = 2;
            // 
            // _loginPanel
            // 
            this._loginPanel.BackColor = System.Drawing.Color.Transparent;
            this._loginPanel.Controls.Add(this._lblLoginTitle);
            this._loginPanel.Controls.Add(this._lblEmailHint);
            this._loginPanel.Controls.Add(this._txtEmail);
            this._loginPanel.Controls.Add(this._lblPasswordHint);
            this._loginPanel.Controls.Add(this._txtPassword);
            this._loginPanel.Controls.Add(this._chkRemember);
            this._loginPanel.Controls.Add(this._btnLogin);
            this._loginPanel.Controls.Add(this._lblLoginError);
            this._loginPanel.Location = new System.Drawing.Point(60, 166);
            this._loginPanel.Name = "_loginPanel";
            this._loginPanel.Size = new System.Drawing.Size(340, 218);
            this._loginPanel.TabIndex = 20;
            this._loginPanel.Visible = false;
            // 
            // _lblLoginTitle
            // 
            this._lblLoginTitle.BackColor = System.Drawing.Color.Transparent;
            this._lblLoginTitle.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this._lblLoginTitle.ForeColor = System.Drawing.Color.White;
            this._lblLoginTitle.Location = new System.Drawing.Point(0, 2);
            this._lblLoginTitle.Name = "_lblLoginTitle";
            this._lblLoginTitle.Size = new System.Drawing.Size(340, 26);
            this._lblLoginTitle.TabIndex = 0;
            this._lblLoginTitle.Text = "Вход в систему";
            this._lblLoginTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // _lblEmailHint
            // 
            this._lblEmailHint.BackColor = System.Drawing.Color.Transparent;
            this._lblEmailHint.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this._lblEmailHint.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(150)))), ((int)(((byte)(170)))), ((int)(((byte)(220)))));
            this._lblEmailHint.Location = new System.Drawing.Point(2, 38);
            this._lblEmailHint.Name = "_lblEmailHint";
            this._lblEmailHint.Size = new System.Drawing.Size(100, 16);
            this._lblEmailHint.TabIndex = 1;
            this._lblEmailHint.Text = "Email";
            // 
            // _txtEmail
            // 
            this._txtEmail.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(22)))), ((int)(((byte)(42)))), ((int)(((byte)(100)))));
            this._txtEmail.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._txtEmail.Font = new System.Drawing.Font("Segoe UI", 10F);
            this._txtEmail.ForeColor = System.Drawing.Color.White;
            this._txtEmail.Location = new System.Drawing.Point(0, 55);
            this._txtEmail.Name = "_txtEmail";
            this._txtEmail.Size = new System.Drawing.Size(340, 25);
            this._txtEmail.TabIndex = 2;
            this._txtEmail.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TxtEmail_KeyDown);
            // 
            // _lblPasswordHint
            // 
            this._lblPasswordHint.BackColor = System.Drawing.Color.Transparent;
            this._lblPasswordHint.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this._lblPasswordHint.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(150)))), ((int)(((byte)(170)))), ((int)(((byte)(220)))));
            this._lblPasswordHint.Location = new System.Drawing.Point(2, 83);
            this._lblPasswordHint.Name = "_lblPasswordHint";
            this._lblPasswordHint.Size = new System.Drawing.Size(100, 16);
            this._lblPasswordHint.TabIndex = 3;
            this._lblPasswordHint.Text = "Пароль";
            // 
            // _txtPassword
            // 
            this._txtPassword.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(22)))), ((int)(((byte)(42)))), ((int)(((byte)(100)))));
            this._txtPassword.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._txtPassword.Font = new System.Drawing.Font("Segoe UI", 10F);
            this._txtPassword.ForeColor = System.Drawing.Color.White;
            this._txtPassword.Location = new System.Drawing.Point(0, 102);
            this._txtPassword.Name = "_txtPassword";
            this._txtPassword.Size = new System.Drawing.Size(340, 25);
            this._txtPassword.TabIndex = 4;
            this._txtPassword.UseSystemPasswordChar = true;
            this._txtPassword.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TxtPassword_KeyDown);
            // 
            // _chkRemember
            // 
            this._chkRemember.BackColor = System.Drawing.Color.Transparent;
            this._chkRemember.Font = new System.Drawing.Font("Segoe UI", 9F);
            this._chkRemember.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(150)))), ((int)(((byte)(170)))), ((int)(((byte)(220)))));
            this._chkRemember.Location = new System.Drawing.Point(4, 133);
            this._chkRemember.Name = "_chkRemember";
            this._chkRemember.Size = new System.Drawing.Size(118, 20);
            this._chkRemember.TabIndex = 5;
            this._chkRemember.Text = "Запомнить меня";
            this._chkRemember.UseVisualStyleBackColor = false;
            // 
            // _btnLogin
            // 
            this._btnLogin.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(55)))), ((int)(((byte)(200)))));
            this._btnLogin.Cursor = System.Windows.Forms.Cursors.Hand;
            this._btnLogin.FlatAppearance.BorderSize = 0;
            this._btnLogin.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(10)))), ((int)(((byte)(35)))), ((int)(((byte)(170)))));
            this._btnLogin.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnLogin.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this._btnLogin.ForeColor = System.Drawing.Color.White;
            this._btnLogin.Location = new System.Drawing.Point(0, 179);
            this._btnLogin.Name = "_btnLogin";
            this._btnLogin.Size = new System.Drawing.Size(340, 36);
            this._btnLogin.TabIndex = 6;
            this._btnLogin.Text = "Войти";
            this._btnLogin.UseVisualStyleBackColor = false;
            this._btnLogin.Click += new System.EventHandler(this.BtnLogin_Click);
            // 
            // _lblLoginError
            // 
            this._lblLoginError.BackColor = System.Drawing.Color.Transparent;
            this._lblLoginError.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this._lblLoginError.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(100)))), ((int)(((byte)(100)))));
            this._lblLoginError.Location = new System.Drawing.Point(0, 156);
            this._lblLoginError.Name = "_lblLoginError";
            this._lblLoginError.Size = new System.Drawing.Size(337, 20);
            this._lblLoginError.TabIndex = 7;
            this._lblLoginError.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this._lblLoginError.Visible = false;
            // 
            // _lblStatus
            // 
            this._lblStatus.BackColor = System.Drawing.Color.Transparent;
            this._lblStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this._lblStatus.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(200)))), ((int)(((byte)(255)))));
            this._lblStatus.Location = new System.Drawing.Point(20, 170);
            this._lblStatus.Name = "_lblStatus";
            this._lblStatus.Size = new System.Drawing.Size(420, 28);
            this._lblStatus.TabIndex = 3;
            this._lblStatus.Text = "Инициализация...";
            this._lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this._lblStatus.Visible = false;
            // 
            // _progressTrack
            // 
            this._progressTrack.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(70)))), ((int)(((byte)(160)))));
            this._progressTrack.Controls.Add(this._progressBar);
            this._progressTrack.Location = new System.Drawing.Point(60, 210);
            this._progressTrack.Name = "_progressTrack";
            this._progressTrack.Size = new System.Drawing.Size(340, 6);
            this._progressTrack.TabIndex = 4;
            this._progressTrack.Visible = false;
            // 
            // _progressBar
            // 
            this._progressBar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(70)))), ((int)(((byte)(160)))));
            this._progressBar.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(93)))), ((int)(((byte)(140)))), ((int)(((byte)(255)))));
            this._progressBar.Location = new System.Drawing.Point(0, 0);
            this._progressBar.Name = "_progressBar";
            this._progressBar.Size = new System.Drawing.Size(0, 6);
            this._progressBar.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this._progressBar.TabIndex = 0;
            // 
            // _btnRetry
            // 
            this._btnRetry.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(53)))), ((int)(((byte)(69)))));
            this._btnRetry.Cursor = System.Windows.Forms.Cursors.Hand;
            this._btnRetry.FlatAppearance.BorderSize = 0;
            this._btnRetry.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnRetry.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold);
            this._btnRetry.ForeColor = System.Drawing.Color.White;
            this._btnRetry.Location = new System.Drawing.Point(130, 232);
            this._btnRetry.Name = "_btnRetry";
            this._btnRetry.Size = new System.Drawing.Size(200, 38);
            this._btnRetry.TabIndex = 5;
            this._btnRetry.Text = "↻  Попробовать снова";
            this._btnRetry.UseVisualStyleBackColor = false;
            this._btnRetry.Visible = false;
            // 
            // _btnUpdate
            // 
            this._btnUpdate.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(55)))), ((int)(((byte)(255)))));
            this._btnUpdate.Cursor = System.Windows.Forms.Cursors.Hand;
            this._btnUpdate.FlatAppearance.BorderSize = 0;
            this._btnUpdate.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnUpdate.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold);
            this._btnUpdate.ForeColor = System.Drawing.Color.White;
            this._btnUpdate.Location = new System.Drawing.Point(60, 232);
            this._btnUpdate.Name = "_btnUpdate";
            this._btnUpdate.Size = new System.Drawing.Size(210, 38);
            this._btnUpdate.TabIndex = 6;
            this._btnUpdate.Text = "↓  Обновить сейчас";
            this._btnUpdate.UseVisualStyleBackColor = false;
            this._btnUpdate.Visible = false;
            // 
            // _btnRemindLater
            // 
            this._btnRemindLater.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(80)))), ((int)(((byte)(140)))));
            this._btnRemindLater.Cursor = System.Windows.Forms.Cursors.Hand;
            this._btnRemindLater.FlatAppearance.BorderSize = 0;
            this._btnRemindLater.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnRemindLater.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold);
            this._btnRemindLater.ForeColor = System.Drawing.Color.White;
            this._btnRemindLater.Location = new System.Drawing.Point(286, 232);
            this._btnRemindLater.Name = "_btnRemindLater";
            this._btnRemindLater.Size = new System.Drawing.Size(114, 38);
            this._btnRemindLater.TabIndex = 7;
            this._btnRemindLater.Text = "⏱  Позже";
            this._btnRemindLater.UseVisualStyleBackColor = false;
            this._btnRemindLater.Visible = false;
            // 
            // _separatorBottom
            // 
            this._separatorBottom.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(100)))), ((int)(((byte)(200)))));
            this._separatorBottom.Location = new System.Drawing.Point(60, 282);
            this._separatorBottom.Name = "_separatorBottom";
            this._separatorBottom.Size = new System.Drawing.Size(340, 1);
            this._separatorBottom.TabIndex = 8;
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
            this._btnMinimize.Location = new System.Drawing.Point(372, 8);
            this._btnMinimize.Name = "_btnMinimize";
            this._btnMinimize.Size = new System.Drawing.Size(34, 22);
            this._btnMinimize.TabIndex = 10;
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
            this._btnClose.Location = new System.Drawing.Point(412, 8);
            this._btnClose.Name = "_btnClose";
            this._btnClose.Size = new System.Drawing.Size(34, 22);
            this._btnClose.TabIndex = 11;
            this._btnClose.Text = "✕";
            this._btnClose.UseVisualStyleBackColor = false;
            this._btnClose.Click += new System.EventHandler(this.BtnClose_Click);
            // 
            // _lblVersion
            // 
            this._lblVersion.Content = "Версия";
            this._lblVersion.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(130)))), ((int)(((byte)(200)))));
            this._lblVersion.HorizontalAlignment = System.Drawing.StringAlignment.Far;
            this._lblVersion.Location = new System.Drawing.Point(12, 390);
            this._lblVersion.Name = "_lblVersion";
            this._lblVersion.Size = new System.Drawing.Size(434, 16);
            this._lblVersion.TabIndex = 21;
            this._lblVersion.VerticalAlignment = System.Drawing.StringAlignment.Center;
            // 
            // StartupForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(11)))), ((int)(((byte)(24)))), ((int)(((byte)(80)))));
            this.ClientSize = new System.Drawing.Size(460, 418);
            this.Controls.Add(this._lblVersion);
            this.Controls.Add(this._btnClose);
            this.Controls.Add(this._btnMinimize);
            this.Controls.Add(this._lblLogo);
            this.Controls.Add(this._lblAppTitle);
            this.Controls.Add(this._separatorTop);
            this.Controls.Add(this._loginPanel);
            this.Controls.Add(this._lblStatus);
            this.Controls.Add(this._progressTrack);
            this.Controls.Add(this._btnRetry);
            this.Controls.Add(this._btnUpdate);
            this.Controls.Add(this._btnRemindLater);
            this.Controls.Add(this._separatorBottom);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "StartupForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Почтовое приложение";
            this._loginPanel.ResumeLayout(false);
            this._loginPanel.PerformLayout();
            this._progressTrack.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        private CuoreUI.Controls.cuiLabel _lblVersion;
    }
}

