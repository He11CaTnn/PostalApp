using GMap.NET.WindowsForms;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Интерфейс
{
    public partial class postmanForm : Form
    {
        [DllImport("user32.dll")] private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")] private static extern bool ReleaseCapture();

        private bool menuStorage;
        private bool gmapStorage;
        private bool panelUp1 = false;
        private int targetYUp1 = -265;
        private int targetYDown1 = 99;
        private bool panelUp2 = false;
        private int targetYUp2 = -210;
        private int targetYDown2 = 99;

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTCAPTION = 0x2;

        private Timer _scrollDebounceTimer;
        private SearchFilter<DataBase.Tasks> _searchTasks;
        private LazyLoader<DataBase.Tasks> _loaderTasks;
        private int routeMarkers = 0;
        private Timer _autoUpdateTasksTimer;

        public postmanForm()
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
            RoundHelper.Attach(gMapPanel4, 45);
            RoundHelper.Attach(tasksDataGridView, 37);
        }

        private void OpenPanel()
        {
            mapPanel.Visible = true;
            mapPanel.Location = new Point(77, 40);
            mapPanel.Size = new Size(1218, 686);
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
            Logger.Info("Выход из приложения с формы почтальона");
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
            mapPanel.Visible = true;
            tasksPanel.Visible = false;
        }

        private async void menuButton2_Click(object sender, EventArgs e)
        {
            mapPanel.Visible = false;
            tasksPanel.Visible = true;
        }

        private async void menuButton3_Click(object sender, EventArgs e)
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

        private async void tasksButton3_1_Click(object sender, EventArgs e)
        {
            TaskOnEmployee.ClickTasksButton(tasksButton3_1, tasksDataGridView, tasksTextBox2_1, tasksLabel3_3, tasksDatePicker3_1, tasksDatePicker3_2);
        }

        private async void postmanForm_Load(object sender, EventArgs e)
        {
            Program.StartCustomizationRoleForm(upperLabel1, menuLabel4);

            await Map.InitializeMap(gMapControl);
            await Map.RefreshMap(gMapControl);
            gMapCheckBox3_1_CheckedChanged(null, null);

            DataTables.InitializeTasksTable(tasksDataGridView);
            InitializeTimer();
            SubscriptionEvents();
            await LoadDataAsync();
            TaskOnEmployee.InitializeTaskComboBox(SearchTasksComboBox1, tasksDataGridView);
            _autoUpdateTasksTimer = TaskOnEmployee.UpdateTasksTimer(menuPictureBox3, cuiPictureBox2);
        }

        private void SubscriptionEvents()
        {
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
            if (firstVisible < 0)
                return;

            // Если прокрутили вниз
            if (firstVisible + tasksDataGridView.DisplayedRowCount(false) >= tasksDataGridView.RowCount - 10)
                await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
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
            catch (Exception ex)
            {
                Logger.Error("Ошибка загрузки таблиц из базы данных", ex);
                Logger.ShowError("Ошибка загрузки таблиц из базы данных");
            }
        }

        private void gmapButtonPlus_Click(object sender, EventArgs e)
        {
            gMapControl.Zoom++;
        }

        private void gmapButtonMinus_Click(object sender, EventArgs e)
        {
            gMapControl.Zoom--;
        }

        private void gmapButtonThisIs_Click(object sender, EventArgs e)
        {
            gMapControl.Position = Map.startPosition;
        }

        private async void TasksDataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
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

                    if (task.AttachedMarkers != string.Empty && task.AttachedMarkers != null)
                        button1.Enabled = true;
                    else
                        button1.Enabled = false;
                }
            }
            catch { }
        }

        private async void routeConstructionButton1_Click(object sender, EventArgs e)
        {
            if (SelectedMarkers._points.Count == 0)
                SelectedMarkers._points = Map._homesOverlay.Markers.ToList();

            if (Map._postOfficeOverlay.Markers.Count > 0 && SelectedMarkers._points.IndexOf(Map._postOfficeOverlay.Markers[0]) == -1)
                SelectedMarkers._points.Add(Map._postOfficeOverlay.Markers[0]);

            await Route.BuildRoute(gMapControl, SelectedMarkers._points);

            routeMarkers = Route.selectedMarkersCount;
            await SelectedMarkers.ClearAllSelection(gMapControl, gMapButton4_1, gMapButton4_2, routeMarkers);
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

        private void gMapButton3_1_Click(object sender, EventArgs e)
        {
            gMapControl.MapProvider = Map.streetProvider;
            gMapButton3_1.NormalBackground = Color.White;
            gMapButton3_2.NormalBackground = Color.FromArgb(242, 243, 250);
            gMapButton3_1.HoverBackground = Color.White;
            gMapButton3_2.HoverBackground = Color.FromArgb(242, 243, 250);
        }

        private void gMapButton3_2_Click(object sender, EventArgs e)
        {
            gMapControl.MapProvider = Map.sputnikProvider;
            gMapButton3_1.NormalBackground = Color.FromArgb(242, 243, 250);
            gMapButton3_2.NormalBackground = Color.White;
            gMapButton3_2.HoverBackground = Color.White;
            gMapButton3_1.HoverBackground = Color.FromArgb(242, 243, 250);
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
            string propertyName = col.DataPropertyName;

            tasksDataGridView.Rows.Clear();

            // Применяем фильтр к нужному движку поиска
            _loaderTasks.Reset();

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
            await LoadDataAsync();
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            if (TaskOnEmployee._selectedTask.Task.Id != null)
            {
                gMapButton4_2_Click(null, null);
                gMapButton4_3_Click(null, null);

                string attachedMarkers = TaskOnEmployee._selectedTask.Task.AttachedMarkers;
                string text = string.Empty;
                for (int i = 0; i < attachedMarkers.Length; i++)
                {
                    if (attachedMarkers[i] == ',')
                    {
                        Guid id = Guid.Parse(text);
                        var response = await DataBase._client.From<DataBase.Markers>().Where(x => x.Id == id).Single();

                        bool logic = false;
                        for (int j = 0; j < Map._homesOverlay.Markers.Count - 1; j++)
                        {
                            if (Map._homesOverlay.Markers[j].Tag.ToString() == text)
                            {
                                SelectedMarkers._points.Add(Map._homesOverlay.Markers[j]);
                                logic = true;
                                break;
                            }
                        }

                        if (!logic)
                        {
                            await Marker.AddMarkerToMap(response);
                            for (int j = 0; j < Map._homesOverlay.Markers.Count - 1; j++)
                            {
                                if (Map._homesOverlay.Markers[j].Tag.ToString() == text)
                                {
                                    SelectedMarkers._points.Add(Map._homesOverlay.Markers[j]);
                                    break;
                                }
                            }
                        }
                        text = string.Empty;
                    }
                    else
                        text += attachedMarkers[i];
                }

                SelectedMarkers._points.Add(Map._postOfficeOverlay.Markers[0]);
                routeConstructionButton1_Click(null, null);
                Logger.Info($"Был построен маршрут по {routeMarkers} меткам");
                Logger.ShowInfo($"Был построен маршрут по {routeMarkers} меткам");
            }
        }

        private void cuiPanel1_Click(object sender, EventArgs e)
        {
            IntegrityCheckForm integrityCheckForm = new IntegrityCheckForm();
            integrityCheckForm.Show(this);
        }
    }
}
