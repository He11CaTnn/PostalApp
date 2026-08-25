using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Интерфейс
{
    public partial class managerForm : Form
    {
        [DllImport("user32.dll")] private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")] private static extern bool ReleaseCapture();

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTCAPTION = 0x2;
        private bool menuStorage;

        private string currentExcelFilePath = null;
        private List<Dictionary<string, object>> validatedData = new List<Dictionary<string, object>>();
        private CancellationTokenSource validationCancellation = null;
        private ExcelPackage excelPackage = null;

        private System.Windows.Forms.Timer _scrollDebounceTimer;
        private TableType _currentTable = TableType.Subscriptions;
        private DataGridView _currentActiveTable;
        private System.Windows.Forms.Timer _autoUpdateTasksTimer;

        private SearchFilter<DataBase.Editions> _searchEditions;
        private LazyLoader<DataBase.Editions> _loaderEditions;
        private SearchFilter<DataBase.Subscriptions> _searchSubs;
        private LazyLoader<DataBase.Subscriptions> _loaderSubs;
        private SearchFilter<DataBase.Tasks> _searchTasks;
        private LazyLoader<DataBase.Tasks> _loaderTasks;
        private DataBase.Editions _selectedEdition;
        private DataBase.Subscriptions _selectedSubscription;

        public enum TableType
        {
            Editions,
            Subscriptions,
            Tasks
        }

        public managerForm()
        {
            InitializeComponent();
            OpenPanel();
            applyRadius();
            subscriptionsEditionsDataGridView1_1.Dock = DockStyle.Fill;
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
            RoundHelper.Attach(subscriptionsEditionsDataGridView1_1, 37);
            RoundHelper.Attach(subscriptionsEditionsDataGridView1_2, 37);
        }

        private void OpenPanel()
        {
            subscriptionsEditionsPanel.Visible = true;
            subscriptionsEditionsPanel.Location = new Point(77, 40);
            subscriptionsEditionsPanel.Size = new Size(1218, 686);
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
            Logger.Info("Выход из приложения с формы руководителя подписок");
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
            subscriptionsEditionsPanel.Visible = true;
        }

        private void menuButton2_Click(object sender, EventArgs e)
        {
            tasksPanel.Visible = true;
            subscriptionsEditionsPanel.Visible = false;
        }

        private async void menuButton3_Click(object sender, EventArgs e)
        {
            await UserData.LogoutAndExit(this);
        }

        private async void subscriptionsButton1_1_Click(object sender, EventArgs e)
        {
            subscriptionsEditionsDataGridView1_1.Dock = DockStyle.Fill;
            subscriptionsEditionsDataGridView1_2.Dock = DockStyle.None;
            subscriptionsEditionsDataGridView1_1.Visible = true;
            subscriptionsEditionsDataGridView1_2.Visible = false;

            if (SearchSubscriptionsEditionsTextBox1.Content != string.Empty)
                SearchSubscriptionsEditionsTextBox1.Content = string.Empty;
            ComboBoxUpdate(subscriptionsEditionsDataGridView1_1);
            _currentActiveTable = subscriptionsEditionsDataGridView1_1;
            _currentTable = TableType.Subscriptions;
            await LoadDataAsync(_currentTable);
        }

        private async void subscriptionsButton1_2_Click(object sender, EventArgs e)
        {
            subscriptionsEditionsDataGridView1_1.Dock = DockStyle.None;
            subscriptionsEditionsDataGridView1_2.Dock = DockStyle.Fill;
            subscriptionsEditionsDataGridView1_1.Visible = false;
            subscriptionsEditionsDataGridView1_2.Visible = true;

            if (SearchSubscriptionsEditionsTextBox1.Content != string.Empty)
                SearchSubscriptionsEditionsTextBox1.Content = string.Empty;
            ComboBoxUpdate(subscriptionsEditionsDataGridView1_2);
            _currentActiveTable = subscriptionsEditionsDataGridView1_2;
            _currentTable = TableType.Editions;
            await LoadDataAsync(_currentTable);
        }

        private void subscriptionsEditionsPanel3_1_1_MouseEnter(object sender, EventArgs e)
        {
            subscriptionsEditionsLabel3_1.ForeColor = Color.FromArgb(26, 52, 232);
        }

        private void subscriptionsEditionsPanel3_1_1_MouseLeave(object sender, EventArgs e)
        {
            subscriptionsEditionsLabel3_1.ForeColor = Color.FromArgb(49, 50, 60);
        }

        private async void managerForm_Load(object sender, EventArgs e)
        {
            Program.StartCustomizationRoleForm(upperLabel1, menuLabel4);

            InitializeTimer();
            DataTables.InitializeSubscriptionsTable(subscriptionsEditionsDataGridView1_1);
            DataTables.InitializeEditionsTable(subscriptionsEditionsDataGridView1_2);
            DataTables.InitializeTasksTable(tasksDataGridView);
            TaskOnEmployee.InitializeTaskComboBox(cuiComboBox1, tasksDataGridView);
            SubscriptionEvents();
            await LoadDataAsync(_currentTable);
            await LoadDataAsync(TableType.Tasks);
            _autoUpdateTasksTimer = TaskOnEmployee.UpdateTasksTimer(menuPictureBox3, cuiPictureBox2);
        }

        private void SubscriptionEvents()
        {
            subscriptionsEditionsDataGridView1_1.Scroll += (s, t) => ResetTimer();
            subscriptionsEditionsDataGridView1_1.MouseWheel += (s, t) => ResetTimer();
            subscriptionsEditionsDataGridView1_2.Scroll += (s, t) => ResetTimer();
            subscriptionsEditionsDataGridView1_2.MouseWheel += (s, t) => ResetTimer();

            _searchEditions = new SearchFilter<DataBase.Editions>();
            _loaderEditions = new LazyLoader<DataBase.Editions>(_searchEditions);
            _searchSubs = new SearchFilter<DataBase.Subscriptions>();
            _loaderSubs = new LazyLoader<DataBase.Subscriptions>(_searchSubs);

            tasksDataGridView.Scroll += (s, t) => ResetTimer();
            tasksDataGridView.MouseWheel += (s, t) => ResetTimer();
            _searchTasks = new SearchFilter<DataBase.Tasks>();
            _loaderTasks = new LazyLoader<DataBase.Tasks>(_searchTasks);
        }

        private void ResetTimer()
        {
            _scrollDebounceTimer.Stop();
            _scrollDebounceTimer.Start();
        }

        private void InitializeTimer()
        {
            _scrollDebounceTimer = new System.Windows.Forms.Timer { Interval = 100 };
            _scrollDebounceTimer.Tick += async (s, e) =>
            {
                _scrollDebounceTimer.Stop();
                await CheckScrollAndLoad();
            };
        }

        private void ComboBoxUpdate(DataGridView dataGridView)
        {
            SearchSubscriptionsEditionsComboBox1.Items = new string[0];
            foreach (DataGridViewColumn item in dataGridView.Columns)
            {
                if (item.Visible)
                    SearchSubscriptionsEditionsComboBox1.AddItem(item.HeaderText);
            }
            SearchSubscriptionsEditionsComboBox1.AddItem("Показывать всё");
            SearchSubscriptionsEditionsComboBox1.SelectedItem = "Показывать всё";
        }

        private async Task CheckScrollAndLoad()
        {
            int firstVisible = _currentActiveTable.FirstDisplayedScrollingRowIndex;
            int firstTaskVisible = tasksDataGridView.FirstDisplayedScrollingRowIndex;
            if (firstVisible < 0 && firstTaskVisible < 0) return;

            // Если прокрутили вниз
            if (firstVisible + _currentActiveTable.DisplayedRowCount(false) >= _currentActiveTable.RowCount - 10)
                await LoadDataAsync(_currentTable);
            if (firstTaskVisible + tasksDataGridView.DisplayedRowCount(false) >= tasksDataGridView.RowCount - 10)
                await LoadDataAsync(TableType.Tasks);
        }

        private async Task LoadDataAsync(TableType tableType)
        {
            try
            {
                if (tableType == TableType.Editions)
                {
                    var data = await _loaderEditions.LoadNextBatchAsync();

                    foreach (var item in data)
                        DataTables.AddEditionRow(subscriptionsEditionsDataGridView1_2, item);
                }
                else if (tableType == TableType.Subscriptions)
                {
                    var data = await _loaderSubs.LoadNextBatchAsync();

                    foreach (var item in data)
                        DataTables.AddSubscriptionRow(subscriptionsEditionsDataGridView1_1, item);
                }
                else if (tableType == TableType.Tasks)
                {
                    var data = await _loaderTasks.LoadNextBatchAsync();

                    foreach (var item in data)
                    {
                        if (item.IdEmployee == UserData.CurrentUser.Employee.Id)
                            await DataTables.AddTaskRow(tasksDataGridView, item);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка загрузки таблиц из базы данных", ex);
                Logger.ShowError("Ошибка загрузки таблиц из базы данных");
            }
        }

        private async void subscriptionsEditionsDataGridView1_1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex >= 0 && e.RowIndex < subscriptionsEditionsDataGridView1_1.Rows.Count - 1)
                {
                    // Получаем строку
                    var row = subscriptionsEditionsDataGridView1_1.Rows[e.RowIndex];

                    Guid id = Guid.Parse(row.Cells["Id"].Value.ToString());
                    var subscription = await DataBase._client.From<DataBase.Subscriptions>().Where(x => x.Id == id).Single();
                    _selectedSubscription = subscription;
                }
            }
            catch { }
        }

        private async void subscriptionsEditionsDataGridView1_2_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex >= 0 && e.RowIndex < subscriptionsEditionsDataGridView1_2.Rows.Count - 1)
                {
                    // Получаем строку
                    var row = subscriptionsEditionsDataGridView1_2.Rows[e.RowIndex];

                    Guid id = Guid.Parse(row.Cells["Id"].Value.ToString());
                    var edition = await DataBase._client.From<DataBase.Editions>().Where(x => x.Id == id).Single();
                    _selectedEdition = edition;
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

        private async void tasksButton3_1_Click(object sender, EventArgs e)
        {
            TaskOnEmployee.ClickTasksButton(tasksButton3_1, tasksDataGridView, tasksTextBox2_1, tasksLabel3_3, tasksDatePicker3_1, tasksDatePicker3_2);
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
                    Logger.Info($"Применен фильтр {propertyName} к таблице Задачи");
                }

                await LoadDataAsync(TableType.Tasks);
            }
            catch { }
        }

        private async void SearchSubscriptionsEditionsButton1_Click(object sender, EventArgs e)
        {
            try
            {
                string searchText = SearchSubscriptionsEditionsTextBox1.Content;
                string selectedHeader = SearchSubscriptionsEditionsComboBox1.SelectedItem?.ToString();

                // Находим реальное имя свойства в классе по заголовку колонки
                var col = _currentActiveTable.Columns.Cast<DataGridViewColumn>().FirstOrDefault(c => c.HeaderText == selectedHeader && c.Visible);
                string propertyName = col?.DataPropertyName;

                _currentActiveTable.Rows.Clear();

                // Применяем фильтр к нужному движку поиска
                if (_currentTable == TableType.Editions)
                {
                    _loaderEditions.Reset();

                    if (string.IsNullOrEmpty(propertyName) || selectedHeader == "Показывать всё")
                    {
                        _searchEditions.Clear();
                        Logger.Info($"Отменен фильтр к таблице Издания");
                    }
                    else
                    {
                        _searchEditions.SetFilter(propertyName, searchText);
                        Logger.Info($"Применен фильтр {propertyName} к таблице Издания");
                    }
                }
                else if (_currentTable == TableType.Subscriptions)
                {
                    _loaderSubs.Reset();

                    if (string.IsNullOrEmpty(propertyName) || selectedHeader == "Показывать всё")
                    {
                        _searchSubs.Clear();
                        Logger.Info($"Отменен фильтр к таблице Подписки");
                    }
                    else
                    {
                        _searchSubs.SetFilter(propertyName, searchText);
                        Logger.Info($"Применен фильтр {propertyName} к таблице Подписки");
                    }
                }

                // Загружаем заново с учетом фильтра
                await LoadDataAsync(_currentTable);
            }
            catch { }
        }

        private async void cuiButton2_Click(object sender, EventArgs e)
        {
            if (_currentTable == TableType.Editions)
            {
                var result = Logger.ShowYesNo("Вы уверены, что хотите удалить все значения с таблицы «Издания»?");
                if (result == DialogResult.Yes)
                {
                    try
                    {
                        await DataBase._client.From<DataBase.Editions>().Where(x => x.Id != null).Delete();
                        subscriptionsEditionsDataGridView1_2.Rows.Clear();
                        _selectedEdition = null;

                        Logger.Info("Таблица «Издания» успешно очищена");
                        Logger.ShowInfo("Таблица «Издания» успешно очищена");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Ошибка при очистки таблицы «Издания»", ex);
                        Logger.ShowError("Ошибка при очистки таблицы «Издания»");
                    }
                }
            }
            else
                Logger.ShowWarning("Можно очистить только таблицу «Издания»");
        }

        private async void gMapButton4_4_Click(object sender, EventArgs e)
        {
            if (_currentTable == TableType.Editions)
            {
                if (_selectedEdition != null)
                {
                    try
                    {
                        await DataBase._client.From<DataBase.Editions>().Where(x => x.Id == _selectedEdition.Id).Delete();

                        // Удаляем строку из DataGridView
                        foreach (DataGridViewRow row in subscriptionsEditionsDataGridView1_1.Rows)
                        {
                            if (row.Cells["Id"].Value != null && (Guid)row.Cells["Id"].Value == _selectedEdition.Id)
                            {
                                subscriptionsEditionsDataGridView1_1.Rows.Remove(row);
                                break;
                            }
                        }

                        _selectedEdition = null;
                        Logger.Info("Строка из таблицы «Издания» успешно удалена");
                        Logger.ShowInfo("Строка из таблицы «Издания» успешно удалена");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Ошибка при удалении строка из таблицы «Издания» успешно удалена", ex);
                        Logger.ShowError("Ошибка при удалении строка из таблицы «Издания» успешно удалена");
                    }
                }
                else
                    Logger.ShowWarning("Выберите строку в таблице для удаления");
            }
            else if (_currentTable == TableType.Subscriptions)
            {
                if (_selectedSubscription != null)
                {
                    try
                    {
                        await DataBase._client.From<DataBase.Subscriptions>().Where(x => x.Id == _selectedSubscription.Id).Delete();

                        // Удаляем строку из DataGridView
                        foreach (DataGridViewRow row in subscriptionsEditionsDataGridView1_2.Rows)
                        {
                            if (row.Cells["Id"].Value != null && (Guid)row.Cells["Id"].Value == _selectedSubscription.Id)
                            {
                                subscriptionsEditionsDataGridView1_2.Rows.Remove(row);
                                break;
                            }
                        }

                        _selectedSubscription = null;
                        Logger.Info("Строка из таблицы «Подписки» успешно удалена");
                        Logger.ShowInfo("Строка из таблицы «Подписки» успешно удалена");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Ошибка при удалении строка из таблицы «Подписки» успешно удалена", ex);
                        Logger.ShowError("Ошибка при удалении строка из таблицы «Подписки» успешно удалена");
                    }
                }
                else
                    Logger.ShowWarning("Выберите строку в таблице для удаления"); ;
            }
        }

        private void cuiFileDropper1_FileDropped(object sender, CuoreUI.Controls.FileDroppedEventArgs e)
        {
            string filePath = e.FileName; // Берем первый сброшенный файл

            // Проверка расширения файла (.xlsx или .xls)
            string extension = Path.GetExtension(filePath).ToLower();
            if (extension != ".xlsx" && extension != ".xls")
            {
                Logger.ShowWarning("Нужен файл формата Excel (.xlsx или .xls)");
                return;
            }

            // Сохраняем путь к файлу
            currentExcelFilePath = filePath;

            // Устанавливаем название файла
            subscriptionsEditionsLabel3_1.Content = Path.GetFileNameWithoutExtension(filePath);

            // Вычисляем и устанавливаем размер файла
            FileInfo fileInfo = new FileInfo(filePath);
            long fileSizeBytes = fileInfo.Length;
            string fileSizeText = GetFileSizeString(fileSizeBytes);
            subscriptionsEditionsLabe3_2.Content = fileSizeText;
        }

        private void subscriptionsEditionsPictureBox3_2_Click(object sender, EventArgs e)
        {
            // Очищаем путь к файлу
            currentExcelFilePath = null;

            // Очищаем лейблы
            subscriptionsEditionsLabel3_1.Content = "Нет загруженного файла";
            subscriptionsEditionsLabe3_2.Content = "0кб";
        }

        private string GetFileSizeString(long bytes)
        {
            string[] sizes = { "б", "кб", "мб", "гб" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return String.Format("{0:0.##} {1}", len, sizes[order]);
        }

        private async void subscriptionsEditionsButton3_1_Click(object sender, EventArgs e)
        {
            ExcelPackage.License.SetNonCommercialPersonal("Ivan");

            if (string.IsNullOrEmpty(currentExcelFilePath))
            {
                Logger.ShowWarning("Файл Excel не выбран");
                return;
            }

            string startRowInput = Microsoft.VisualBasic.Interaction.InputBox(
                "Введите номер строки, с которой начинаются данные:",
                "Начало данных", "2");

            if (!int.TryParse(startRowInput, out int startRow) || startRow < 2)
            {
                MessageBox.Show("Неверный номер строки!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            validationCancellation = new CancellationTokenSource();
            validatedData.Clear();

            var validationForm = new ValidationForm();
            validationForm.Show();

            try
            {
                await ValidateAndParseExcel(startRow, validationForm);

                if (validationCancellation.Token.IsCancellationRequested)
                {
                    RollbackValidatedData();
                    return;
                }

                await SaveToDatabase(validationForm);

                currentExcelFilePath = null;
                subscriptionsEditionsLabel3_1.Content = "Нет загруженного файла";
                subscriptionsEditionsLabe3_2.Content = "0кб";

                Logger.Info($"Успешно загружено {validatedData.Count} записей в таблицу «Издания»");
                Logger.ShowInfo($"Успешно загружено {validatedData.Count} записей");
            }
            catch (OperationCanceledException)
            {
                RollbackValidatedData();
                Logger.Info("Загрузка записей в таблицу «Издания» отменена");
                Logger.ShowInfo("Загрузка отменена");
            }
            catch (Exception ex)
            {
                RollbackValidatedData();
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Logger.Error("Ошибка при загрузке данных в таблицу «Издания»", ex);
                Logger.ShowError("Ошибка загрузки");
            }
            finally
            {
                validationForm.Close();
                validationCancellation?.Dispose();
                excelPackage?.Dispose();
                subscriptionsEditionsDataGridView1_2.Rows.Clear();
                await LoadDataAsync(TableType.Editions);
            }
        }

        private async Task ValidateAndParseExcel(int startRow, ValidationForm validationForm)
        {
            excelPackage = new ExcelPackage(new FileInfo(currentExcelFilePath));
            var worksheet = excelPackage.Workbook.Worksheets[0];

            validationForm.UpdateStatus("Чтение файла...");

            int rowCount = worksheet.Dimension.End.Row;

            for (int row = startRow; row <= rowCount; row++)
            {
                if (validationCancellation.Token.IsCancellationRequested)
                    return;

                validationForm.UpdateStatus($"Проверка строки {row}...");
                validationForm.SetProgress(row, rowCount);
                await Task.Delay(1);

                var rowData = new Dictionary<string, object>();

                try
                {
                    rowData["Index"] = worksheet.Cells[row, 1].Text?.Trim() ?? "";
                    rowData["Name"] = worksheet.Cells[row, 2].Text?.Trim() ?? "";
                    rowData["TypeEdition"] = worksheet.Cells[row, 3].Text?.Trim() ?? "";

                    ParseFloatValue(rowData, "MinTermSubscription", worksheet.Cells[row, 4], row);
                    ParseFloatValue(rowData, "MinTermHousePrice", worksheet.Cells[row, 5], row);
                    ParseFloatValue(rowData, "MinTermPricePerMailbox", worksheet.Cells[row, 6], row);
                    ParseFloatValue(rowData, "MaxTermSubscription", worksheet.Cells[row, 7], row);
                    ParseFloatValue(rowData, "MaxTermHousePrice", worksheet.Cells[row, 8], row);
                    ParseFloatValue(rowData, "MaxTermPricePerMailbox", worksheet.Cells[row, 9], row);

                    validatedData.Add(rowData);
                }
                catch (ValidationException vex)
                {
                    var result = validationForm.ShowError(vex.Column, vex.Row, vex.Value, vex.Message);

                    if (result == DialogResult.Cancel)
                    {
                        validationCancellation.Cancel();
                        return;
                    }
                    else if (result == DialogResult.OK)
                    {
                        rowData[vex.Column] = validationForm.GetCorrectedValue();
                        validatedData.Add(rowData);
                    }
                    row--;
                }
            }
        }

        private void ParseFloatValue(Dictionary<string, object> rowData, string columnName, ExcelRange cell, int row)
        {
            string cellText = cell.Text?.Trim();
            if (string.IsNullOrEmpty(cellText))
                throw new ValidationException(columnName, row, "", "Пустое значение");

            string normalized = cellText.Replace(',', '.');
            if (!float.TryParse(normalized, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float value))
            {
                throw new ValidationException(columnName, row, cellText, "Неверный формат числа");
            }

            rowData[columnName] = value;
        }

        private async Task SaveToDatabase(ValidationForm validationForm)
        {
            int total = validatedData.Count;
            int current = 0;

            // Переключаем ValidationForm в режим Фазы 2 (прогресс-бар записи в БД)
            validationForm.StartPhase2(total);

            foreach (var data in validatedData)
            {
                var edition = new DataBase.Editions
                {
                    Id = Guid.NewGuid(),
                    Index = data["Index"]?.ToString() ?? "",
                    Name = data["Name"]?.ToString() ?? "",
                    TypeEdition = data["TypeEdition"]?.ToString() ?? "",
                    MinTermSubscription = (float)data["MinTermSubscription"],
                    MinTermHousePrice = (float)data["MinTermHousePrice"],
                    MinTermPricePerMailbox = (float)data["MinTermPricePerMailbox"],
                    MaxTermSubscription = (float)data["MaxTermSubscription"],
                    MaxTermHousePrice = (float)data["MaxTermHousePrice"],
                    MaxTermPricePerMailbox = (float)data["MaxTermPricePerMailbox"]
                };

                await DataBase._client.From<DataBase.Editions>().Upsert(edition);

                current++;
                validationForm.SetPhase2Progress(current, total);

                // Небольшая уступка UI-потоку каждые 10 записей,
                // чтобы прогресс-бар обновлялся плавно
                if (current % 10 == 0)
                    await Task.Delay(1);
            }
        }

        private void RollbackValidatedData()
        {
            validatedData.Clear();
        }

        private void cuiPanel2_Click(object sender, EventArgs e)
        {
            IntegrityCheckForm integrityCheckForm = new IntegrityCheckForm();
            integrityCheckForm.Show(this);
        }
    }

    public class ValidationException : Exception
    {
        public string Column { get; }
        public int Row { get; }
        public string Value { get; }

        public ValidationException(string column, int row, string value, string message) : base(message)
        {
            Column = column;
            Row = row;
            Value = value;
        }
    }
}
