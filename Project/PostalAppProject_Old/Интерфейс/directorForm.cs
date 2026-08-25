using CuoreUI.Controls;
using GMap.NET.WindowsForms;
using RussianTransliteration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Интерфейс
{
    public partial class directorForm : Form
    {
        [DllImport("user32.dll")] private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")] private static extern bool ReleaseCapture();

        private bool menuStorage;
        private bool gmapStorage;
        private bool tasks1;
        private bool tasks2;
        private bool employee1;
        private bool employee2;
        private bool panelUp1 = false;
        private int targetYUp1 = -265;
        private int targetYDown1 = 99;
        private bool panelUp2 = false;
        private int targetYUp2 = -210;
        private int targetYDown2 = 99;
        private int routeMarkers = 0;
        private DataBase.Employees currentEmployee;

        private Timer _scrollDebounceTimer;
        private SearchFilter<DataBase.Tasks> _searchTasks;
        private LazyLoader<DataBase.Tasks> _loaderTasks;
        private SearchFilter<DataBase.Employees> _searchEmployees;
        private LazyLoader<DataBase.Employees> _loaderEmployees;

        private readonly HashSet<Guid> _locallyAddedTaskIds = new HashSet<Guid>();
        private readonly HashSet<Guid> _locallyAddedEmployeesIds = new HashSet<Guid>();

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTCAPTION = 0x2;

        public enum TableType
        {
            Employees,
            Tasks
        }

        public directorForm()
        {
            InitializeComponent();
            menuBurgerTimer.Start();
            OpenPanel();
            applyRadius();
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
            RoundHelper.Attach(gMapPanel, 45);
            RoundHelper.Attach(gMapPanel1, 45);
            RoundHelper.Attach(gMapPanel2, 45);
            RoundHelper.Attach(gMapPanel3, 45);
            RoundHelper.Attach(tasksDataGridView, 37);
            RoundHelper.Attach(registrationDataGridView, 37);
            RoundHelper.Attach(gMapPanel4, 45);
        }

        private void OpenPanel()
        {
            tasksPanel.Visible = false;
            tasksPanel.Location = new Point(77, 40);
            tasksPanel.Size = new Size(1218, 686);
            mapPanel.Visible = true;
            mapPanel.Location = new Point(77, 40);
            mapPanel.Size = new Size(1218, 686);
            registrationPanel.Visible = false;
            registrationPanel.Location = new Point(77, 40);
            registrationPanel.Size = new Size(1218, 686);
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
                menuButton4.Size = new Size(66, 84);
                menuLabel1.Visible = false;
                menuLabel2.Visible = false;
                menuLabel3.Visible = false;
                menuLabel4.Visible = false;
                menuLabel6.Visible = false;
                menuLabel5.Visible = false;
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
                menuButton4.Size = new Size(228, 84);
                menuLabel1.Visible = true;
                menuLabel2.Visible = true;
                menuLabel3.Visible = true;
                menuLabel4.Visible = true;
                menuLabel6.Visible = true;
                menuLabel5.Visible = true;
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
            menuLabel4.BackColor = Color.FromArgb(26, 52, 232);
            menuPictureBox4.BackColor = Color.FromArgb(26, 52, 232);
        }

        private void menuButton4_MouseEnter(object sender, EventArgs e)
        {
            menuButton4.PanelColor = Color.FromArgb(26, 52, 232);
            menuLabel6.BackColor = Color.FromArgb(26, 52, 232);
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
            menuLabel4.BackColor = Color.FromArgb(25, 55, 255);
            menuPictureBox4.BackColor = Color.FromArgb(25, 55, 255);
        }

        private void menuButton4_MouseLeave(object sender, EventArgs e)
        {
            menuButton4.PanelColor = Color.FromArgb(25, 55, 255);
            menuLabel6.BackColor = Color.FromArgb(25, 55, 255);
            menuPictureBox5.BackColor = Color.FromArgb(25, 55, 255);
        }

        private void upperButton3_Click(object sender, EventArgs e)
        {
            Logger.Info("Выход из приложения с формы директора");
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

        private void menuButton1_Click(object sender, EventArgs e)
        {
            tasksPanel.Visible = false;
            mapPanel.Visible = true;
            registrationPanel.Visible = false;
        }

        private async void menuButton2_Click(object sender, EventArgs e)
        {
            tasksPanel.Visible = true;
            mapPanel.Visible = false;
            registrationPanel.Visible = false;
        }

        private async void menuButton3_Click(object sender, EventArgs e)
        {
            registrationPanel.Visible = true;
            tasksPanel.Visible = false;
            mapPanel.Visible = false;
        }

        private async void menuButton4_Click(object sender, EventArgs e)
        {
            await UserData.LogoutAndExit(this);
        }

        private void menuBurgerTimer_Tick(object sender, EventArgs e)
        {
            int step;
            int diff;
            if (gmapStorage)
            {
                diff = gMapPanel2.Width - gMapPanel2.MinimumSize.Width;
                step = Math.Max(2, diff / 5);
                gMapPanel2.Width -= step;

                if (gMapPanel2.Width <= gMapPanel2.MinimumSize.Width)
                {
                    gMapPanel2.Width = gMapPanel2.MinimumSize.Width;
                    gmapStorage = false;
                    menuBurgerTimer.Stop();
                }
            }
            else
            {
                diff = gMapPanel2.MaximumSize.Width - gMapPanel2.Width;
                step = Math.Max(2, diff / 5);
                gMapPanel2.Width += step;

                if (gMapPanel2.Width >= gMapPanel2.MaximumSize.Width)
                {
                    gMapPanel2.Width = gMapPanel2.MaximumSize.Width;
                    gmapStorage = true;
                    menuBurgerTimer.Stop();
                }
            }
        }

        private void gMapPictureBox2_1_Click(object sender, EventArgs e)
        {
            menuBurgerTimer.Start();
        }

        private void gMapButton2_3_Click(object sender, EventArgs e)
        {
            panelUp1 = !panelUp1;
            layersTimer.Start();
        }

        private void layersTimer_Tick(object sender, EventArgs e)
        {
            int target = panelUp1 ? targetYDown1 : targetYUp1;
            int distance = target - gMapPanel3.Top;
            int step = (int)(distance * 0.1f);
            if (Math.Abs(distance) < 1)
            {
                gMapPanel3.Top = target;
                layersTimer.Stop();
                return;
            }
            gMapPanel3.Top += step;
        }

        private void gMapButton2_1_Click(object sender, EventArgs e)
        {
            panelUp2 = !panelUp2;
            routeTimer.Start();
        }

        private void routeTimer_Tick(object sender, EventArgs e)
        {
            int target = panelUp2 ? targetYDown2 : targetYUp2;
            int distance = target - gMapPanel4.Top;
            int step = (int)(distance * 0.1f);
            if (Math.Abs(distance) < 1)
            {
                gMapPanel4.Top = target;
                routeTimer.Stop();
                return;
            }
            gMapPanel4.Top += step;
        }

        private void gMapButton2_4_Click(object sender, EventArgs e)
        {
            /*changingBordersForm cb = new changingBordersForm();
            cb.Show();
            this.Close();*/

            Logger.ShowInfo("Данная функция недоступна на данный момент!");
        }

        private void gMapButton2_5_Click(object sender, EventArgs e)
        {
            /*changingLabelsForm cl = new changingLabelsForm();
            cl.Show();
            this.Close();*/

            Logger.ShowInfo("Данная функция недоступна на данный момент!");
        }

        private void assignTaskPanel_Tick(object sender, EventArgs e)
        {
            int step;
            int diff;
            if (tasks1)
            {
                diff = assignTaskPanel.Height - assignTaskPanel.MinimumSize.Height;
                step = Math.Max(2, diff / 5);
                assignTaskPanel.Height -= step;
                editTaskPanel.Top -= step;

                if (assignTaskPanel.Height <= assignTaskPanel.MinimumSize.Height)
                {
                    assignTaskPanel.Height = assignTaskPanel.MinimumSize.Height;
                    tasks1 = false;
                    assignTaskTimer.Stop();
                }
            }
            else
            {
                diff = assignTaskPanel.MaximumSize.Height - assignTaskPanel.Height;
                step = Math.Max(2, diff / 5);
                assignTaskPanel.Height += step;
                editTaskPanel.Top += step;

                if (assignTaskPanel.Height >= assignTaskPanel.MaximumSize.Height)
                {
                    assignTaskPanel.Height = assignTaskPanel.MaximumSize.Height;
                    tasks1 = true;
                    assignTaskTimer.Stop();
                }
            }
        }

        private void editTaskTimer_Tick(object sender, EventArgs e)
        {
            int step;
            int diff;
            if (tasks2)
            {
                diff = editTaskPanel.Height - editTaskPanel.MinimumSize.Height;
                step = Math.Max(2, diff / 5);
                editTaskPanel.Height -= step;

                if (editTaskPanel.Height <= editTaskPanel.MinimumSize.Height)
                {
                    editTaskPanel.Height = editTaskPanel.MinimumSize.Height;
                    tasks2 = false;
                    editTaskTimer.Stop();
                }
            }
            else
            {
                diff = editTaskPanel.MaximumSize.Height - editTaskPanel.Height;
                step = Math.Max(2, diff / 5);
                editTaskPanel.Height += step;

                if (editTaskPanel.Height >= editTaskPanel.MaximumSize.Height)
                {
                    editTaskPanel.Height = editTaskPanel.MaximumSize.Height;
                    tasks2 = true;
                    editTaskTimer.Stop();
                }
            }
        }

        private void assignTaskPanel1_Click(object sender, EventArgs e)
        {
            assignTaskTimer.Start();
        }

        private void editTaskPanel1_Click(object sender, EventArgs e)
        {
            editTaskTimer.Start();
        }

        private void registrationEmployeeTimer_Tick(object sender, EventArgs e)
        {
            int step;
            int diff;
            if (employee1)
            {
                diff = registrationEmployeePanel.Height - registrationEmployeePanel.MinimumSize.Height;
                step = Math.Max(2, diff / 5);
                registrationEmployeePanel.Height -= step;
                editEmployeePanel.Top -= step;
                if (registrationEmployeePanel.Height <= registrationEmployeePanel.MinimumSize.Height)
                {
                    registrationEmployeePanel.Height = registrationEmployeePanel.MinimumSize.Height;
                    employee1 = false;
                    registrationEmployeeTimer.Stop();
                }
            }
            else
            {
                diff = registrationEmployeePanel.MaximumSize.Height - registrationEmployeePanel.Height;
                step = Math.Max(2, diff / 5);
                registrationEmployeePanel.Height += step;
                editEmployeePanel.Top += step;
                if (registrationEmployeePanel.Height >= registrationEmployeePanel.MaximumSize.Height)
                {
                    registrationEmployeePanel.Height = registrationEmployeePanel.MaximumSize.Height;
                    employee1 = true;
                    registrationEmployeeTimer.Stop();
                }
            }
        }

        private void editEmployeeTimer_Tick(object sender, EventArgs e)
        {
            int step;
            int diff;
            if (employee2)
            {
                diff = editEmployeePanel.Height - editEmployeePanel.MinimumSize.Height;
                step = Math.Max(2, diff / 5);
                editEmployeePanel.Height -= step;
                if (editEmployeePanel.Height <= editEmployeePanel.MinimumSize.Height)
                {
                    editEmployeePanel.Height = editEmployeePanel.MinimumSize.Height;
                    employee2 = false;
                    editEmployeeTimer.Stop();
                }
            }
            else
            {
                diff = editEmployeePanel.MaximumSize.Height - editEmployeePanel.Height;
                step = Math.Max(2, diff / 5);
                editEmployeePanel.Height += step;
                if (editEmployeePanel.Height >= editEmployeePanel.MaximumSize.Height)
                {
                    editEmployeePanel.Height = editEmployeePanel.MaximumSize.Height;
                    employee2 = true;
                    editEmployeeTimer.Stop();
                }
            }
        }

        private void registrationEmployeePanel1_Click(object sender, EventArgs e)
        {
            registrationEmployeeTimer.Start();
        }

        private void editEmployeePanel1_Click(object sender, EventArgs e)
        {
            editEmployeeTimer.Start();
        }

        private void registrationEmployeeButton1_Click(object sender, EventArgs e)
        {
            Random p = new Random();
            int[] k = new int[8];
            for (int i = 0; i < k.Length; i++)
                k[i] = p.Next(1, 10);
            registrationEmployeeTextBox5.Content = string.Join("", k);
        }

        private void editEmployeeiButton1_Click(object sender, EventArgs e)
        {
            Random p = new Random();
            int[] k = new int[8];
            for (int i = 0; i < k.Length; i++)
                k[i] = p.Next(1, 10);
            editEmployeeTextBox5.Content = string.Join("", k);
        }

        private void gMapCheckBox3_1_Click(object sender, EventArgs e)
        {
            if (gMapCheckBox3_1.Checked == false)
            {
                gMapCheckBox3_2.Checked = true;
                gMapCheckBox3_3.Checked = true;
                gMapCheckBox3_4.Checked = true;
            }
        }

        public static string TransliterateRussian(string text1)
        {
            var text = RussianTransliterator.GetTransliteration(text1);
            Logger.Debug($"Совершен перевод текста с {text1} в {text}");
            return text;
        }

        private void registrationEmployeeButton1_Click_1(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(registrationEmployeeBox1.Content) && !string.IsNullOrWhiteSpace(registrationEmployeeTextBox2.Content))
            {
                string bukvi1 = registrationEmployeeBox1.Content.Substring(0, 3);
                string bukvi2 = registrationEmployeeTextBox2.Content.ToString();
                string bukvi = bukvi2 + bukvi1;
                string transliterated = TransliterateRussian(bukvi.ToString());
                registrationEmployeeTextBox4.Content = transliterated;
            }
            else
                Logger.ShowWarning("Заполните имя и фамилию для генерации логина");
        }

        private void editEmployeeButton1_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(editEmployeeTextBox1.Content) && !string.IsNullOrWhiteSpace(editEmployeeTextBox2.Content))
            {
                string bukvi1 = editEmployeeTextBox1.Content.Substring(0, 3);
                string bukvi2 = editEmployeeTextBox2.Content.ToString();
                string bukvi = bukvi2 + bukvi1;
                string transliterated = TransliterateRussian(bukvi.ToString());
                editEmployeeTextBox4.Content = transliterated;
            }
            else
                Logger.ShowWarning("Заполните имя и фамилию для генерации логина");
        }

        private async void gMapButton3_1_Click(object sender, EventArgs e)
        {
            gMapControl.MapProvider = Map.streetProvider;
            gMapButton3_1.NormalBackground = Color.White;
            gMapButton3_2.NormalBackground = Color.FromArgb(242, 243, 250);
            gMapButton3_1.HoverBackground = Color.White;
            gMapButton3_2.HoverBackground = Color.FromArgb(242, 243, 250);
        }

        private async void gMapButton3_2_Click(object sender, EventArgs e)
        {
            gMapControl.MapProvider = Map.sputnikProvider;
            gMapButton3_1.NormalBackground = Color.FromArgb(242, 243, 250);
            gMapButton3_2.NormalBackground = Color.White;
            gMapButton3_2.HoverBackground = Color.White;
            gMapButton3_1.HoverBackground = Color.FromArgb(242, 243, 250);
        }

        private async void directorForm_Load(object sender, EventArgs e)
        {
            Program.StartCustomizationRoleForm(upperLabel1, menuLabel5);

            await Map.InitializeMap(gMapControl);
            await Map.RefreshMap(gMapControl);
            gMapCheckBox3_1_CheckedChanged(null, null);

            InitializeTimer();
            DataTables.InitializeTasksTable(tasksDataGridView);
            DataTables.InitializeEmployeesTable(registrationDataGridView);
            SubscriptionEvents();
            await LoadDataAsync(TableType.Employees);
            await LoadDataAsync(TableType.Tasks);

            AllComboBoxUpdate();
            SetupTextBoxValidation();
        }

        private void AllComboBoxUpdate()
        {
            ComboBoxAddRoles(assignTaskComboBox1);
            ComboBoxAddRoles(editTaskComboBox1);
            ComboBoxAddRoles(cuiComboBox1);
            ComboBoxAddRoles(editEmployeeComboBox1);
            InitializeComboBoxTasks();
            InitializeComboBoxEmployees();
        }

        private void InitializeComboBoxTasks()
        {
            SearchTasksComboBox1.Items = new string[0];
            for (int i = 0; i < tasksDataGridView.ColumnCount; i++)
            {
                if (!tasksDataGridView.Columns[i].Visible)
                    continue;
                SearchTasksComboBox1.AddItem(tasksDataGridView.Columns[i].HeaderText);
            }

            SearchTasksComboBox1.AddItem("Показывать всё");
            SearchTasksComboBox1.SelectedIndex = SearchTasksComboBox1.Items.Length - 1;
        }

        private void InitializeComboBoxEmployees()
        {
            SearchEmployeeComboBox1.Items = new string[0];
            for (int i = 0; i < registrationDataGridView.ColumnCount; i++)
            {
                if (!registrationDataGridView.Columns[i].Visible)
                    continue;
                
                // Пропускаем столбец "Логин" так как он из связанной таблицы
                string headerText = registrationDataGridView.Columns[i].HeaderText;
                if (headerText == "Логин")
                    continue;
                
                SearchEmployeeComboBox1.AddItem(headerText);
            }

            SearchEmployeeComboBox1.AddItem("Показывать всё");
            SearchEmployeeComboBox1.SelectedIndex = SearchEmployeeComboBox1.Items.Length - 1;
        }

        private void SetupTextBoxValidation()
        {
            // Запрещаем пробелы в полях ввода
            editEmployeeTextBox1.KeyPress += TextBox_KeyPress;
            editEmployeeTextBox2.KeyPress += TextBox_KeyPress;
            editEmployeeTextBox3.KeyPress += TextBox_KeyPress;
            registrationEmployeeBox1.KeyPress += TextBox_KeyPress;
            registrationEmployeeTextBox2.KeyPress += TextBox_KeyPress;
            registrationEmployeeTextBox3.KeyPress += TextBox_KeyPress;
            registrationEmployeeTextBox4.KeyPress += TextBox_KeyPress;
            registrationEmployeeTextBox5.KeyPress += TextBox_KeyPress;
            editEmployeeTextBox4.KeyPress += TextBox_KeyPress;
            editEmployeeTextBox5.KeyPress += TextBox_KeyPress;
        }

        private void TextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Блокируем ввод пробелов
            if (e.KeyChar == ' ')
                e.Handled = true;
        }

        private void SubscriptionEvents()
        {
            tasksDataGridView.Scroll += (s, t) => ResetTimer();
            tasksDataGridView.MouseWheel += (s, t) => ResetTimer();
            _searchTasks = new SearchFilter<DataBase.Tasks>();
            _loaderTasks = new LazyLoader<DataBase.Tasks>(_searchTasks);

            registrationDataGridView.Scroll += (s, t) => ResetTimer();
            registrationDataGridView.MouseWheel += (s, t) => ResetTimer();
            _searchEmployees = new SearchFilter<DataBase.Employees>();
            _loaderEmployees = new LazyLoader<DataBase.Employees>(_searchEmployees);
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
            // Проверка, не грузим ли мы уже
            int firstVisible = tasksDataGridView.FirstDisplayedScrollingRowIndex;
            int secondVisible = registrationDataGridView.FirstDisplayedScrollingRowIndex;
            if (firstVisible < 0 && secondVisible < 0) return;

            // Если прокрутили вниз
            if (firstVisible + tasksDataGridView.DisplayedRowCount(false) >= tasksDataGridView.RowCount - 10)
                await LoadDataAsync(TableType.Tasks);
            else if (secondVisible + registrationDataGridView.DisplayedRowCount(false) >= registrationDataGridView.RowCount - 10)
                await LoadDataAsync(TableType.Employees);
        }

        private async Task LoadDataAsync(TableType tableType)
        {
            try
            {
                if (tableType == TableType.Tasks)
                {
                    var data = await _loaderTasks.LoadNextBatchAsync();

                    foreach (var item in data)
                    {
                        if (_locallyAddedTaskIds.Contains(item.Id))
                            continue;

                        await DataTables.AddTaskRow(tasksDataGridView, item);
                    }
                }
                else if (tableType == TableType.Employees)
                {
                    var data = await _loaderEmployees.LoadNextBatchAsync();

                    foreach (var item in data)
                    {
                        if (_locallyAddedEmployeesIds.Contains(item.Id))
                            continue;

                        await DataTables.AddEmployeeRow(registrationDataGridView, item);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка загрузки таблиц из базы данных", ex);
                Logger.ShowError("Ошибка загрузки таблиц из базы данных");
            }
        }

        private void ComboBoxAddRoles(cuiComboBox cuiComboBox)
        {
            cuiComboBox.Items = new string[0];
            for (int i = 0; i < UserData._allRoles.Count; i++)
                cuiComboBox.AddItem(UserData._allRoles[i]);
        }

        private void gMapButton1_1_Click(object sender, EventArgs e)
        {
            gMapControl.Zoom++;
        }

        private void gMapButton1_2_Click(object sender, EventArgs e)
        {
            gMapControl.Zoom--;
        }

        private void gMapButton1_3_Click(object sender, EventArgs e)
        {
            gMapControl.Position = Map.startPosition;
        }

        private async void gMapButton4_1_Click(object sender, EventArgs e)
        {
            if (SelectedMarkers._points.Count == 0)
                SelectedMarkers._points = Map._homesOverlay.Markers.ToList();

            if (Map._postOfficeOverlay.Markers.Count > 0 && SelectedMarkers._points.IndexOf(Map._postOfficeOverlay.Markers[0]) == -1)
                SelectedMarkers._points.Add(Map._postOfficeOverlay.Markers[0]);

            await Route.BuildRoute(gMapControl, SelectedMarkers._points);

            routeMarkers = Route.selectedMarkersCount;
            await SelectedMarkers.ClearAllSelection(gMapControl, gMapButton4_1, gMapButton4_2, routeMarkers);
        }

        private async void gMapButton4_2_Click(object sender, EventArgs e)
        {
            if (Map._routesOverlay.Routes.Count == 0)
                routeMarkers = 0;
            await SelectedMarkers.ClearAllSelection(gMapControl, gMapButton4_1, gMapButton4_2, routeMarkers);
        }

        private async void gMapButton4_3_Click(object sender, EventArgs e)
        {
            routeMarkers = 0;
            await SelectedMarkers.ClearAllSelection(gMapControl, gMapButton4_1, gMapButton4_2, routeMarkers);
            Map._routesOverlay.Clear();
            gMapButton4_1.Content = "----";
            gMapButton4_2.Content = "----";
        }

        private async void gMapControl_OnMarkerDoubleClick(GMapMarker item, MouseEventArgs e)
        {
            SelectedMarkers._selectionMode = true;
            if (!SelectedMarkers._points.Contains(item))
                await SelectedMarkers.SelectMarker(item, gMapButton4_1);
            else
                await SelectedMarkers.RemoveMarkerSelection(item, gMapButton4_1, routeMarkers);

            gMapControl.Refresh();
        }

        private async void gMapControl_OnMarkerClick(GMapMarker item, MouseEventArgs e)
        {
            if (SelectedMarkers._selectionMode)
            {
                if (!SelectedMarkers._points.Contains(item))
                    await SelectedMarkers.SelectMarker(item, gMapButton4_1);
                else
                    await SelectedMarkers.RemoveMarkerSelection(item, gMapButton4_1, routeMarkers);

                gMapControl.Refresh();
            }
        }
        private void gMapCheckBox3_1_CheckedChanged(object sender, EventArgs e)
        {
            // Автоматическая настройка
            if (gMapCheckBox3_1.Checked)
            {
                // Блокируем и включаем остальные чекбоксы
                gMapCheckBox3_2.Enabled = false;
                gMapCheckBox3_3.Enabled = false;
                gMapCheckBox3_4.Enabled = false;
                Map.ApplyAutoZoomSettings(gMapControl, gMapCheckBox3_1.Checked);
            }
            else
            {
                // Разблокируем остальные чекбоксы
                gMapCheckBox3_2.Enabled = true;
                gMapCheckBox3_3.Enabled = true;
                gMapCheckBox3_4.Enabled = true;
                gMapCheckBox3_2_CheckedChanged(null, null);
                gMapCheckBox3_3_CheckedChanged(null, null);
                gMapCheckBox3_4_CheckedChanged(null, null);
            }
        }

        private void gMapCheckBox3_2_CheckedChanged(object sender, EventArgs e)
        {
            // полигоны
            if (!gMapCheckBox3_1.Checked && Map._regionsOverlay != null)
            {
                Map._regionsOverlay.IsVisibile = gMapCheckBox3_2.Checked;
                gMapControl.Refresh();
            }
        }

        private void gMapCheckBox3_3_CheckedChanged(object sender, EventArgs e)
        {
            // границы полигонов
            if (!gMapCheckBox3_1.Checked && Map._boundsOverlay != null)
            {
                Map._boundsOverlay.IsVisibile = gMapCheckBox3_3.Checked;
                gMapControl.Refresh();
            }
        }

        private void gMapCheckBox3_4_CheckedChanged(object sender, EventArgs e)
        {
            // маркеры
            if (!gMapCheckBox3_1.Checked && Map._homesOverlay != null)
            {
                Map._homesOverlay.IsVisibile = gMapCheckBox3_4.Checked;
                gMapControl.Refresh();
            }
        }

        private async void registrationEmployeeButton2_Click(object sender, EventArgs e)
        {
            string fio = string.Empty;
            if (string.IsNullOrWhiteSpace(registrationEmployeeTextBox3.Content))
                fio = $"{registrationEmployeeBox1.Content} {registrationEmployeeTextBox2.Content}";
            else
                fio = $"{registrationEmployeeBox1.Content} {registrationEmployeeTextBox2.Content} {registrationEmployeeTextBox3.Content}";
            UserData.Register(registrationEmployeeTextBox4.Content, registrationEmployeeTextBox5.Content, fio, cuiComboBox1.SelectedItem, registrationDataGridView, _locallyAddedEmployeesIds);
        }

        private async void editEmployeeButton2_Click(object sender, EventArgs e)
        {
            try
            {
                // Проверяем, что строка выбрана
                if (UserData._selectedEmployee == null)
                {
                    Logger.ShowWarning("Выберите сотрудника для редактирования");
                    return;
                }

                // Получаем новые значения из полей
                string newF = editEmployeeTextBox1?.Content?.ToString() ?? "";
                string newI = editEmployeeTextBox2?.Content?.ToString() ?? "";
                string newO = editEmployeeTextBox3?.Content?.ToString() ?? "";
                string newRole = editEmployeeComboBox1?.SelectedItem?.ToString() ?? "";
                string newLogin = editEmployeeTextBox4?.Content?.ToString() ?? "";
                string newPassword = editEmployeeTextBox5?.Content?.ToString() ?? "";

                // Валидация
                if (string.IsNullOrWhiteSpace(newF) && string.IsNullOrWhiteSpace(newI))
                {
                    Logger.ShowWarning("Введите ФИО сотрудник");
                    return;
                }

                if (string.IsNullOrWhiteSpace(newRole))
                {
                    Logger.ShowWarning("Выберите роль сотрудника");
                    return;
                }

                if (string.IsNullOrWhiteSpace(newLogin))
                {
                    Logger.ShowWarning("Введите логин (email)");
                    return;
                }

                string fio = string.Empty;
                if (string.IsNullOrWhiteSpace(newO))
                    fio = $"{newF} {newI}";
                else
                    fio = $"{newF} {newI} {newO}";

                // Обновляем запись в таблице Employees
                var employeeUpdate = new DataBase.Employees
                {
                    Id = UserData._selectedEmployee.Id,
                    FIO = $"{newF} {newI} {newO}",
                    Role = newRole,
                    IdLogin = UserData._selectedEmployee.IdLogin,
                    CreatedAt = UserData._selectedEmployee.CreatedAt
                };

                // Обновляем запись в таблице Login
                var loginUpdate = new DataBase.Login();
                if (newPassword != "")
                {
                    loginUpdate = new DataBase.Login
                    {
                        Id = UserData._selectedLogin.Id,
                        Email = newLogin,
                        Password = PasswordHasher.HashPassword(newPassword)
                    };
                }
                else
                {
                    loginUpdate = new DataBase.Login
                    {
                        Id = UserData._selectedLogin.Id,
                        Email = newLogin,
                    };
                }

                // Обновляем в БД
                await DataBase._client.From<DataBase.Employees>().Update(employeeUpdate);
                await DataBase._client.From<DataBase.Login>().Update(loginUpdate);

                // Обновляем в таблице
                foreach (DataGridViewRow row in registrationDataGridView.Rows)
                {
                    if (row.Cells["Id"].Value != null && (Guid)row.Cells["Id"].Value == employeeUpdate.Id)
                    {
                        row.Cells["FIO"].Value = employeeUpdate.FIO;
                        row.Cells["Role"].Value = employeeUpdate.Role;
                        row.Cells["Login"].Value = loginUpdate.Email;
                        break;
                    }
                }

                Logger.Info("Данные сотрудника обновлены");
                Logger.ShowInfo("Данные сотрудника обновлены");
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка при обновлении данных сотрудника", ex);
                Logger.ShowError("Ошибка при обновлении данных сотрудника");
            }
        }

        private async void editEmployeeButton3_Click(object sender, EventArgs e)
        {
            try
            {
                // Проверяем, что строка выбрана
                if (UserData._selectedEmployee == null)
                {
                    Logger.ShowWarning("Выберите сотрудника для удаления");
                    return;
                }

                // Подтверждение удаления
                var result = Logger.ShowYesNo("Вы уверены, что хотите удалить выбранного сотрудника?\nЭто действие нельзя отменить");
                if (result != DialogResult.Yes)
                    return;

                var employeeId = UserData._selectedEmployee.Id;
                var loginId = UserData._selectedLogin.Id;

                // Удаляем запись из таблицы Employees
                await DataBase._client.From<DataBase.Employees>().Where(t => t.Id == employeeId).Delete();

                // Удаляем запись из таблицы Login
                await DataBase._client.From<DataBase.Login>().Where(l => l.Id == loginId).Delete();

                // Очищаем поля после удаления
                ClearEmployeesControls();

                // Сбрасываем сохраненные ID
                UserData._selectedEmployee = null;
                UserData._selectedLogin = null;

                // Обновляем в таблице
                foreach (DataGridViewRow row in registrationDataGridView.Rows)
                {
                    if (row.Cells["Id"].Value != null && (Guid)row.Cells["Id"].Value == employeeId)
                    {
                        registrationDataGridView.Rows.Remove(row);
                        break;
                    }
                }

                Logger.Info("Сотрудник успешно удален");
                Logger.ShowInfo("Сотрудник успешно удален");
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка при удалении сотрудника", ex);
                Logger.ShowError("Ошибка при удалении сотрудника");
            }
        }

        private void ClearEmployeesControls()
        {
            editEmployeeTextBox1.Content = string.Empty;
            editEmployeeTextBox2.Content = string.Empty;
            editEmployeeTextBox3.Content = string.Empty;
            editEmployeeTextBox4.Content = string.Empty;
            UserData._selectedEmployee = null;
            UserData._selectedLogin = null;
        }

        private async void registrationDataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex >= 0 && e.RowIndex < registrationDataGridView.Rows.Count - 1)
                {
                    ClearEmployeesControls();

                    DataGridViewRow row = registrationDataGridView.Rows[e.RowIndex];
                    var idEmployee = Guid.Parse(row.Cells["Id"].Value.ToString());
                    var idLogin = Guid.Parse(row.Cells["IdLogin"].Value.ToString());

                    var employee = await DataBase._client.From<DataBase.Employees>().Where(x => x.Id == idEmployee).Single();
                    var login = await DataBase._client.From<DataBase.Login>().Where(x => x.Id == idLogin).Single();
                    if (employee == null || login == null)
                        return;

                    // Заполняем поля данными из выбранной строки
                    // FIO
                    string text = string.Empty;
                    int logic = 1;
                    foreach (var item in employee.FIO)
                    {
                        if (item != ' ')
                            text += item.ToString();
                        else if (logic <= 2)
                        {
                            if (logic == 1)
                                editEmployeeTextBox1.Content = text.ToString();
                            else if (logic == 2)
                                editEmployeeTextBox2.Content = text.ToString();

                            text = string.Empty;
                            logic++;
                        }
                        else
                            break;
                    }
                    editEmployeeTextBox3.Content = text.ToString();

                    // Role (заполняем ComboBox)
                    editEmployeeComboBox1.SelectedItem = employee.Role;
                    // Если нужно установить SelectedItem
                    if (editEmployeeComboBox1.Items.Contains(employee.Role))
                        editEmployeeComboBox1.SelectedItem = employee.Role;
                    else
                        editEmployeeComboBox1.SelectedIndex = -1;

                    // Login
                    editEmployeeTextBox4.Content = login.Email;
                    UserData._originalLogin = login.Email;

                    // Сохраняем ID выбранной записи
                    UserData._selectedEmployee = employee;
                    UserData._selectedLogin = login;
                }
            }
            catch { }
        }

        private async void editTaskButton1_Click(object sender, EventArgs e)
        {
            if (TaskOnEmployee._selectedTask == null)
            {
                Logger.ShowWarning("Выберите задачу для редактирования");
                return;
            }

            try
            {
                // Получаем ID выбранного сотрудника по ФИО
                var selectedFIO = editTaskComboBox2.SelectedItem?.ToString();
                if (string.IsNullOrEmpty(selectedFIO))
                {
                    Logger.ShowWarning("Выберите сотрудника");
                    return;
                }

                var employee = await DataBase._client.From<DataBase.Employees>().Where(x => x.FIO == selectedFIO).Single();

                string attachedMarkers = string.Empty;
                if (checkBox2.Checked && SelectedMarkers._points.Count != 0)
                {
                    if (Map._postOfficeOverlay.Markers.Count > 0)
                        SelectedMarkers._points.Add(Map._postOfficeOverlay.Markers[0]);
                    for (int i = 0; i < SelectedMarkers._points.Count; i++)
                    {
                        attachedMarkers += SelectedMarkers._points[i].Tag.ToString();
                        attachedMarkers += ",";
                    }
                }
                else
                    attachedMarkers = TaskOnEmployee._selectedTask.Task.AttachedMarkers;

                // Обновляем задачу
                var updatedTask = new DataBase.Tasks
                {
                    Id = TaskOnEmployee._selectedTask.Task.Id,
                    IdEmployee = employee.Id,
                    Text = editTaskTextBox1.Text,
                    Status = editTaskComboBox1.SelectedItem?.ToString(),
                    DateIssue = editTaskCalendarDatePicker1.Content,
                    DateDelivery = editTaskCalendarDatePicker2.Content,
                    AttachedMarkers = attachedMarkers
                };

                // Обновляем в БД
                await DataBase._client.From<DataBase.Tasks>().Upsert(updatedTask);

                // Обновляем в DataGridView
                foreach (DataGridViewRow row in tasksDataGridView.Rows)
                {
                    if (row.Cells["Id"].Value != null && (Guid)row.Cells["Id"].Value == updatedTask.Id)
                    {
                        // Обновляем значения
                        row.Cells["FIO"].Value = employee.FIO;
                        row.Cells["TaskText"].Value = updatedTask.Text;
                        row.Cells["Status"].Value = updatedTask.Status;
                        row.Cells["DateIssue"].Value = updatedTask.DateIssue;
                        row.Cells["DateDelivery"].Value = updatedTask.DateDelivery;
                        break;
                    }
                }

                Logger.Info($"Задача для сотрудника {employee.FIO} успешно обновлена");
                Logger.ShowInfo("Задача успешно обновлена");
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка при обновлении задачи", ex);
                Logger.ShowError("Ошибка при обновлении задачи");
            }
        }

        private async void editTaskButton2_Click(object sender, EventArgs e)
        {
            if (TaskOnEmployee._selectedTask == null)
            {
                Logger.ShowWarning("Выберите задачу для удаления");
                return;
            }

            var result = Logger.ShowYesNo("Вы уверены, что хотите удалить задачу?\nЭто действие нельзя отменить");
            if (result == DialogResult.Yes)
            {
                try
                {
                    // Удаляем задачу из БД
                    await DataBase._client.From<DataBase.Tasks>().Where(x => x.Id == TaskOnEmployee._selectedTask.Task.Id).Delete();
                    // Удаляем строку из DataGridView
                    foreach (DataGridViewRow row in tasksDataGridView.Rows)
                    {
                        if (row.Cells["Id"].Value != null && (Guid)row.Cells["Id"].Value == TaskOnEmployee._selectedTask.Task.Id)
                        {
                            tasksDataGridView.Rows.Remove(row);
                            break;
                        }
                    }
                    // Очищаем элементы управления
                    ClearEditTaskControls();

                    Logger.Info($"Задача у сотрудника {TaskOnEmployee._selectedTask.Employee.FIO} успешно удалена");
                    Logger.ShowInfo("Задача успешно удалена");
                }
                catch (Exception ex)
                {
                    Logger.Error("Ошибка при удалении задачи", ex);
                    Logger.ShowError("Ошибка при удалении задачи");
                }
            }
        }

        private void ClearEditTaskControls()
        {
            editTaskTextBox1.Content = string.Empty;
            editTaskComboBox1.SelectedIndex = -1;
            editTaskComboBox2.Items = new string[0];
            editTaskCalendarDatePicker1.Content = DateTime.Now;
            editTaskCalendarDatePicker2.Content = DateTime.Now.AddDays(7);
            TaskOnEmployee._selectedTask = null;
        }

        private async void tasksDataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex >= 0 && e.RowIndex < tasksDataGridView.Rows.Count && e.RowIndex < tasksDataGridView.Rows.Count - 1)
                {
                    // Получаем строку
                    var row = tasksDataGridView.Rows[e.RowIndex];
                    Guid id = Guid.Parse(row.Cells["Id"].Value.ToString());

                    var task = await DataBase._client.From<DataBase.Tasks>().Where(x => x.Id == id).Single();
                    if (task == null)
                        return;

                    await TaskOnEmployee.UpdateSelectedTask(task);

                    // Загружаем данные в элементы управления
                    editTaskTextBox1.Content = TaskOnEmployee._selectedTask.Task.Text;
                    editTaskCalendarDatePicker1.Content = TaskOnEmployee._selectedTask.Task.DateIssue;
                    editTaskCalendarDatePicker2.Content = TaskOnEmployee._selectedTask.Task.DateDelivery;

                    LoadRoleByEmployeeId(task.IdEmployee);
                }
            }
            catch { }
        }

        private async void LoadRoleByEmployeeId(Guid employeeId)
        {
            try
            {
                // Получаем сотрудника из БД по IdEmployee
                var employee = await DataBase._client.From<DataBase.Employees>().Where(x => x.Id == employeeId).Single();
                // Устанавливаем выбранную роль в ComboBox1
                currentEmployee = employee;
                editTaskComboBox1.SelectedItem = employee.Role;
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка при загрузке роли", ex);
                Logger.ShowError("Ошибка при загрузке роли");
            }
        }

        private async void editTaskComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (editTaskComboBox1.SelectedItem == null)
                return;

            var selectedRole = editTaskComboBox1.SelectedItem.ToString();

            try
            {
                var allEmployees = await DataBase._client.From<DataBase.Employees>().Get();

                // Получаем сотрудников с выбранной ролью
                var employees = allEmployees.Models.Where(x => x.Role != null && x.Role.Trim().Equals(selectedRole, StringComparison.OrdinalIgnoreCase)).ToList();

                // Заполняем ComboBox2 ФИО сотрудников
                editTaskComboBox2.Items = new string[0];

                // 2. Сначала добавляем его ФИО, если найден
                if (currentEmployee != null)
                    editTaskComboBox2.AddItem(currentEmployee.FIO);

                // 3. Потом добавляем остальных, кроме уже добавленного
                foreach (var employee in employees)
                {
                    if (currentEmployee != null && employee.Id == currentEmployee.Id)
                        continue;

                    editTaskComboBox2.AddItem(employee.FIO);
                }

                currentEmployee = null;
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка при загрузке сотрудников", ex);
                Logger.ShowError("Ошибка при загрузке сотрудников");
            }
        }

        private async void assignTaskComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (assignTaskComboBox1.SelectedItem == null)
                return;

            var selectedRole = assignTaskComboBox1.SelectedItem.ToString();

            try
            {
                var allEmployees = await DataBase._client.From<DataBase.Employees>().Get();

                // Получаем сотрудников с выбранной ролью
                var employees = allEmployees.Models.Where(x => x.Role != null && x.Role.Trim().Equals(selectedRole, StringComparison.OrdinalIgnoreCase)).ToList();

                // Заполняем ComboBox2 ФИО сотрудников
                assignTaskComboBox2.Items = new string[0];

                foreach (var employee in employees)
                    assignTaskComboBox2.AddItem(employee.FIO);

                // Устанавливаем первый элемент по умолчанию
                assignTaskComboBox2.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка при загрузке сотрудников", ex);
                Logger.ShowError("Ошибка при загрузке сотрудников");
            }
        }

        private async void assignTaskButton1_Click(object sender, EventArgs e)
        {
            if (assignTaskTextBox1.Content == string.Empty)
            {
                Logger.ShowWarning("Введите текст задания");
                return;
            }

            try
            {
                // Получаем выбранного сотрудника по ФИО
                var selectedFIO = assignTaskComboBox2.SelectedItem.ToString().Trim();

                var allEmployees = await DataBase._client.From<DataBase.Employees>().Get();
                var employee = allEmployees.Models.FirstOrDefault(emp => emp.FIO.Trim().Equals(selectedFIO, StringComparison.OrdinalIgnoreCase));

                if (employee == null)
                {
                    Logger.ShowWarning($"Сотрудник с ФИО «{selectedFIO}» не найден");
                    return;
                }

                string attachedMarkers = string.Empty;
                if (checkBox1.Checked && SelectedMarkers._points.Count != 0)
                {
                    if (Map._postOfficeOverlay.Markers.Count > 0)
                        SelectedMarkers._points.Add(Map._postOfficeOverlay.Markers[0]);
                    for (int i = 0; i < SelectedMarkers._points.Count; i++)
                    {
                        attachedMarkers += SelectedMarkers._points[i].Tag.ToString();
                        attachedMarkers += ",";
                    }
                }

                // Создаем новую задачу
                var newTask = new DataBase.Tasks
                {
                    Id = Guid.NewGuid(),
                    IdEmployee = employee.Id,
                    Text = assignTaskTextBox1.Content,
                    Status = TaskOnEmployee._taskStatus[0],
                    DateIssue = DateTime.UtcNow,
                    DateDelivery = assignTaskCalendarDatePicker2.Content.ToUniversalTime(),
                    AttachedMarkers = attachedMarkers
                };

                // Сохраняем в БД
                await DataBase._client.From<DataBase.Tasks>().Insert(newTask);
                await DataTables.AddTaskRow(tasksDataGridView, newTask);
                _locallyAddedTaskIds.Add(newTask.Id);
                // Очищаем форму
                ClearAssignTaskForm();

                Logger.Info($"Задание для сотрудника {employee.FIO} успешно создано");
                Logger.ShowInfo("Задание успешно создано");
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка при создании задания", ex);
                Logger.ShowError("Ошибка при создании задания");
            }
        }

        private void ClearAssignTaskForm()
        {
            assignTaskComboBox2.Items = new string[0];
            assignTaskCalendarDatePicker2.Content = DateTime.Now.AddDays(7);
        }

        private void gMapControl_OnMapZoomChanged()
        {
            // Если включена автоматическая настройка, применяем правила
            if (gMapCheckBox3_1.Checked)
                Map.ApplyAutoZoomSettings(gMapControl, gMapCheckBox3_1.Checked);
        }

        private async void SearchTasksButton1_Click(object sender, EventArgs e)
        {
            string searchText = SearchTasksTextBox1.Content;
            string selectedHeader = SearchTasksComboBox1.SelectedItem?.ToString();

            // Находим реальное имя свойства в классе по заголовку колонки
            var col = tasksDataGridView.Columns.Cast<DataGridViewColumn>().FirstOrDefault(c => c.HeaderText == selectedHeader && c.Visible);
            string propertyName = col?.DataPropertyName;

            tasksDataGridView.Rows.Clear();
            _loaderTasks.Reset();
            _locallyAddedTaskIds.Clear();

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

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (SelectedMarkers._points.Count == 0)
            {
                checkBox1.Checked = false;
                Logger.ShowWarning("Прикрепите хотя бы одну метку");
            }
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (SelectedMarkers._points.Count == 0)
            {
                checkBox2.Checked = false;
                Logger.ShowWarning("Прикрепите хотя бы одну метку");
            }
        }

        private async void SearchEmployeeButton1_Click(object sender, EventArgs e)
        {
            string searchText = SearchEmployeeTextBox1.Content;
            string selectedHeader = SearchEmployeeComboBox1.SelectedItem?.ToString();

            registrationDataGridView.Rows.Clear();
            _loaderEmployees.Reset();
            _locallyAddedEmployeesIds.Clear();

            // Находим реальное имя свойства в классе по заголовку колонки
            var col = registrationDataGridView.Columns.Cast<DataGridViewColumn>().FirstOrDefault(c => c.HeaderText == selectedHeader && c.Visible);
            string propertyName = col?.DataPropertyName;

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

        private void cuiPanel1_Click(object sender, EventArgs e)
        {
            IntegrityCheckForm integrityCheckForm = new IntegrityCheckForm();
            integrityCheckForm.Show(this);
        }
    }
}
