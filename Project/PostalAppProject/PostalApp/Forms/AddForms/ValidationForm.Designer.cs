namespace PostalApp
{
    partial class ValidationForm
    {
        private System.ComponentModel.IContainer components = null;

        // Шапка
        private System.Windows.Forms.Panel _header;
        private System.Windows.Forms.Label _headerTitle;
        private System.Windows.Forms.Button _headerClose;

        // Индикатор фаз
        private System.Windows.Forms.Panel _phasePanel;
        private System.Windows.Forms.Panel _phase1Pill;
        private System.Windows.Forms.Label _phase1Label;
        private System.Windows.Forms.Label _arrowLabel;
        private System.Windows.Forms.Panel _phase2Pill;
        private System.Windows.Forms.Label _phase2Label;
        private System.Windows.Forms.Panel _phaseLine;

        // Статус и прогресс
        private System.Windows.Forms.Label _statusLabel;
        private System.Windows.Forms.Panel _progressTrack;
        private System.Windows.Forms.ProgressBar _progressBar;
        private System.Windows.Forms.Label _counterLabel;

        // Панель ошибок
        private System.Windows.Forms.Panel _errorPanel;
        private System.Windows.Forms.Panel _errorAccent;
        private System.Windows.Forms.Label _errorTitle;
        private System.Windows.Forms.Label _errorMessage;
        private System.Windows.Forms.Panel _errorSep;
        private System.Windows.Forms.Label _lblOriginal;
        private CuoreUI.Controls.cuiTextBox _originalValueBox;
        private System.Windows.Forms.Label _lblCorrected;
        private CuoreUI.Controls.cuiTextBox _correctedValueBox;
        private CuoreUI.Controls.cuiButton _fixButton;
        private CuoreUI.Controls.cuiButton _skipButton;
        private CuoreUI.Controls.cuiButton _cancelButton;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ValidationForm));
            this._header = new System.Windows.Forms.Panel();
            this._headerTitle = new System.Windows.Forms.Label();
            this._headerClose = new System.Windows.Forms.Button();
            this._phasePanel = new System.Windows.Forms.Panel();
            this._phase1Pill = new System.Windows.Forms.Panel();
            this._phase1Label = new System.Windows.Forms.Label();
            this._arrowLabel = new System.Windows.Forms.Label();
            this._phase2Pill = new System.Windows.Forms.Panel();
            this._phase2Label = new System.Windows.Forms.Label();
            this._phaseLine = new System.Windows.Forms.Panel();
            this._statusLabel = new System.Windows.Forms.Label();
            this._progressTrack = new System.Windows.Forms.Panel();
            this._progressBar = new System.Windows.Forms.ProgressBar();
            this._counterLabel = new System.Windows.Forms.Label();
            this._errorPanel = new System.Windows.Forms.Panel();
            this._errorAccent = new System.Windows.Forms.Panel();
            this._errorTitle = new System.Windows.Forms.Label();
            this._errorMessage = new System.Windows.Forms.Label();
            this._errorSep = new System.Windows.Forms.Panel();
            this._lblOriginal = new System.Windows.Forms.Label();
            this._originalValueBox = new CuoreUI.Controls.cuiTextBox();
            this._lblCorrected = new System.Windows.Forms.Label();
            this._correctedValueBox = new CuoreUI.Controls.cuiTextBox();
            this._fixButton = new CuoreUI.Controls.cuiButton();
            this._skipButton = new CuoreUI.Controls.cuiButton();
            this._cancelButton = new CuoreUI.Controls.cuiButton();
            this._header.SuspendLayout();
            this._phasePanel.SuspendLayout();
            this._phase1Pill.SuspendLayout();
            this._phase2Pill.SuspendLayout();
            this._progressTrack.SuspendLayout();
            this._errorPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // _header
            // 
            this._header.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(55)))), ((int)(((byte)(200)))));
            this._header.Controls.Add(this._headerTitle);
            this._header.Controls.Add(this._headerClose);
            this._header.Dock = System.Windows.Forms.DockStyle.Top;
            this._header.Location = new System.Drawing.Point(0, 0);
            this._header.Name = "_header";
            this._header.Size = new System.Drawing.Size(620, 48);
            this._header.TabIndex = 0;
            this._header.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Header_MouseDown);
            // 
            // _headerTitle
            // 
            this._headerTitle.BackColor = System.Drawing.Color.Transparent;
            this._headerTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Bold);
            this._headerTitle.ForeColor = System.Drawing.Color.White;
            this._headerTitle.Location = new System.Drawing.Point(16, 0);
            this._headerTitle.Name = "_headerTitle";
            this._headerTitle.Size = new System.Drawing.Size(480, 48);
            this._headerTitle.TabIndex = 0;
            this._headerTitle.Text = "📊  Загрузка данных из Excel";
            this._headerTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this._headerTitle.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Header_MouseDown);
            // 
            // _headerClose
            // 
            this._headerClose.BackColor = System.Drawing.Color.Transparent;
            this._headerClose.Cursor = System.Windows.Forms.Cursors.Hand;
            this._headerClose.FlatAppearance.BorderSize = 0;
            this._headerClose.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(140)))), ((int)(((byte)(15)))), ((int)(((byte)(15)))));
            this._headerClose.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this._headerClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._headerClose.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this._headerClose.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(220)))), ((int)(((byte)(255)))));
            this._headerClose.Location = new System.Drawing.Point(576, 4);
            this._headerClose.Name = "_headerClose";
            this._headerClose.Size = new System.Drawing.Size(38, 38);
            this._headerClose.TabIndex = 1;
            this._headerClose.Text = "✕";
            this._headerClose.UseVisualStyleBackColor = false;
            this._headerClose.Click += new System.EventHandler(this.HeaderClose_Click);
            // 
            // _phasePanel
            // 
            this._phasePanel.BackColor = System.Drawing.Color.White;
            this._phasePanel.Controls.Add(this._phase1Pill);
            this._phasePanel.Controls.Add(this._arrowLabel);
            this._phasePanel.Controls.Add(this._phase2Pill);
            this._phasePanel.Controls.Add(this._phaseLine);
            this._phasePanel.Location = new System.Drawing.Point(0, 48);
            this._phasePanel.Name = "_phasePanel";
            this._phasePanel.Size = new System.Drawing.Size(620, 56);
            this._phasePanel.TabIndex = 1;
            // 
            // _phase1Pill
            // 
            this._phase1Pill.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(55)))), ((int)(((byte)(200)))));
            this._phase1Pill.Controls.Add(this._phase1Label);
            this._phase1Pill.Location = new System.Drawing.Point(70, 12);
            this._phase1Pill.Name = "_phase1Pill";
            this._phase1Pill.Size = new System.Drawing.Size(210, 32);
            this._phase1Pill.TabIndex = 0;
            // 
            // _phase1Label
            // 
            this._phase1Label.BackColor = System.Drawing.Color.Transparent;
            this._phase1Label.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold);
            this._phase1Label.ForeColor = System.Drawing.Color.White;
            this._phase1Label.Location = new System.Drawing.Point(0, 0);
            this._phase1Label.Name = "_phase1Label";
            this._phase1Label.Size = new System.Drawing.Size(210, 32);
            this._phase1Label.TabIndex = 0;
            this._phase1Label.Text = "⬤  Фаза 1: Проверка данных";
            this._phase1Label.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // _arrowLabel
            // 
            this._arrowLabel.BackColor = System.Drawing.Color.Transparent;
            this._arrowLabel.Font = new System.Drawing.Font("Segoe UI", 13F);
            this._arrowLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(150)))), ((int)(((byte)(160)))), ((int)(((byte)(190)))));
            this._arrowLabel.Location = new System.Drawing.Point(286, 14);
            this._arrowLabel.Name = "_arrowLabel";
            this._arrowLabel.Size = new System.Drawing.Size(28, 28);
            this._arrowLabel.TabIndex = 1;
            this._arrowLabel.Text = "→";
            this._arrowLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // _phase2Pill
            // 
            this._phase2Pill.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(225)))), ((int)(((byte)(228)))), ((int)(((byte)(240)))));
            this._phase2Pill.Controls.Add(this._phase2Label);
            this._phase2Pill.Location = new System.Drawing.Point(320, 12);
            this._phase2Pill.Name = "_phase2Pill";
            this._phase2Pill.Size = new System.Drawing.Size(210, 32);
            this._phase2Pill.TabIndex = 2;
            // 
            // _phase2Label
            // 
            this._phase2Label.BackColor = System.Drawing.Color.Transparent;
            this._phase2Label.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold);
            this._phase2Label.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(130)))), ((int)(((byte)(160)))));
            this._phase2Label.Location = new System.Drawing.Point(0, 0);
            this._phase2Label.Name = "_phase2Label";
            this._phase2Label.Size = new System.Drawing.Size(210, 32);
            this._phase2Label.TabIndex = 0;
            this._phase2Label.Text = "○  Фаза 2: Импорт в БД";
            this._phase2Label.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // _phaseLine
            // 
            this._phaseLine.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(225)))), ((int)(((byte)(228)))), ((int)(((byte)(240)))));
            this._phaseLine.Location = new System.Drawing.Point(0, 55);
            this._phaseLine.Name = "_phaseLine";
            this._phaseLine.Size = new System.Drawing.Size(620, 1);
            this._phaseLine.TabIndex = 3;
            // 
            // _statusLabel
            // 
            this._statusLabel.BackColor = System.Drawing.Color.Transparent;
            this._statusLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold);
            this._statusLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(50)))), ((int)(((byte)(80)))));
            this._statusLabel.Location = new System.Drawing.Point(32, 120);
            this._statusLabel.Name = "_statusLabel";
            this._statusLabel.Size = new System.Drawing.Size(556, 24);
            this._statusLabel.TabIndex = 2;
            this._statusLabel.Text = "Подготовка к загрузке данных...";
            // 
            // _progressTrack
            // 
            this._progressTrack.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(225)))), ((int)(((byte)(240)))));
            this._progressTrack.Controls.Add(this._progressBar);
            this._progressTrack.Location = new System.Drawing.Point(32, 152);
            this._progressTrack.Name = "_progressTrack";
            this._progressTrack.Size = new System.Drawing.Size(556, 10);
            this._progressTrack.TabIndex = 3;
            // 
            // _progressBar
            // 
            this._progressBar.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(55)))), ((int)(((byte)(200)))));
            this._progressBar.Location = new System.Drawing.Point(0, 0);
            this._progressBar.Name = "_progressBar";
            this._progressBar.Size = new System.Drawing.Size(0, 10);
            this._progressBar.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this._progressBar.TabIndex = 0;
            // 
            // _counterLabel
            // 
            this._counterLabel.BackColor = System.Drawing.Color.Transparent;
            this._counterLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            this._counterLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(110)))), ((int)(((byte)(120)))), ((int)(((byte)(155)))));
            this._counterLabel.Location = new System.Drawing.Point(32, 167);
            this._counterLabel.Name = "_counterLabel";
            this._counterLabel.Size = new System.Drawing.Size(556, 20);
            this._counterLabel.TabIndex = 4;
            this._counterLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // _errorPanel
            // 
            this._errorPanel.BackColor = System.Drawing.Color.White;
            this._errorPanel.Controls.Add(this._errorAccent);
            this._errorPanel.Controls.Add(this._errorTitle);
            this._errorPanel.Controls.Add(this._errorMessage);
            this._errorPanel.Controls.Add(this._errorSep);
            this._errorPanel.Controls.Add(this._lblOriginal);
            this._errorPanel.Controls.Add(this._originalValueBox);
            this._errorPanel.Controls.Add(this._lblCorrected);
            this._errorPanel.Controls.Add(this._correctedValueBox);
            this._errorPanel.Controls.Add(this._fixButton);
            this._errorPanel.Controls.Add(this._skipButton);
            this._errorPanel.Controls.Add(this._cancelButton);
            this._errorPanel.Location = new System.Drawing.Point(32, 196);
            this._errorPanel.Name = "_errorPanel";
            this._errorPanel.Size = new System.Drawing.Size(556, 278);
            this._errorPanel.TabIndex = 5;
            this._errorPanel.Visible = false;
            // 
            // _errorAccent
            // 
            this._errorAccent.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(53)))), ((int)(((byte)(69)))));
            this._errorAccent.Location = new System.Drawing.Point(0, 0);
            this._errorAccent.Name = "_errorAccent";
            this._errorAccent.Size = new System.Drawing.Size(5, 278);
            this._errorAccent.TabIndex = 0;
            // 
            // _errorTitle
            // 
            this._errorTitle.BackColor = System.Drawing.Color.Transparent;
            this._errorTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold);
            this._errorTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(20)))), ((int)(((byte)(40)))));
            this._errorTitle.Location = new System.Drawing.Point(20, 14);
            this._errorTitle.Name = "_errorTitle";
            this._errorTitle.Size = new System.Drawing.Size(520, 22);
            this._errorTitle.TabIndex = 1;
            this._errorTitle.Text = "⚠  Обнаружена ошибка";
            // 
            // _errorMessage
            // 
            this._errorMessage.BackColor = System.Drawing.Color.Transparent;
            this._errorMessage.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            this._errorMessage.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(90)))), ((int)(((byte)(120)))));
            this._errorMessage.Location = new System.Drawing.Point(20, 40);
            this._errorMessage.Name = "_errorMessage";
            this._errorMessage.Size = new System.Drawing.Size(520, 18);
            this._errorMessage.TabIndex = 2;
            // 
            // _errorSep
            // 
            this._errorSep.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(238)))), ((int)(((byte)(248)))));
            this._errorSep.Location = new System.Drawing.Point(20, 64);
            this._errorSep.Name = "_errorSep";
            this._errorSep.Size = new System.Drawing.Size(516, 1);
            this._errorSep.TabIndex = 3;
            // 
            // _lblOriginal
            // 
            this._lblOriginal.BackColor = System.Drawing.Color.Transparent;
            this._lblOriginal.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            this._lblOriginal.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(110)))), ((int)(((byte)(145)))));
            this._lblOriginal.Location = new System.Drawing.Point(20, 74);
            this._lblOriginal.Name = "_lblOriginal";
            this._lblOriginal.Size = new System.Drawing.Size(200, 18);
            this._lblOriginal.TabIndex = 4;
            this._lblOriginal.Text = "Текущее значение:";
            // 
            // _originalValueBox
            // 
            this._originalValueBox.BackgroundColor = System.Drawing.Color.FromArgb(((int)(((byte)(246)))), ((int)(((byte)(247)))), ((int)(((byte)(252)))));
            this._originalValueBox.Content = "";
            this._originalValueBox.Cursor = System.Windows.Forms.Cursors.IBeam;
            this._originalValueBox.Enabled = false;
            this._originalValueBox.FocusBackgroundColor = System.Drawing.Color.White;
            this._originalValueBox.FocusImageTint = System.Drawing.Color.White;
            this._originalValueBox.FocusOutlineColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(106)))), ((int)(((byte)(0)))));
            this._originalValueBox.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this._originalValueBox.ForeColor = System.Drawing.Color.Gray;
            this._originalValueBox.Image = null;
            this._originalValueBox.ImageExpand = new System.Drawing.Point(0, 0);
            this._originalValueBox.ImageOffset = new System.Drawing.Point(0, 0);
            this._originalValueBox.Location = new System.Drawing.Point(20, 96);
            this._originalValueBox.Margin = new System.Windows.Forms.Padding(4);
            this._originalValueBox.Multiline = false;
            this._originalValueBox.Name = "_originalValueBox";
            this._originalValueBox.NormalImageTint = System.Drawing.Color.White;
            this._originalValueBox.OutlineColor = System.Drawing.Color.FromArgb(((int)(((byte)(225)))), ((int)(((byte)(228)))), ((int)(((byte)(240)))));
            this._originalValueBox.Padding = new System.Windows.Forms.Padding(16, 10, 16, 0);
            this._originalValueBox.PasswordChar = false;
            this._originalValueBox.PlaceholderColor = System.Drawing.Color.LightGray;
            this._originalValueBox.PlaceholderText = "Placeholder text..";
            this._originalValueBox.Rounding = new System.Windows.Forms.Padding(8);
            this._originalValueBox.Size = new System.Drawing.Size(516, 36);
            this._originalValueBox.TabIndex = 5;
            this._originalValueBox.TextOffset = new System.Drawing.Size(0, 0);
            this._originalValueBox.UnderlinedStyle = true;
            // 
            // _lblCorrected
            // 
            this._lblCorrected.BackColor = System.Drawing.Color.Transparent;
            this._lblCorrected.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            this._lblCorrected.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(110)))), ((int)(((byte)(145)))));
            this._lblCorrected.Location = new System.Drawing.Point(20, 142);
            this._lblCorrected.Name = "_lblCorrected";
            this._lblCorrected.Size = new System.Drawing.Size(200, 18);
            this._lblCorrected.TabIndex = 6;
            this._lblCorrected.Text = "Исправить на:";
            // 
            // _correctedValueBox
            // 
            this._correctedValueBox.BackgroundColor = System.Drawing.Color.White;
            this._correctedValueBox.Content = "";
            this._correctedValueBox.Cursor = System.Windows.Forms.Cursors.IBeam;
            this._correctedValueBox.FocusBackgroundColor = System.Drawing.Color.White;
            this._correctedValueBox.FocusImageTint = System.Drawing.Color.White;
            this._correctedValueBox.FocusOutlineColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(106)))), ((int)(((byte)(0)))));
            this._correctedValueBox.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this._correctedValueBox.ForeColor = System.Drawing.Color.Gray;
            this._correctedValueBox.Image = null;
            this._correctedValueBox.ImageExpand = new System.Drawing.Point(0, 0);
            this._correctedValueBox.ImageOffset = new System.Drawing.Point(0, 0);
            this._correctedValueBox.Location = new System.Drawing.Point(20, 164);
            this._correctedValueBox.Margin = new System.Windows.Forms.Padding(4);
            this._correctedValueBox.Multiline = false;
            this._correctedValueBox.Name = "_correctedValueBox";
            this._correctedValueBox.NormalImageTint = System.Drawing.Color.White;
            this._correctedValueBox.OutlineColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(55)))), ((int)(((byte)(200)))));
            this._correctedValueBox.Padding = new System.Windows.Forms.Padding(16, 10, 16, 0);
            this._correctedValueBox.PasswordChar = false;
            this._correctedValueBox.PlaceholderColor = System.Drawing.Color.LightGray;
            this._correctedValueBox.PlaceholderText = "Placeholder text..";
            this._correctedValueBox.Rounding = new System.Windows.Forms.Padding(8);
            this._correctedValueBox.Size = new System.Drawing.Size(516, 36);
            this._correctedValueBox.TabIndex = 7;
            this._correctedValueBox.TextOffset = new System.Drawing.Size(0, 0);
            this._correctedValueBox.UnderlinedStyle = true;
            // 
            // _fixButton
            // 
            this._fixButton.CheckButton = false;
            this._fixButton.Checked = false;
            this._fixButton.CheckedBackground = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(106)))), ((int)(((byte)(0)))));
            this._fixButton.CheckedForeColor = System.Drawing.Color.White;
            this._fixButton.CheckedImageTint = System.Drawing.Color.White;
            this._fixButton.CheckedOutline = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(106)))), ((int)(((byte)(0)))));
            this._fixButton.Content = "✓  Исправить";
            this._fixButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this._fixButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._fixButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold);
            this._fixButton.ForeColor = System.Drawing.Color.Black;
            this._fixButton.HoverBackground = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(40)))), ((int)(((byte)(170)))));
            this._fixButton.HoverForeColor = System.Drawing.Color.Black;
            this._fixButton.HoverImageTint = System.Drawing.Color.White;
            this._fixButton.HoverOutline = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this._fixButton.Image = null;
            this._fixButton.ImageExpand = new System.Drawing.Point(0, 0);
            this._fixButton.Location = new System.Drawing.Point(20, 218);
            this._fixButton.Name = "_fixButton";
            this._fixButton.NormalBackground = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(55)))), ((int)(((byte)(200)))));
            this._fixButton.NormalForeColor = System.Drawing.Color.Black;
            this._fixButton.NormalImageTint = System.Drawing.Color.White;
            this._fixButton.NormalOutline = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this._fixButton.OutlineThickness = 1F;
            this._fixButton.PressedBackground = System.Drawing.Color.FromArgb(((int)(((byte)(10)))), ((int)(((byte)(30)))), ((int)(((byte)(140)))));
            this._fixButton.PressedForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(32)))), ((int)(((byte)(32)))));
            this._fixButton.PressedImageTint = System.Drawing.Color.White;
            this._fixButton.PressedOutline = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this._fixButton.Rounding = new System.Windows.Forms.Padding(10);
            this._fixButton.Size = new System.Drawing.Size(158, 40);
            this._fixButton.TabIndex = 8;
            this._fixButton.TextAlignment = System.Drawing.StringAlignment.Center;
            this._fixButton.Click += new System.EventHandler(this.FixButton_Click);
            // 
            // _skipButton
            // 
            this._skipButton.CheckButton = false;
            this._skipButton.Checked = false;
            this._skipButton.CheckedBackground = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(106)))), ((int)(((byte)(0)))));
            this._skipButton.CheckedForeColor = System.Drawing.Color.White;
            this._skipButton.CheckedImageTint = System.Drawing.Color.White;
            this._skipButton.CheckedOutline = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(106)))), ((int)(((byte)(0)))));
            this._skipButton.Content = "⏭  Пропустить";
            this._skipButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this._skipButton.DialogResult = System.Windows.Forms.DialogResult.Ignore;
            this._skipButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold);
            this._skipButton.ForeColor = System.Drawing.Color.Black;
            this._skipButton.HoverBackground = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(90)))), ((int)(((byte)(120)))));
            this._skipButton.HoverForeColor = System.Drawing.Color.Black;
            this._skipButton.HoverImageTint = System.Drawing.Color.White;
            this._skipButton.HoverOutline = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this._skipButton.Image = null;
            this._skipButton.ImageExpand = new System.Drawing.Point(0, 0);
            this._skipButton.Location = new System.Drawing.Point(192, 218);
            this._skipButton.Name = "_skipButton";
            this._skipButton.NormalBackground = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(110)))), ((int)(((byte)(145)))));
            this._skipButton.NormalForeColor = System.Drawing.Color.Black;
            this._skipButton.NormalImageTint = System.Drawing.Color.White;
            this._skipButton.NormalOutline = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this._skipButton.OutlineThickness = 1F;
            this._skipButton.PressedBackground = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(70)))), ((int)(((byte)(100)))));
            this._skipButton.PressedForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(32)))), ((int)(((byte)(32)))));
            this._skipButton.PressedImageTint = System.Drawing.Color.White;
            this._skipButton.PressedOutline = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this._skipButton.Rounding = new System.Windows.Forms.Padding(10);
            this._skipButton.Size = new System.Drawing.Size(158, 40);
            this._skipButton.TabIndex = 9;
            this._skipButton.TextAlignment = System.Drawing.StringAlignment.Center;
            this._skipButton.Click += new System.EventHandler(this.SkipButton_Click);
            // 
            // _cancelButton
            // 
            this._cancelButton.CheckButton = false;
            this._cancelButton.Checked = false;
            this._cancelButton.CheckedBackground = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(106)))), ((int)(((byte)(0)))));
            this._cancelButton.CheckedForeColor = System.Drawing.Color.White;
            this._cancelButton.CheckedImageTint = System.Drawing.Color.White;
            this._cancelButton.CheckedOutline = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(106)))), ((int)(((byte)(0)))));
            this._cancelButton.Content = "✕  Отменить всё";
            this._cancelButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this._cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._cancelButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold);
            this._cancelButton.ForeColor = System.Drawing.Color.Black;
            this._cancelButton.HoverBackground = System.Drawing.Color.FromArgb(((int)(((byte)(170)))), ((int)(((byte)(20)))), ((int)(((byte)(40)))));
            this._cancelButton.HoverForeColor = System.Drawing.Color.Black;
            this._cancelButton.HoverImageTint = System.Drawing.Color.White;
            this._cancelButton.HoverOutline = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this._cancelButton.Image = null;
            this._cancelButton.ImageExpand = new System.Drawing.Point(0, 0);
            this._cancelButton.Location = new System.Drawing.Point(364, 218);
            this._cancelButton.Name = "_cancelButton";
            this._cancelButton.NormalBackground = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(30)))), ((int)(((byte)(50)))));
            this._cancelButton.NormalForeColor = System.Drawing.Color.Black;
            this._cancelButton.NormalImageTint = System.Drawing.Color.White;
            this._cancelButton.NormalOutline = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this._cancelButton.OutlineThickness = 1F;
            this._cancelButton.PressedBackground = System.Drawing.Color.FromArgb(((int)(((byte)(140)))), ((int)(((byte)(15)))), ((int)(((byte)(30)))));
            this._cancelButton.PressedForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(32)))), ((int)(((byte)(32)))), ((int)(((byte)(32)))));
            this._cancelButton.PressedImageTint = System.Drawing.Color.White;
            this._cancelButton.PressedOutline = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this._cancelButton.Rounding = new System.Windows.Forms.Padding(10);
            this._cancelButton.Size = new System.Drawing.Size(158, 40);
            this._cancelButton.TabIndex = 10;
            this._cancelButton.TextAlignment = System.Drawing.StringAlignment.Center;
            this._cancelButton.Click += new System.EventHandler(this.CancelButton_Click);
            // 
            // ValidationForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(246)))), ((int)(((byte)(252)))));
            this.ClientSize = new System.Drawing.Size(620, 490);
            this.Controls.Add(this._header);
            this.Controls.Add(this._phasePanel);
            this.Controls.Add(this._statusLabel);
            this.Controls.Add(this._progressTrack);
            this.Controls.Add(this._counterLabel);
            this.Controls.Add(this._errorPanel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ValidationForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Загрузка данных из Excel";
            this._header.ResumeLayout(false);
            this._phasePanel.ResumeLayout(false);
            this._phase1Pill.ResumeLayout(false);
            this._phase2Pill.ResumeLayout(false);
            this._progressTrack.ResumeLayout(false);
            this._errorPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}

