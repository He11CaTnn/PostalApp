using CuoreUI.Controls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Интерфейс
{
    public partial class operatorForm : Form
    {
        [DllImport("user32.dll")] private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")] private static extern bool ReleaseCapture();

        private bool menuStorage;
        private bool Subscriptions1;
        private bool Subscriptions2;
        private bool Reader1;
        private bool Reader2;

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTCAPTION = 0x2;

        private Timer _scrollDebounceTimer;
        private TableType _currentTable = TableType.SubscriptionsMain;
        private int delay = 100;
        private Timer _autoUpdateTasksTimer;

        private SearchFilter<DataBase.Editions> _searchEditions;
        private LazyLoader<DataBase.Editions> _loaderEditions;
        private SearchFilter<DataBase.Subscriptions> _searchSubs;
        private LazyLoader<DataBase.Subscriptions> _loaderSubs;
        private SearchFilter<DataBase.Readers> _searchReds;
        private LazyLoader<DataBase.Readers> _loaderReds;

        private SearchFilter<DataBase.Tasks> _searchTasks;
        private LazyLoader<DataBase.Tasks> _loaderTasks;

        private SearchFilter<DataBase.Readers> _searchReds1;
        private LazyLoader<DataBase.Readers> _loaderReds1;
        private SearchFilter<DataBase.Readers> _searchReds2;
        private LazyLoader<DataBase.Readers> _loaderReds2;

        private SearchFilter<DataBase.Markers> _searchStreet;
        private LazyLoader<DataBase.Markers> _loaderStreet;
        private SearchFilter<DataBase.Markers> _searchStreet1;
        private LazyLoader<DataBase.Markers> _loaderStreet1;

        private DataGridView _currentActiveTable;
        private DataBase.Editions _selectedEdition;
        private DataBase.Subscriptions _selectedSubscription;
        private DataBase.Readers _selectedReader;
        private DataBase.Readers _selectedReader1;
        private DataBase.Markers _selectedStreet;

        private readonly HashSet<Guid> _locallyAddedSubscriptionIds = new HashSet<Guid>();
        private readonly HashSet<Guid> _locallyAddedReaderIds = new HashSet<Guid>();

        public enum TableType
        {
            EditionsMain,
            SubscriptionsMain,
            ReadersMain,
            ReadersAdd,
            ReadersEdit,
            AddressesAdd,
            AddressesEdit,
            Tasks
        }

        public operatorForm()
        {
            InitializeComponent();
            OpenPanel();
            applyRadius();
            subscriptionsDataGridView1_2.Dock = DockStyle.Fill;
            RegistrationReaderPanel.Location = new Point(796, 15);
            EditReaderPanel.Location = new Point(796, 66);
        }

        public static class RoundHelper
        {
            public static void Apply(Control ctl, int radius = 15)
            {
                if (ctl.Width == 0 || ctl.Height == 0)
                    return;
                int r = Math.Min(radius, Math.Min(ctl.Width / 2, ctl.Height / 2));
                using (GraphicsPath path = new GraphicsPath())
                {
                    path.StartFigure();
                    path.AddArc(0, 0, r, r, 180, 90);
                    path.AddArc(ctl.Width - r, 0, r, r, 270, 90);
                    path.AddArc(ctl.Width - r, ctl.Height - r, r, r, 0, 90);
                    path.AddArc(0, ctl.Height - r, r, r, 90, 90);
                    path.CloseFigure();
                    ctl.Region?.Dispose();
                    ctl.Region = new Region(path);
                }
            }

            public static void Attach(Control ctl, int radius = 15)
            {
                Apply(ctl, radius);
                ctl.Resize += (s, e) =>
                {
                    Apply(ctl, radius);
                };
            }
        }

        private void applyRadius()
        {
            RoundHelper.Attach(tasksDataGridView, 37);
            RoundHelper.Attach(subscriptionsDataGridView1_1, 37);
            RoundHelper.Attach(subscriptionsDataGridView1_2, 37);
            RoundHelper.Attach(subscriptionsDataGridView1_3, 37);
            RoundHelper.Attach(AcceptSubscriptionsPanel2, 37);
            RoundHelper.Attach(EditSubscriptionsPanel2, 37);
            RoundHelper.Attach(RegistrationReaderPanel2, 37);
            RoundHelper.Attach(EditReaderPanel2, 37);
        }

        private void OpenPanel()
        {
            subscriptionsPanel.Visible = true;
            subscriptionsPanel.Location = new Point(77, 40);
            subscriptionsPanel.Size = new Size(1218, 686);
            tasksPanel.Visible = true;
            tasksPanel.Location = new Point(77, 40);
            tasksPanel.Size = new Size(1218, 686);
        }

        private void menuTimer_Tick(object sender, EventArgs e)
        {
            int step;
            int diff;
            if (menuStorage)
            {
                menuButton1.Size = new Size(66, 84);
                menuButton2.Size = new Size(66, 84);
                menuButton3.Size = new Size(66, 84);
                menuLabel1.Visible = false;
                menuLabel2.Visible = false;
                menuLabel3.Visible = false;
                menuLabel5.Visible = false;
                menuLabel4.Visible = false;
                diff = menuPanel.Width - menuPanel.MinimumSize.Width;
                step = Math.Max(2, diff / 5);
                menuPanel.Width -= step;
                if (menuPanel.Width <= menuPanel.MinimumSize.Width)
                {
                    menuPanel.Width = menuPanel.MinimumSize.Width;
                    menuStorage = false;
                    menuTimer.Stop();
                }
            }
            else
            {
                menuButton1.Size = new Size(228, 84);
                menuButton2.Size = new Size(228, 84);
                menuButton3.Size = new Size(228, 84);
                menuLabel1.Visible = true;
                menuLabel2.Visible = true;
                menuLabel3.Visible = true;
                menuLabel5.Visible = true;
                menuLabel4.Visible = true;
                diff = menuPanel.MaximumSize.Width - menuPanel.Width;
                step = Math.Max(2, diff / 5);
                menuPanel.Width += step;
                if (menuPanel.Width >= menuPanel.MaximumSize.Width)
                {
                    menuPanel.Width = menuPanel.MaximumSize.Width;
                    menuStorage = true;
                    menuTimer.Stop();

                }
            }
        }

        private void menuButton_Click(object sender, EventArgs e)
        {
            menuTimer.Start();
        }

        private void menuButton1_MouseEnter(object sender, EventArgs e)
        {
            menuButton1.PanelColor = Color.FromArgb(26, 52, 232);
            menuLabel2.BackColor = Color.FromArgb(26, 52, 232);
            menuPictureBox2.BackColor = Color.FromArgb(26, 52, 232);
        }

        private void menuButton2_MouseEnter(object sender, EventArgs e)
        {
            menuButton2.PanelColor = Color.FromArgb(26, 52, 232);
            menuLabel3.BackColor = Color.FromArgb(26, 52, 232);
            menuPictureBox3.BackColor = Color.FromArgb(26, 52, 232);
        }

        private void menuButton3_MouseEnter(object sender, EventArgs e)
        {
            menuButton3.PanelColor = Color.FromArgb(26, 52, 232);
            menuLabel5.BackColor = Color.FromArgb(26, 52, 232);
            menuPictureBox5.BackColor = Color.FromArgb(26, 52, 232);
        }

        private void menuButton1_MouseLeave(object sender, EventArgs e)
        {
            menuButton1.PanelColor = Color.FromArgb(25, 55, 255);
            menuLabel2.BackColor = Color.FromArgb(25, 55, 255);
            menuPictureBox2.BackColor = Color.FromArgb(25, 55, 255);
        }

        private void menuButton2_MouseLeave(object sender, EventArgs e)
        {
            menuButton2.PanelColor = Color.FromArgb(25, 55, 255);
            menuLabel3.BackColor = Color.FromArgb(25, 55, 255);
            menuPictureBox3.BackColor = Color.FromArgb(25, 55, 255);
        }

        private void menuButton3_MouseLeave(object sender, EventArgs e)
        {
            menuButton3.PanelColor = Color.FromArgb(25, 55, 255);
            menuLabel5.BackColor = Color.FromArgb(25, 55, 255);
            menuPictureBox5.BackColor = Color.FromArgb(25, 55, 255);
        }

        private void upperButton3_Click(object sender, EventArgs e)
        {
            Logger.Info("Выход из приложения с формы оператора");
            Program.AppExit();
        }

        private void upperButton2_Click(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Normal)
                this.WindowState = FormWindowState.Maximized;
            else
                this.WindowState = FormWindowState.Normal;
        }

        private void upperButton1_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void upperPanel_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
            }
        }

        private void upperLabel1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
            }
        }

        private void upperPictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
            }
        }

        private void PanelClose(Timer timer)
        {
            if (timer == AcceptSubscriptionsTimer && !Subscriptions1)
                AcceptSubscriptionsTimer.Start();
            logicPanelClose(Subscriptions1, AcceptSubscriptionsTimer);
        }

        private void logicPanelClose(bool logic, Timer timer)
        {
            if (logic)
                timer.Start();
        }

        private void menuButton1_Click(object sender, EventArgs e)
        {
            tasksPanel.Visible = false;
            subscriptionsPanel.Visible = true;
            if (subscriptionsPanel.Visible == false)
                PanelClose(AcceptSubscriptionsTimer);
        }

        private async void menuButton2_Click(object sender, EventArgs e)
        {
            tasksPanel.Visible = true;
            subscriptionsPanel.Visible = false;
            PanelClose(null);
        }

        private async void menuButton3_Click(object sender, EventArgs e)
        {
            await UserData.LogoutAndExit(this);
        }

        private async void tasksButton3_1_Click(object sender, EventArgs e)
        {
            TaskOnEmployee.ClickTasksButton(tasksButton3_1, tasksDataGridView, tasksTextBox2_1, tasksLabel3_3, tasksDatePicker3_1, tasksDatePicker3_2);
        }

        private async void subscriptionsButton1_1_Click(object sender, EventArgs e)
        {
            SwitchMainTables(subscriptionsDataGridView1_1);
        }

        private async void subscriptionsButton1_2_Click(object sender, EventArgs e)
        {
            SwitchMainTables(subscriptionsDataGridView1_2);
        }

        private async void subscriptionsButton1_3_Click(object sender, EventArgs e)
        {
            SwitchMainTables(subscriptionsDataGridView1_3);
        }

        private async void SwitchMainTables(DataGridView dataGridView)
        {
            subscriptionsDataGridView1_1.Dock = DockStyle.None;
            subscriptionsDataGridView1_2.Dock = DockStyle.None;
            subscriptionsDataGridView1_3.Dock = DockStyle.None;
            subscriptionsDataGridView1_1.Visible = false;
            subscriptionsDataGridView1_2.Visible = false;
            subscriptionsDataGridView1_3.Visible = false;

            dataGridView.Dock = DockStyle.Fill;
            dataGridView.Visible = true;

            if (SearchTasksTextBox1.Content != string.Empty)
                SearchTasksTextBox1.Content = string.Empty;

            ComboBoxUpdate(dataGridView);
            _currentActiveTable = dataGridView;

            if (dataGridView == subscriptionsDataGridView1_1)
            {
                EditReaderPanel.Visible = false;
                RegistrationReaderPanel.Visible = false;
                EditSubscriptionsPanel.Visible = true;
                AcceptSubscriptionsPanel.Visible = true;
                _currentTable = TableType.SubscriptionsMain;
            }
            else if (dataGridView == subscriptionsDataGridView1_2)
            {
                EditReaderPanel.Visible = false;
                RegistrationReaderPanel.Visible = false;
                EditSubscriptionsPanel.Visible = true;
                AcceptSubscriptionsPanel.Visible = true;
                _currentTable = TableType.EditionsMain;
            }
            else if (dataGridView == subscriptionsDataGridView1_3)
            {
                EditReaderPanel.Visible = true;
                RegistrationReaderPanel.Visible = true;
                EditSubscriptionsPanel.Visible = false;
                AcceptSubscriptionsPanel.Visible = false;
                _currentTable = TableType.ReadersMain;
            }
            await LoadDataAsync(_currentTable);
        }

        private void ComboBoxUpdate(DataGridView dataGridView)
        {
            SearchTasksComboBox1.Items = new string[0];
            foreach (DataGridViewColumn item in dataGridView.Columns)
            {
                if (item.Visible)
                    SearchTasksComboBox1.AddItem(item.HeaderText);
            }
            SearchTasksComboBox1.AddItem("Показывать всё");
            SearchTasksComboBox1.SelectedItem = "Показывать всё";
        }

        private void AcceptSubscriptionsPanelTimer_Tick(object sender, EventArgs e)
        {
            int step;
            int diff;
            if (Subscriptions1)
            {
                diff = AcceptSubscriptionsPanel.Height - AcceptSubscriptionsPanel.MinimumSize.Height;
                step = Math.Max(2, diff / 5);
                AcceptSubscriptionsPanel.Height -= step;
                EditSubscriptionsPanel.Top -= step;
                if (AcceptSubscriptionsPanel.Height <= AcceptSubscriptionsPanel.MinimumSize.Height)
                {
                    AcceptSubscriptionsPanel.Height = AcceptSubscriptionsPanel.MinimumSize.Height;
                    Subscriptions1 = false;
                    AcceptSubscriptionsTimer.Stop();
                }
            }
            else
            {
                diff = AcceptSubscriptionsPanel.MaximumSize.Height - AcceptSubscriptionsPanel.Height;
                step = Math.Max(2, diff / 5);
                AcceptSubscriptionsPanel.Height += step;
                EditSubscriptionsPanel.Top += step;
                if (AcceptSubscriptionsPanel.Height >= AcceptSubscriptionsPanel.MaximumSize.Height)
                {
                    AcceptSubscriptionsPanel.Height = AcceptSubscriptionsPanel.MaximumSize.Height;
                    Subscriptions1 = true;
                    AcceptSubscriptionsTimer.Stop();
                }
            }
        }

        private void AcceptSubscriptionsPanel1_Click(object sender, EventArgs e)
        {
            AcceptSubscriptionsTimer.Start();
        }

        private void EditSubscriptionsTimer_Tick(object sender, EventArgs e)
        {
            int step;
            int diff;
            if (Subscriptions2)
            {
                diff = EditSubscriptionsPanel.Height - EditSubscriptionsPanel.MinimumSize.Height;
                step = Math.Max(2, diff / 5);
                EditSubscriptionsPanel.Height -= step;
                if (EditSubscriptionsPanel.Height <= EditSubscriptionsPanel.MinimumSize.Height)
                {
                    EditSubscriptionsPanel.Height = EditSubscriptionsPanel.MinimumSize.Height;
                    Subscriptions2 = false;
                    EditSubscriptionsTimer.Stop();
                }
            }
            else
            {
                diff = EditSubscriptionsPanel.MaximumSize.Height - EditSubscriptionsPanel.Height;
                step = Math.Max(2, diff / 5);
                EditSubscriptionsPanel.Height += step;
                if (EditSubscriptionsPanel.Height >= EditSubscriptionsPanel.MaximumSize.Height)
                {
                    EditSubscriptionsPanel.Height = EditSubscriptionsPanel.MaximumSize.Height;
                    Subscriptions2 = true;
                    EditSubscriptionsTimer.Stop();
                }
            }
        }

        private void EditSubscriptionsPanel1_Click(object sender, EventArgs e)
        {
            EditSubscriptionsTimer.Start();
        }

        private void RegistrationReaderTimer_Tick(object sender, EventArgs e)
        {
            int step;
            int diff;
            if (Reader1)
            {
                diff = RegistrationReaderPanel.Height - RegistrationReaderPanel.MinimumSize.Height;
                step = Math.Max(2, diff / 5);
                RegistrationReaderPanel.Height -= step;
                EditReaderPanel.Top -= step;
                if (RegistrationReaderPanel.Height <= RegistrationReaderPanel.MinimumSize.Height)
                {
                    RegistrationReaderPanel.Height = RegistrationReaderPanel.MinimumSize.Height;
                    Reader1 = false;
                    RegistrationReaderTimer.Stop();
                }
            }
            else
            {
                diff = RegistrationReaderPanel.MaximumSize.Height - RegistrationReaderPanel.Height;
                step = Math.Max(2, diff / 5);
                RegistrationReaderPanel.Height += step;
                EditReaderPanel.Top += step;
                if (RegistrationReaderPanel.Height >= RegistrationReaderPanel.MaximumSize.Height)
                {
                    RegistrationReaderPanel.Height = RegistrationReaderPanel.MaximumSize.Height;
                    Reader1 = true;
                    RegistrationReaderTimer.Stop();
                }
            }
        }

        private void RegistrationReaderPanel1_Click(object sender, EventArgs e)
        {
            RegistrationReaderTimer.Start();
        }

        private void EditReaderTimer_Tick(object sender, EventArgs e)
        {
            int step;
            int diff;
            if (Reader2)
            {
                diff = EditReaderPanel.Height - EditReaderPanel.MinimumSize.Height;
                step = Math.Max(2, diff / 5);
                EditReaderPanel.Height -= step;
                if (EditReaderPanel.Height <= EditReaderPanel.MinimumSize.Height)
                {
                    EditReaderPanel.Height = EditReaderPanel.MinimumSize.Height;
                    Reader2 = false;
                    EditReaderTimer.Stop();
                }
            }
            else
            {
                diff = EditReaderPanel.MaximumSize.Height - EditReaderPanel.Height;
                step = Math.Max(2, diff / 5);
                EditReaderPanel.Height += step;
                if (EditReaderPanel.Height >= EditReaderPanel.MaximumSize.Height)
                {
                    EditReaderPanel.Height = EditReaderPanel.MaximumSize.Height;
                    Reader2 = true;
                    EditReaderTimer.Stop();
                }
            }
        }

        private void EditReader1_Click(object sender, EventArgs e)
        {
            EditReaderTimer.Start();
        }

        private async void operatorForm_Load(object sender, EventArgs e)
        {
            Program.StartCustomizationRoleForm(upperLabel1, menuLabel4);

            InitializeTables();
            InitializeTimer();
            SubscriptionEvents();
            SwitchMainTables(subscriptionsDataGridView1_1);
            SetupTextBoxValidation();

            TaskOnEmployee.InitializeTaskComboBox(cuiComboBox1, tasksDataGridView);
            LoadDataStart();
            _autoUpdateTasksTimer = TaskOnEmployee.UpdateTasksTimer(menuPictureBox3, cuiPictureBox2);
        }

        private void InitializeTables()
        {
            DataTables.InitializeTasksTable(tasksDataGridView);
            DataTables.InitializeSubscriptionsTable(subscriptionsDataGridView1_1);
            DataTables.InitializeEditionsTable(subscriptionsDataGridView1_2);
            DataTables.InitializeReadersTable(subscriptionsDataGridView1_3);
            DataTables.InitializeReadersTable(AcceptSubscriptionsDataGridView2_1);
            DataTables.InitializeReadersTable(EditSubscriptionsDataGridView2_1);
            DataTables.InitializeAddressTable(RegistrationReaderDataGridView2_1);
            DataTables.InitializeAddressTable(EditReaderDataGridView2_1);
        }

        private async void LoadDataStart()
        {
            await LoadDataAsync(TableType.ReadersAdd);
            await LoadDataAsync(TableType.ReadersEdit);
            await LoadDataAsync(TableType.AddressesAdd);
            await LoadDataAsync(TableType.AddressesEdit);
            await LoadDataAsync(TableType.Tasks);
        }

        private void SetupTextBoxValidation()
        {
            // Запрещаем пробелы в полях ввода
            RegistrationReaderTextBox1.KeyPress += TextBox_KeyPress;
            RegistrationReaderTextBox2.KeyPress += TextBox_KeyPress;
            RegistrationReaderTextBox3.KeyPress += TextBox_KeyPress;
            EditReaderTextBox1.KeyPress += TextBox_KeyPress;
            EditReaderTextBox2.KeyPress += TextBox_KeyPress;
            EditReaderTextBox3.KeyPress += TextBox_KeyPress;

            cuiTextBox3.KeyPress += BlockTextBox_KeyPress;
            AcceptSubscriptionsTextBox2.KeyPress += BlockTextBox_KeyPress;
            cuiTextBox2.KeyPress += BlockTextBox_KeyPress;
            EditSubscriptionsTextBox2.KeyPress += BlockTextBox_KeyPress;
            RegistrationReaderTextBox4.KeyPress += BlockTextBox_KeyPress;
            EditReaderTextBox4.KeyPress += BlockTextBox_KeyPress;
        }

        private void TextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Блокируем ввод пробелов
            if (e.KeyChar == ' ')
                e.Handled = true;
        }

        private void BlockTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
        }

        private void SubscriptionEvents()
        {
            subscriptionsDataGridView1_1.Scroll += (s, t) => ResetTimer();
            subscriptionsDataGridView1_1.MouseWheel += (s, t) => ResetTimer();
            subscriptionsDataGridView1_2.Scroll += (s, t) => ResetTimer();
            subscriptionsDataGridView1_2.MouseWheel += (s, t) => ResetTimer();
            subscriptionsDataGridView1_3.Scroll += (s, t) => ResetTimer();
            subscriptionsDataGridView1_3.MouseWheel += (s, t) => ResetTimer();

            _searchEditions = new SearchFilter<DataBase.Editions>();
            _loaderEditions = new LazyLoader<DataBase.Editions>(_searchEditions);
            _searchSubs = new SearchFilter<DataBase.Subscriptions>();
            _loaderSubs = new LazyLoader<DataBase.Subscriptions>(_searchSubs);
            _searchReds = new SearchFilter<DataBase.Readers>();
            _loaderReds = new LazyLoader<DataBase.Readers>(_searchReds);

            tasksDataGridView.Scroll += (s, t) => ResetTimer();
            tasksDataGridView.MouseWheel += (s, t) => ResetTimer();
            _searchTasks = new SearchFilter<DataBase.Tasks>();
            _loaderTasks = new LazyLoader<DataBase.Tasks>(_searchTasks);

            AcceptSubscriptionsDataGridView2_1.Scroll += (s, t) => ResetTimer();
            AcceptSubscriptionsDataGridView2_1.MouseWheel += (s, t) => ResetTimer();
            _searchReds1 = new SearchFilter<DataBase.Readers>();
            _loaderReds1 = new LazyLoader<DataBase.Readers>(_searchReds1);

            EditSubscriptionsDataGridView2_1.Scroll += (s, t) => ResetTimer();
            EditSubscriptionsDataGridView2_1.MouseWheel += (s, t) => ResetTimer();
            _searchReds2 = new SearchFilter<DataBase.Readers>();
            _loaderReds2 = new LazyLoader<DataBase.Readers>(_searchReds2);

            RegistrationReaderDataGridView2_1.Scroll += (s, t) => ResetTimer();
            RegistrationReaderDataGridView2_1.MouseWheel += (s, t) => ResetTimer();
            _searchStreet = new SearchFilter<DataBase.Markers>();
            _loaderStreet = new LazyLoader<DataBase.Markers>(_searchStreet);

            EditReaderDataGridView2_1.Scroll += (s, t) => ResetTimer();
            EditReaderDataGridView2_1.MouseWheel += (s, t) => ResetTimer();
            _searchStreet1 = new SearchFilter<DataBase.Markers>();
            _loaderStreet1 = new LazyLoader<DataBase.Markers>(_searchStreet1);
        }

        private void ResetTimer()
        {
            _scrollDebounceTimer.Stop();
            _scrollDebounceTimer.Start();
        }

        private void InitializeTimer()
        {
            _scrollDebounceTimer = new Timer { Interval = 100 };
            _scrollDebounceTimer.Tick += async (s, e) =>
            {
                _scrollDebounceTimer.Stop();
                await CheckScrollAndLoad();
            };
        }

        private async Task CheckScrollAndLoad()
        {
            // Защита от null и от конкурентных вызовов
            if (_currentActiveTable == null)
                return;

            try
            {
                int firstVisible = _currentActiveTable.FirstDisplayedScrollingRowIndex;
                int firstTaskVisible = tasksDataGridView.FirstDisplayedScrollingRowIndex;
                int firstStreetVisible = RegistrationReaderDataGridView2_1.FirstDisplayedScrollingRowIndex;
                int firstStreetVisible1 = EditReaderDataGridView2_1.FirstDisplayedScrollingRowIndex;
                int firstReaderVisible = AcceptSubscriptionsDataGridView2_1.FirstDisplayedScrollingRowIndex;
                int firstReaderVisible1 = EditSubscriptionsDataGridView2_1.FirstDisplayedScrollingRowIndex;

                if (firstVisible < 0 && firstTaskVisible < 0 && firstStreetVisible < 0 &&
                    firstStreetVisible1 < 0 && firstReaderVisible < 0 && firstReaderVisible1 < 0)
                    return;

                if (firstVisible + _currentActiveTable.DisplayedRowCount(false) >= _currentActiveTable.RowCount - 10)
                    await LoadDataAsync(_currentTable);
                if (firstTaskVisible + tasksDataGridView.DisplayedRowCount(false) >= tasksDataGridView.RowCount - 10)
                    await LoadDataAsync(TableType.Tasks);
                if (firstStreetVisible + RegistrationReaderDataGridView2_1.DisplayedRowCount(false) >= RegistrationReaderDataGridView2_1.RowCount - 10)
                    await LoadDataAsync(TableType.AddressesAdd);
                if (firstStreetVisible1 + EditReaderDataGridView2_1.DisplayedRowCount(false) >= EditReaderDataGridView2_1.RowCount - 10)
                    await LoadDataAsync(TableType.AddressesEdit);
                if (firstReaderVisible + AcceptSubscriptionsDataGridView2_1.DisplayedRowCount(false) >= AcceptSubscriptionsDataGridView2_1.RowCount - 10)
                    await LoadDataAsync(TableType.ReadersAdd);
                if (firstReaderVisible1 + EditSubscriptionsDataGridView2_1.DisplayedRowCount(false) >= EditSubscriptionsDataGridView2_1.RowCount - 10)
                    await LoadDataAsync(TableType.ReadersEdit);
            }
            catch { }
        }

        private async void SearchTasksButton1_Click(object sender, EventArgs e)
        {
            try
            {
                string searchText = SearchTasksTextBox1.Content;
                string selectedHeader = SearchTasksComboBox1.SelectedItem?.ToString();

                // Находим реальное имя свойства в классе по заголовку колонки
                var col = _currentActiveTable.Columns.Cast<DataGridViewColumn>().FirstOrDefault(c => c.HeaderText == selectedHeader && c.Visible);
                string propertyName = col?.DataPropertyName;

                _currentActiveTable.Rows.Clear();

                // Применяем фильтр к нужному движку поиска
                if (_currentTable == TableType.EditionsMain)
                {
                    _loaderEditions.Reset(); // Сбрасываем пагинацию на 0

                    if (string.IsNullOrEmpty(propertyName) || selectedHeader == "Показывать всё")
                    {
                        _searchEditions.Clear();
                        Logger.Info($"Отменен фильтр к таблице Издания");
                    }
                    else
                    {
                        _searchEditions.SetFilter(propertyName, searchText);
                        Logger.Info($"Применен фильтр {propertyName}:{searchText} к таблице Издания");
                    }
                }
                else if (_currentTable == TableType.SubscriptionsMain)
                {
                    _loaderSubs.Reset();

                    if (string.IsNullOrEmpty(propertyName) || selectedHeader == "Показывать всё")
                    {
                        _searchSubs.Clear();
                        _locallyAddedSubscriptionIds.Clear();
                        Logger.Info($"Отменен фильтр к таблице Подписки");
                    }
                    else
                    {
                        _searchSubs.SetFilter(propertyName, searchText);
                        Logger.Info($"Применен фильтр {propertyName}:{searchText} к таблице Подписки");
                    }
                }
                else if (_currentTable == TableType.ReadersMain)
                {
                    _loaderReds.Reset();

                    if (string.IsNullOrEmpty(propertyName) || selectedHeader == "Показывать всё")
                    {
                        _searchReds.Clear();
                        _locallyAddedReaderIds.Clear();
                        Logger.Info($"Отменен фильтр к таблице Читатели");
                    }
                    else
                    {
                        _searchReds.SetFilter(propertyName, searchText);
                        Logger.Info($"Применен фильтр {propertyName}:{searchText} к таблице Читатели");
                    }
                }

                // Загружаем заново с учетом фильтра
                await LoadDataAsync(_currentTable);
            }
            catch { }
        }

        private async Task LoadDataAsync(TableType tableType)
        {
            try
            {
                if (tableType == TableType.EditionsMain)
                {
                    var data = await _loaderEditions.LoadNextBatchAsync();

                    foreach (var item in data)
                        DataTables.AddEditionRow(subscriptionsDataGridView1_2, item);
                }
                else if (tableType == TableType.SubscriptionsMain)
                {
                    var data = await _loaderSubs.LoadNextBatchAsync();

                    foreach (var item in data)
                    {
                        if (_locallyAddedSubscriptionIds.Contains(item.Id))
                            continue;

                        DataTables.AddSubscriptionRow(subscriptionsDataGridView1_1, item);
                    }
                }
                else if (tableType == TableType.ReadersMain)
                {
                    var data = await _loaderReds.LoadNextBatchAsync();

                    foreach (var item in data)
                    {
                        if (_locallyAddedReaderIds.Contains(item.Id))
                            continue;

                        DataTables.AddReaderTableRow(subscriptionsDataGridView1_3, item);
                    }
                }
                else if (tableType == TableType.ReadersAdd)
                {
                    var data = await _loaderReds1.LoadNextBatchAsync();

                    foreach (var item in data)
                    {
                        if (_locallyAddedReaderIds.Contains(item.Id))
                            continue;

                        DataTables.AddReaderTableRow(AcceptSubscriptionsDataGridView2_1, item);
                    }
                }
                else if (tableType == TableType.ReadersEdit)
                {
                    var data = await _loaderReds2.LoadNextBatchAsync();

                    foreach (var item in data)
                    {
                        if (_locallyAddedReaderIds.Contains(item.Id))
                            continue;

                        DataTables.AddReaderTableRow(EditSubscriptionsDataGridView2_1, item);
                    }
                }
                else if (tableType == TableType.AddressesAdd)
                {
                    var data = await _loaderStreet.LoadNextBatchAsync();

                    foreach (var item in data)
                        DataTables.AddStreetRow(RegistrationReaderDataGridView2_1, item);
                }
                else if (tableType == TableType.AddressesEdit)
                {
                    var data = await _loaderStreet1.LoadNextBatchAsync();

                    foreach (var item in data)
                        DataTables.AddStreetRow(EditReaderDataGridView2_1, item);
                }
                else if (tableType == TableType.Tasks)
                {
                    var data = await _loaderTasks.LoadNextBatchAsync();

                    foreach (var item in data)
                    {
                        if (item.IdEmployee == UserData.CurrentUser.Employee.Id)
                        {
                            await DataTables.AddTaskRow(tasksDataGridView, item);
                            await TaskOnEmployee.MarkAsAcceptedIfNew(item, tasksDataGridView, menuPictureBox3);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка загрузки таблиц из базы данных", ex);
                Logger.ShowError("Ошибка загрузки таблиц из базы данных");
            }
        }

        private async void subscriptionsDataGridView1_1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex >= 0 && e.RowIndex < subscriptionsDataGridView1_1.Rows.Count - 1)
                {
                    // Получаем строку
                    var row = subscriptionsDataGridView1_1.Rows[e.RowIndex];
                    Guid id = Guid.Parse(row.Cells["Id"].Value.ToString());

                    var subscriptionMain = await DataBase._client.From<DataBase.Subscriptions>().Where(x => x.Id == id).Single();
                    if (row.Cells["Id"].Value == null || string.IsNullOrEmpty(row.Cells["Id"].Value.ToString()))
                        return;

                    _selectedSubscription = subscriptionMain;
                    var editionMain = await DataBase._client.From<DataBase.Editions>().Where(x => x.Index != null && x.Index == subscriptionMain.IndexEdition).Single();
                    _selectedEdition = editionMain;

                    cuiTextBox2.Content = _selectedEdition.Name;
                    EditSubscriptionsTextBox1.Content = subscriptionMain.Kit.ToString();
                    EditSubscriptionsTextBox2.Content = _selectedReader1.FIO;

                    string term = subscriptionMain.TermSubscription;
                    TermResult(EditSubscriptionsButton2, term);
                    TermResult(EditSubscriptionsButton3, term);
                    TermResult(EditSubscriptionsButton4, term);
                    TermResult(EditSubscriptionsButton5, term);
                    TermResult(EditSubscriptionsButton6, term);
                    TermResult(EditSubscriptionsButton7, term);
                    TermResult(EditSubscriptionsButton8, term);
                    TermResult(EditSubscriptionsButton9, term);
                    TermResult(EditSubscriptionsButton10, term);
                    TermResult(EditSubscriptionsButton11, term);
                    TermResult(EditSubscriptionsButton12, term);
                    TermResult(EditSubscriptionsButton13, term);

                    // Автоматически вызываем метод для расчета цен
                    EditSubscriptionsButton2_Click(null, null);
                }
            }
            catch { }
        }

        private void TermResult(cuiButton button, string term)
        {
            if (!button.Checked && term[0] == '1')
                button.Checked = true;
            else
                button.Checked = false;
        }

        private async void subscriptionsDataGridView1_2_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex >= 0 && e.RowIndex < subscriptionsDataGridView1_2.Rows.Count - 1)
                {
                    // Получаем строку
                    var row = subscriptionsDataGridView1_2.Rows[e.RowIndex];
                    Guid id = Guid.Parse(row.Cells["Id"].Value.ToString());

                    var editionMain = await DataBase._client.From<DataBase.Editions>().Where(x => x.Id == id).Single();
                    if (row.Cells["Id"].Value == null || string.IsNullOrEmpty(row.Cells["Id"].Value.ToString()))
                        return;

                    _selectedEdition = editionMain;

                    float priceMonth = _selectedEdition.MinTermHousePrice / _selectedEdition.MinTermSubscription;
                    AcceptSubscriptionsLabel9.Content = $"{priceMonth} ₽";
                    cuiTextBox2.Content = _selectedEdition.Name;
                    cuiTextBox3.Content = _selectedEdition.Name;

                    // Автоматически вызываем методы для расчета цен
                    AcceptSubscriptionsButton2_Click(null, null);
                    EditSubscriptionsButton2_Click(null, null);
                }
            }
            catch { }
        }

        private async void subscriptionsDataGridView1_3_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex >= 0 && e.RowIndex < subscriptionsDataGridView1_3.Rows.Count - 1)
                {
                    // Получаем строку
                    var row = subscriptionsDataGridView1_3.Rows[e.RowIndex];
                    Guid id = Guid.Parse(row.Cells["Id"].Value.ToString());

                    var readerMain = await DataBase._client.From<DataBase.Readers>().Where(x => x.Id != null && x.Id == id).Single();
                    if (row.Cells["Id"].Value == null || string.IsNullOrEmpty(row.Cells["Id"].Value.ToString()))
                        return;

                    _selectedReader = readerMain;
                }
            }
            catch { }
        }

        private async void tasksDataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && e.RowIndex < tasksDataGridView.Rows.Count - 1)
                {
                    var row = tasksDataGridView.Rows[e.RowIndex];
                    var id = Guid.Parse(row.Cells["Id"].Value.ToString());

                    var task = await DataBase._client.From<DataBase.Tasks>().Where(x => x.Id == id).Single();
                    if (task == null)
                        return;

                    await TaskOnEmployee.UpdateSelectedTask(task);
                    TaskOnEmployee.VisualChangedStatus(tasksButton3_1, tasksTextBox2_1, tasksLabel3_3, tasksDatePicker3_1, tasksDatePicker3_2, task);
                }
            }
            catch { }
        }

        private async void AcceptSubscriptionsDataGridView2_1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex >= 0 && e.RowIndex < AcceptSubscriptionsDataGridView2_1.Rows.Count - 1)
                {
                    // Получаем строку
                    var row = AcceptSubscriptionsDataGridView2_1.Rows[e.RowIndex];

                    AcceptSubscriptionsTextBox2.Content = row.Cells["FIO"].Value?.ToString();
                    var readerAdd = await DataBase._client.From<DataBase.Readers>().Where(x => x.Id == Guid.Parse(row.Cells["Id"].Value.ToString())).Single();

                    _selectedReader = readerAdd;
                }
            }
            catch { }
        }

        private async void EditSubscriptionsDataGridView2_1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex >= 0 && e.RowIndex < EditSubscriptionsDataGridView2_1.Rows.Count - 1)
                {
                    // Получаем строку
                    var row = EditSubscriptionsDataGridView2_1.Rows[e.RowIndex];

                    EditSubscriptionsTextBox2.Content = row.Cells["FIO"].Value?.ToString();
                    var readerEdit = await DataBase._client.From<DataBase.Readers>().Where(x => x.Id == Guid.Parse(row.Cells["Id"].Value.ToString())).Single();

                    _selectedReader1 = readerEdit;
                }
            }
            catch { }
        }

        private async void AcceptSubscriptionsButton1_Click(object sender, EventArgs e)
        {
            try
            {
                string searchText = AcceptSubscriptionsTextBox2.Content;

                // Находим реальное имя свойства в классе по заголовку колонки
                var col = AcceptSubscriptionsDataGridView2_1.Columns.Cast<DataGridViewColumn>().FirstOrDefault(c => c.HeaderText == "FIO" && c.Visible);
                string propertyName = col?.DataPropertyName;

                AcceptSubscriptionsDataGridView2_1.Rows.Clear();

                _loaderReds1.Reset();

                if (string.IsNullOrEmpty(propertyName))
                {
                    _searchReds1.Clear();
                    _locallyAddedReaderIds.Clear();
                    Logger.Info($"Отменен фильтр к таблице Читатели");
                }
                else
                {
                    _searchReds1.SetFilter(propertyName, searchText);
                    Logger.Info($"Применен фильтр {propertyName} к таблице Читатели");
                }

                // Загружаем заново с учетом фильтра
                await LoadDataAsync(TableType.ReadersAdd);
            }
            catch { }
        }

        private async void EditSubscriptionsButton1_Click(object sender, EventArgs e)
        {
            try
            {
                string searchText = EditSubscriptionsTextBox2.Content;

                // Находим реальное имя свойства в классе по заголовку колонки
                var col = EditSubscriptionsDataGridView2_1.Columns.Cast<DataGridViewColumn>().FirstOrDefault(c => c.HeaderText == "FIO" && c.Visible);
                string propertyName = col?.DataPropertyName;

                EditSubscriptionsDataGridView2_1.Rows.Clear();

                _loaderReds2.Reset();
                _locallyAddedReaderIds.Clear();

                if (string.IsNullOrEmpty(propertyName))
                    _searchReds2.Clear();
                else
                    _searchReds2.SetFilter(propertyName, searchText);

                // Загружаем заново с учетом фильтра
                await LoadDataAsync(TableType.ReadersEdit);
            }
            catch { }
        }

        private async void AcceptSubscriptionsButton14_Click(object sender, EventArgs e)
        {
            if (_selectedReader == null)
            {
                Logger.ShowWarning("Выберите читателя");
                return;
            }
            if (_selectedEdition == null)
            {
                Logger.ShowWarning("Выберите издание");
                return;
            }

            try
            {
                string term = string.Empty;
                int kits = 1;
                term += TermCalculate(AcceptSubscriptionsButton2);
                term += TermCalculate(AcceptSubscriptionsButton3);
                term += TermCalculate(AcceptSubscriptionsButton4);
                term += TermCalculate(AcceptSubscriptionsButton5);
                term += TermCalculate(AcceptSubscriptionsButton6);
                term += TermCalculate(AcceptSubscriptionsButton7);
                term += TermCalculate(AcceptSubscriptionsButton8);
                term += TermCalculate(AcceptSubscriptionsButton9);
                term += TermCalculate(AcceptSubscriptionsButton10);
                term += TermCalculate(AcceptSubscriptionsButton11);
                term += TermCalculate(AcceptSubscriptionsButton12);
                term += TermCalculate(AcceptSubscriptionsButton13);

                int count = 0;
                for (int i = 0; i < term.Length; i++)
                {
                    if (term[i] == '1')
                        count++;
                }
                if (count < _selectedEdition.MinTermSubscription)
                {
                    Logger.ShowWarning($"Минимальный срок подписки должен составлять не менее {_selectedEdition.MinTermSubscription}");
                    return;
                }
                if (count > _selectedEdition.MaxTermSubscription)
                {
                    Logger.ShowWarning($"Максимальный срок подписки должен составлять не более {_selectedEdition.MaxTermSubscription}");
                    return;
                }

                int.TryParse(AcceptSubscriptionsTextBox1.Content, out kits);
                float priceMonth = _selectedEdition.MinTermHousePrice / _selectedEdition.MinTermSubscription;
                var newSubscription = new DataBase.Subscriptions
                {
                    Id = Guid.NewGuid(),
                    TermSubscription = term,
                    PriceSubscription = $"{priceMonth * count} ₽",
                    Kit = kits,
                    DateRegistred = DateTime.Now,
                    IndexEdition = _selectedEdition.Index
                };

                // Сохраняем в БД
                await DataBase._client.From<DataBase.Subscriptions>().Insert(newSubscription);

                string idSubscription = string.Empty;
                if (_selectedReader.IdActiveSubscriptions == string.Empty)
                    idSubscription = newSubscription.Id.ToString();
                else
                    idSubscription = $"{_selectedReader.IdActiveSubscriptions},{newSubscription.Id.ToString()}";

                var updatedReader = new DataBase.Readers
                {
                    Id = _selectedReader.Id,
                    FIO = _selectedReader.FIO,
                    IdActiveSubscriptions = idSubscription
                };

                // Обновляем в БД
                await DataBase._client.From<DataBase.Readers>().Upsert(updatedReader);

                // Очищаем форму
                ClearAssignSubscriptionForm();

                Logger.Info($"Подписка {idSubscription} успешно оформлена");
                Logger.ShowInfo("Подписка успешно оформлена");
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка при оформлении подписки", ex);
                Logger.ShowError("Ошибка при оформлении подписки");
            }
        }
        private void ClearAssignSubscriptionForm()
        {
            AcceptSubscriptionsTextBox1.Content = string.Empty;
            if (AcceptSubscriptionsButton2.Checked) AcceptSubscriptionsButton2.Checked = false;
            if (AcceptSubscriptionsButton3.Checked) AcceptSubscriptionsButton3.Checked = false;
            if (AcceptSubscriptionsButton4.Checked) AcceptSubscriptionsButton4.Checked = false;
            if (AcceptSubscriptionsButton5.Checked) AcceptSubscriptionsButton5.Checked = false;
            if (AcceptSubscriptionsButton6.Checked) AcceptSubscriptionsButton6.Checked = false;
            if (AcceptSubscriptionsButton7.Checked) AcceptSubscriptionsButton7.Checked = false;
            if (AcceptSubscriptionsButton8.Checked) AcceptSubscriptionsButton8.Checked = false;
            if (AcceptSubscriptionsButton9.Checked) AcceptSubscriptionsButton9.Checked = false;
            if (AcceptSubscriptionsButton10.Checked) AcceptSubscriptionsButton10.Checked = false;
            if (AcceptSubscriptionsButton11.Checked) AcceptSubscriptionsButton11.Checked = false;
            if (AcceptSubscriptionsButton12.Checked) AcceptSubscriptionsButton12.Checked = false;
            if (AcceptSubscriptionsButton13.Checked) AcceptSubscriptionsButton13.Checked = false;
        }
        private string TermCalculate(cuiButton button)
        {
            if (button.Checked)
                return "1";
            else
                return "0";
        }
        private async void AcceptSubscriptionsButton2_Click(object sender, MouseEventArgs e)
        {
            if (_selectedEdition != null)
            {
                await Task.Delay(delay);
                string term = string.Empty;
                term += TermCalculate(AcceptSubscriptionsButton2);
                term += TermCalculate(AcceptSubscriptionsButton3);
                term += TermCalculate(AcceptSubscriptionsButton4);
                term += TermCalculate(AcceptSubscriptionsButton5);
                term += TermCalculate(AcceptSubscriptionsButton6);
                term += TermCalculate(AcceptSubscriptionsButton7);
                term += TermCalculate(AcceptSubscriptionsButton8);
                term += TermCalculate(AcceptSubscriptionsButton9);
                term += TermCalculate(AcceptSubscriptionsButton10);
                term += TermCalculate(AcceptSubscriptionsButton11);
                term += TermCalculate(AcceptSubscriptionsButton12);
                term += TermCalculate(AcceptSubscriptionsButton13);

                int count = 0;
                for (int i = 0; i < term.Length; i++)
                {
                    if (term[i] == '1')
                        count++;
                }

                float priceMonth = _selectedEdition.MinTermHousePrice / _selectedEdition.MinTermSubscription;
                AcceptSubscriptionsLabel9.Content = $"{priceMonth} ₽";
                AcceptSubscriptionsLabel10.Content = $"{priceMonth * count} ₽";
                cuiLabel11.Content = $"За {count} мес 2026 г";
            }
        }

        private async void EditSubscriptionsButton2_Click(object sender, MouseEventArgs e)
        {
            if (_selectedSubscription != null)
            {
                await Task.Delay(delay);
                string term = string.Empty;
                term += TermCalculate(EditSubscriptionsButton2);
                term += TermCalculate(EditSubscriptionsButton3);
                term += TermCalculate(EditSubscriptionsButton4);
                term += TermCalculate(EditSubscriptionsButton5);
                term += TermCalculate(EditSubscriptionsButton6);
                term += TermCalculate(EditSubscriptionsButton7);
                term += TermCalculate(EditSubscriptionsButton8);
                term += TermCalculate(EditSubscriptionsButton9);
                term += TermCalculate(EditSubscriptionsButton10);
                term += TermCalculate(EditSubscriptionsButton11);
                term += TermCalculate(EditSubscriptionsButton12);
                term += TermCalculate(EditSubscriptionsButton13);

                int count = 0;
                for (int i = 0; i < term.Length; i++)
                {
                    if (term[i] == '1')
                        count++;
                }

                float priceMonth = _selectedEdition.MinTermHousePrice / _selectedEdition.MinTermSubscription;
                EditSubscriptionsLabel9.Content = $"{priceMonth} ₽";
                EditSubscriptionsLabel11.Content = $"{priceMonth * count} ₽";
                EditSubscriptionsLabel10.Content = $"За {count} мес 2026 г";
            }
        }

        private async void EditSubscriptionsButton14_Click(object sender, EventArgs e)
        {
            if (_selectedReader1 == null)
            {
                Logger.ShowWarning("Выберите читателя");
                return;
            }
            if (_selectedEdition == null)
            {
                Logger.ShowWarning("Выберите издание");
                return;
            }

            try
            {
                string term = string.Empty;
                int kits = 1;
                term += TermCalculate(EditSubscriptionsButton2);
                term += TermCalculate(EditSubscriptionsButton3);
                term += TermCalculate(EditSubscriptionsButton4);
                term += TermCalculate(EditSubscriptionsButton5);
                term += TermCalculate(EditSubscriptionsButton6);
                term += TermCalculate(EditSubscriptionsButton7);
                term += TermCalculate(EditSubscriptionsButton8);
                term += TermCalculate(EditSubscriptionsButton9);
                term += TermCalculate(EditSubscriptionsButton10);
                term += TermCalculate(EditSubscriptionsButton11);
                term += TermCalculate(EditSubscriptionsButton12);
                term += TermCalculate(EditSubscriptionsButton13);

                int count = 0;
                for (int i = 0; i < term.Length; i++)
                {
                    if (term[i] == '1')
                        count++;
                }
                if (count < _selectedEdition.MinTermSubscription)
                {
                    Logger.ShowWarning($"Минимальный срок подписки должен составлять не менее {_selectedEdition.MinTermSubscription}");
                    return;
                }
                if (count > _selectedEdition.MaxTermSubscription)
                {
                    Logger.ShowWarning($"Максимальный срок подписки должен составлять не более {_selectedEdition.MaxTermSubscription}");
                    return;
                }

                int.TryParse(EditSubscriptionsTextBox1.Content, out kits);
                float priceMonth = _selectedEdition.MinTermHousePrice / _selectedEdition.MinTermSubscription;
                var newSubscription = new DataBase.Subscriptions
                {
                    Id = _selectedSubscription.Id,
                    TermSubscription = term,
                    PriceSubscription = $"{priceMonth * count} ₽",
                    Kit = kits,
                    DateRegistred = _selectedSubscription.DateRegistred,
                    IndexEdition = _selectedEdition.Index
                };

                // Сохраняем в БД
                await DataBase._client.From<DataBase.Subscriptions>().Upsert(newSubscription);

                string idSubscription = string.Empty;
                if (_selectedReader1.IdActiveSubscriptions == string.Empty)
                    idSubscription = newSubscription.Id.ToString();
                else
                    idSubscription = $"{_selectedReader1.IdActiveSubscriptions},{newSubscription.Id.ToString()}";

                var updatedReader = new DataBase.Readers
                {
                    Id = _selectedReader1.Id,
                    FIO = _selectedReader1.FIO,
                    IdActiveSubscriptions = idSubscription
                };

                // Обновляем в БД
                await DataBase._client.From<DataBase.Readers>().Upsert(updatedReader);

                // Обновляем в таблице
                foreach (DataGridViewRow row in subscriptionsDataGridView1_1.Rows)
                {
                    if (row.Cells["Id"].Value != null && (Guid)row.Cells["Id"].Value == _selectedSubscription.Id)
                    {
                        var response = await DataBase._client.From<DataBase.Editions>().Where(x => x.Index == _selectedSubscription.IndexEdition).Single();
                        row.Cells["TermSubscription"].Value = newSubscription.TermSubscription;
                        row.Cells["PriceSubscription"].Value = newSubscription.PriceSubscription;
                        row.Cells["Kit"].Value = newSubscription.Kit;
                        row.Cells["IndexEdition"].Value = newSubscription.IndexEdition;
                        row.Cells["Edition"].Value = response;
                        break;
                    }
                }

                Logger.Info($"Подписка {idSubscription} успешно изменена");
                Logger.ShowInfo("Подписка успешно изменена");
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка при изменении подписки", ex);
                Logger.ShowError("Ошибка при изменении подписки");
            }
        }

        private async void EditSubscriptionsButton15_Click(object sender, EventArgs e)
        {
            if (_selectedSubscription == null)
            {
                Logger.ShowWarning("Выберите подписку для удаления");
                return;
            }

            var result = Logger.ShowYesNo("Вы уверены, что хотите удалить подписку?");
            if (result == DialogResult.Yes)
            {
                try
                {
                    // Удаляем подписку из БД
                    await DataBase._client.From<DataBase.Subscriptions>().Where(x => x.Id == _selectedSubscription.Id).Delete();

                    // Удаляем строку из DataGridView
                    foreach (DataGridViewRow row in subscriptionsDataGridView1_1.Rows)
                    {
                        if (row.Cells["Id"].Value != null && (Guid)row.Cells["Id"].Value == _selectedSubscription.Id)
                        {
                            subscriptionsDataGridView1_1.Rows.Remove(row);
                            break;
                        }
                    }

                    // Очищаем элементы управления
                    ClearEditControls();
                    Logger.Info("Подписка успешно удалена");
                    Logger.ShowInfo("Подписка успешно удалена");

                }
                catch (Exception ex)
                {
                    Logger.Error("Ошибка при удалении подписки", ex);
                    Logger.ShowError("Ошибка при удалении подписки");
                }
            }
        }

        private void ClearEditControls()
        {
            EditSubscriptionsTextBox1.Content = string.Empty;
            EditSubscriptionsButton1_Click(null, null);
            if (EditSubscriptionsButton2.Checked) EditSubscriptionsButton2.Checked = false;
            if (EditSubscriptionsButton3.Checked) EditSubscriptionsButton3.Checked = false;
            if (EditSubscriptionsButton4.Checked) EditSubscriptionsButton4.Checked = false;
            if (EditSubscriptionsButton5.Checked) EditSubscriptionsButton5.Checked = false;
            if (EditSubscriptionsButton6.Checked) EditSubscriptionsButton6.Checked = false;
            if (EditSubscriptionsButton7.Checked) EditSubscriptionsButton7.Checked = false;
            if (EditSubscriptionsButton8.Checked) EditSubscriptionsButton8.Checked = false;
            if (EditSubscriptionsButton9.Checked) EditSubscriptionsButton9.Checked = false;
            if (EditSubscriptionsButton10.Checked) EditSubscriptionsButton10.Checked = false;
            if (EditSubscriptionsButton11.Checked) EditSubscriptionsButton11.Checked = false;
            if (EditSubscriptionsButton12.Checked) EditSubscriptionsButton12.Checked = false;
            if (EditSubscriptionsButton13.Checked) EditSubscriptionsButton13.Checked = false;
            _selectedSubscription = null;
        }

        private async void RegistrationReaderButton1_Click(object sender, EventArgs e)
        {
            try
            {
                string searchText = RegistrationReaderTextBox4.Content;

                // Находим реальное имя свойства в классе по заголовку колонки
                var col = RegistrationReaderDataGridView2_1.Columns.Cast<DataGridViewColumn>().FirstOrDefault(c => c.HeaderText == "Address" && c.Visible);
                string propertyName = col?.DataPropertyName;

                RegistrationReaderDataGridView2_1.Rows.Clear();

                _loaderStreet.Reset();

                if (string.IsNullOrEmpty(propertyName))
                {
                    _searchStreet.Clear();
                    Logger.Info($"Отменен фильтр к таблице Адреса");
                }
                else
                {
                    _searchStreet.SetFilter(propertyName, searchText);
                    Logger.Info($"Применен фильтр {propertyName} к таблице Адреса");
                }

                // Загружаем заново с учетом фильтра
                await LoadDataAsync(TableType.AddressesAdd);
            }
            catch { }
        }

        private async void EditReaderButton1_Click(object sender, EventArgs e)
        {
            try
            {
                string searchText = EditReaderTextBox4.Content;

                // Находим реальное имя свойства в классе по заголовку колонки
                var col = EditReaderDataGridView2_1.Columns.Cast<DataGridViewColumn>().FirstOrDefault(c => c.HeaderText == "Address" && c.Visible);
                string propertyName = col?.DataPropertyName;

                EditReaderDataGridView2_1.Rows.Clear();

                _loaderStreet1.Reset();

                if (string.IsNullOrEmpty(propertyName))
                {
                    _searchStreet1.Clear();
                    Logger.Info($"Отменен фильтр к таблице Адреса");
                }
                else
                {
                    _searchStreet1.SetFilter(propertyName, searchText);
                    Logger.Info($"Применен фильтр {propertyName} к таблице Адреса");
                }

                // Загружаем заново с учетом фильтра
                await LoadDataAsync(TableType.AddressesEdit);
            }
            catch { }
        }

        private async void RegistrationReaderDataGridView2_1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex >= 0 && e.RowIndex < RegistrationReaderDataGridView2_1.Rows.Count - 1)
                {
                    // Получаем строку
                    var row = RegistrationReaderDataGridView2_1.Rows[e.RowIndex];

                    // Проверяем, что ячейка Id не пустая
                    if (row.Cells["Id"].Value == null || string.IsNullOrEmpty(row.Cells["Id"].Value.ToString()))
                        return;

                    Guid id = Guid.Parse(row.Cells["Id"].Value.ToString());
                    var street = await DataBase._client.From<DataBase.Markers>().Where(x => x.Id == id).Single();
                    _selectedStreet = street;

                    if (street.Apartment != string.Empty)
                        RegistrationReaderTextBox4.Content = $"{street.Street} {street.Apartment}";
                    else if (street.Building != string.Empty)
                        RegistrationReaderTextBox4.Content = $"{street.Street} {street.Building}";
                    else if (street.House != string.Empty)
                        RegistrationReaderTextBox4.Content = $"{street.Street} {street.House}";
                    else
                        RegistrationReaderTextBox4.Content = $"{street.Street} {street.TypeBuilding}";
                }
            }
            catch { }
        }

        private async void EditReaderDataGridView2_1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex >= 0 && e.RowIndex < EditReaderDataGridView2_1.Rows.Count - 1)
                {
                    // Получаем строку
                    var row = EditReaderDataGridView2_1.Rows[e.RowIndex];

                    // Проверяем, что ячейка Id не пустая
                    if (row.Cells["Id"].Value == null || string.IsNullOrEmpty(row.Cells["Id"].Value.ToString()))
                        return;

                    Guid id = Guid.Parse(row.Cells["Id"].Value.ToString());
                    var streetEdit = await DataBase._client.From<DataBase.Markers>().Where(x => x.Id == id).Single();

                    _selectedStreet = streetEdit;

                    if (streetEdit.Apartment != string.Empty)
                        EditReaderTextBox4.Content = $"{streetEdit.Street} {streetEdit.Apartment}";
                    else if (streetEdit.Building != string.Empty)
                        EditReaderTextBox4.Content = $"{streetEdit.Street} {streetEdit.Building}";
                    else if (streetEdit.House != string.Empty)
                        EditReaderTextBox4.Content = $"{streetEdit.Street} {streetEdit.House}";
                    else
                        EditReaderTextBox4.Content = $"{streetEdit.Street} {streetEdit.TypeBuilding}";

                    List<Guid> list = new List<Guid>();
                    string idReader = string.Empty;
                    for (int i = 0; i < streetEdit.IdReaders.Length; i++)
                    {
                        if (streetEdit.IdReaders[i] == ',')
                        {
                            list.Add(Guid.Parse(idReader));
                            idReader = string.Empty;
                            continue;
                        }
                        else
                            idReader += streetEdit.IdReaders[i];
                    }

                    if (list.Count > 0)
                    {
                        var reader = await DataBase._client.From<DataBase.Readers>().Where(x => x.Id == list[0]).Single();

                        string fio = reader.FIO;
                        string name = string.Empty;
                        string fam = string.Empty;
                        string otch = string.Empty;
                        int logic = 0;
                        bool isLogic = true;
                        for (int i = 0; i < fio.Length; i++)
                        {
                            if (fio[i] != ' ' && isLogic)
                            {
                                isLogic = false;
                                logic++;
                            }

                            if (logic == 1)
                                name += fio[i];
                            else if (logic == 2)
                                fam += fio[i];
                            else if (logic == 3)
                                otch += fio[i];
                        }

                        EditReaderTextBox1.Content = name;
                        EditReaderTextBox2.Content = fam;
                        EditReaderTextBox3.Content = otch;
                    }
                }
            }
            catch { }
        }

        private async void RegistrationReaderButton2_Click(object sender, EventArgs e)
        {
            // Проверка заполнения полей
            if (RegistrationReaderTextBox1.Content == string.Empty && RegistrationReaderTextBox2.Content == string.Empty)
            {
                Logger.ShowWarning("Введите хотя бы Имя и Фамилию");
                return;
            }

            if (_selectedStreet == null)
            {
                Logger.ShowWarning("Выберите улицу");
                return;
            }

            try
            {
                Guid id = Guid.NewGuid();
                var newReader = new DataBase.Readers
                {
                    Id = id,
                    FIO = $"{RegistrationReaderTextBox1.Content} {RegistrationReaderTextBox2.Content} {RegistrationReaderTextBox3.Content}",
                    IdActiveSubscriptions = string.Empty,
                };

                string readerIds = string.Empty;
                if (_selectedStreet.IdReaders.Length > 0)
                    readerIds = $"{_selectedStreet.IdReaders},{id.ToString()}";
                else
                    readerIds = id.ToString();

                // Сохраняем в БД
                await DataBase._client.From<DataBase.Readers>().Upsert(newReader);
                await DataBase._client.From<DataBase.Markers>().Where(x => x.Id == _selectedStreet.Id).Set(x => x.IdReaders, readerIds).Update();
                DataTables.AddReaderTableRow(subscriptionsDataGridView1_3, newReader);
                DataTables.AddReaderTableRow(AcceptSubscriptionsDataGridView2_1, newReader);
                DataTables.AddReaderTableRow(EditSubscriptionsDataGridView2_1, newReader);
                _locallyAddedReaderIds.Add(newReader.Id);

                // Очищаем форму
                _selectedReader = null;
                _selectedStreet = null;
                RegistrationReaderTextBox1.Content = string.Empty;
                RegistrationReaderTextBox2.Content = string.Empty;
                RegistrationReaderTextBox3.Content = string.Empty;
                RegistrationReaderTextBox4.Content = string.Empty;
                RegistrationReaderButton1_Click(null, null);

                MessageBox.Show("Читатель успешно зарегистрирован!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Logger.Info($"Читатель {newReader.FIO} успешно зарегистрирован");
                Logger.ShowInfo("Читатель успешно зарегистрирован");
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка при регистрации читателя", ex);
                Logger.ShowError("Ошибка при регистрации читателя");
            }
        }

        private async void EditReaderButton2_Click(object sender, EventArgs e)
        {
            // Проверка заполнения полей
            if (EditReaderTextBox1.Content == string.Empty && EditReaderTextBox2.Content == string.Empty)
            {
                Logger.ShowWarning("Введите хотя бы Имя и Фамилию");
                return;
            }

            if (_selectedStreet == null)
            {
                Logger.ShowWarning("Выберите улицу");
                return;
            }

            try
            {
                Guid id = _selectedReader.Id;
                var newReader = new DataBase.Readers
                {
                    Id = id,
                    FIO = $"{EditReaderTextBox1.Content} {EditReaderTextBox2.Content} {EditReaderTextBox3.Content}",
                    IdActiveSubscriptions = _selectedReader.IdActiveSubscriptions
                };

                string readerIds = string.Empty;
                if (_selectedStreet.IdReaders.Length > 0)
                    readerIds = $"{_selectedStreet.IdReaders},{id.ToString()}";
                else
                    readerIds = id.ToString();

                // Сохраняем в БД
                await DataBase._client.From<DataBase.Readers>().Upsert(newReader);
                await DataBase._client.From<DataBase.Markers>().Where(x => x.Id == _selectedStreet.Id).Set(x => x.IdReaders, readerIds).Update();

                // Обновляем DataGridView
                DataTables.AddReaderTableRow(subscriptionsDataGridView1_3, newReader);
                DataTables.AddReaderTableRow(AcceptSubscriptionsDataGridView2_1, newReader);
                DataTables.AddReaderTableRow(EditSubscriptionsDataGridView2_1, newReader);

                // Обновляем в таблицах
                foreach (DataGridViewRow row in subscriptionsDataGridView1_3.Rows)
                {
                    if (row.Cells["Id"].Value != null && (Guid)row.Cells["Id"].Value == newReader.Id)
                    {
                        row.Cells["FIO"].Value = newReader.FIO;
                        row.Cells["Id активных подписок"].Value = newReader.IdActiveSubscriptions;
                        break;
                    }
                }
                foreach (DataGridViewRow row in AcceptSubscriptionsDataGridView2_1.Rows)
                {
                    if (row.Cells["Id"].Value != null && (Guid)row.Cells["Id"].Value == newReader.Id)
                    {
                        row.Cells["FIO"].Value = newReader.FIO;
                        row.Cells["IdActiveSubscriptions"].Value = newReader.IdActiveSubscriptions;
                        break;
                    }
                }
                foreach (DataGridViewRow row in AcceptSubscriptionsDataGridView2_1.Rows)
                {
                    if (row.Cells["Id"].Value != null && (Guid)row.Cells["Id"].Value == newReader.Id)
                    {
                        row.Cells["FIO"].Value = newReader.FIO;
                        row.Cells["IdActiveSubscriptions"].Value = newReader.IdActiveSubscriptions;
                        break;
                    }
                }

                // Очищаем форму
                _selectedReader = null;
                _selectedStreet = null;
                EditReaderTextBox1.Content = string.Empty;
                EditReaderTextBox2.Content = string.Empty;
                EditReaderTextBox3.Content = string.Empty;
                EditReaderTextBox4.Content = string.Empty;
                EditReaderButton1_Click(null, null);

                Logger.Info($"Читатель {newReader.FIO} успешно изменен");
                Logger.ShowInfo("Читатель успешно изменен");
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка при изменении читателя", ex);
                Logger.ShowError("Ошибка при изменении читателя");
            }
        }

        private async void EditReaderButton3_Click(object sender, EventArgs e)
        {
            if (_selectedReader == null)
            {
                Logger.ShowWarning("Выберите читателя для удаления");
                return;
            }

            var result = Logger.ShowYesNo("Вы уверены, что хотите удалить читателя?");
            if (result == DialogResult.Yes)
            {
                try
                {
                    // Удаляем подписку из БД
                    await DataBase._client.From<DataBase.Readers>().Where(x => x.Id == _selectedReader.Id).Delete();

                    // Удаляем строку из DataGridView
                    foreach (DataGridViewRow row in subscriptionsDataGridView1_3.Rows)
                    {
                        if (row.Cells["Id"].Value != null && (Guid)row.Cells["Id"].Value == _selectedReader.Id)
                        {
                            subscriptionsDataGridView1_3.Rows.Remove(row);
                            break;
                        }
                    }
                    foreach (DataGridViewRow row in AcceptSubscriptionsDataGridView2_1.Rows)
                    {
                        if (row.Cells["Id"].Value != null && (Guid)row.Cells["Id"].Value == _selectedReader.Id)
                        {
                            AcceptSubscriptionsDataGridView2_1.Rows.Remove(row);
                            break;
                        }
                    }
                    foreach (DataGridViewRow row in EditSubscriptionsDataGridView2_1.Rows)
                    {
                        if (row.Cells["Id"].Value != null && (Guid)row.Cells["Id"].Value == _selectedReader.Id)
                        {
                            EditSubscriptionsDataGridView2_1.Rows.Remove(row);
                            break;
                        }
                    }

                    // Очищаем элементы управления
                    ClearEditControls();
                    Logger.Info($"Читатель успешно удален");
                    Logger.ShowInfo("Читатель успешно удален");
                }
                catch (Exception ex)
                {
                    Logger.Error("Ошибка при удалении читателя", ex);
                    Logger.ShowError("\"Ошибка при удалении читателя");
                }
            }
        }

        private async void cuiButton1_Click(object sender, EventArgs e)
        {
            try
            {
                string searchText = cuiTextBox1.Content;
                string selectedHeader = cuiComboBox1.SelectedItem?.ToString();

                // Находим реальное имя свойства в классе по заголовку колонки
                var col = tasksDataGridView.Columns.Cast<DataGridViewColumn>().FirstOrDefault(c => c.HeaderText == selectedHeader && c.Visible);
                string propertyName = col?.DataPropertyName;

                tasksDataGridView.Rows.Clear();

                // Применяем фильтр к нужному движку поиска
                _loaderTasks.Reset(); // Сбрасываем пагинацию на 0

                if (string.IsNullOrEmpty(propertyName) || selectedHeader == "Показывать всё")
                {
                    _searchTasks.Clear();
                    Logger.Info($"Отменен фильтр к таблице Задачи");
                }
                else
                {
                    _searchTasks.SetFilter(propertyName, searchText);
                    Logger.Info($"Применен фильтр {propertyName}:{searchText} к таблице Задачи");
                }
                await LoadDataAsync(TableType.Tasks);
            }
            catch { }
        }

        private void cuiPanel2_Click(object sender, EventArgs e)
        {
            IntegrityCheckForm integrityCheckForm = new IntegrityCheckForm();
            integrityCheckForm.Show(this);
        }
    }
}
