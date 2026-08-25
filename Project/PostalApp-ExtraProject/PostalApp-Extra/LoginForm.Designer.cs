using System.Drawing;
using System.Windows.Forms;

namespace PostalApp_Extra
{
    partial class LoginForm
    {
        private System.ComponentModel.IContainer components = null;

        // ── Шапка / лого ────────────────────────────────────────────
        private Label _lblLogo;
        private Label _lblAppTitle;
        private Panel _separatorTop;

        // ── Панель входа ─────────────────────────────────────────────
        private Panel  _loginPanel;
        private Label  _lblLoginTitle;
        private Label  _lblEmailHint;
        private TextBox _txtEmail;
        private Label  _lblPasswordHint;
        private TextBox _txtPassword;
        private Button _btnLogin;
        private Label  _lblLoginError;

        // ── Прогресс и статус ────────────────────────────────────────
        private Label  _lblStatus;
        private Panel  _progressTrack;
        private Panel  _progressBar;

        // ── Кнопки обновления ────────────────────────────────────────
        private Button _btnUpdate;
        private Button _btnRemindLater;

        // ── Нижняя часть ─────────────────────────────────────────────
        private Panel  _separatorBottom;
        private Button _btnMinimize;
        private Button _btnClose;
        private Label  _lblVersion;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this._lblLogo          = new System.Windows.Forms.Label();
            this._lblAppTitle      = new System.Windows.Forms.Label();
            this._separatorTop     = new System.Windows.Forms.Panel();
            this._loginPanel       = new System.Windows.Forms.Panel();
            this._lblLoginTitle    = new System.Windows.Forms.Label();
            this._lblEmailHint     = new System.Windows.Forms.Label();
            this._txtEmail         = new System.Windows.Forms.TextBox();
            this._lblPasswordHint  = new System.Windows.Forms.Label();
            this._txtPassword      = new System.Windows.Forms.TextBox();
            this._btnLogin         = new System.Windows.Forms.Button();
            this._lblLoginError    = new System.Windows.Forms.Label();
            this._lblStatus        = new System.Windows.Forms.Label();
            this._progressTrack    = new System.Windows.Forms.Panel();
            this._progressBar      = new System.Windows.Forms.Panel();
            this._btnUpdate        = new System.Windows.Forms.Button();
            this._btnRemindLater   = new System.Windows.Forms.Button();
            this._separatorBottom  = new System.Windows.Forms.Panel();
            this._btnMinimize      = new System.Windows.Forms.Button();
            this._btnClose         = new System.Windows.Forms.Button();
            this._lblVersion       = new System.Windows.Forms.Label();
            this._loginPanel.SuspendLayout();
            this._progressTrack.SuspendLayout();
            this.SuspendLayout();
            // 
            // _lblLogo
            // 
            this._lblLogo.Font      = new System.Drawing.Font("Segoe UI Emoji", 42F);
            this._lblLogo.ForeColor = System.Drawing.Color.White;
            this._lblLogo.Location  = new System.Drawing.Point(0, 30);
            this._lblLogo.Name      = "_lblLogo";
            this._lblLogo.Size      = new System.Drawing.Size(460, 80);
            this._lblLogo.TabIndex  = 0;
            this._lblLogo.Text      = "📬";
            this._lblLogo.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this._lblLogo.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Form_MouseDown);
            // 
            // _lblAppTitle
            // 
            this._lblAppTitle.BackColor = System.Drawing.Color.Transparent;
            this._lblAppTitle.Font      = new System.Drawing.Font("Microsoft Sans Serif", 16F, System.Drawing.FontStyle.Bold);
            this._lblAppTitle.ForeColor = System.Drawing.Color.White;
            this._lblAppTitle.Location  = new System.Drawing.Point(0, 116);
            this._lblAppTitle.Name      = "_lblAppTitle";
            this._lblAppTitle.Size      = new System.Drawing.Size(460, 32);
            this._lblAppTitle.TabIndex  = 1;
            this._lblAppTitle.Text      = "Инструментальное приложение";
            this._lblAppTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this._lblAppTitle.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Form_MouseDown);
            // 
            // _separatorTop
            // 
            this._separatorTop.BackColor = System.Drawing.Color.FromArgb(60, 100, 200);
            this._separatorTop.Location  = new System.Drawing.Point(60, 158);
            this._separatorTop.Name      = "_separatorTop";
            this._separatorTop.Size      = new System.Drawing.Size(340, 1);
            this._separatorTop.TabIndex  = 2;
            // 
            // _loginPanel
            // 
            this._loginPanel.BackColor = System.Drawing.Color.Transparent;
            this._loginPanel.Controls.Add(this._lblLoginTitle);
            this._loginPanel.Controls.Add(this._lblEmailHint);
            this._loginPanel.Controls.Add(this._txtEmail);
            this._loginPanel.Controls.Add(this._lblPasswordHint);
            this._loginPanel.Controls.Add(this._txtPassword);
            this._loginPanel.Controls.Add(this._btnLogin);
            this._loginPanel.Controls.Add(this._lblLoginError);
            this._loginPanel.Location = new System.Drawing.Point(60, 166);
            this._loginPanel.Name     = "_loginPanel";
            this._loginPanel.Size     = new System.Drawing.Size(340, 195);
            this._loginPanel.TabIndex = 3;
            // 
            // _lblLoginTitle
            // 
            this._lblLoginTitle.BackColor = System.Drawing.Color.Transparent;
            this._lblLoginTitle.Font      = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this._lblLoginTitle.ForeColor = System.Drawing.Color.White;
            this._lblLoginTitle.Location  = new System.Drawing.Point(0, 2);
            this._lblLoginTitle.Name      = "_lblLoginTitle";
            this._lblLoginTitle.Size      = new System.Drawing.Size(340, 26);
            this._lblLoginTitle.TabIndex  = 0;
            this._lblLoginTitle.Text      = "Вход в систему";
            this._lblLoginTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // _lblEmailHint
            // 
            this._lblEmailHint.BackColor = System.Drawing.Color.Transparent;
            this._lblEmailHint.Font      = new System.Drawing.Font("Segoe UI", 8.5F);
            this._lblEmailHint.ForeColor = System.Drawing.Color.FromArgb(150, 170, 220);
            this._lblEmailHint.Location  = new System.Drawing.Point(2, 38);
            this._lblEmailHint.Name      = "_lblEmailHint";
            this._lblEmailHint.Size      = new System.Drawing.Size(100, 16);
            this._lblEmailHint.TabIndex  = 1;
            this._lblEmailHint.Text      = "Email";
            // 
            // _txtEmail
            // 
            this._txtEmail.BackColor   = System.Drawing.Color.FromArgb(22, 42, 100);
            this._txtEmail.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._txtEmail.Font        = new System.Drawing.Font("Segoe UI", 10F);
            this._txtEmail.ForeColor   = System.Drawing.Color.White;
            this._txtEmail.Location    = new System.Drawing.Point(0, 55);
            this._txtEmail.Name        = "_txtEmail";
            this._txtEmail.Size        = new System.Drawing.Size(340, 25);
            this._txtEmail.TabIndex    = 2;
            this._txtEmail.KeyDown    += new System.Windows.Forms.KeyEventHandler(this.TxtEmail_KeyDown);
            // 
            // _lblPasswordHint
            // 
            this._lblPasswordHint.BackColor = System.Drawing.Color.Transparent;
            this._lblPasswordHint.Font      = new System.Drawing.Font("Segoe UI", 8.5F);
            this._lblPasswordHint.ForeColor = System.Drawing.Color.FromArgb(150, 170, 220);
            this._lblPasswordHint.Location  = new System.Drawing.Point(2, 90);
            this._lblPasswordHint.Name      = "_lblPasswordHint";
            this._lblPasswordHint.Size      = new System.Drawing.Size(100, 16);
            this._lblPasswordHint.TabIndex  = 3;
            this._lblPasswordHint.Text      = "Пароль";
            // 
            // _txtPassword
            // 
            this._txtPassword.BackColor              = System.Drawing.Color.FromArgb(22, 42, 100);
            this._txtPassword.BorderStyle            = System.Windows.Forms.BorderStyle.FixedSingle;
            this._txtPassword.Font                   = new System.Drawing.Font("Segoe UI", 10F);
            this._txtPassword.ForeColor              = System.Drawing.Color.White;
            this._txtPassword.Location               = new System.Drawing.Point(0, 107);
            this._txtPassword.Name                   = "_txtPassword";
            this._txtPassword.Size                   = new System.Drawing.Size(340, 25);
            this._txtPassword.TabIndex               = 4;
            this._txtPassword.UseSystemPasswordChar  = true;
            this._txtPassword.KeyDown               += new System.Windows.Forms.KeyEventHandler(this.TxtPassword_KeyDown);
            // 
            // _btnLogin
            // 
            this._btnLogin.BackColor                        = System.Drawing.Color.FromArgb(25, 55, 200);
            this._btnLogin.Cursor                           = System.Windows.Forms.Cursors.Hand;
            this._btnLogin.FlatAppearance.BorderSize        = 0;
            this._btnLogin.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(10, 35, 170);
            this._btnLogin.FlatStyle                        = System.Windows.Forms.FlatStyle.Flat;
            this._btnLogin.Font                             = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this._btnLogin.ForeColor                        = System.Drawing.Color.White;
            this._btnLogin.Location                         = new System.Drawing.Point(0, 142);
            this._btnLogin.Name                             = "_btnLogin";
            this._btnLogin.Size                             = new System.Drawing.Size(340, 36);
            this._btnLogin.TabIndex                         = 5;
            this._btnLogin.Text                             = "Войти";
            this._btnLogin.UseVisualStyleBackColor          = false;
            this._btnLogin.Click                           += new System.EventHandler(this.BtnLogin_Click);
            // 
            // _lblLoginError
            // 
            this._lblLoginError.BackColor = System.Drawing.Color.Transparent;
            this._lblLoginError.Font      = new System.Drawing.Font("Segoe UI", 8.5F);
            this._lblLoginError.ForeColor = System.Drawing.Color.FromArgb(255, 100, 100);
            this._lblLoginError.Location  = new System.Drawing.Point(0, 181);
            this._lblLoginError.Name      = "_lblLoginError";
            this._lblLoginError.Size      = new System.Drawing.Size(340, 16);
            this._lblLoginError.TabIndex  = 6;
            this._lblLoginError.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this._lblLoginError.Visible   = false;
            // 
            // _lblStatus
            //   Отображается вместо loginPanel во время прогресса.
            //   Изначально скрыт — становится видимым при ShowProgressMode().
            // 
            this._lblStatus.BackColor = System.Drawing.Color.Transparent;
            this._lblStatus.Font      = new System.Drawing.Font("Segoe UI", 9.5F);
            this._lblStatus.ForeColor = System.Drawing.Color.FromArgb(180, 210, 255);
            this._lblStatus.Location  = new System.Drawing.Point(60, 210);
            this._lblStatus.Name      = "_lblStatus";
            this._lblStatus.Size      = new System.Drawing.Size(340, 46);
            this._lblStatus.TabIndex  = 11;
            this._lblStatus.Text      = "";
            this._lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this._lblStatus.Visible   = false;
            // 
            // _progressTrack  (дорожка прогресс-бара)
            // 
            this._progressTrack.BackColor = System.Drawing.Color.FromArgb(22, 42, 100);
            this._progressTrack.Controls.Add(this._progressBar);
            this._progressTrack.Location  = new System.Drawing.Point(60, 265);
            this._progressTrack.Name      = "_progressTrack";
            this._progressTrack.Size      = new System.Drawing.Size(340, 6);
            this._progressTrack.TabIndex  = 12;
            this._progressTrack.Visible   = false;
            // 
            // _progressBar  (заполнение прогресс-бара, дочерний к _progressTrack)
            // 
            this._progressBar.BackColor = System.Drawing.Color.FromArgb(25, 55, 200);
            this._progressBar.Location  = new System.Drawing.Point(0, 0);
            this._progressBar.Name      = "_progressBar";
            this._progressBar.Size      = new System.Drawing.Size(0, 6);
            this._progressBar.TabIndex  = 0;
            // 
            // _btnUpdate  («Обновить сейчас» / «Восстановить файлы»)
            //   Изначально скрыт. Позиция и текст задаются кодом при показе.
            // 
            this._btnUpdate.BackColor                        = System.Drawing.Color.FromArgb(25, 55, 200);
            this._btnUpdate.Cursor                           = System.Windows.Forms.Cursors.Hand;
            this._btnUpdate.FlatAppearance.BorderSize        = 0;
            this._btnUpdate.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(10, 35, 170);
            this._btnUpdate.FlatStyle                        = System.Windows.Forms.FlatStyle.Flat;
            this._btnUpdate.Font                             = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this._btnUpdate.ForeColor                        = System.Drawing.Color.White;
            this._btnUpdate.Location                         = new System.Drawing.Point(130, 285);
            this._btnUpdate.Name                             = "_btnUpdate";
            this._btnUpdate.Size                             = new System.Drawing.Size(200, 38);
            this._btnUpdate.TabIndex                         = 13;
            this._btnUpdate.Text                             = "↓  Обновить сейчас";
            this._btnUpdate.UseVisualStyleBackColor          = false;
            this._btnUpdate.Visible                          = false;
            // 
            // _btnRemindLater  («Позже»)
            //   Изначально скрыт. Показывается только когда обновление необязательно.
            // 
            this._btnRemindLater.BackColor                        = System.Drawing.Color.FromArgb(60, 80, 140);
            this._btnRemindLater.Cursor                           = System.Windows.Forms.Cursors.Hand;
            this._btnRemindLater.FlatAppearance.BorderSize        = 0;
            this._btnRemindLater.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(45, 65, 120);
            this._btnRemindLater.FlatStyle                        = System.Windows.Forms.FlatStyle.Flat;
            this._btnRemindLater.Font                             = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this._btnRemindLater.ForeColor                        = System.Drawing.Color.White;
            this._btnRemindLater.Location                         = new System.Drawing.Point(286, 285);
            this._btnRemindLater.Name                             = "_btnRemindLater";
            this._btnRemindLater.Size                             = new System.Drawing.Size(114, 38);
            this._btnRemindLater.TabIndex                         = 14;
            this._btnRemindLater.Text                             = "⏱  Позже";
            this._btnRemindLater.UseVisualStyleBackColor          = false;
            this._btnRemindLater.Visible                          = false;
            // 
            // _separatorBottom
            // 
            this._separatorBottom.BackColor = System.Drawing.Color.FromArgb(60, 100, 200);
            this._separatorBottom.Location  = new System.Drawing.Point(60, 372);
            this._separatorBottom.Name      = "_separatorBottom";
            this._separatorBottom.Size      = new System.Drawing.Size(340, 1);
            this._separatorBottom.TabIndex  = 4;
            // 
            // _btnMinimize
            // 
            this._btnMinimize.BackColor                          = System.Drawing.Color.FromArgb(30, 58, 138);
            this._btnMinimize.Cursor                             = System.Windows.Forms.Cursors.Hand;
            this._btnMinimize.FlatAppearance.BorderSize          = 0;
            this._btnMinimize.FlatAppearance.MouseDownBackColor  = System.Drawing.Color.FromArgb(70, 100, 215);
            this._btnMinimize.FlatAppearance.MouseOverBackColor  = System.Drawing.Color.FromArgb(55, 85, 190);
            this._btnMinimize.FlatStyle                          = System.Windows.Forms.FlatStyle.Flat;
            this._btnMinimize.Font                               = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this._btnMinimize.ForeColor                          = System.Drawing.Color.FromArgb(160, 190, 255);
            this._btnMinimize.Location                           = new System.Drawing.Point(372, 8);
            this._btnMinimize.Name                               = "_btnMinimize";
            this._btnMinimize.Size                               = new System.Drawing.Size(34, 22);
            this._btnMinimize.TabIndex                           = 5;
            this._btnMinimize.Text                               = "—";
            this._btnMinimize.UseVisualStyleBackColor            = false;
            this._btnMinimize.Click                             += new System.EventHandler(this.BtnMinimize_Click);
            // 
            // _btnClose
            // 
            this._btnClose.BackColor                          = System.Drawing.Color.FromArgb(30, 58, 138);
            this._btnClose.Cursor                             = System.Windows.Forms.Cursors.Hand;
            this._btnClose.FlatAppearance.BorderSize          = 0;
            this._btnClose.FlatAppearance.MouseDownBackColor  = System.Drawing.Color.FromArgb(140, 15, 15);
            this._btnClose.FlatAppearance.MouseOverBackColor  = System.Drawing.Color.FromArgb(180, 30, 30);
            this._btnClose.FlatStyle                          = System.Windows.Forms.FlatStyle.Flat;
            this._btnClose.Font                               = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this._btnClose.ForeColor                          = System.Drawing.Color.FromArgb(160, 190, 255);
            this._btnClose.Location                           = new System.Drawing.Point(412, 8);
            this._btnClose.Name                               = "_btnClose";
            this._btnClose.Size                               = new System.Drawing.Size(34, 22);
            this._btnClose.TabIndex                           = 6;
            this._btnClose.Text                               = "✕";
            this._btnClose.UseVisualStyleBackColor            = false;
            this._btnClose.Click                             += new System.EventHandler(this.BtnClose_Click);
            // 
            // _lblVersion
            // 
            this._lblVersion.BackColor = System.Drawing.Color.Transparent;
            this._lblVersion.Font      = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            this._lblVersion.ForeColor = System.Drawing.Color.FromArgb(100, 130, 200);
            this._lblVersion.Location  = new System.Drawing.Point(12, 363);
            this._lblVersion.Name      = "_lblVersion";
            this._lblVersion.Size      = new System.Drawing.Size(436, 18);
            this._lblVersion.TabIndex  = 10;
            this._lblVersion.Text      = "Версия ";
            this._lblVersion.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // LoginForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode       = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor           = System.Drawing.Color.FromArgb(11, 24, 80);
            this.ClientSize          = new System.Drawing.Size(460, 390);
            this.Controls.Add(this._lblVersion);
            this.Controls.Add(this._btnClose);
            this.Controls.Add(this._btnMinimize);
            this.Controls.Add(this._lblLogo);
            this.Controls.Add(this._lblAppTitle);
            this.Controls.Add(this._separatorTop);
            this.Controls.Add(this._loginPanel);
            this.Controls.Add(this._lblStatus);
            this.Controls.Add(this._progressTrack);
            this.Controls.Add(this._btnUpdate);
            this.Controls.Add(this._btnRemindLater);
            this.Controls.Add(this._separatorBottom);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.MaximizeBox     = false;
            this.MinimizeBox     = false;
            this.Name            = "LoginForm";
            this.StartPosition   = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text            = "PostalApp Extra";
            this._loginPanel.ResumeLayout(false);
            this._progressTrack.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
