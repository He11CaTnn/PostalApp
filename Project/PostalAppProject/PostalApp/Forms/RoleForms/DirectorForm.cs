using CuoreUI.Controls;
using GMap.NET;
using GMap.NET.MapProviders;
using RussianTransliteration;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PostalApp
{
    public partial class DirectorForm : Form
    {
        // Верхняя панель
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HTCAPTION = 0x2;
        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        [DllImport("gdi32.dll")]
        // Скругление элементов
        private static extern IntPtr CreateRoundRectRgn(int x1, int y1, int x2, int y2, int cx, int cy);
        // Панель настроек карты, во вкладке [Карта]
        bool mapStyle = false;
        int mapStyleUp = -267;
        int mapStyleDown = 64;
        bool mapStyle2_animation = false;
        bool _closingForHide = false;
        // Переключатель спутник/схема, во вкладке [Карта]
        bool mapSwitch = false;
        int mapSwitchRight = 90;
        int mapSwitchLeft = -6;
        // Настройки отображения на карте, во вкладке [Карта]
        bool mouseOpenPanel = false;
        bool mapStyleSettings_check = false;
        bool mapStyleSettings_polygons = true;
        bool mapStyleSettings_borders = true;
        bool mapStyleSettings_markers = true;
        bool mapStyleSettings_homes = true;
        bool mapStyleSettings_apartments = true;
        bool mapStyleSettings_organizations = true;
        bool mapStyleSettings_post = true;
        // Панель построения маршрутов, во вкладке [Карта]
        bool mapRoute = false;
        bool mapRouteBtn = false;
        bool mapRouteWay = true;
        // Панель информации о маршруте, во вкладке [Карта]
        bool mapInfoRegion_animation = false;
        bool mapInfoSearch_animation = false;
        bool mapInfoTravel_animation = false;
        bool mapInfoRegionTxt_check = false;
        // Панель меток на карте, во вкладке [Карта]
        bool mapTags_animation = false;
        // Панель типа задания, во вкладке [Задания]
        bool issueTransTaskType_btn = false;
        // Кнопки фильтра сотрудников, во вкладке [Задания]
        bool issueTransStaffUpper_btn1 = true;
        bool issueTransStaffUpper_btn2 = false;
        bool issueTransStaffUpper_btn3 = false;
        bool issueTransStaffUpper_btn4 = false;
        // Панель поиска работника, во вкладке [Работники]
        bool workerRegistrationSearch_animation = false;
        // Панель информации о задании, во вкладке [Задания]
        bool taskWatchOpen = false;
        // Панель фильтра заданий, во вкладке [Задания]
        bool taskUpperFilter_animation = false;
        bool taskUpperFilterArrow = false;
        // Панель таблицы маршрутов заданий, во вкладке [Карта]
        bool mapRouteTaskTabel_animation1 = false;
        bool mapRouteTaskTabel_animation2 = false;
        // Таймеры и загрузчики данных
        private Timer _scrollDebounceTimer;
        private SearchFilter<DataBase.Tasks> _searchTasks;
        private LazyLoader<DataBase.Tasks> _loaderTasks;
        private Timer _autoUpdateTasksTimer;
        private readonly HashSet<string> _excludedStatuses = new HashSet<string>();
        // Маршрут по заданию
        private Timer _routeTaskUpdateTimer;
        private List<DataBase.Tasks> _routeTaskItems = new List<DataBase.Tasks>();
        private List<DataBase.Markers> _currentTaskMarkers = new List<DataBase.Markers>();
        private bool _isUpdatingTaskCmb = false;
        private int routeMarkers = 0;
        private bool _isDoubleCheckInternet = false;
        public DirectorForm()
        {
            InitializeComponent();
            OpenPanel();
            UpdateLabelText();
            RoundedCorners();
            GmapWork();
            // Таблицы
            AddSampleData();
            AddSampleData5();
            AddSampleData6();
        }
        // Метод инициализации таблицы сотрудников
        private void AddSampleData()
        {
            issueTransStaff_Dgw.Columns.Clear();
            issueTransStaff_Dgw.Rows.Clear();
            issueTransStaff_Dgw.Columns.Add("ФИО", "ФИО");
            issueTransStaff_Dgw.Columns.Add("Сотрудник", "Сотрудник");

            issueTransStaff_Dgw.Rows.Add("Иванов Иван Иванович", "Почтальон");
            issueTransStaff_Dgw.Rows.Add("Петров Пётр Петрович", "Почтальон");
            issueTransStaff_Dgw.Rows.Add("Сидорова Анна Сергеевна", "Почтальон");
            issueTransStaff_Dgw.Rows.Add("Козлов Дмитрий Алексеевич", "Почтальон");
        }
        // Метод инициализации карты
        private void GmapWork()
        {
            map_gmapCnl.MapProvider = GMapProviders.GoogleMap;
            map_gmapCnl.Position = new PointLatLng(55.533919, 58.244463);
            map_gmapCnl.MinZoom = 2;
            map_gmapCnl.MaxZoom = 18;
            map_gmapCnl.Zoom = 12;
            map_gmapCnl.ShowCenter = false;
            map_gmapCnl.DragButton = MouseButtons.Left;
        }
        // Метод начальной настройки панелей
        private void OpenPanel()
        {
            mapInfo_Pnl.Location = new Point(79, this.ClientSize.Height + 10);

            issue_Pnl.Visible = true;
            map_Pnl.Visible = false;
            worker_Pnl.Visible = false;
            issue_Pnl.Dock = DockStyle.Fill;
            map_Pnl.Dock = DockStyle.Fill;
            worker_Pnl.Dock = DockStyle.Fill;

            workerRegistrationSearch_Pnl.Size = new Size(356, 0);
            workerRegistrationSearch_Pnl.Location = new Point(15, 326);

            mapInfoSearch_Pnl.Location = new Point(10, 215);
            mapInfoSearch_Pnl.Size = new Size(352, 0);
        }
        // Метод скругляющий элементы на форме
        private void SetRoundedCorners(Control control, int radius)
        {
            control.Region = Region.FromHrgn(
                CreateRoundRectRgn(0, 0, control.Width, control.Height, radius, radius)
            );
            control.Resize -= (s, e) => UpdateRoundedCorners(control, radius);
            control.Resize += (s, e) => UpdateRoundedCorners(control, radius);
        }
        private void UpdateRoundedCorners(Control control, int radius)
        {
            control.Region?.Dispose();
            control.Region = Region.FromHrgn(
                CreateRoundRectRgn(0, 0, control.Width, control.Height, radius, radius)
            );
        }
        private void RoundedCorners()
        {
            SetRoundedCorners(mapTop_Pnl, 23);
            SetRoundedCorners(mapBottom_Pnl, 23);
            SetRoundedCorners(mapStyle_Pnl, 24);
            SetRoundedCorners(mapInfo_Pnl, 24);
            SetRoundedCorners(mapLocation_Pnl, 23);
            SetRoundedCorners(mapZoom_Pnl, 17);
            SetRoundedCorners(mapTools_Pnl, 17);
            SetRoundedCorners(mapStyleSwitchBlock_Pnl, 10);
            SetRoundedCorners(issueTop_Pnl, 23);
            SetRoundedCorners(issueBottom_Pnl, 23);
            SetRoundedCorners(issueTransTask_Pnl, 24);
            SetRoundedCorners(issueTabel_Pnl, 24);
            SetRoundedCorners(issueTransStaff_Pnl, 24);
            SetRoundedCorners(workerTabel_Pnl, 24);
        }
        // upper_Pnl Верхняя панель формы
        // Кнопка перемещения формы в верхней части панели
        private void upper_Pnl_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
                SetRoundedCorners(this, 15);
            }
        }
        // Кнопка минимальный размер формы в верхней части панели
        private void upper_minimizeBtn_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }
        // Кнопка расширить экран в верхней части панели
        private void upper_windowBtn_Click(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Normal)
            {
                this.MaximizedBounds = Screen.FromHandle(this.Handle).WorkingArea;
                this.WindowState = FormWindowState.Maximized;
                SetRoundedCorners(this, 0);
            }
            else
            {
                this.WindowState = FormWindowState.Normal;
                SetRoundedCorners(this, 15);
            }
        }
        // Кнопка закрытие формы в верхней части панели
        private void upper_closeBtn_Click(object sender, EventArgs e)
        {
            Program.AppExit();
        }
        // mapTools_Pnl + mapStyle_Pnl Панель настроек карты, во вкладке [Карта]
        // Обработчик наведения мыши на кнопку настроек карты, во вкладке [Карта]
        private async void mapToolsDrop_Pnl_MouseEnter(object sender, EventArgs e)
        {
            _closingForHide = false;
            mapStyle_checkTmr.Stop();

            if (!mapStyle)
            {
                mapStyle = true;
                mapStyle1_animationTmr.Start();
            }
            mapToolsDrop_dropPic.Image = Properties.Resources.СтильКарты1;
            await Task.Delay(20);
            mapToolsDrop_dropPic.Image = Properties.Resources.СтильКарты2;
            await Task.Delay(20);
            mapToolsDrop_dropPic.Image = Properties.Resources.СтильКарты3;
            await Task.Delay(20);
            mapToolsDrop_dropPic.Image = Properties.Resources.СтильКарты4;
            await Task.Delay(20);
            mapToolsDrop_dropPic.Image = Properties.Resources.СтильКарты5;
        }
        // Обработчик ухода мыши с панели настроек карты, во вкладке [Карта]
        private void mapStyle_Pnl_MouseLeave(object sender, EventArgs e)
        {
            mouseOpenPanel = false;
            mapStyle_checkTmr.Start();
        }
        // Таймер проверки положения мыши для скрытия панели настроек, во вкладке [Карта]
        private void mapStyle_checkTmr_Tick(object sender, EventArgs e)
        {
            if (!IsMouseOverPanelHierarchy())
            {
                mapStyle_checkTmr.Stop();
                mouseOpenPanel = false;
                mapStyle = false;

                if (mapStyle2_animation)
                {
                    _closingForHide = true;
                    mapStyle2_animationTmr.Start();
                }
                else
                    HideMapStylePanel();
            }
            else
            {
                _closingForHide = false;
                mapToolsDrop_dropPic.Image = Properties.Resources.СтильКарты5;
            }
        }
        // Таймер анимации открытия/закрытия панели настроек, во вкладке [Карта]
        private void mapStyle1_animationTmr_Tick(object sender, EventArgs e)
        {
            int target = mapStyle ? mapStyleDown : mapStyleUp;
            int distance = target - mapStyle_Pnl.Top;
            int step = (int)(distance * 0.1f);

            if (Math.Abs(distance) < 1)
            {
                mapStyle_Pnl.Top = target;
                mapStyle1_animationTmr.Stop();
                return;
            }
            mapStyle_Pnl.Top += step;
        }
        // Метод проверки нахождения мыши над панелью, во вкладке [Карта]
        private bool IsMouseOverPanelHierarchy()
        {
            Point cursorPos = Cursor.Position;
            if (IsMouseOverControl(mapStyle_Pnl, cursorPos))
            {
                return true;
            }
            return false;
        }
        // Метод проверки нахождения мыши над контролом, во вкладке [Карта]
        private bool IsMouseOverControl(Control control, Point cursorPos)
        {
            if (control == null || !control.Visible)
                return false;
            Point controlPoint = control.PointToClient(cursorPos);
            if (control.ClientRectangle.Contains(controlPoint))
            {
                return true;
            }
            return false;
        }
        // mapStyleSwitch Переключатель спутник/схема, во вкладке [Карта]
        // Кнопка переключения на схему, во вкладке [Карта]
        private void mapStyleSwitchScheme_Pnl_Click(object sender, EventArgs e)
        {
            if (map_gmapCnl.MapProvider == Map.satelliteProvider && mapSwitch)
            {
                mapStyleSwitch_animationTmr.Start();
                mapSwitch = false;
                map_gmapCnl.MapProvider = Map.streetProvider;
            }
        }
        // Кнопка переключения на спутник, во вкладке [Карта]
        private void mapStyleSwitchSatellite_Pnl_Click(object sender, EventArgs e)
        {
            if (map_gmapCnl.MapProvider == Map.streetProvider && !mapSwitch)
            {
                mapStyleSwitch_animationTmr.Start();
                mapSwitch = true;
                map_gmapCnl.MapProvider = Map.satelliteProvider;
            }
        }
        // Обработчик наведения мыши на кнопку схемы, во вкладке [Карта]
        private async void mapStyleSwitchScheme_Pnl_MouseEnter(object sender, EventArgs e)
        {
            mapStyleSwitchSchemeCheck_Pnl.PanelColor = Color.FromArgb(235, 235, 244);
            mapStyleSwitchSchemeCheck_Pnl.PanelOutlineColor = Color.FromArgb(235, 235, 244);
            await Task.Delay(20);
            mapStyleSwitchSchemeCheck_Pnl.PanelColor = Color.FromArgb(225, 225, 240);
            mapStyleSwitchSchemeCheck_Pnl.PanelOutlineColor = Color.FromArgb(225, 225, 240);
            await Task.Delay(20);
            mapStyleSwitchSchemeCheck_Pnl.PanelColor = Color.FromArgb(214, 216, 236);
            mapStyleSwitchSchemeCheck_Pnl.PanelOutlineColor = Color.FromArgb(214, 216, 236);
            await Task.Delay(20);
            mapStyleSwitchSchemeCheck_Pnl.PanelColor = Color.FromArgb(204, 206, 232);
            mapStyleSwitchSchemeCheck_Pnl.PanelOutlineColor = Color.FromArgb(204, 206, 232);
        }
        // Обработчик ухода мыши с кнопки схемы, во вкладке [Карта]
        private async void mapStyleSwitchScheme_Pnl_MouseLeave(object sender, EventArgs e)
        {
            mapStyleSwitchSchemeCheck_Pnl.PanelColor = Color.FromArgb(204, 206, 232);
            mapStyleSwitchSchemeCheck_Pnl.PanelOutlineColor = Color.FromArgb(204, 206, 232);
            await Task.Delay(20);
            mapStyleSwitchSchemeCheck_Pnl.PanelColor = Color.FromArgb(214, 216, 236);
            mapStyleSwitchSchemeCheck_Pnl.PanelOutlineColor = Color.FromArgb(214, 216, 236);
            await Task.Delay(20);
            mapStyleSwitchSchemeCheck_Pnl.PanelColor = Color.FromArgb(225, 225, 240);
            mapStyleSwitchSchemeCheck_Pnl.PanelOutlineColor = Color.FromArgb(225, 225, 240);
            await Task.Delay(20);
            mapStyleSwitchSchemeCheck_Pnl.PanelColor = Color.FromArgb(235, 235, 244);
            mapStyleSwitchSchemeCheck_Pnl.PanelOutlineColor = Color.FromArgb(235, 235, 244);
        }
        // Обработчик наведения мыши на кнопку спутника, во вкладке [Карта]
        private async void mapStyleSwitchSatellite_Pnl_MouseEnter(object sender, EventArgs e)
        {
            mapStyleSwitchSatelliteCheck_Pnl.PanelColor = Color.FromArgb(235, 235, 244);
            mapStyleSwitchSatelliteCheck_Pnl.PanelOutlineColor = Color.FromArgb(235, 235, 244);
            await Task.Delay(20);
            mapStyleSwitchSatelliteCheck_Pnl.PanelColor = Color.FromArgb(225, 225, 240);
            mapStyleSwitchSatelliteCheck_Pnl.PanelOutlineColor = Color.FromArgb(225, 225, 240);
            await Task.Delay(20);
            mapStyleSwitchSatelliteCheck_Pnl.PanelColor = Color.FromArgb(214, 216, 236);
            mapStyleSwitchSatelliteCheck_Pnl.PanelOutlineColor = Color.FromArgb(214, 216, 236);
            await Task.Delay(20);
            mapStyleSwitchSatelliteCheck_Pnl.PanelColor = Color.FromArgb(204, 206, 232);
            mapStyleSwitchSatelliteCheck_Pnl.PanelOutlineColor = Color.FromArgb(204, 206, 232);
        }
        // Обработчик ухода мыши с кнопки спутника, во вкладке [Карта]
        private async void mapStyleSwitchSatellite_Pnl_MouseLeave(object sender, EventArgs e)
        {
            mapStyleSwitchSatelliteCheck_Pnl.PanelColor = Color.FromArgb(204, 206, 232);
            mapStyleSwitchSatelliteCheck_Pnl.PanelOutlineColor = Color.FromArgb(204, 206, 232);
            await Task.Delay(20);
            mapStyleSwitchSatelliteCheck_Pnl.PanelColor = Color.FromArgb(214, 216, 236);
            mapStyleSwitchSatelliteCheck_Pnl.PanelOutlineColor = Color.FromArgb(214, 216, 236);
            await Task.Delay(20);
            mapStyleSwitchSatelliteCheck_Pnl.PanelColor = Color.FromArgb(225, 225, 240);
            mapStyleSwitchSatelliteCheck_Pnl.PanelOutlineColor = Color.FromArgb(225, 225, 240);
            await Task.Delay(20);
            mapStyleSwitchSatelliteCheck_Pnl.PanelColor = Color.FromArgb(235, 235, 244);
            mapStyleSwitchSatelliteCheck_Pnl.PanelOutlineColor = Color.FromArgb(235, 235, 244);
        }
        // Таймер анимации переключения спутник/схема, во вкладке [Карта]
        private void mapStyleSwitch_animationTmr_Tick(object sender, EventArgs e)
        {
            int target = mapSwitch ? mapSwitchRight : mapSwitchLeft;
            int distance = target - mapStyleSwitchBlock_Pnl.Left;
            int step = (int)(distance * 0.1f);
            if (Math.Abs(distance) < 1)
            {
                mapStyleSwitchBlock_Pnl.Left = target;
                mapStyleSwitch_animationTmr.Stop();
                UpdateLabelText();
                return;
            }
            mapStyleSwitchBlock_Pnl.Left += step;
            UpdateLabelText();
        }
        // Метод обновления текста переключателя спутник/схема, во вкладке [Карта]
        private void UpdateLabelText()
        {
            int checkPosition = (mapSwitchRight + mapSwitchLeft) / 2;
            if (mapStyleSwitchBlock_Pnl.Left > checkPosition)
            {
                mapStyleSwitchBlock_namePnl.Content = "Спутник";
                mapStyleSwitchSatelliteCheck_satelliteLbl.Visible = false;
                mapStyleSwitchSchemeCheck_schemeLbl.Visible = true;
            }
            else
            {
                mapStyleSwitchBlock_namePnl.Content = "Схема";
                mapStyleSwitchSatelliteCheck_satelliteLbl.Visible = true;
                mapStyleSwitchSchemeCheck_schemeLbl.Visible = false;
            }
        }
        // mapTools_Pnl Кнопки быстрого доступа к настройкам карты, во вкладке [Карта]
        // Кнопка переключения меток на карте, во вкладке [Карта]
        private async void mapToolsTags_Pnl_Click(object sender, EventArgs e)
        {
            mapStyleSettingsTags_Pnl_Click(sender, e);
        }
        // Кнопка переключения границ на карте, во вкладке [Карта]
        private async void mapToolsBorder_Pnl_Click(object sender, EventArgs e)
        {
            mapStyleSettingsBorder_Pnl_Click(sender, e);
        }
        // Кнопка переключения полигонов на карте, во вкладке [Карта]
        private async void mapToolsPolygon_Pnl_Click(object sender, EventArgs e)
        {
            mapStyleSettingsPolygon_Pnl_Click(sender, e);
        }
        // mapRoute Панель построения маршрутов на карте, во вкладке [Карта]
        // Таймер анимации открытия/закрытия панели маршрутов, во вкладке [Карта]
        private void mapInfo_animationTmr_Tick(object sender, EventArgs e)
        {
            int targetYDown2 = this.ClientSize.Height - mapInfo_Pnl.Height - 43;
            int targetYUp2 = this.ClientSize.Height + 10;
            int target = mapRoute ? targetYDown2 : targetYUp2;
            int distance = target - mapInfo_Pnl.Top;
            int step = (int)(distance * 0.1f);
            if (Math.Abs(distance) < 1)
            {
                mapInfo_Pnl.Top = target;
                mapInfo_animationTmr.Stop();
                return;
            }
            mapInfo_Pnl.Top += step;
        }
        // Обработчик наведения мыши на кнопку начала маршрута, во вкладке [Карта]
        private async void mapRouteStart_Pnl_MouseEnter(object sender, EventArgs e)
        {
            mapInfoTagsDone_Pnl.PanelColor = Color.FromArgb(25, 55, 255); //1
            mapInfoTagsDone_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
            await Task.Delay(20);
            mapInfoTagsDone_Pnl.PanelColor = Color.FromArgb(26, 54, 244); //2
            mapInfoTagsDone_Pnl.PanelOutlineColor = Color.FromArgb(26, 54, 244);
            await Task.Delay(20);
            mapInfoTagsDone_Pnl.PanelColor = Color.FromArgb(26, 52, 232); //3
            mapInfoTagsDone_Pnl.PanelOutlineColor = Color.FromArgb(26, 52, 232);
        }
        // Обработчик ухода мыши с кнопки начала маршрута, во вкладке [Карта]
        private async void mapRouteStart_Pnl_MouseLeave(object sender, EventArgs e)
        {
            await Task.Delay(100);
            mapInfoTagsDone_Pnl.PanelColor = Color.FromArgb(26, 52, 232); //3
            mapInfoTagsDone_Pnl.PanelOutlineColor = Color.FromArgb(26, 52, 232);
            await Task.Delay(20);
            mapInfoTagsDone_Pnl.PanelColor = Color.FromArgb(26, 54, 244); //2
            mapInfoTagsDone_Pnl.PanelOutlineColor = Color.FromArgb(26, 54, 244);
            await Task.Delay(20);
            mapInfoTagsDone_Pnl.PanelColor = Color.FromArgb(25, 55, 255); //1
            mapInfoTagsDone_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
        }
        // mapTop_Pnl + mapBottom_Pnl Навигационные кнопки, во вкладке [Карта]
        // Кнопка переключения на вкладку карта, во вкладке [Карта]
        private void mapTopMap_Pnl_Click(object sender, EventArgs e)
        {
            map_Pnl.Visible = true;
            issue_Pnl.Visible = false;
        }
        // Обработчик наведения мыши на кнопку карта, во вкладке [Карта]
        private async void mapTopMap_Pnl_MouseEnter(object sender, EventArgs e)
        {
            mapTopMap_mapPic.Image = Properties.Resources.Карта1;
            await Task.Delay(20);
            mapTopMap_mapPic.Image = Properties.Resources.Карта2;
            await Task.Delay(20);
            mapTopMap_mapPic.Image = Properties.Resources.Карта3;
            await Task.Delay(20);
            mapTopMap_mapPic.Image = Properties.Resources.Карта4;
            await Task.Delay(20);
            mapTopMap_mapPic.Image = Properties.Resources.Карта5;
        }
        // Обработчик ухода мыши с кнопки карта, во вкладке [Карта]
        private async void mapTopMap_Pnl_MouseLeave(object sender, EventArgs e)
        {
            mapTopMap_mapPic.Image = Properties.Resources.Карта5;
            await Task.Delay(20);
            mapTopMap_mapPic.Image = Properties.Resources.Карта4;
            await Task.Delay(20);
            mapTopMap_mapPic.Image = Properties.Resources.Карта3;
            await Task.Delay(20);
            mapTopMap_mapPic.Image = Properties.Resources.Карта2;
            await Task.Delay(20);
            mapTopMap_mapPic.Image = Properties.Resources.Карта1;
        }
        // Кнопка переключения на вкладку задания, во вкладке [Карта]
        private void mapTopIssue_Pnl_Click(object sender, EventArgs e)
        {
            this.SuspendLayout();
            issue_Pnl.Visible = true;
            map_Pnl.Visible = false;
            worker_Pnl.Visible = false;
            this.ResumeLayout();
        }
        // Обработчик наведения мыши на кнопку задания, во вкладке [Карта]
        private async void mapTopIssue_Pnl_MouseEnter(object sender, EventArgs e)
        {
            mapTopIssue_issuePic.Image = Properties.Resources.ЗаданиеДобавить1;
            await Task.Delay(20);
            mapTopIssue_issuePic.Image = Properties.Resources.ЗаданиеДобавить2;
            await Task.Delay(20);
            mapTopIssue_issuePic.Image = Properties.Resources.ЗаданиеДобавить3;
            await Task.Delay(20);
            mapTopIssue_issuePic.Image = Properties.Resources.ЗаданиеДобавить4;
            await Task.Delay(20);
            mapTopIssue_issuePic.Image = Properties.Resources.ЗаданиеДобавить5;
        }
        // Обработчик ухода мыши с кнопки задания, во вкладке [Карта]
        private async void mapTopIssue_Pnl_MouseLeave(object sender, EventArgs e)
        {
            mapTopIssue_issuePic.Image = Properties.Resources.ЗаданиеДобавить5;
            await Task.Delay(20);
            mapTopIssue_issuePic.Image = Properties.Resources.ЗаданиеДобавить4;
            await Task.Delay(20);
            mapTopIssue_issuePic.Image = Properties.Resources.ЗаданиеДобавить3;
            await Task.Delay(20);
            mapTopIssue_issuePic.Image = Properties.Resources.ЗаданиеДобавить2;
            await Task.Delay(20);
            mapTopIssue_issuePic.Image = Properties.Resources.ЗаданиеДобавить1;
        }
        // Кнопка переключения на вкладку работники, во вкладке [Карта]
        private void mapTopWorker_Pnl_Click(object sender, EventArgs e)
        {
            issue_Pnl.Visible = false;
            map_Pnl.Visible = false;
            worker_Pnl.Visible = true;
            this.SuspendLayout();
            this.ResumeLayout();
        }
        // Обработчик наведения мыши на кнопку работники, во вкладке [Карта]
        private async void mapTopWorker_Pnl_MouseEnter(object sender, EventArgs e)
        {
            mapTopWorker_workerPic.Image = Properties.Resources.Регистрация1;
            await Task.Delay(20);
            mapTopWorker_workerPic.Image = Properties.Resources.Регистрация2;
            await Task.Delay(20);
            mapTopWorker_workerPic.Image = Properties.Resources.Регистрация3;
            await Task.Delay(20);
            mapTopWorker_workerPic.Image = Properties.Resources.Регистрация4;
            await Task.Delay(20);
            mapTopWorker_workerPic.Image = Properties.Resources.Регистрация5;
        }
        // Обработчик ухода мыши с кнопки работники, во вкладке [Карта]
        private async void mapTopWorker_Pnl_MouseLeave(object sender, EventArgs e)
        {
            mapTopWorker_workerPic.Image = Properties.Resources.Регистрация5;
            await Task.Delay(20);
            mapTopWorker_workerPic.Image = Properties.Resources.Регистрация4;
            await Task.Delay(20);
            mapTopWorker_workerPic.Image = Properties.Resources.Регистрация3;
            await Task.Delay(20);
            mapTopWorker_workerPic.Image = Properties.Resources.Регистрация2;
            await Task.Delay(20);
            mapTopWorker_workerPic.Image = Properties.Resources.Регистрация1;
        }
        // Обработчик наведения мыши на кнопку настройки, во вкладке [Карта]
        private async void mapBottomSettings_Pnl_MouseEnter(object sender, EventArgs e)
        {
            mapBottomSettings_settingsPic.Image = Properties.Resources.Настройки1;
            await Task.Delay(20);
            mapBottomSettings_settingsPic.Image = Properties.Resources.Настройки2;
            await Task.Delay(20);
            mapBottomSettings_settingsPic.Image = Properties.Resources.Настройки3;
            await Task.Delay(20);
            mapBottomSettings_settingsPic.Image = Properties.Resources.Настройки4;
            await Task.Delay(20);
            mapBottomSettings_settingsPic.Image = Properties.Resources.Настройки5;
        }
        // Обработчик ухода мыши с кнопки настройки, во вкладке [Карта]
        private async void mapBottomSettings_Pnl_MouseLeave(object sender, EventArgs e)
        {
            mapBottomSettings_settingsPic.Image = Properties.Resources.Настройки5;
            await Task.Delay(20);
            mapBottomSettings_settingsPic.Image = Properties.Resources.Настройки4;
            await Task.Delay(20);
            mapBottomSettings_settingsPic.Image = Properties.Resources.Настройки3;
            await Task.Delay(20);
            mapBottomSettings_settingsPic.Image = Properties.Resources.Настройки2;
            await Task.Delay(20);
            mapBottomSettings_settingsPic.Image = Properties.Resources.Настройки1;
        }
        // Кнопка выхода из системы, во вкладке [Карта]
        private async void mapBottomExit_Pnl_Click(object sender, EventArgs e)
        {
            await UserData.LogoutAndExit(this);
        }
        // Обработчик наведения мыши на кнопку выхода, во вкладке [Карта]
        private async void mapBottomExit_Pnl_MouseEnter(object sender, EventArgs e)
        {
            mapBottomExit_exitPic.Image = Properties.Resources.Выход1;
            await Task.Delay(20);
            mapBottomExit_exitPic.Image = Properties.Resources.Выход2;
            await Task.Delay(20);
            mapBottomExit_exitPic.Image = Properties.Resources.Выход3;
            await Task.Delay(20);
            mapBottomExit_exitPic.Image = Properties.Resources.Выход4;
            await Task.Delay(20);
            mapBottomExit_exitPic.Image = Properties.Resources.Выход5;
        }
        // Обработчик ухода мыши с кнопки выхода, во вкладке [Карта]
        private async void mapBottomExit_Pnl_MouseLeave(object sender, EventArgs e)
        {
            mapBottomExit_exitPic.Image = Properties.Resources.Выход5;
            await Task.Delay(20);
            mapBottomExit_exitPic.Image = Properties.Resources.Выход4;
            await Task.Delay(20);
            mapBottomExit_exitPic.Image = Properties.Resources.Выход3;
            await Task.Delay(20);
            mapBottomExit_exitPic.Image = Properties.Resources.Выход2;
            await Task.Delay(20);
            mapBottomExit_exitPic.Image = Properties.Resources.Выход1;
        }
        // mapZoom_Pnl + mapLocation_Pnl Кнопки управления картой, во вкладке [Карта]
        // Кнопка увеличения масштаба карты, во вкладке [Карта]
        private void mapZoomPlus_Pnl_Click(object sender, EventArgs e)
        {
            map_gmapCnl.Zoom++;
        }
        // Обработчик наведения мыши на кнопку увеличения масштаба, во вкладке [Карта]
        private async void mapZoomPlus_Pnl_MouseEnter(object sender, EventArgs e)
        {
            mapZoomPlus_plusPic.Image = Properties.Resources.Плюс1;
            await Task.Delay(20);
            mapZoomPlus_plusPic.Image = Properties.Resources.Плюс2;
            await Task.Delay(20);
            mapZoomPlus_plusPic.Image = Properties.Resources.Плюс3;
            await Task.Delay(20);
            mapZoomPlus_plusPic.Image = Properties.Resources.Плюс4;
            await Task.Delay(20);
            mapZoomPlus_plusPic.Image = Properties.Resources.Плюс5;
            await Task.Delay(20);
        }
        // Обработчик ухода мыши с кнопки увеличения масштаба, во вкладке [Карта]
        private async void mapZoomPlus_Pnl_MouseLeave(object sender, EventArgs e)
        {
            mapZoomPlus_plusPic.Image = Properties.Resources.Плюс5;
            await Task.Delay(20);
            mapZoomPlus_plusPic.Image = Properties.Resources.Плюс4;
            await Task.Delay(20);
            mapZoomPlus_plusPic.Image = Properties.Resources.Плюс3;
            await Task.Delay(20);
            mapZoomPlus_plusPic.Image = Properties.Resources.Плюс2;
            await Task.Delay(20);
            mapZoomPlus_plusPic.Image = Properties.Resources.Плюс1;
        }
        // Кнопка уменьшения масштаба карты, во вкладке [Карта]
        private void mapZoomMinus_Pnl_Click(object sender, EventArgs e)
        {
            map_gmapCnl.Zoom--;
        }
        // Обработчик наведения мыши на кнопку уменьшения масштаба, во вкладке [Карта]
        private async void mapZoomMinus_Pnl_MouseEnter(object sender, EventArgs e)
        {
            mapZoomMinus_minusPic.Image = Properties.Resources.Минус1;
            await Task.Delay(20);
            mapZoomMinus_minusPic.Image = Properties.Resources.Минус2;
            await Task.Delay(20);
            mapZoomMinus_minusPic.Image = Properties.Resources.Минус3;
            await Task.Delay(20);
            mapZoomMinus_minusPic.Image = Properties.Resources.Минус4;
            await Task.Delay(20);
            mapZoomMinus_minusPic.Image = Properties.Resources.Минус5;
        }
        // Обработчик ухода мыши с кнопки уменьшения масштаба, во вкладке [Карта]
        private async void mapZoomMinus_Pnl_MouseLeave(object sender, EventArgs e)
        {
            mapZoomMinus_minusPic.Image = Properties.Resources.Минус5;
            await Task.Delay(20);
            mapZoomMinus_minusPic.Image = Properties.Resources.Минус4;
            await Task.Delay(20);
            mapZoomMinus_minusPic.Image = Properties.Resources.Минус3;
            await Task.Delay(20);
            mapZoomMinus_minusPic.Image = Properties.Resources.Минус2;
            await Task.Delay(20);
            mapZoomMinus_minusPic.Image = Properties.Resources.Минус1;
        }
        // Кнопка возврата к начальной позиции на карте, во вкладке [Карта]
        private void mapLocation_Pnl_Click(object sender, EventArgs e)
        {
            map_gmapCnl.Position = Map.startPosition;
        }
        // Обработчик наведения мыши на кнопку возврата к начальной позиции, во вкладке [Карта]
        private async void mapLocation_Pnl_MouseEnter(object sender, EventArgs e)
        {
            mapLocation_locationPic.Image = Properties.Resources.ГдеЯ1;
            await Task.Delay(20);
            mapLocation_locationPic.Image = Properties.Resources.ГдеЯ2;
            await Task.Delay(20);
            mapLocation_locationPic.Image = Properties.Resources.ГдеЯ3;
            await Task.Delay(20);
            mapLocation_locationPic.Image = Properties.Resources.ГдеЯ4;
            await Task.Delay(20);
            mapLocation_locationPic.Image = Properties.Resources.ГдеЯ5;
        }
        // Обработчик ухода мыши с кнопки возврата к начальной позиции, во вкладке [Карта]
        private async void mapLocation_Pnl_MouseLeave(object sender, EventArgs e)
        {
            mapLocation_locationPic.Image = Properties.Resources.ГдеЯ5;
            await Task.Delay(20);
            mapLocation_locationPic.Image = Properties.Resources.ГдеЯ4;
            await Task.Delay(20);
            mapLocation_locationPic.Image = Properties.Resources.ГдеЯ3;
            await Task.Delay(20);
            mapLocation_locationPic.Image = Properties.Resources.ГдеЯ2;
            await Task.Delay(20);
            mapLocation_locationPic.Image = Properties.Resources.ГдеЯ1;
        }
        // issueTransTask_Pnl Панель информации о задании, во вкладке [Задания]
        // Кнопка подтверждения задания, во вкладке [Задания]
        private void issueTransTaskBottomDone_Pnl_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Отправлено");
        }
        // Обработчик наведения мыши на кнопку подтверждения задания, во вкладке [Задания]
        private async void taskWatchsTransitionStart_Pnl_MouseEnter(object sender, EventArgs e)
        {
            issueTransTaskBottomDone_Pnl.PanelColor = Color.FromArgb(25, 55, 255); //1
            issueTransTaskBottomDone_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
            await Task.Delay(20);
            issueTransTaskBottomDone_Pnl.PanelColor = Color.FromArgb(26, 54, 244); //2
            issueTransTaskBottomDone_Pnl.PanelOutlineColor = Color.FromArgb(26, 54, 244);
            await Task.Delay(20);
            issueTransTaskBottomDone_Pnl.PanelColor = Color.FromArgb(26, 52, 232); //3
            issueTransTaskBottomDone_Pnl.PanelOutlineColor = Color.FromArgb(26, 52, 232);
        }
        // Обработчик ухода мыши с кнопки подтверждения задания, во вкладке [Задания]
        private async void taskWatchsTransitionStart_Pnl_MouseLeave(object sender, EventArgs e)
        {
            issueTransTaskBottomDone_Pnl.PanelColor = Color.FromArgb(26, 52, 232); //3
            issueTransTaskBottomDone_Pnl.PanelOutlineColor = Color.FromArgb(26, 52, 232);
            await Task.Delay(20);
            issueTransTaskBottomDone_Pnl.PanelColor = Color.FromArgb(26, 54, 244); //2
            issueTransTaskBottomDone_Pnl.PanelOutlineColor = Color.FromArgb(26, 54, 244);
            await Task.Delay(20);
            issueTransTaskBottomDone_Pnl.PanelColor = Color.FromArgb(25, 55, 255); //1
            issueTransTaskBottomDone_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
        }
        // issueTop_Pnl + issueBottom_Pnl Навигационные кнопки, во вкладке [Задания]
        // Кнопка переключения на вкладку задания, во вкладке [Задания]
        private void taskTopMap_Pnl_Click(object sender, EventArgs e)
        {
            map_Pnl.Visible = false;
            issue_Pnl.Visible = true;
        }
        // Обработчик наведения мыши на кнопку задания, во вкладке [Задания]
        private async void taskTopMap_Pnl_MouseEnter(object sender, EventArgs e)
        {
            issueTopIssue_issuePic.Image = Properties.Resources.ЗаданиеДобавить1;
            await Task.Delay(20);
            issueTopIssue_issuePic.Image = Properties.Resources.ЗаданиеДобавить2;
            await Task.Delay(20);
            issueTopIssue_issuePic.Image = Properties.Resources.ЗаданиеДобавить3;
            await Task.Delay(20);
            issueTopIssue_issuePic.Image = Properties.Resources.ЗаданиеДобавить4;
            await Task.Delay(20);
            issueTopIssue_issuePic.Image = Properties.Resources.ЗаданиеДобавить5;
        }
        // Обработчик ухода мыши с кнопки задания, во вкладке [Задания]
        private async void taskTopMap_Pnl_MouseLeave(object sender, EventArgs e)
        {
            issueTopIssue_issuePic.Image = Properties.Resources.ЗаданиеДобавить5;
            await Task.Delay(20);
            issueTopIssue_issuePic.Image = Properties.Resources.ЗаданиеДобавить4;
            await Task.Delay(20);
            issueTopIssue_issuePic.Image = Properties.Resources.ЗаданиеДобавить3;
            await Task.Delay(20);
            issueTopIssue_issuePic.Image = Properties.Resources.ЗаданиеДобавить2;
            await Task.Delay(20);
            issueTopIssue_issuePic.Image = Properties.Resources.ЗаданиеДобавить1;
        }
        // Кнопка переключения на вкладку карта, во вкладке [Задания]
        private async void taskTopTask_Pnl_Click(object sender, EventArgs e)
        {
            this.SuspendLayout();
            issue_Pnl.Visible = false;
            map_Pnl.Visible = true;
            this.ResumeLayout();
        }

        private async void taskTopTask_Pnl_MouseEnter(object sender, EventArgs e)
        {
            issueTopMap_mapPic.Image = Properties.Resources.Карта1;
            await Task.Delay(20);
            issueTopMap_mapPic.Image = Properties.Resources.Карта2;
            await Task.Delay(20);
            issueTopMap_mapPic.Image = Properties.Resources.Карта3;
            await Task.Delay(20);
            issueTopMap_mapPic.Image = Properties.Resources.Карта4;
            await Task.Delay(20);
            issueTopMap_mapPic.Image = Properties.Resources.Карта5;
        }

        private async void taskTopTask_Pnl_MouseLeave(object sender, EventArgs e)
        {
            issueTopMap_mapPic.Image = Properties.Resources.Карта5;
            await Task.Delay(20);
            issueTopMap_mapPic.Image = Properties.Resources.Карта4;
            await Task.Delay(20);
            issueTopMap_mapPic.Image = Properties.Resources.Карта3;
            await Task.Delay(20);
            issueTopMap_mapPic.Image = Properties.Resources.Карта2;
            await Task.Delay(20);
            issueTopMap_mapPic.Image = Properties.Resources.Карта1;
        }

        //Кнопка работники в панели задания
        private void issueTopWorker_Pnl_Click(object sender, EventArgs e)
        {
            issue_Pnl.Visible = false;
            map_Pnl.Visible = false;
            worker_Pnl.Visible = true;
            this.SuspendLayout();
            this.ResumeLayout();
        }

        private async void issueTopWorker_Pnl_MouseEnter(object sender, EventArgs e)
        {
            issueTopWorker_workerPic.Image = Properties.Resources.Регистрация1;
            await Task.Delay(20);
            issueTopWorker_workerPic.Image = Properties.Resources.Регистрация2;
            await Task.Delay(20);
            issueTopWorker_workerPic.Image = Properties.Resources.Регистрация3;
            await Task.Delay(20);
            issueTopWorker_workerPic.Image = Properties.Resources.Регистрация4;
            await Task.Delay(20);
            issueTopWorker_workerPic.Image = Properties.Resources.Регистрация5;
        }

        private async void issueTopWorker_Pnl_MouseLeave(object sender, EventArgs e)
        {
            issueTopWorker_workerPic.Image = Properties.Resources.Регистрация5;
            await Task.Delay(20);
            issueTopWorker_workerPic.Image = Properties.Resources.Регистрация4;
            await Task.Delay(20);
            issueTopWorker_workerPic.Image = Properties.Resources.Регистрация3;
            await Task.Delay(20);
            issueTopWorker_workerPic.Image = Properties.Resources.Регистрация2;
            await Task.Delay(20);
            issueTopWorker_workerPic.Image = Properties.Resources.Регистрация1;
        }

        private async void taskBottomSettings_Pnl_MouseEnter(object sender, EventArgs e)
        {
            issueBottomSettings_settingsPic.Image = Properties.Resources.Настройки1;
            await Task.Delay(20);
            issueBottomSettings_settingsPic.Image = Properties.Resources.Настройки2;
            await Task.Delay(20);
            issueBottomSettings_settingsPic.Image = Properties.Resources.Настройки3;
            await Task.Delay(20);
            issueBottomSettings_settingsPic.Image = Properties.Resources.Настройки4;
            await Task.Delay(20);
            issueBottomSettings_settingsPic.Image = Properties.Resources.Настройки5;
        }

        private async void taskBottomSettings_Pnl_MouseLeave(object sender, EventArgs e)
        {
            issueBottomSettings_settingsPic.Image = Properties.Resources.Настройки5;
            await Task.Delay(20);
            issueBottomSettings_settingsPic.Image = Properties.Resources.Настройки4;
            await Task.Delay(20);
            issueBottomSettings_settingsPic.Image = Properties.Resources.Настройки3;
            await Task.Delay(20);
            issueBottomSettings_settingsPic.Image = Properties.Resources.Настройки2;
            await Task.Delay(20);
            issueBottomSettings_settingsPic.Image = Properties.Resources.Настройки1;
        }

        //Кнопка выйти в панели задания
        private async void taskBottomExit_Pnl_Click(object sender, EventArgs e)
        {
            await UserData.LogoutAndExit(this);
        }

        private async void taskBottomExit_Pnl_MouseEnter(object sender, EventArgs e)
        {
            issueBottomExit_exitPic.Image = Properties.Resources.Выход1;
            await Task.Delay(20);
            issueBottomExit_exitPic.Image = Properties.Resources.Выход2;
            await Task.Delay(20);
            issueBottomExit_exitPic.Image = Properties.Resources.Выход3;
            await Task.Delay(20);
            issueBottomExit_exitPic.Image = Properties.Resources.Выход4;
            await Task.Delay(20);
            issueBottomExit_exitPic.Image = Properties.Resources.Выход5;
        }

        private async void taskBottomExit_Pnl_MouseLeave(object sender, EventArgs e)
        {
            issueBottomExit_exitPic.Image = Properties.Resources.Выход5;
            await Task.Delay(20);
            issueBottomExit_exitPic.Image = Properties.Resources.Выход4;
            await Task.Delay(20);
            issueBottomExit_exitPic.Image = Properties.Resources.Выход3;
            await Task.Delay(20);
            issueBottomExit_exitPic.Image = Properties.Resources.Выход2;
            await Task.Delay(20);
            issueBottomExit_exitPic.Image = Properties.Resources.Выход1;
        }
        
        private async void PostmanForm_Load(object sender, EventArgs e)
        {
            /*
            await Map.InitializeMap(map_gmapCnl);
            await Map.RefreshMap(map_gmapCnl);

            DataTables.InitializeTasksTable(taskTabel_Dgw, false);
            InitializeTimer();
            SubscriptionEvents();
            await LoadDataAsync();
            TaskOnEmployee.AssignTaskDashboard(taskData_waitLbl, taskData_doneLbl, taskData_newLbl,
                taskData_failedLbl, taskData_progressBarCpb, taskData_percentLbl,
                _searchTasks, subTabelInsertFilterDate_fromCdp, subTabelInsertFilterDate_toCdp);
            TaskOnEmployee.UpdateTasksTimer(_autoUpdateTasksTimer);

            // Первоначальное заполнение комбо-бокса заданий
            await UpdateRouteTaskComboBox();
            // Таймер автообновления заданий каждые 5 секунд
            InitializeTimerUpdateRouteTaskComboBox();
            */
        }
        

        private void InitializeTimerUpdateRouteTaskComboBox()
        {
            _routeTaskUpdateTimer = new Timer { Interval = 5000 };
            _routeTaskUpdateTimer.Start();
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

        private void ResetTimer()
        {
            _scrollDebounceTimer.Stop();
            _scrollDebounceTimer.Start();
        }

        private void SubscriptionEvents()
        {
            issueTabel_Dgw.Scroll += (s, t) => ResetTimer();
            issueTabel_Dgw.MouseWheel += (s, t) => ResetTimer();
            _searchTasks = new SearchFilter<DataBase.Tasks>();
            _loaderTasks = new LazyLoader<DataBase.Tasks>(_searchTasks);
        }

        private async Task CheckScrollAndLoad()
        {
            // Проверка, не грузим ли мы уже
            int firstVisible = issueTabel_Dgw.FirstDisplayedScrollingRowIndex;
            if (firstVisible < 0)
                return;

            // Если прокрутили вниз
            if (firstVisible + issueTabel_Dgw.DisplayedRowCount(false) >= issueTabel_Dgw.RowCount - 10)
                await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                var data = await _loaderTasks.LoadNextBatchAsync();
                data = ApplyStatusBlacklist(data);
                foreach (var item in data)
                {
                    if (item.IdEmployee == UserData.CurrentUser.Employee.Id)
                    {
                        int numMarkers = 0;
                        if (!string.IsNullOrEmpty(item.AttachedMarkers))
                            numMarkers = item.AttachedMarkers.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Length;

                        await DataTables.AddTaskRow(issueTabel_Dgw, item, issueTabel_Dgw.RowCount + 1, numMarkers);
                        await TaskOnEmployee.MarkAsAcceptedIfNew(item);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка загрузки таблиц из базы данных", ex);
                Logger.ShowError("Ошибка загрузки таблиц из базы данных");
            }
        }

        private List<DataBase.Tasks> ApplyStatusBlacklist(List<DataBase.Tasks> tasks)
        {
            if (_excludedStatuses.Count == 0) return tasks;
            return tasks.Where(t => !_excludedStatuses.Contains(t.Status)).ToList();
        }

        private void mapRouteTask_animationTmr_Tick(object sender, EventArgs e)
        {

        }

        private async void mapStyleSettingsPolygon_Pnl_Click(object sender, EventArgs e)
        {
            if (!mapStyleSettings_polygons)
            {
                mapStyleSettings_polygons = true;
                mapStyleSettingsPolygon_polygonPic.Image = Properties.Resources.Полигон1;
                mapToolsPolygon_polygonPic.Image = Properties.Resources.Полигон1;
                await Task.Delay(20);
                mapStyleSettingsPolygon_polygonPic.Image = Properties.Resources.Полигон2;
                mapToolsPolygon_polygonPic.Image = Properties.Resources.Полигон2;
                await Task.Delay(20);
                mapStyleSettingsPolygon_polygonPic.Image = Properties.Resources.Полигон3;
                mapToolsPolygon_polygonPic.Image = Properties.Resources.Полигон3;
                await Task.Delay(20);
                mapStyleSettingsPolygon_polygonPic.Image = Properties.Resources.Полигон4;
                mapToolsPolygon_polygonPic.Image = Properties.Resources.Полигон4;
                await Task.Delay(20);
                mapStyleSettingsPolygon_polygonPic.Image = Properties.Resources.Полигон5;
                mapToolsPolygon_polygonPic.Image = Properties.Resources.Полигон5;
            }
            else if (mapStyleSettings_polygons)
            {
                mapStyleSettings_polygons = false;
                mapStyleSettingsPolygon_polygonPic.Image = Properties.Resources.Полигон5;
                mapToolsPolygon_polygonPic.Image = Properties.Resources.Полигон5;
                await Task.Delay(20);
                mapStyleSettingsPolygon_polygonPic.Image = Properties.Resources.Полигон4;
                mapToolsPolygon_polygonPic.Image = Properties.Resources.Полигон4;
                await Task.Delay(20);
                mapStyleSettingsPolygon_polygonPic.Image = Properties.Resources.Полигон3;
                mapToolsPolygon_polygonPic.Image = Properties.Resources.Полигон3;
                await Task.Delay(20);
                mapStyleSettingsPolygon_polygonPic.Image = Properties.Resources.Полигон2;
                mapToolsPolygon_polygonPic.Image = Properties.Resources.Полигон2;
                await Task.Delay(20);
                mapStyleSettingsPolygon_polygonPic.Image = Properties.Resources.Полигон1;
                mapToolsPolygon_polygonPic.Image = Properties.Resources.Полигон1;
            }
        }
        private async void mapStyleSettingsBorder_Pnl_Click(object sender, EventArgs e)
        {
            if (!mapStyleSettings_borders)
            {
                mapStyleSettings_borders = true;
                mapStyleSettingsBorder_borderPic.Image = Properties.Resources.Граница1;
                mapToolsBorder_borderPic.Image = Properties.Resources.Граница1;
                await Task.Delay(20);
                mapStyleSettingsBorder_borderPic.Image = Properties.Resources.Граница2;
                mapToolsBorder_borderPic.Image = Properties.Resources.Граница2;
                await Task.Delay(20);
                mapStyleSettingsBorder_borderPic.Image = Properties.Resources.Граница3;
                mapToolsBorder_borderPic.Image = Properties.Resources.Граница3;
                await Task.Delay(20);
                mapStyleSettingsBorder_borderPic.Image = Properties.Resources.Граница4;
                mapToolsBorder_borderPic.Image = Properties.Resources.Граница4;
                await Task.Delay(20);
                mapStyleSettingsBorder_borderPic.Image = Properties.Resources.Граница5;
                mapToolsBorder_borderPic.Image = Properties.Resources.Граница5;
            }
            else if (mapStyleSettings_borders)
            {
                mapStyleSettings_borders = false;
                mapStyleSettingsBorder_borderPic.Image = Properties.Resources.Граница5;
                mapToolsBorder_borderPic.Image = Properties.Resources.Граница5;
                await Task.Delay(20);
                mapStyleSettingsBorder_borderPic.Image = Properties.Resources.Граница4;
                mapToolsBorder_borderPic.Image = Properties.Resources.Граница4;
                await Task.Delay(20);
                mapStyleSettingsBorder_borderPic.Image = Properties.Resources.Граница3;
                mapToolsBorder_borderPic.Image = Properties.Resources.Граница3;
                await Task.Delay(20);
                mapStyleSettingsBorder_borderPic.Image = Properties.Resources.Граница2;
                mapToolsBorder_borderPic.Image = Properties.Resources.Граница2;
                await Task.Delay(20);
                mapStyleSettingsBorder_borderPic.Image = Properties.Resources.Граница1;
                mapToolsBorder_borderPic.Image = Properties.Resources.Граница1;
            }
        }
        private async void mapStyleSettingsTags_Pnl_Click(object sender, EventArgs e)
        {
            if (!mapStyleSettings_markers)
            {
                mapStyleSettings_markers = true;
                mapStyleSettingsTagsChanged(mapStyleSettings_markers);

                if (!mapStyleSettings_homes)
                    mapTagsHome_Pnl_Click(sender, e);
                if (!mapStyleSettings_apartments)
                    mapTagsApartment_Pnl_Click(sender, e);
                if (!mapStyleSettings_organizations)
                    mapTagsOrganizations_Pnl_Click(sender, e);
                if (!mapStyleSettings_post)
                    mapTagsPost_Pnl_Click(sender, e);
            }
            else if (mapStyleSettings_markers)
            {
                mapStyleSettings_markers = false;
                mapStyleSettingsTagsChanged(mapStyleSettings_markers);

                if (mapStyleSettings_homes)
                    mapTagsHome_Pnl_Click(sender, e);
                if (mapStyleSettings_apartments)
                    mapTagsApartment_Pnl_Click(sender, e);
                if (mapStyleSettings_organizations)
                    mapTagsOrganizations_Pnl_Click(sender, e);
                if (mapStyleSettings_post)
                    mapTagsPost_Pnl_Click(sender, e);
            }
        }
        private async void mapStyleSettingsTagsChanged(bool value)
        {
            if (value)
            {
                mapStyleSettingsTags_tagsPic.Image = Properties.Resources.Метка1;
                mapToolsTags_tagsPic.Image = Properties.Resources.Метка1;
                await Task.Delay(20);
                mapStyleSettingsTags_tagsPic.Image = Properties.Resources.Метка2;
                mapToolsTags_tagsPic.Image = Properties.Resources.Метка2;
                await Task.Delay(20);
                mapStyleSettingsTags_tagsPic.Image = Properties.Resources.Метка3;
                mapToolsTags_tagsPic.Image = Properties.Resources.Метка3;
                await Task.Delay(20);
                mapStyleSettingsTags_tagsPic.Image = Properties.Resources.Метка4;
                mapToolsTags_tagsPic.Image = Properties.Resources.Метка4;
                await Task.Delay(20);
                mapStyleSettingsTags_tagsPic.Image = Properties.Resources.Метка5;
                mapToolsTags_tagsPic.Image = Properties.Resources.Метка5;
            }
            else if (!value)
            {
                mapStyleSettingsTags_tagsPic.Image = Properties.Resources.Метка5;
                mapToolsTags_tagsPic.Image = Properties.Resources.Метка5;
                await Task.Delay(20);
                mapStyleSettingsTags_tagsPic.Image = Properties.Resources.Метка4;
                mapToolsTags_tagsPic.Image = Properties.Resources.Метка4;
                await Task.Delay(20);
                mapStyleSettingsTags_tagsPic.Image = Properties.Resources.Метка3;
                mapToolsTags_tagsPic.Image = Properties.Resources.Метка3;
                await Task.Delay(20);
                mapStyleSettingsTags_tagsPic.Image = Properties.Resources.Метка2;
                mapToolsTags_tagsPic.Image = Properties.Resources.Метка2;
                await Task.Delay(20);
                mapStyleSettingsTags_tagsPic.Image = Properties.Resources.Метка1;
                mapToolsTags_tagsPic.Image = Properties.Resources.Метка1;
            }
        }
        private async void mapTagsHome_Pnl_Click(object sender, EventArgs e)
        {
            if (!mapStyleSettings_homes)
            {
                mapStyleSettings_homes = true;
                mapTagsHome_homePic.Image = Properties.Resources.Домик1;
                await Task.Delay(20);
                mapTagsHome_homePic.Image = Properties.Resources.Домик2;
                await Task.Delay(20);
                mapTagsHome_homePic.Image = Properties.Resources.Домик3;
                await Task.Delay(20);
                mapTagsHome_homePic.Image = Properties.Resources.Домик4;
                await Task.Delay(20);
                mapTagsHome_homePic.Image = Properties.Resources.Домик5;

                if (!mapStyleSettings_markers)
                {
                    mapStyleSettings_markers = true;
                    mapStyleSettingsTagsChanged(mapStyleSettings_markers);
                }
            }
            else if (mapStyleSettings_homes)
            {
                mapStyleSettings_homes = false;
                mapTagsHome_homePic.Image = Properties.Resources.Домик5;
                await Task.Delay(20);
                mapTagsHome_homePic.Image = Properties.Resources.Домик4;
                await Task.Delay(20);
                mapTagsHome_homePic.Image = Properties.Resources.Домик3;
                await Task.Delay(20);
                mapTagsHome_homePic.Image = Properties.Resources.Домик2;
                await Task.Delay(20);
                mapTagsHome_homePic.Image = Properties.Resources.Домик1;

                if (mapStyleSettings_markers && !mapStyleSettings_homes && !mapStyleSettings_apartments && !mapStyleSettings_organizations && !mapStyleSettings_post)
                {
                    mapStyleSettings_markers = false;
                    mapStyleSettingsTagsChanged(mapStyleSettings_markers);
                }
            }
        }
        private async void mapTagsApartment_Pnl_Click(object sender, EventArgs e)
        {
            if (!mapStyleSettings_apartments)
            {
                mapStyleSettings_apartments = true;
                mapTagsApartment_apartmentPic.Image = Properties.Resources.Квартира1;
                await Task.Delay(20);
                mapTagsApartment_apartmentPic.Image = Properties.Resources.Квартира2;
                await Task.Delay(20);
                mapTagsApartment_apartmentPic.Image = Properties.Resources.Квартира3;
                await Task.Delay(20);
                mapTagsApartment_apartmentPic.Image = Properties.Resources.Квартира4;
                await Task.Delay(20);
                mapTagsApartment_apartmentPic.Image = Properties.Resources.Квартира5;

                if (!mapStyleSettings_markers)
                {
                    mapStyleSettings_markers = true;
                    mapStyleSettingsTagsChanged(mapStyleSettings_markers);
                }
            }
            else if (mapStyleSettings_apartments)
            {
                mapStyleSettings_apartments = false;
                mapTagsApartment_apartmentPic.Image = Properties.Resources.Квартира5;
                await Task.Delay(20);
                mapTagsApartment_apartmentPic.Image = Properties.Resources.Квартира4;
                await Task.Delay(20);
                mapTagsApartment_apartmentPic.Image = Properties.Resources.Квартира3;
                await Task.Delay(20);
                mapTagsApartment_apartmentPic.Image = Properties.Resources.Квартира2;
                await Task.Delay(20);
                mapTagsApartment_apartmentPic.Image = Properties.Resources.Квартира1;

                if (mapStyleSettings_markers && !mapStyleSettings_homes && !mapStyleSettings_apartments && !mapStyleSettings_organizations && !mapStyleSettings_post)
                {
                    mapStyleSettings_markers = false;
                    mapStyleSettingsTagsChanged(mapStyleSettings_markers);
                }
            }
        }
        private async void mapTagsOrganizations_Pnl_Click(object sender, EventArgs e)
        {
            if (!mapStyleSettings_organizations)
            {
                mapStyleSettings_organizations = true;
                mapTagsOrganizations_organizationsPic.Image = Properties.Resources.Организация1;
                await Task.Delay(20);
                mapTagsOrganizations_organizationsPic.Image = Properties.Resources.Организация2;
                await Task.Delay(20);
                mapTagsOrganizations_organizationsPic.Image = Properties.Resources.Организация3;
                await Task.Delay(20);
                mapTagsOrganizations_organizationsPic.Image = Properties.Resources.Организация4;
                await Task.Delay(20);
                mapTagsOrganizations_organizationsPic.Image = Properties.Resources.Организация5;

                if (!mapStyleSettings_markers)
                {
                    mapStyleSettings_markers = true;
                    mapStyleSettingsTagsChanged(mapStyleSettings_markers);
                }
            }
            else if (mapStyleSettings_organizations)
            {
                mapStyleSettings_organizations = false;
                mapTagsOrganizations_organizationsPic.Image = Properties.Resources.Организация5;
                await Task.Delay(20);
                mapTagsOrganizations_organizationsPic.Image = Properties.Resources.Организация4;
                await Task.Delay(20);
                mapTagsOrganizations_organizationsPic.Image = Properties.Resources.Организация3;
                await Task.Delay(20);
                mapTagsOrganizations_organizationsPic.Image = Properties.Resources.Организация2;
                await Task.Delay(20);
                mapTagsOrganizations_organizationsPic.Image = Properties.Resources.Организация1;

                if (mapStyleSettings_markers && !mapStyleSettings_homes && !mapStyleSettings_apartments && !mapStyleSettings_organizations && !mapStyleSettings_post)
                {
                    mapStyleSettings_markers = false;
                    mapStyleSettingsTagsChanged(mapStyleSettings_markers);
                }
            }
        }
        private async void mapTagsPost_Pnl_Click(object sender, EventArgs e)
        {
            if (!mapStyleSettings_post)
            {
                mapStyleSettings_post = true;
                mapTagsPost_postPic.Image = Properties.Resources.Почта1;
                await Task.Delay(20);
                mapTagsPost_postPic.Image = Properties.Resources.Почта2;
                await Task.Delay(20);
                mapTagsPost_postPic.Image = Properties.Resources.Почта3;
                await Task.Delay(20);
                mapTagsPost_postPic.Image = Properties.Resources.Почта4;
                await Task.Delay(20);
                mapTagsPost_postPic.Image = Properties.Resources.Почта5;

                if (!mapStyleSettings_markers)
                {
                    mapStyleSettings_markers = true;
                    mapStyleSettingsTagsChanged(mapStyleSettings_markers);
                }
            }
            else if (mapStyleSettings_post)
            {
                mapStyleSettings_post = false;
                mapTagsPost_postPic.Image = Properties.Resources.Почта5;
                await Task.Delay(20);
                mapTagsPost_postPic.Image = Properties.Resources.Почта4;
                await Task.Delay(20);
                mapTagsPost_postPic.Image = Properties.Resources.Почта3;
                await Task.Delay(20);
                mapTagsPost_postPic.Image = Properties.Resources.Почта2;
                await Task.Delay(20);
                mapTagsPost_postPic.Image = Properties.Resources.Почта1;

                if (mapStyleSettings_markers && !mapStyleSettings_homes && !mapStyleSettings_apartments && !mapStyleSettings_organizations && !mapStyleSettings_post)
                {
                    mapStyleSettings_markers = false;
                    mapStyleSettingsTagsChanged(mapStyleSettings_markers);
                }
            }
        }

        private async void mapStyleSettingsPolygon_Pnl_MouseEnter(object sender, EventArgs e)
        {
            mapStyleSettingsPolygon_Pnl.PanelColor = Color.FromArgb(255, 255, 255); //1
            mapStyleSettingsPolygon_Pnl.PanelOutlineColor = Color.FromArgb(255, 255, 255);
            await Task.Delay(20);
            mapStyleSettingsPolygon_Pnl.PanelColor = Color.FromArgb(249, 249, 253); //2
            mapStyleSettingsPolygon_Pnl.PanelOutlineColor = Color.FromArgb(249, 249, 253);
            await Task.Delay(20);
            mapStyleSettingsPolygon_Pnl.PanelColor = Color.FromArgb(242, 243, 250); //3
            mapStyleSettingsPolygon_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
        }
        private async void mapStyleSettingsPolygon_Pnl_MouseLeave(object sender, EventArgs e)
        {
            mapStyleSettingsPolygon_Pnl.PanelColor = Color.FromArgb(242, 243, 250); //3
            mapStyleSettingsPolygon_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
            await Task.Delay(20);
            mapStyleSettingsPolygon_Pnl.PanelColor = Color.FromArgb(249, 249, 253); //2
            mapStyleSettingsPolygon_Pnl.PanelOutlineColor = Color.FromArgb(249, 249, 253);
            await Task.Delay(20);
            mapStyleSettingsPolygon_Pnl.PanelColor = Color.FromArgb(255, 255, 255); //1
            mapStyleSettingsPolygon_Pnl.PanelOutlineColor = Color.FromArgb(255, 255, 255);
        }
        private async void mapStyleSettingsBorder_Pnl_MouseEnter(object sender, EventArgs e)
        {
            mapStyleSettingsBorder_Pnl.PanelColor = Color.FromArgb(255, 255, 255); //1
            mapStyleSettingsBorder_Pnl.PanelOutlineColor = Color.FromArgb(255, 255, 255);
            await Task.Delay(20);
            mapStyleSettingsBorder_Pnl.PanelColor = Color.FromArgb(249, 249, 253); //2
            mapStyleSettingsBorder_Pnl.PanelOutlineColor = Color.FromArgb(249, 249, 253);
            await Task.Delay(20);
            mapStyleSettingsBorder_Pnl.PanelColor = Color.FromArgb(242, 243, 250); //3
            mapStyleSettingsBorder_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
        }

        private async void mapStyleSettingsBorder_Pnl_MouseLeave(object sender, EventArgs e)
        {
            mapStyleSettingsBorder_Pnl.PanelColor = Color.FromArgb(242, 243, 250); //3
            mapStyleSettingsBorder_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
            await Task.Delay(20);
            mapStyleSettingsBorder_Pnl.PanelColor = Color.FromArgb(249, 249, 253); //2
            mapStyleSettingsBorder_Pnl.PanelOutlineColor = Color.FromArgb(249, 249, 253);
            await Task.Delay(20);
            mapStyleSettingsBorder_Pnl.PanelColor = Color.FromArgb(255, 255, 255); //1
            mapStyleSettingsBorder_Pnl.PanelOutlineColor = Color.FromArgb(255, 255, 255);
        }

        private async void mapStyleSettingsTags_Pnl_MouseEnter(object sender, EventArgs e)
        {
            mapStyleSettingsTags_Pnl.PanelColor = Color.FromArgb(255, 255, 255); //1
            mapStyleSettingsTags_Pnl.PanelOutlineColor = Color.FromArgb(255, 255, 255);
            await Task.Delay(20);
            mapStyleSettingsTags_Pnl.PanelColor = Color.FromArgb(249, 249, 253); //2
            mapStyleSettingsTags_Pnl.PanelOutlineColor = Color.FromArgb(249, 249, 253);
            await Task.Delay(20);
            mapStyleSettingsTags_Pnl.PanelColor = Color.FromArgb(242, 243, 250); //3
            mapStyleSettingsTags_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
            mapStyleSettings_check = true;
        }

        private async void mapStyleSettingsTags_Pnl_MouseLeave(object sender, EventArgs e)
        {
            mapStyleSettingsTags_Pnl.PanelColor = Color.FromArgb(242, 243, 250); //3
            mapStyleSettingsTags_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
            await Task.Delay(20);
            mapStyleSettingsTags_Pnl.PanelColor = Color.FromArgb(249, 249, 253); //2
            mapStyleSettingsTags_Pnl.PanelOutlineColor = Color.FromArgb(249, 249, 253);
            await Task.Delay(20);
            mapStyleSettingsTags_Pnl.PanelColor = Color.FromArgb(255, 255, 255); //1
            mapStyleSettingsTags_Pnl.PanelOutlineColor = Color.FromArgb(255, 255, 255);
            mapStyleSettings_check = false;
        }

        private void mapStyleSettingsTagsCmb_Pnl_Click(object sender, EventArgs e)
        {
            mapStyle2_animationTmr.Start();
        }
        private async void mapStyleSettingsTagsCmb_Pnl_MouseEnter(object sender, EventArgs e)
        {
            mapStyleSettingsTags_Pnl.PanelColor = Color.FromArgb(255, 255, 255); //1
            mapStyleSettingsTags_Pnl.PanelOutlineColor = Color.FromArgb(255, 255, 255);
            await Task.Delay(20);
            mapStyleSettingsTags_Pnl.PanelColor = Color.FromArgb(249, 249, 253); //2
            mapStyleSettingsTags_Pnl.PanelOutlineColor = Color.FromArgb(249, 249, 253);
            await Task.Delay(20);
            mapStyleSettingsTags_Pnl.PanelColor = Color.FromArgb(242, 243, 250); //3
            mapStyleSettingsTags_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
        }
        private async void mapStyleSettingsTagsCmb_Pnl_MouseLeave(object sender, EventArgs e)
        {
            mapStyleSettingsTags_Pnl.PanelColor = Color.FromArgb(242, 243, 250); //3
            mapStyleSettingsTags_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
            await Task.Delay(20);
            mapStyleSettingsTags_Pnl.PanelColor = Color.FromArgb(249, 249, 253); //2
            mapStyleSettingsTags_Pnl.PanelOutlineColor = Color.FromArgb(249, 249, 253);
            await Task.Delay(20);
            mapStyleSettingsTags_Pnl.PanelColor = Color.FromArgb(255, 255, 255); //1
            mapStyleSettingsTags_Pnl.PanelOutlineColor = Color.FromArgb(255, 255, 255);
        }
        private void mapStyle2_animationTmr_Tick(object sender, EventArgs e)
        {
            int step;
            int diff;
            if (mapStyle2_animation)
            {
                diff = mapStyle_Pnl.Height - mapStyle_Pnl.MinimumSize.Height;
                step = Math.Max(1, diff / 4);
                mapStyle_Pnl.Height -= step;
                mapZoom_Pnl.Top -= step;
                mapLocation_Pnl.Top -= step;
                mapStyleHide_Pnl.Visible = true;
                mapStyleSettingsTagsCmb_arrowPic.Image = Properties.Resources.КомбоБокс2;
                if (mapStyle_Pnl.Height <= mapStyle_Pnl.MinimumSize.Height)
                {
                    mapStyle_Pnl.Height = mapStyle_Pnl.MinimumSize.Height;
                    mapStyle2_animation = false;
                    mapStyle2_animationTmr.Stop();

                    if (_closingForHide)
                    {
                        _closingForHide = false;
                        if (!IsMouseOverPanelHierarchy())
                            HideMapStylePanel();
                    }
                }
            }
            else
            {
                diff = mapStyle_Pnl.MaximumSize.Height - mapStyle_Pnl.Height;
                step = Math.Max(1, diff / 4);
                mapStyle_Pnl.Height += step;
                mapZoom_Pnl.Top += step;
                mapLocation_Pnl.Top += step;
                mapStyleHide_Pnl.Visible = false;
                mapStyleSettingsTagsCmb_arrowPic.Image = Properties.Resources.КомбоБокс1;
                if (mapStyle_Pnl.Height >= mapStyle_Pnl.MaximumSize.Height)
                {
                    mapStyle_Pnl.Height = mapStyle_Pnl.MaximumSize.Height;
                    mapStyle2_animation = true;
                    mapStyle2_animationTmr.Stop();
                }
            }
        }
        private async void mapTagsHome_Pnl_MouseEnter(object sender, EventArgs e)
        {
            mapTagsHome_Pnl.PanelColor = Color.FromArgb(255, 255, 255); //1
            mapTagsHome_Pnl.PanelOutlineColor = Color.FromArgb(255, 255, 255);
            await Task.Delay(20);
            mapTagsHome_Pnl.PanelColor = Color.FromArgb(249, 249, 253); //2
            mapTagsHome_Pnl.PanelOutlineColor = Color.FromArgb(249, 249, 253);
            await Task.Delay(20);
            mapTagsHome_Pnl.PanelColor = Color.FromArgb(242, 243, 250); //3
            mapTagsHome_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
        }
        private async void mapTagsHome_Pnl_MouseLeave(object sender, EventArgs e)
        {
            mapTagsHome_Pnl.PanelColor = Color.FromArgb(242, 243, 250); //3
            mapTagsHome_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
            await Task.Delay(20);
            mapTagsHome_Pnl.PanelColor = Color.FromArgb(249, 249, 253); //2
            mapTagsHome_Pnl.PanelOutlineColor = Color.FromArgb(249, 249, 253);
            await Task.Delay(20);
            mapTagsHome_Pnl.PanelColor = Color.FromArgb(255, 255, 255); //1
            mapTagsHome_Pnl.PanelOutlineColor = Color.FromArgb(255, 255, 255);
        }


        private async void mapTagsApartment_Pnl_MouseEnter(object sender, EventArgs e)
        {
            mapTagsApartment_Pnl.PanelColor = Color.FromArgb(255, 255, 255); //1
            mapTagsApartment_Pnl.PanelOutlineColor = Color.FromArgb(255, 255, 255);
            await Task.Delay(20);
            mapTagsApartment_Pnl.PanelColor = Color.FromArgb(249, 249, 253); //2
            mapTagsApartment_Pnl.PanelOutlineColor = Color.FromArgb(249, 249, 253);
            await Task.Delay(20);
            mapTagsApartment_Pnl.PanelColor = Color.FromArgb(242, 243, 250); //3
            mapTagsApartment_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
        }

        private async void mapTagsApartment_Pnl_MouseLeave(object sender, EventArgs e)
        {
            mapTagsApartment_Pnl.PanelColor = Color.FromArgb(242, 243, 250); //3
            mapTagsApartment_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
            await Task.Delay(20);
            mapTagsApartment_Pnl.PanelColor = Color.FromArgb(249, 249, 253); //2
            mapTagsApartment_Pnl.PanelOutlineColor = Color.FromArgb(249, 249, 253);
            await Task.Delay(20);
            mapTagsApartment_Pnl.PanelColor = Color.FromArgb(255, 255, 255); //1
            mapTagsApartment_Pnl.PanelOutlineColor = Color.FromArgb(255, 255, 255);
        }


        private async void mapTagsOrganizations_Pnl_MouseEnter(object sender, EventArgs e)
        {
            mapTagsOrganizations_Pnl.PanelColor = Color.FromArgb(255, 255, 255); //1
            mapTagsOrganizations_Pnl.PanelOutlineColor = Color.FromArgb(255, 255, 255);
            await Task.Delay(20);
            mapTagsOrganizations_Pnl.PanelColor = Color.FromArgb(249, 249, 253); //2
            mapTagsOrganizations_Pnl.PanelOutlineColor = Color.FromArgb(249, 249, 253);
            await Task.Delay(20);
            mapTagsOrganizations_Pnl.PanelColor = Color.FromArgb(242, 243, 250); //3
            mapTagsOrganizations_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
        }

        private async void mapTagsOrganizations_Pnl_MouseLeave(object sender, EventArgs e)
        {
            mapTagsOrganizations_Pnl.PanelColor = Color.FromArgb(242, 243, 250); //3
            mapTagsOrganizations_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
            await Task.Delay(20);
            mapTagsOrganizations_Pnl.PanelColor = Color.FromArgb(249, 249, 253); //2
            mapTagsOrganizations_Pnl.PanelOutlineColor = Color.FromArgb(249, 249, 253);
            await Task.Delay(20);
            mapTagsOrganizations_Pnl.PanelColor = Color.FromArgb(255, 255, 255); //1
            mapTagsOrganizations_Pnl.PanelOutlineColor = Color.FromArgb(255, 255, 255);
        }


        private async void mapTagsPost_Pnl_MouseEnter(object sender, EventArgs e)
        {
            mapTagsPost_Pnl.PanelColor = Color.FromArgb(255, 255, 255); //1
            mapTagsPost_Pnl.PanelOutlineColor = Color.FromArgb(255, 255, 255);
            await Task.Delay(20);
            mapTagsPost_Pnl.PanelColor = Color.FromArgb(249, 249, 253); //2
            mapTagsPost_Pnl.PanelOutlineColor = Color.FromArgb(249, 249, 253);
            await Task.Delay(20);
            mapTagsPost_Pnl.PanelColor = Color.FromArgb(242, 243, 250); //3
            mapTagsPost_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
        }

        private async void mapTagsPost_Pnl_MouseLeave(object sender, EventArgs e)
        {
            mapTagsPost_Pnl.PanelColor = Color.FromArgb(242, 243, 250); //3
            mapTagsPost_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
            await Task.Delay(20);
            mapTagsPost_Pnl.PanelColor = Color.FromArgb(249, 249, 253); //2
            mapTagsPost_Pnl.PanelOutlineColor = Color.FromArgb(249, 249, 253);
            await Task.Delay(20);
            mapTagsPost_Pnl.PanelColor = Color.FromArgb(255, 255, 255); //1
            mapTagsPost_Pnl.PanelOutlineColor = Color.FromArgb(255, 255, 255);
        }

        private async void HideMapStylePanel()
        {
            mapStyle1_animationTmr.Start();
            mapToolsDrop_dropPic.Image = Properties.Resources.СтильКарты5;
            await Task.Delay(20);
            mapToolsDrop_dropPic.Image = Properties.Resources.СтильКарты4;
            await Task.Delay(20);
            mapToolsDrop_dropPic.Image = Properties.Resources.СтильКарты3;
            await Task.Delay(20);
            mapToolsDrop_dropPic.Image = Properties.Resources.СтильКарты2;
            await Task.Delay(20);
            mapToolsDrop_dropPic.Image = Properties.Resources.СтильКарты1;
        }

        private void mapRoutePanelButton1_1_Click(object sender, EventArgs e)
        {

        }

        private void mapRouteTaskShort_Pnl_Click(object sender, EventArgs e)
        {

        }

        private void mapRouteTaskShort_Pnl_MouseEnter(object sender, EventArgs e)
        {

        }

        private void mapRouteTaskShort_Pnl_MouseLeave(object sender, EventArgs e)
        {

        }

        private void mapRouteTaskFull_Pnl_Click(object sender, EventArgs e)
        {

        }

        private void mapRouteTaskFull_Pnl_MouseEnter(object sender, EventArgs e)
        {

        }

        private void mapRouteTaskFull_Pnl_MouseLeave(object sender, EventArgs e)
        {

        }

        private void mapRouteTaskBike_Pnl_Click(object sender, EventArgs e)
        {

        }

        private void mapRouteTaskBike_Pnl_MouseEnter(object sender, EventArgs e)
        {

        }

        private void mapRouteTaskBike_Pnl_MouseLeave(object sender, EventArgs e)
        {

        }

        private void mapRouteTaskWalk_Pnl_Click(object sender, EventArgs e)
        {

        }

        private void mapRouteTaskWalk_Pnl_MouseEnter(object sender, EventArgs e)
        {

        }

        private void mapRouteTaskWalk_Pnl_MouseLeave(object sender, EventArgs e)
        {

        }

        private void mapInfoUpper_Pnl_Click(object sender, EventArgs e)
        {
            mapRoute = !mapRoute;
            mapInfo_animationTmr.Start();
        }

        private void mapInfoTagsDelete_Pnl_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Сбросить");
        }

        private async void mapInfoTagsDelete_Pnl_MouseEnter(object sender, EventArgs e)
        {
            mapInfoTagsDelete_Pnl.PanelColor = Color.FromArgb(242, 243, 250); //1
            mapInfoTagsDelete_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
            mapInfoTagsDelete_deletePic.Image = Properties.Resources.Сброс1;
            await Task.Delay(20);
            mapInfoTagsDelete_Pnl.PanelColor = Color.FromArgb(240, 195, 204); //2
            mapInfoTagsDelete_Pnl.PanelOutlineColor = Color.FromArgb(240, 195, 204);
            mapInfoTagsDelete_deletePic.Image = Properties.Resources.Сброс2;
            await Task.Delay(20);
            mapInfoTagsDelete_Pnl.PanelColor = Color.FromArgb(237, 147, 158); //3
            mapInfoTagsDelete_Pnl.PanelOutlineColor = Color.FromArgb(237, 147, 158);
            mapInfoTagsDelete_deletePic.Image = Properties.Resources.Сброс3;
            await Task.Delay(20);
            mapInfoTagsDelete_Pnl.PanelColor = Color.FromArgb(235, 98, 111);  //4
            mapInfoTagsDelete_Pnl.PanelOutlineColor = Color.FromArgb(235, 98, 111);
            mapInfoTagsDelete_deletePic.Image = Properties.Resources.Сброс4;
            await Task.Delay(20);
            mapInfoTagsDelete_Pnl.PanelColor = Color.FromArgb(232, 50, 65);  //5
            mapInfoTagsDelete_Pnl.PanelOutlineColor = Color.FromArgb(232, 50, 65);
            mapInfoTagsDelete_deletePic.Image = Properties.Resources.Сброс5;
        }

        private async void mapInfoTagsDelete_Pnl_MouseLeave(object sender, EventArgs e)
        {
            mapInfoTagsDelete_Pnl.PanelColor = Color.FromArgb(232, 50, 65);  //5
            mapInfoTagsDelete_Pnl.PanelOutlineColor = Color.FromArgb(232, 50, 65);
            mapInfoTagsDelete_deletePic.Image = Properties.Resources.Сброс5;
            await Task.Delay(20);
            mapInfoTagsDelete_Pnl.PanelColor = Color.FromArgb(235, 98, 111);  //4
            mapInfoTagsDelete_Pnl.PanelOutlineColor = Color.FromArgb(235, 98, 111);
            mapInfoTagsDelete_deletePic.Image = Properties.Resources.Сброс4;
            await Task.Delay(20);
            mapInfoTagsDelete_Pnl.PanelColor = Color.FromArgb(237, 147, 158); //3
            mapInfoTagsDelete_Pnl.PanelOutlineColor = Color.FromArgb(237, 147, 158);
            mapInfoTagsDelete_deletePic.Image = Properties.Resources.Сброс3;
            await Task.Delay(20);
            mapInfoTagsDelete_Pnl.PanelColor = Color.FromArgb(240, 195, 204); //2
            mapInfoTagsDelete_Pnl.PanelOutlineColor = Color.FromArgb(240, 195, 204);
            mapInfoTagsDelete_deletePic.Image = Properties.Resources.Сброс2;
            await Task.Delay(20);
            mapInfoTagsDelete_Pnl.PanelColor = Color.FromArgb(242, 243, 250); //1
            mapInfoTagsDelete_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
            mapInfoTagsDelete_deletePic.Image = Properties.Resources.Сброс1;
        }

        private void mapInfoTagsDone_Pnl_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Принять");
        }

        private void mapInfoSearch_animationTmr_Tick(object sender, EventArgs e)
        {
            int step;
            int diff;
            if (mapInfoSearch_animation)
            {
                diff = mapInfoSearch_Pnl.Height - mapInfoSearch_Pnl.MinimumSize.Height;
                step = Math.Max(2, diff / 5);
                mapInfoSearch_Pnl.Height -= step;
                if (mapInfoSearch_Pnl.Height <= mapInfoSearch_Pnl.MinimumSize.Height)
                {
                    mapInfoSearch_Pnl.Height = mapInfoSearch_Pnl.MinimumSize.Height;
                    mapInfoSearch_animation = false;
                    mapInfoSearch_animationTmr.Stop();
                }
            }
            else
            {
                diff = mapInfoSearch_Pnl.MaximumSize.Height - mapInfoSearch_Pnl.Height;
                step = Math.Max(2, diff / 5);
                mapInfoSearch_Pnl.Height += step;
                if (mapInfoSearch_Pnl.Height >= mapInfoSearch_Pnl.MaximumSize.Height)
                {
                    mapInfoSearch_Pnl.Height = mapInfoSearch_Pnl.MaximumSize.Height;
                    mapInfoSearch_animation = true;
                    mapInfoSearch_animationTmr.Stop();
                }
            }
        }
        private void mapInfoRegionTxt_postTxt_Click(object sender, EventArgs e)
        {

        }
        private void mapInfoRegion_animationTmr_Tick(object sender, EventArgs e)
        {
            int step;
            int diff;
            if (mapInfoRegion_animation)
            {
                diff = mapInfoRegion_Pnl.Height - mapInfoRegion_Pnl.MinimumSize.Height;
                step = Math.Max(2, diff / 5);
                mapInfoRegion_Pnl.Height -= step;
                if (mapInfoRegion_Pnl.Height <= mapInfoRegion_Pnl.MinimumSize.Height)
                {
                    mapInfoRegion_Pnl.Height = mapInfoRegion_Pnl.MinimumSize.Height;
                    mapInfoRegion_animation = false;
                    mapInfoRegion_animationTmr.Stop();
                }
            }
            else
            {
                diff = mapInfoRegion_Pnl.MaximumSize.Height - mapInfoRegion_Pnl.Height;
                step = Math.Max(2, diff / 5);
                mapInfoRegion_Pnl.Height += step;
                if (mapInfoRegion_Pnl.Height >= mapInfoRegion_Pnl.MaximumSize.Height)
                {
                    mapInfoRegion_Pnl.Height = mapInfoRegion_Pnl.MaximumSize.Height;
                    mapInfoRegion_animation = true;
                    mapInfoRegion_animationTmr.Stop();
                }
            }
        }

        private void mapInfoTravel_animationTmr_Tick(object sender, EventArgs e)
        {
            int step;
            int diff;
            if (mapInfoTravel_animation)
            {
                diff = mapInfo_Pnl.Height - mapInfo_Pnl.MinimumSize.Height;
                step = Math.Max(2, diff / 5);
                mapInfo_Pnl.Height -= step;
                if (mapInfo_Pnl.Height <= mapInfo_Pnl.MinimumSize.Height)
                {
                    mapInfoSearch_Pnl.Height = mapInfoSearch_Pnl.MinimumSize.Height;
                    mapInfoTravel_animation = false;
                    mapInfoTravel_animationTmr.Stop();
                }
            }
            else
            {
                diff = mapInfo_Pnl.MaximumSize.Height - mapInfo_Pnl.Height;
                step = Math.Max(2, diff / 5);
                mapInfo_Pnl.Height += step;
                if (mapInfo_Pnl.Height >= mapInfo_Pnl.MaximumSize.Height)
                {
                    mapInfo_Pnl.Height = mapInfo_Pnl.MaximumSize.Height;
                    mapInfoTravel_animation = true;
                    mapInfoTravel_animationTmr.Stop();
                }
            }
        }

        private void mapInfoRegionEdit_Pnl_Click(object sender, EventArgs e)
        {
            if (mapInfoRegionTxt_check == false)
            {
                mapInfoRegionTxt_check = true;
                mapInfoRegionTxt_postTxt.Enabled = true;
                mapInfoRegionTxt_postTxt.BackgroundColor = Color.White;
                mapInfoRegionTxt_postTxt.OutlineColor = Color.White;
                mapInfoRegionEdit_Pnl.PanelColor = Color.FromArgb(25, 55, 255);
                mapInfoRegionEdit_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
                mapInfoSearch_animationTmr.Start();
                mapInfoRegion_animationTmr.Start();
                mapInfoTravel_animationTmr.Start();
                mapInfoRegionEdit_editPic.Image = Properties.Resources.Изменить2;
            }
            else
            {
                mapInfoRegionTxt_check = false;
                mapInfoRegionTxt_postTxt.Enabled = false;
                mapInfoRegionTxt_postTxt.BackgroundColor = Color.FromArgb(242, 243, 250);
                mapInfoRegionTxt_postTxt.OutlineColor = Color.FromArgb(242, 243, 250);
                mapInfoRegionEdit_Pnl.PanelColor = Color.White;
                mapInfoRegionEdit_Pnl.PanelOutlineColor = Color.FromArgb(214, 216, 235);
                mapInfoSearch_animationTmr.Start();
                mapInfoRegion_animationTmr.Start();
                mapInfoTravel_animationTmr.Start();
                mapInfoRegionEdit_editPic.Image = Properties.Resources.Изменить1;
            }
        }

        private async void mapInfoRegionEdit_Pnl_MouseEnter(object sender, EventArgs e)
        {
            if (mapInfoRegionTxt_check == false)
            {
                mapInfoRegionEdit_Pnl.PanelColor = Color.FromArgb(255, 255, 255); //1
                mapInfoRegionEdit_Pnl.PanelOutlineColor = Color.FromArgb(214, 216, 235);
                await Task.Delay(20);
                mapInfoRegionEdit_Pnl.PanelColor = Color.FromArgb(250, 251, 255); //2
                await Task.Delay(20);
                mapInfoRegionEdit_Pnl.PanelColor = Color.FromArgb(244, 248, 254); //3
                await Task.Delay(20);
                mapInfoRegionEdit_Pnl.PanelColor = Color.FromArgb(239, 244, 254); //4
            }
            else
            {
                mapInfoRegionEdit_Pnl.PanelColor = Color.FromArgb(25, 55, 255); //1
                mapInfoRegionEdit_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
                await Task.Delay(20);
                mapInfoRegionEdit_Pnl.PanelColor = Color.FromArgb(26, 54, 244); //2
                mapInfoRegionEdit_Pnl.PanelOutlineColor = Color.FromArgb(26, 54, 244);
                await Task.Delay(20);
                mapInfoRegionEdit_Pnl.PanelColor = Color.FromArgb(26, 52, 232); //3
                mapInfoRegionEdit_Pnl.PanelOutlineColor = Color.FromArgb(26, 52, 232);
            }
        }
        private async void mapInfoRegionEdit_Pnl_MouseLeave(object sender, EventArgs e)
        {
            if (mapInfoRegionTxt_check == false)
            {
                mapInfoRegionEdit_Pnl.PanelColor = Color.FromArgb(239, 244, 254); //4
                mapInfoRegionEdit_Pnl.PanelOutlineColor = Color.FromArgb(214, 216, 235);
                await Task.Delay(20);
                mapInfoRegionEdit_Pnl.PanelColor = Color.FromArgb(244, 248, 254); //3
                await Task.Delay(20);
                mapInfoRegionEdit_Pnl.PanelColor = Color.FromArgb(250, 251, 255); //2
                await Task.Delay(20);
                mapInfoRegionEdit_Pnl.PanelColor = Color.FromArgb(255, 255, 255); //1
            }
            else
            {
                mapInfoRegionEdit_Pnl.PanelColor = Color.FromArgb(26, 52, 232); //3
                mapInfoRegionEdit_Pnl.PanelOutlineColor = Color.FromArgb(26, 52, 232);
                await Task.Delay(20);
                mapInfoRegionEdit_Pnl.PanelColor = Color.FromArgb(26, 54, 244); //2
                mapInfoRegionEdit_Pnl.PanelOutlineColor = Color.FromArgb(26, 54, 244);
                await Task.Delay(20);
                mapInfoRegionEdit_Pnl.PanelColor = Color.FromArgb(25, 55, 255); //1
                mapInfoRegionEdit_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
            }
        }
        private void issueTransTaskTypeMission1Ordinary_Pnl_Click(object sender, EventArgs e)
        {
            issueTransTaskType_btn = false;

            issueTransTaskTypeMission1Ordinary_ordinaryLbl.ForeColor = Color.FromArgb(26, 52, 232);
            issueTransTaskTypeMission2Tags_tagsLbl.ForeColor = Color.FromArgb(49, 50, 60);

            issueTransTaskTypeMission1Ordinary_Pnl.PanelColor = Color.FromArgb(242, 243, 250);
            issueTransTaskTypeMission1Ordinary_Pnl.PanelOutlineColor = Color.FromArgb(26, 52, 232);

            issueTransTaskTypeMission2Tags_Pnl.PanelColor = Color.FromArgb(242, 243, 250);
            issueTransTaskTypeMission2Tags_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);

            issueTransTaskTypeMission1Ordinary_ordinaryPic.Image = Properties.Resources.ЗаданиеОбычный2;
            issueTransTaskTypeMission2Tags_tagsPic.Image = Properties.Resources.ЗаданиеМетка1;
            issueTransTaskTypeMission1Ordinary_Pnl.Cursor = Cursors.Arrow;
            issueTransTaskTypeMission2Tags_Pnl.Cursor = Cursors.Hand;
            issueTransTaskTypeMission1Ordinary_Pnl.Enabled = false;
            issueTransTaskTypeMission2Tags_Pnl.Enabled = true;
        }

        private async void issueTransTaskTypeMission1Ordinary_Pnl_MouseEnter(object sender, EventArgs e)
        {
            if (issueTransTaskType_btn == true)
            {
                issueTransTaskTypeMission1Ordinary_Pnl.PanelColor = Color.FromArgb(242, 243, 250); //1
                issueTransTaskTypeMission1Ordinary_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
                await Task.Delay(20);
                issueTransTaskTypeMission1Ordinary_Pnl.PanelColor = Color.FromArgb(240, 240, 248); //2
                issueTransTaskTypeMission1Ordinary_Pnl.PanelOutlineColor = Color.FromArgb(240, 240, 248);
                await Task.Delay(20);
                issueTransTaskTypeMission1Ordinary_Pnl.PanelColor = Color.FromArgb(237, 238, 246); //3
                issueTransTaskTypeMission1Ordinary_Pnl.PanelOutlineColor = Color.FromArgb(237, 238, 246);
                await Task.Delay(20);
                issueTransTaskTypeMission1Ordinary_Pnl.PanelColor = Color.FromArgb(235, 235, 244); //4
                issueTransTaskTypeMission1Ordinary_Pnl.PanelOutlineColor = Color.FromArgb(235, 235, 244);
            }
        }

        private async void issueTransTaskTypeMission1Ordinary_Pnl_MouseLeave(object sender, EventArgs e)
        {
            if (issueTransTaskType_btn == true)
            {
                issueTransTaskTypeMission1Ordinary_Pnl.PanelColor = Color.FromArgb(235, 235, 244); //4
                issueTransTaskTypeMission1Ordinary_Pnl.PanelOutlineColor = Color.FromArgb(235, 235, 244);
                await Task.Delay(20);
                issueTransTaskTypeMission1Ordinary_Pnl.PanelColor = Color.FromArgb(237, 238, 246); //3
                issueTransTaskTypeMission1Ordinary_Pnl.PanelOutlineColor = Color.FromArgb(237, 238, 246);
                await Task.Delay(20);
                issueTransTaskTypeMission1Ordinary_Pnl.PanelColor = Color.FromArgb(240, 240, 248); //2
                issueTransTaskTypeMission1Ordinary_Pnl.PanelOutlineColor = Color.FromArgb(240, 240, 248);
                await Task.Delay(20);
                issueTransTaskTypeMission1Ordinary_Pnl.PanelColor = Color.FromArgb(242, 243, 250); //1
                issueTransTaskTypeMission1Ordinary_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
            }
        }

        private void issueTransTaskTypeMission2Tags_Pnl_Click(object sender, EventArgs e)
        {
            issueTransTaskType_btn = true;

            issueTransTaskTypeMission2Tags_tagsLbl.ForeColor = Color.FromArgb(26, 52, 232);
            issueTransTaskTypeMission1Ordinary_ordinaryLbl.ForeColor = Color.FromArgb(49, 50, 60);
            
            issueTransTaskTypeMission2Tags_Pnl.PanelColor = Color.FromArgb(242, 243, 250);
            issueTransTaskTypeMission2Tags_Pnl.PanelOutlineColor = Color.FromArgb(26, 52, 232);

            issueTransTaskTypeMission1Ordinary_Pnl.PanelColor = Color.FromArgb(242, 243, 250);
            issueTransTaskTypeMission1Ordinary_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);

            issueTransTaskTypeMission1Ordinary_ordinaryPic.Image = Properties.Resources.ЗаданиеОбычный1;
            issueTransTaskTypeMission2Tags_tagsPic.Image = Properties.Resources.ЗаданиеМетка2;
            issueTransTaskTypeMission1Ordinary_Pnl.Cursor = Cursors.Hand;
            issueTransTaskTypeMission2Tags_Pnl.Cursor = Cursors.Arrow;
            issueTransTaskTypeMission1Ordinary_Pnl.Enabled = true;
            issueTransTaskTypeMission2Tags_Pnl.Enabled = false;
        }
        private async void issueTransTaskTypeMission2Tags_Pnl_MouseEnter(object sender, EventArgs e)
        {
            if (issueTransTaskType_btn == false)
            {
                issueTransTaskTypeMission2Tags_Pnl.PanelColor = Color.FromArgb(242, 243, 250); //1
                issueTransTaskTypeMission2Tags_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
                await Task.Delay(20);
                issueTransTaskTypeMission2Tags_Pnl.PanelColor = Color.FromArgb(240, 240, 248); //2
                issueTransTaskTypeMission2Tags_Pnl.PanelOutlineColor = Color.FromArgb(240, 240, 248);
                await Task.Delay(20);
                issueTransTaskTypeMission2Tags_Pnl.PanelColor = Color.FromArgb(237, 238, 246); //3
                issueTransTaskTypeMission2Tags_Pnl.PanelOutlineColor = Color.FromArgb(237, 238, 246);
                await Task.Delay(20);
                issueTransTaskTypeMission2Tags_Pnl.PanelColor = Color.FromArgb(235, 235, 244); //4
                issueTransTaskTypeMission2Tags_Pnl.PanelOutlineColor = Color.FromArgb(235, 235, 244);
            }
        }
        private async void issueTransTaskTypeMission2Tags_Pnl_MouseLeave(object sender, EventArgs e)
        {
            if (issueTransTaskType_btn == false)
            {
                issueTransTaskTypeMission2Tags_Pnl.PanelColor = Color.FromArgb(235, 235, 244); //4
                issueTransTaskTypeMission2Tags_Pnl.PanelOutlineColor = Color.FromArgb(235, 235, 244);
                await Task.Delay(20);
                issueTransTaskTypeMission2Tags_Pnl.PanelColor = Color.FromArgb(237, 238, 246); //3
                issueTransTaskTypeMission2Tags_Pnl.PanelOutlineColor = Color.FromArgb(237, 238, 246);
                await Task.Delay(20);
                issueTransTaskTypeMission2Tags_Pnl.PanelColor = Color.FromArgb(240, 240, 248); //2
                issueTransTaskTypeMission2Tags_Pnl.PanelOutlineColor = Color.FromArgb(240, 240, 248);
                await Task.Delay(20);
                issueTransTaskTypeMission2Tags_Pnl.PanelColor = Color.FromArgb(242, 243, 250); //1
                issueTransTaskTypeMission2Tags_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
            }
        }
        private void issueTransTaskPattern1_Pnl_Click(object sender, EventArgs e)
        {
            issueTransTaskTxt_taskTxt.Content = "[Имя], начните выполнять прямо сейчас!";
        }
        private async void issueTransTaskPattern1_Pnl_MouseEnter(object sender, EventArgs e)
        {
            issueTransTaskPattern1_Pnl.PanelColor = Color.FromArgb(255, 255, 255); //1
            await Task.Delay(20);
            issueTransTaskPattern1_Pnl.PanelColor = Color.FromArgb(250, 251, 255); //2
            await Task.Delay(20);
            issueTransTaskPattern1_Pnl.PanelColor = Color.FromArgb(244, 248, 254); //3
            await Task.Delay(20);
            issueTransTaskPattern1_Pnl.PanelColor = Color.FromArgb(239, 244, 254); //4
        }
        private async void issueTransTaskPattern1_Pnl_MouseLeave(object sender, EventArgs e)
        {
            issueTransTaskPattern1_Pnl.PanelColor = Color.FromArgb(239, 244, 254); //4
            await Task.Delay(20);
            issueTransTaskPattern1_Pnl.PanelColor = Color.FromArgb(244, 248, 254); //3
            await Task.Delay(20);
            issueTransTaskPattern1_Pnl.PanelColor = Color.FromArgb(250, 251, 255); //2
            await Task.Delay(20);
            issueTransTaskPattern1_Pnl.PanelColor = Color.FromArgb(255, 255, 255); //1
        }

        private void issueTransTaskPattern2_Pnl_Click(object sender, EventArgs e)
        {
            issueTransTaskTxt_taskTxt.Content = "[Имя], срочно начните выполнять задание!";
        }

        private async void issueTransTaskPattern2_Pnl_MouseEnter(object sender, EventArgs e)
        {
            issueTransTaskPattern2_Pnl.PanelColor = Color.FromArgb(255, 255, 255); //1
            await Task.Delay(20);
            issueTransTaskPattern2_Pnl.PanelColor = Color.FromArgb(250, 251, 255); //2
            await Task.Delay(20);
            issueTransTaskPattern2_Pnl.PanelColor = Color.FromArgb(244, 248, 254); //3
            await Task.Delay(20);
            issueTransTaskPattern2_Pnl.PanelColor = Color.FromArgb(239, 244, 254); //4
        }
        private async void issueTransTaskPattern2_Pnl_MouseLeave(object sender, EventArgs e)
        {
            issueTransTaskPattern2_Pnl.PanelColor = Color.FromArgb(239, 244, 254); //4
            await Task.Delay(20);
            issueTransTaskPattern2_Pnl.PanelColor = Color.FromArgb(244, 248, 254); //3
            await Task.Delay(20);
            issueTransTaskPattern2_Pnl.PanelColor = Color.FromArgb(250, 251, 255); //2
            await Task.Delay(20);
            issueTransTaskPattern2_Pnl.PanelColor = Color.FromArgb(255, 255, 255); //1
        }

        private void issueTransTaskBottomClean_Tlp_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Отмена");
        }
        private async void issueTransTaskBottomClean_Tlp_MouseEnter(object sender, EventArgs e)
        {
            issueTransTaskBottomClean_Tlp.PanelColor = Color.FromArgb(242, 243, 250); //1
            issueTransTaskBottomClean_Tlp.PanelOutlineColor = Color.FromArgb(242, 243, 250);
            await Task.Delay(20);
            issueTransTaskBottomClean_Tlp.PanelColor = Color.FromArgb(240, 240, 248); //2
            issueTransTaskBottomClean_Tlp.PanelOutlineColor = Color.FromArgb(240, 240, 248);
            await Task.Delay(20);
            issueTransTaskBottomClean_Tlp.PanelColor = Color.FromArgb(237, 238, 246); //3
            issueTransTaskBottomClean_Tlp.PanelOutlineColor = Color.FromArgb(237, 238, 246);
            await Task.Delay(20);
            issueTransTaskBottomClean_Tlp.PanelColor = Color.FromArgb(235, 235, 244); //4
            issueTransTaskBottomClean_Tlp.PanelOutlineColor = Color.FromArgb(235, 235, 244);
        }
        private async void issueTransTaskBottomClean_Tlp_MouseLeave(object sender, EventArgs e)
        {
            issueTransTaskBottomClean_Tlp.PanelColor = Color.FromArgb(235, 235, 244); //4
            issueTransTaskBottomClean_Tlp.PanelOutlineColor = Color.FromArgb(235, 235, 244);
            await Task.Delay(20);
            issueTransTaskBottomClean_Tlp.PanelColor = Color.FromArgb(237, 238, 246); //3
            issueTransTaskBottomClean_Tlp.PanelOutlineColor = Color.FromArgb(237, 238, 246);
            await Task.Delay(20);
            issueTransTaskBottomClean_Tlp.PanelColor = Color.FromArgb(240, 240, 248); //2
            issueTransTaskBottomClean_Tlp.PanelOutlineColor = Color.FromArgb(240, 240, 248);
            await Task.Delay(20);
            issueTransTaskBottomClean_Tlp.PanelColor = Color.FromArgb(242, 243, 250); //1
            issueTransTaskBottomClean_Tlp.PanelOutlineColor = Color.FromArgb(242, 243, 250);
        }
        //----------------------------------------------------------------------------------------------------
        private void issueTransStaffUpperAll_Pnl_Click(object sender, EventArgs e)
        {
            issueTransStaffUpper_btn1 = true;
            issueTransStaffUpperAll_Pnl.PanelColor = Color.FromArgb(25, 55, 255);
            issueTransStaffUpperAll_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
            issueTransStaffUpperAll_allLbl.ForeColor = Color.White;

            issueTransStaffUpper_btn2 = false;
            issueTransStaffUpperPost_Pnl.PanelColor = Color.White;
            issueTransStaffUpperPost_Pnl.PanelOutlineColor = Color.FromArgb(214, 216, 235);
            issueTransStaffUpperPost_postLbl.ForeColor = Color.FromArgb(49, 50, 60);

            issueTransStaffUpper_btn3 = false;
            issueTransStaffUpperOperator_Pnl.PanelColor = Color.White;
            issueTransStaffUpperOperator_Pnl.PanelOutlineColor = Color.FromArgb(214, 216, 235);
            issueTransStaffUpperOperator_operatorLbl.ForeColor = Color.FromArgb(49, 50, 60);

            issueTransStaffUpper_btn4 = false;
            issueTransStaffUpperManager_Pnl.PanelColor = Color.White;
            issueTransStaffUpperManager_Pnl.PanelOutlineColor = Color.FromArgb(214, 216, 235);
            issueTransStaffUpperManager_managerLbl.ForeColor = Color.FromArgb(49, 50, 60);
        }
        private async void issueTransStaffUpperAll_Pnl_MouseEnter(object sender, EventArgs e)
        {
            if (issueTransStaffUpper_btn1 == true)
            {
                issueTransStaffUpperAll_Pnl.PanelColor = Color.FromArgb(25, 55, 255); //1
                issueTransStaffUpperAll_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
                issueTransStaffUpperAll_allLbl.ForeColor = Color.White;
                await Task.Delay(20);
                issueTransStaffUpperAll_Pnl.PanelColor = Color.FromArgb(26, 54, 244); //2
                issueTransStaffUpperAll_Pnl.PanelOutlineColor = Color.FromArgb(26, 54, 244);
                await Task.Delay(20);
                issueTransStaffUpperAll_Pnl.PanelColor = Color.FromArgb(26, 52, 232); //3
                issueTransStaffUpperAll_Pnl.PanelOutlineColor = Color.FromArgb(26, 52, 232);
            }
            else
            {
                issueTransStaffUpperAll_Pnl.PanelColor = Color.FromArgb(255, 255, 255); //1
                issueTransStaffUpperAll_Pnl.PanelOutlineColor = Color.FromArgb(214, 216, 235);
                issueTransStaffUpperAll_allLbl.ForeColor = Color.FromArgb(49, 50, 60);
                await Task.Delay(20);
                issueTransStaffUpperAll_Pnl.PanelColor = Color.FromArgb(250, 251, 255); //2
                await Task.Delay(20);
                issueTransStaffUpperAll_Pnl.PanelColor = Color.FromArgb(244, 248, 254); //3
                await Task.Delay(20);
                issueTransStaffUpperAll_Pnl.PanelColor = Color.FromArgb(239, 244, 254); //4
            }
        }
        private async void issueTransStaffUpperAll_Pnl_MouseLeave(object sender, EventArgs e)
        {
            if (issueTransStaffUpper_btn1 == true)
            {
                issueTransStaffUpperAll_Pnl.PanelColor = Color.FromArgb(26, 52, 232); //3
                issueTransStaffUpperAll_Pnl.PanelOutlineColor = Color.FromArgb(26, 52, 232);
                issueTransStaffUpperAll_allLbl.ForeColor = Color.White;
                await Task.Delay(20);
                issueTransStaffUpperAll_Pnl.PanelColor = Color.FromArgb(26, 54, 244); //2
                issueTransStaffUpperAll_Pnl.PanelOutlineColor = Color.FromArgb(26, 54, 244);
                await Task.Delay(20);
                issueTransStaffUpperAll_Pnl.PanelColor = Color.FromArgb(25, 55, 255); //1
                issueTransStaffUpperAll_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
            }
            else
            {
                issueTransStaffUpperAll_Pnl.PanelColor = Color.FromArgb(239, 244, 254); //4
                await Task.Delay(20);
                issueTransStaffUpperAll_Pnl.PanelColor = Color.FromArgb(244, 248, 254); //3
                await Task.Delay(20);
                issueTransStaffUpperAll_Pnl.PanelColor = Color.FromArgb(250, 251, 255); //2
                await Task.Delay(20);
                issueTransStaffUpperAll_Pnl.PanelColor = Color.FromArgb(255, 255, 255); //1
                issueTransStaffUpperAll_Pnl.PanelOutlineColor = Color.FromArgb(214, 216, 235);
                issueTransStaffUpperAll_allLbl.ForeColor = Color.FromArgb(49, 50, 60);
            }
        }
        //
        private void issueTransStaffUpperPost_Pnl_Click(object sender, EventArgs e)
        {
            issueTransStaffUpper_btn1 = false;
            issueTransStaffUpperAll_Pnl.PanelColor = Color.White;
            issueTransStaffUpperAll_Pnl.PanelOutlineColor = Color.FromArgb(214, 216, 235);
            issueTransStaffUpperAll_allLbl.ForeColor = Color.FromArgb(49, 50, 60);

            issueTransStaffUpper_btn2 = true;
            issueTransStaffUpperPost_Pnl.PanelColor = Color.FromArgb(25, 55, 255);
            issueTransStaffUpperPost_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
            issueTransStaffUpperPost_postLbl.ForeColor = Color.White;

            issueTransStaffUpper_btn3 = false;
            issueTransStaffUpperOperator_Pnl.PanelColor = Color.White;
            issueTransStaffUpperOperator_Pnl.PanelOutlineColor = Color.FromArgb(214, 216, 235);
            issueTransStaffUpperOperator_operatorLbl.ForeColor = Color.FromArgb(49, 50, 60);

            issueTransStaffUpper_btn4 = false;
            issueTransStaffUpperManager_Pnl.PanelColor = Color.White;
            issueTransStaffUpperManager_Pnl.PanelOutlineColor = Color.FromArgb(214, 216, 235);
            issueTransStaffUpperManager_managerLbl.ForeColor = Color.FromArgb(49, 50, 60);
        }
        private async void issueTransStaffUpperPost_Pnl_MouseEnter(object sender, EventArgs e)
        {
            if (issueTransStaffUpper_btn2 == true)
            {
                issueTransStaffUpperPost_Pnl.PanelColor = Color.FromArgb(25, 55, 255); //1
                issueTransStaffUpperPost_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
                issueTransStaffUpperPost_postLbl.ForeColor = Color.White;
                await Task.Delay(20);
                issueTransStaffUpperPost_Pnl.PanelColor = Color.FromArgb(26, 54, 244); //2
                issueTransStaffUpperPost_Pnl.PanelOutlineColor = Color.FromArgb(26, 54, 244);
                await Task.Delay(20);
                issueTransStaffUpperPost_Pnl.PanelColor = Color.FromArgb(26, 52, 232); //3
                issueTransStaffUpperPost_Pnl.PanelOutlineColor = Color.FromArgb(26, 52, 232);
            }
            else
            {
                issueTransStaffUpperPost_Pnl.PanelColor = Color.FromArgb(255, 255, 255); //1
                issueTransStaffUpperPost_Pnl.PanelOutlineColor = Color.FromArgb(214, 216, 235);
                issueTransStaffUpperPost_postLbl.ForeColor = Color.FromArgb(49, 50, 60);
                await Task.Delay(20);
                issueTransStaffUpperPost_Pnl.PanelColor = Color.FromArgb(250, 251, 255); //2
                await Task.Delay(20);
                issueTransStaffUpperPost_Pnl.PanelColor = Color.FromArgb(244, 248, 254); //3
                await Task.Delay(20);
                issueTransStaffUpperPost_Pnl.PanelColor = Color.FromArgb(239, 244, 254); //4
            }
        }
        private async void issueTransStaffUpperPost_Pnl_MouseLeave(object sender, EventArgs e)
        {
            if (issueTransStaffUpper_btn2 == true)
            {
                issueTransStaffUpperPost_Pnl.PanelColor = Color.FromArgb(26, 52, 232); //3
                issueTransStaffUpperPost_Pnl.PanelOutlineColor = Color.FromArgb(26, 52, 232);
                issueTransStaffUpperPost_postLbl.ForeColor = Color.White;
                await Task.Delay(20);
                issueTransStaffUpperPost_Pnl.PanelColor = Color.FromArgb(26, 54, 244); //2
                issueTransStaffUpperPost_Pnl.PanelOutlineColor = Color.FromArgb(26, 54, 244);
                await Task.Delay(20);
                issueTransStaffUpperPost_Pnl.PanelColor = Color.FromArgb(25, 55, 255); //1
                issueTransStaffUpperPost_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
            }
            else
            {
                issueTransStaffUpperPost_Pnl.PanelColor = Color.FromArgb(239, 244, 254); //4
                issueTransStaffUpperPost_Pnl.PanelOutlineColor = Color.FromArgb(214, 216, 235);
                issueTransStaffUpperPost_postLbl.ForeColor = Color.FromArgb(49, 50, 60);
                await Task.Delay(20);
                issueTransStaffUpperPost_Pnl.PanelColor = Color.FromArgb(244, 248, 254); //3
                await Task.Delay(20);
                issueTransStaffUpperPost_Pnl.PanelColor = Color.FromArgb(250, 251, 255); //2
                await Task.Delay(20);
                issueTransStaffUpperPost_Pnl.PanelColor = Color.FromArgb(255, 255, 255); //1
            }
        }
        //
        private void issueTransStaffUpperOperator_Pnl_Click(object sender, EventArgs e)
        {
            issueTransStaffUpper_btn1 = false;
            issueTransStaffUpperAll_Pnl.PanelColor = Color.White;
            issueTransStaffUpperAll_Pnl.PanelOutlineColor = Color.FromArgb(214, 216, 235);
            issueTransStaffUpperAll_allLbl.ForeColor = Color.FromArgb(49, 50, 60);

            issueTransStaffUpper_btn2 = false;
            issueTransStaffUpperPost_Pnl.PanelColor = Color.White;
            issueTransStaffUpperPost_Pnl.PanelOutlineColor = Color.FromArgb(214, 216, 235);
            issueTransStaffUpperPost_postLbl.ForeColor = Color.FromArgb(49, 50, 60);

            issueTransStaffUpper_btn3 = true;
            issueTransStaffUpperOperator_Pnl.PanelColor = Color.FromArgb(25, 55, 255);
            issueTransStaffUpperOperator_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
            issueTransStaffUpperOperator_operatorLbl.ForeColor = Color.White;

            issueTransStaffUpper_btn4 = false;
            issueTransStaffUpperManager_Pnl.PanelColor = Color.White;
            issueTransStaffUpperManager_Pnl.PanelOutlineColor = Color.FromArgb(214, 216, 235);
            issueTransStaffUpperManager_managerLbl.ForeColor = Color.FromArgb(49, 50, 60);
        }
        private async void issueTransStaffUpperOperator_Pnl_MouseEnter(object sender, EventArgs e)
        {
            if (issueTransStaffUpper_btn3 == true)
            {
                issueTransStaffUpperOperator_Pnl.PanelColor = Color.FromArgb(25, 55, 255); //1
                issueTransStaffUpperOperator_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
                issueTransStaffUpperOperator_operatorLbl.ForeColor = Color.White;
                await Task.Delay(20);
                issueTransStaffUpperOperator_Pnl.PanelColor = Color.FromArgb(26, 54, 244); //2
                issueTransStaffUpperOperator_Pnl.PanelOutlineColor = Color.FromArgb(26, 54, 244);
                await Task.Delay(20);
                issueTransStaffUpperOperator_Pnl.PanelColor = Color.FromArgb(26, 52, 232); //3
                issueTransStaffUpperOperator_Pnl.PanelOutlineColor = Color.FromArgb(26, 52, 232);
            }
            else
            {
                issueTransStaffUpperOperator_Pnl.PanelColor = Color.FromArgb(255, 255, 255); //1
                issueTransStaffUpperOperator_Pnl.PanelOutlineColor = Color.FromArgb(214, 216, 235);
                issueTransStaffUpperOperator_operatorLbl.ForeColor = Color.FromArgb(49, 50, 60);
                await Task.Delay(20);
                issueTransStaffUpperOperator_Pnl.PanelColor = Color.FromArgb(250, 251, 255); //2
                await Task.Delay(20);
                issueTransStaffUpperOperator_Pnl.PanelColor = Color.FromArgb(244, 248, 254); //3
                await Task.Delay(20);
                issueTransStaffUpperOperator_Pnl.PanelColor = Color.FromArgb(239, 244, 254); //4
            }
        }
        private async void issueTransStaffUpperOperator_Pnl_MouseLeave(object sender, EventArgs e)
        {
            if (issueTransStaffUpper_btn3 == true)
            {
                issueTransStaffUpperOperator_Pnl.PanelColor = Color.FromArgb(26, 52, 232); //3
                issueTransStaffUpperOperator_Pnl.PanelOutlineColor = Color.FromArgb(26, 52, 232);
                issueTransStaffUpperOperator_operatorLbl.ForeColor = Color.White;
                await Task.Delay(20);
                issueTransStaffUpperOperator_Pnl.PanelColor = Color.FromArgb(26, 54, 244); //2
                issueTransStaffUpperOperator_Pnl.PanelOutlineColor = Color.FromArgb(26, 54, 244);
                await Task.Delay(20);
                issueTransStaffUpperOperator_Pnl.PanelColor = Color.FromArgb(25, 55, 255); //1
                issueTransStaffUpperOperator_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
            }
            else
            {
                issueTransStaffUpperOperator_Pnl.PanelColor = Color.FromArgb(239, 244, 254); //4
                issueTransStaffUpperOperator_Pnl.PanelOutlineColor = Color.FromArgb(214, 216, 235);
                issueTransStaffUpperOperator_operatorLbl.ForeColor = Color.FromArgb(49, 50, 60);
                await Task.Delay(20);
                issueTransStaffUpperOperator_Pnl.PanelColor = Color.FromArgb(244, 248, 254); //3
                await Task.Delay(20);
                issueTransStaffUpperOperator_Pnl.PanelColor = Color.FromArgb(250, 251, 255); //2
                await Task.Delay(20);
                issueTransStaffUpperOperator_Pnl.PanelColor = Color.FromArgb(255, 255, 255); //1
            }
        }
        //
        private void issueTransStaffUpperManager_Pnl_Click(object sender, EventArgs e)
        {
            issueTransStaffUpper_btn1 = false;
            issueTransStaffUpperAll_Pnl.PanelColor = Color.White;
            issueTransStaffUpperAll_Pnl.PanelOutlineColor = Color.FromArgb(214, 216, 235);
            issueTransStaffUpperAll_allLbl.ForeColor = Color.FromArgb(49, 50, 60);

            issueTransStaffUpper_btn2 = false;
            issueTransStaffUpperPost_Pnl.PanelColor = Color.White;
            issueTransStaffUpperPost_Pnl.PanelOutlineColor = Color.FromArgb(214, 216, 235);
            issueTransStaffUpperPost_postLbl.ForeColor = Color.FromArgb(49, 50, 60);

            issueTransStaffUpper_btn3 = false;
            issueTransStaffUpperOperator_Pnl.PanelColor = Color.White;
            issueTransStaffUpperOperator_Pnl.PanelOutlineColor = Color.FromArgb(214, 216, 235);
            issueTransStaffUpperOperator_operatorLbl.ForeColor = Color.FromArgb(49, 50, 60);

            issueTransStaffUpper_btn4 = true;
            issueTransStaffUpperManager_Pnl.PanelColor = Color.FromArgb(25, 55, 255);
            issueTransStaffUpperManager_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
            issueTransStaffUpperManager_managerLbl.ForeColor = Color.White;
        }
        private async void issueTransStaffUpperManager_Pnl_MouseEnter(object sender, EventArgs e)
        {
            if (issueTransStaffUpper_btn4 == true)
            {
                issueTransStaffUpperManager_Pnl.PanelColor = Color.FromArgb(25, 55, 255); //1
                issueTransStaffUpperManager_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
                issueTransStaffUpperManager_managerLbl.ForeColor = Color.White;
                await Task.Delay(20);
                issueTransStaffUpperManager_Pnl.PanelColor = Color.FromArgb(26, 54, 244); //2
                issueTransStaffUpperManager_Pnl.PanelOutlineColor = Color.FromArgb(26, 54, 244);
                await Task.Delay(20);
                issueTransStaffUpperManager_Pnl.PanelColor = Color.FromArgb(26, 52, 232); //3
                issueTransStaffUpperManager_Pnl.PanelOutlineColor = Color.FromArgb(26, 52, 232);
            }
            else
            {
                issueTransStaffUpperManager_Pnl.PanelColor = Color.FromArgb(255, 255, 255); //1
                issueTransStaffUpperManager_Pnl.PanelOutlineColor = Color.FromArgb(214, 216, 235);
                issueTransStaffUpperManager_managerLbl.ForeColor = Color.FromArgb(49, 50, 60);
                await Task.Delay(20);
                issueTransStaffUpperManager_Pnl.PanelColor = Color.FromArgb(250, 251, 255); //2
                await Task.Delay(20);
                issueTransStaffUpperManager_Pnl.PanelColor = Color.FromArgb(244, 248, 254); //3
                await Task.Delay(20);
                issueTransStaffUpperManager_Pnl.PanelColor = Color.FromArgb(239, 244, 254); //4
            }
        }
        private async void issueTransStaffUpperManager_Pnl_MouseLeave(object sender, EventArgs e)
        {
            if (issueTransStaffUpper_btn4 == true)
            {
                issueTransStaffUpperManager_Pnl.PanelColor = Color.FromArgb(26, 52, 232); //3
                issueTransStaffUpperManager_Pnl.PanelOutlineColor = Color.FromArgb(26, 52, 232);
                issueTransStaffUpperManager_managerLbl.ForeColor = Color.White;
                await Task.Delay(20);
                issueTransStaffUpperManager_Pnl.PanelColor = Color.FromArgb(26, 54, 244); //2
                issueTransStaffUpperManager_Pnl.PanelOutlineColor = Color.FromArgb(26, 54, 244);
                await Task.Delay(20);
                issueTransStaffUpperManager_Pnl.PanelColor = Color.FromArgb(25, 55, 255); //1
                issueTransStaffUpperManager_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
            }
            else
            {
                issueTransStaffUpperManager_Pnl.PanelColor = Color.FromArgb(239, 244, 254); //4
                issueTransStaffUpperManager_Pnl.PanelOutlineColor = Color.FromArgb(214, 216, 235);
                issueTransStaffUpperManager_managerLbl.ForeColor = Color.FromArgb(49, 50, 60);
                await Task.Delay(20);
                issueTransStaffUpperManager_Pnl.PanelColor = Color.FromArgb(244, 248, 254); //3
                await Task.Delay(20);
                issueTransStaffUpperManager_Pnl.PanelColor = Color.FromArgb(250, 251, 255); //2
                await Task.Delay(20);
                issueTransStaffUpperManager_Pnl.PanelColor = Color.FromArgb(255, 255, 255); //1
            }
        }
        private void AddSampleData5()
        {
            issueTabel_Dgw.Columns.Add("Number", "№");
            issueTabel_Dgw.Columns.Add("FIO", "ФИО сотрудника");
            issueTabel_Dgw.Columns.Add("Status", "Статус");
            issueTabel_Dgw.Columns.Add("Tags", "Меток");
            issueTabel_Dgw.Columns.Add("DateIssue", "Дата выдачи");
            issueTabel_Dgw.Columns.Add("DateDelivery", "Дата сдачи");
            issueTabel_Dgw.Columns.Add("Delete", "Удалить");

            issueTabel_Dgw.Columns["Number"].Width = 60;
            issueTabel_Dgw.Columns["Number"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            issueTabel_Dgw.Columns["Number"].DefaultCellStyle.Font = new Font("Montserrat", 9F, FontStyle.Bold);

            issueTabel_Dgw.Rows.Add("№1", "Иванов Иван Петрович", "Готово", "231 штук", "21.05.26", "25.05.26", "Удалить");
            issueTabel_Dgw.Rows.Add("№2", "Иванов Иван Петрович", "Готово", "231 штук", "21.05.26", "25.05.26", "Удалить");
            issueTabel_Dgw.Rows.Add("№3", "Иванов Иван Петрович", "Готово", "231 штук", "21.05.26", "25.05.26", "Удалить");
            issueTabel_Dgw.Rows.Add("№4", "Иванов Иван Петрович", "Готово", "231 штук", "21.05.26", "25.05.26", "Удалить");
            issueTabel_Dgw.Rows.Add("№5", "Иванов Иван Петрович", "Готово", "231 штук", "21.05.26", "25.05.26", "Удалить");
            issueTabel_Dgw.Rows.Add("№6", "Иванов Иван Петрович", "Готово", "231 штук", "21.05.26", "25.05.26", "Удалить");
            issueTabel_Dgw.Rows.Add("№7", "Иванов Иван Петрович", "Готово", "231 штук", "21.05.26", "25.05.26", "Удалить");
            issueTabel_Dgw.Rows.Add("№8", "Иванов Иван Петрович", "Готово", "231 штук", "21.05.26", "25.05.26", "Удалить");
            issueTabel_Dgw.Rows.Add("№9", "Иванов Иван Петрович", "Готово", "231 штук", "21.05.26", "25.05.26", "Удалить");
            issueTabel_Dgw.Rows.Add("№10", "Иванов Иван Петрович", "Готово", "231 штук", "21.05.26", "25.05.26", "Удалить");
        }
        private void AddSampleData6()
        {
            workerTabel_Dgw.Columns.Add("FIO", "ФИО");
            workerTabel_Dgw.Columns.Add("Position", "Должность");
            workerTabel_Dgw.Columns.Add("Login", "Логин");
            workerTabel_Dgw.Columns.Add("Password", "Пароль");
            workerTabel_Dgw.Columns.Add("Delete", "Удалить");

            workerTabel_Dgw.Columns["Delete"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            workerTabel_Dgw.Rows.Add("Иванов Иван Петрович", "Системный администратор", "ivanov.i", "******", "Удалить");
            workerTabel_Dgw.Rows.Add("Петров Сергей Владимирович", "Разработчик", "petrov.s", "******", "Удалить");
            workerTabel_Dgw.Rows.Add("Сидорова Анна Дмитриевна", "HR-менеджер", "sidorova.a", "******", "Удалить");
            workerTabel_Dgw.Rows.Add("Козлов Андрей Николаевич", "Тестировщик", "kozlov.a", "******", "Удалить");
            workerTabel_Dgw.Rows.Add("Смирнова Екатерина Павловна", "Дизайнер", "smirnova.e", "******", "Удалить");
            workerTabel_Dgw.Rows.Add("Васильев Дмитрий Олегович", "Project Manager", "vasiliev.d", "******", "Удалить");
            workerTabel_Dgw.Rows.Add("Морозова Татьяна Сергеевна", "Бизнес-аналитик", "morozova.t", "******", "Удалить");
            workerTabel_Dgw.Rows.Add("Новиков Алексей Игоревич", "DevOps инженер", "novikov.a", "******", "Удалить");
            workerTabel_Dgw.Rows.Add("Фёдорова Мария Александровна", "Маркетолог", "fedorova.m", "******", "Удалить");
            workerTabel_Dgw.Rows.Add("Егоров Павел Андреевич", "Системный аналитик", "egorov.p", "******", "Удалить");
        }

        private void workerRegistrationsGenerate1_Pnl_Click(object sender, EventArgs e)
        {
            Random p = new Random();
            int[] k = new int[2];
            for (int i = 0; i < k.Length; i++)
                k[i] = p.Next(1, 10);
            string bukvi1 = workerRegistrationSurname_surnameTxt.Content.ToString();
            string bukvi2 = workerRegistrationName_nameTxt.Content.Substring(0, 1);
            string bukvi3 = workerRegistrationPatronymic_patronymicTxt.Content.Substring(0, 1);
            string bukvi = bukvi1 + bukvi2 + bukvi3 + string.Join("", k);
            string transliterated = RussianTransliterator.GetTransliteration(bukvi.ToString());
            workerRegistrationLogin_loginTxt.Content = transliterated;
        }
        private void workerRegistrationsGenerate2_Pnl_Click(object sender, EventArgs e)
        {
            Random p = new Random();
            int[] k = new int[8];
            for (int i = 0; i < k.Length; i++)
                k[i] = p.Next(1, 10);
            workerRegistrationPassword_passwordTxt.Content = string.Join("", k);
        }
        private async void workerRegistrationsGenerate1_Pnl_MouseEnter(object sender, EventArgs e)
        {
            workerRegistrationsGenerate1_Pnl.PanelColor = Color.FromArgb(255, 255, 255); //1
            await Task.Delay(20);
            workerRegistrationsGenerate1_Pnl.PanelColor = Color.FromArgb(250, 251, 255); //2
            await Task.Delay(20);
            workerRegistrationsGenerate1_Pnl.PanelColor = Color.FromArgb(244, 248, 254); //3
            await Task.Delay(20);
            workerRegistrationsGenerate1_Pnl.PanelColor = Color.FromArgb(239, 244, 254); //4
        }
        private async void workerRegistrationsGenerate1_Pnl_MouseLeave(object sender, EventArgs e)
        {
            workerRegistrationsGenerate1_Pnl.PanelColor = Color.FromArgb(239, 244, 254); //4
            await Task.Delay(20);
            workerRegistrationsGenerate1_Pnl.PanelColor = Color.FromArgb(244, 248, 254); //3
            await Task.Delay(20);
            workerRegistrationsGenerate1_Pnl.PanelColor = Color.FromArgb(250, 251, 255); //2
            await Task.Delay(20);
            workerRegistrationsGenerate1_Pnl.PanelColor = Color.FromArgb(255, 255, 255); //1
        }
        private async void workerRegistrationsGenerate2_Pnl_MouseEnter(object sender, EventArgs e)
        {
            workerRegistrationsGenerate2_Pnl.PanelColor = Color.FromArgb(255, 255, 255); //1
            await Task.Delay(20);
            workerRegistrationsGenerate2_Pnl.PanelColor = Color.FromArgb(250, 251, 255); //2
            await Task.Delay(20);
            workerRegistrationsGenerate2_Pnl.PanelColor = Color.FromArgb(244, 248, 254); //3
            await Task.Delay(20);
            workerRegistrationsGenerate2_Pnl.PanelColor = Color.FromArgb(239, 244, 254); //4
        }
        private async void workerRegistrationsGenerate2_Pnl_MouseLeave(object sender, EventArgs e)
        {
            workerRegistrationsGenerate2_Pnl.PanelColor = Color.FromArgb(239, 244, 254); //4
            await Task.Delay(20);
            workerRegistrationsGenerate2_Pnl.PanelColor = Color.FromArgb(244, 248, 254); //3
            await Task.Delay(20);
            workerRegistrationsGenerate2_Pnl.PanelColor = Color.FromArgb(250, 251, 255); //2
            await Task.Delay(20);
            workerRegistrationsGenerate2_Pnl.PanelColor = Color.FromArgb(255, 255, 255); //1
        }
        private void workerRegistrationClear_Pnl_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Очистилось");
        }

        private async void workerRegistrationClear_Pnl_MouseEnter(object sender, EventArgs e)
        {
            workerRegistrationClear_Pnl.PanelColor = Color.FromArgb(242, 243, 250); //1
            workerRegistrationClear_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
            await Task.Delay(20);
            workerRegistrationClear_Pnl.PanelColor = Color.FromArgb(240, 240, 248); //2
            workerRegistrationClear_Pnl.PanelOutlineColor = Color.FromArgb(240, 240, 248);
            await Task.Delay(20);
            workerRegistrationClear_Pnl.PanelColor = Color.FromArgb(237, 238, 246); //3
            workerRegistrationClear_Pnl.PanelOutlineColor = Color.FromArgb(237, 238, 246);
            await Task.Delay(20);
            workerRegistrationClear_Pnl.PanelColor = Color.FromArgb(235, 235, 244); //4
            workerRegistrationClear_Pnl.PanelOutlineColor = Color.FromArgb(235, 235, 244);
        }
        private async void workerRegistrationClear_Pnl_MouseLeave(object sender, EventArgs e)
        {
            workerRegistrationClear_Pnl.PanelColor = Color.FromArgb(235, 235, 244); //4
            workerRegistrationClear_Pnl.PanelOutlineColor = Color.FromArgb(235, 235, 244);
            await Task.Delay(20);
            workerRegistrationClear_Pnl.PanelColor = Color.FromArgb(237, 238, 246); //3
            workerRegistrationClear_Pnl.PanelOutlineColor = Color.FromArgb(237, 238, 246);
            await Task.Delay(20);
            workerRegistrationClear_Pnl.PanelColor = Color.FromArgb(240, 240, 248); //2
            workerRegistrationClear_Pnl.PanelOutlineColor = Color.FromArgb(240, 240, 248);
            await Task.Delay(20);
            workerRegistrationClear_Pnl.PanelColor = Color.FromArgb(242, 243, 250); //1
            workerRegistrationClear_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
        }

        private void readerRegistrationDone_Pnl_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Зарегистрирован");
        }

        private async void readerRegistrationDone_Pnl_MouseEnter(object sender, EventArgs e)
        {
            readerRegistrationDone_Pnl.PanelColor = Color.FromArgb(25, 55, 255); //1
            readerRegistrationDone_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
            await Task.Delay(20);
            readerRegistrationDone_Pnl.PanelColor = Color.FromArgb(26, 54, 244); //2
            readerRegistrationDone_Pnl.PanelOutlineColor = Color.FromArgb(26, 54, 244);
            await Task.Delay(20);
            readerRegistrationDone_Pnl.PanelColor = Color.FromArgb(26, 52, 232); //3
            readerRegistrationDone_Pnl.PanelOutlineColor = Color.FromArgb(26, 52, 232);
        }

        private async void readerRegistrationDone_Pnl_MouseLeave(object sender, EventArgs e)
        {
            readerRegistrationDone_Pnl.PanelColor = Color.FromArgb(26, 52, 232); //3
            readerRegistrationDone_Pnl.PanelOutlineColor = Color.FromArgb(26, 52, 232);
            await Task.Delay(20);
            readerRegistrationDone_Pnl.PanelColor = Color.FromArgb(26, 54, 244); //2
            readerRegistrationDone_Pnl.PanelOutlineColor = Color.FromArgb(26, 54, 244);
            await Task.Delay(20);
            readerRegistrationDone_Pnl.PanelColor = Color.FromArgb(25, 55, 255); //1
            readerRegistrationDone_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
        }

        private void workerRegistrationSearch_animationTmr_Tick(object sender, EventArgs e)
        {
            int step;
            int diff;
            if (workerRegistrationSearch_animation)
            {
                diff = workerRegistrationSearch_Pnl.Height - workerRegistrationSearch_Pnl.MinimumSize.Height;
                step = Math.Max(2, diff / 5);
                workerRegistrationSearch_Pnl.Height -= step;
                if (workerRegistrationSearch_Pnl.Height <= workerRegistrationSearch_Pnl.MinimumSize.Height)
                {
                    workerRegistrationSearch_Pnl.Height = workerRegistrationSearch_Pnl.MinimumSize.Height;
                    workerRegistrationSearch_animation = false;
                    workerRegistrationSearch_animationTmr.Stop();
                }
            }
            else
            {
                diff = workerRegistrationSearch_Pnl.MaximumSize.Height - workerRegistrationSearch_Pnl.Height;
                step = Math.Max(2, diff / 5);
                workerRegistrationSearch_Pnl.Height += step;
                if (workerRegistrationSearch_Pnl.Height >= workerRegistrationSearch_Pnl.MaximumSize.Height)
                {
                    workerRegistrationSearch_Pnl.Height = workerRegistrationSearch_Pnl.MaximumSize.Height;
                    workerRegistrationSearch_animation = true;
                    workerRegistrationSearch_animationTmr.Stop();
                }
            }
        }
        private void workerRegistrationPost_postTxt_Click(object sender, EventArgs e)
        {
            workerRegistrationSearch_animationTmr.Start();
        }

        private void workerTopIssue_Pnl_Click(object sender, EventArgs e)
        {
            issue_Pnl.Visible = true;
            map_Pnl.Visible = false;
            worker_Pnl.Visible = false;
            this.SuspendLayout();
            this.ResumeLayout();
        }

        private async void workerTopIssue_Pnl_MouseEnter(object sender, EventArgs e)
        {
            workerTopIssue_issuePic.Image = Properties.Resources.ЗаданиеДобавить1;
            await Task.Delay(20);
            workerTopIssue_issuePic.Image = Properties.Resources.ЗаданиеДобавить2;
            await Task.Delay(20);
            workerTopIssue_issuePic.Image = Properties.Resources.ЗаданиеДобавить3;
            await Task.Delay(20);
            workerTopIssue_issuePic.Image = Properties.Resources.ЗаданиеДобавить4;
            await Task.Delay(20);
            workerTopIssue_issuePic.Image = Properties.Resources.ЗаданиеДобавить5;
        }
        private async void workerTopIssue_Pnl_MouseLeave(object sender, EventArgs e)
        {
            workerTopIssue_issuePic.Image = Properties.Resources.ЗаданиеДобавить5;
            await Task.Delay(20);
            workerTopIssue_issuePic.Image = Properties.Resources.ЗаданиеДобавить4;
            await Task.Delay(20);
            workerTopIssue_issuePic.Image = Properties.Resources.ЗаданиеДобавить3;
            await Task.Delay(20);
            workerTopIssue_issuePic.Image = Properties.Resources.ЗаданиеДобавить2;
            await Task.Delay(20);
            workerTopIssue_issuePic.Image = Properties.Resources.ЗаданиеДобавить1;
        }

        private void workerTopMap_Pnl_Click(object sender, EventArgs e)
        {
            issue_Pnl.Visible = false;
            map_Pnl.Visible = true;
            worker_Pnl.Visible = false;
            this.SuspendLayout();
            this.ResumeLayout();
        }
        private async void workerTopMap_Pnl_MouseEnter(object sender, EventArgs e)
        {
            workerTopMap_mapPic.Image = Properties.Resources.Карта1;
            await Task.Delay(20);
            workerTopMap_mapPic.Image = Properties.Resources.Карта2;
            await Task.Delay(20);
            workerTopMap_mapPic.Image = Properties.Resources.Карта3;
            await Task.Delay(20);
            workerTopMap_mapPic.Image = Properties.Resources.Карта4;
            await Task.Delay(20);
            workerTopMap_mapPic.Image = Properties.Resources.Карта5;
        }
        private async void workerTopMap_Pnl_MouseLeave(object sender, EventArgs e)
        {
            workerTopMap_mapPic.Image = Properties.Resources.Карта5;
            await Task.Delay(20);
            workerTopMap_mapPic.Image = Properties.Resources.Карта4;
            await Task.Delay(20);
            workerTopMap_mapPic.Image = Properties.Resources.Карта3;
            await Task.Delay(20);
            workerTopMap_mapPic.Image = Properties.Resources.Карта2;
            await Task.Delay(20);
            workerTopMap_mapPic.Image = Properties.Resources.Карта1;
        }

        private void workerTopWorker_Pnl_Click(object sender, EventArgs e)
        {
            issue_Pnl.Visible = false;
            map_Pnl.Visible = false;
            worker_Pnl.Visible = true;
            this.SuspendLayout();
            this.ResumeLayout();
        }
        private async void workerTopWorker_Pnl_MouseEnter(object sender, EventArgs e)
        {
            workerTopWorker_workerPic.Image = Properties.Resources.Регистрация1;
            await Task.Delay(20);
            workerTopWorker_workerPic.Image = Properties.Resources.Регистрация2;
            await Task.Delay(20);
            workerTopWorker_workerPic.Image = Properties.Resources.Регистрация3;
            await Task.Delay(20);
            workerTopWorker_workerPic.Image = Properties.Resources.Регистрация4;
            await Task.Delay(20);
            workerTopWorker_workerPic.Image = Properties.Resources.Регистрация5;
        }
        private async void workerTopWorker_Pnl_MouseLeave(object sender, EventArgs e)
        {
            workerTopWorker_workerPic.Image = Properties.Resources.Регистрация5;
            await Task.Delay(20);
            workerTopWorker_workerPic.Image = Properties.Resources.Регистрация4;
            await Task.Delay(20);
            workerTopWorker_workerPic.Image = Properties.Resources.Регистрация3;
            await Task.Delay(20);
            workerTopWorker_workerPic.Image = Properties.Resources.Регистрация2;
            await Task.Delay(20);
            workerTopWorker_workerPic.Image = Properties.Resources.Регистрация1;
        }

        private void workerBottomSettings_Pnl_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Настройка");
        }

        private async void workerBottomSettings_Pnl_MouseEnter(object sender, EventArgs e)
        {
            workerBottomSettings_settingsPic.Image = Properties.Resources.Настройки1;
            await Task.Delay(20);
            workerBottomSettings_settingsPic.Image = Properties.Resources.Настройки2;
            await Task.Delay(20);
            workerBottomSettings_settingsPic.Image = Properties.Resources.Настройки3;
            await Task.Delay(20);
            workerBottomSettings_settingsPic.Image = Properties.Resources.Настройки4;
            await Task.Delay(20);
            workerBottomSettings_settingsPic.Image = Properties.Resources.Настройки5;
        }
        private async void workerBottomSettings_Pnl_MouseLeave(object sender, EventArgs e)
        {
            workerBottomSettings_settingsPic.Image = Properties.Resources.Настройки5;
            await Task.Delay(20);
            workerBottomSettings_settingsPic.Image = Properties.Resources.Настройки4;
            await Task.Delay(20);
            workerBottomSettings_settingsPic.Image = Properties.Resources.Настройки3;
            await Task.Delay(20);
            workerBottomSettings_settingsPic.Image = Properties.Resources.Настройки2;
            await Task.Delay(20);
            workerBottomSettings_settingsPic.Image = Properties.Resources.Настройки1;
        }

        private void workerBottomExit_Pnl_Click(object sender, EventArgs e)
        {
            loginForm lf = new loginForm();
            lf.Show();
            this.Hide();
        }

        private async void workerBottomExit_Pnl_MouseEnter(object sender, EventArgs e)
        {
            workerBottomExit_exitPic.Image = Properties.Resources.Выход1;
            await Task.Delay(20);
            workerBottomExit_exitPic.Image = Properties.Resources.Выход2;
            await Task.Delay(20);
            workerBottomExit_exitPic.Image = Properties.Resources.Выход3;
            await Task.Delay(20);
            workerBottomExit_exitPic.Image = Properties.Resources.Выход4;
            await Task.Delay(20);
            workerBottomExit_exitPic.Image = Properties.Resources.Выход5;
        }

        private async void workerBottomExit_Pnl_MouseLeave(object sender, EventArgs e)
        {
            workerBottomExit_exitPic.Image = Properties.Resources.Выход5;
            await Task.Delay(20);
            workerBottomExit_exitPic.Image = Properties.Resources.Выход4;
            await Task.Delay(20);
            workerBottomExit_exitPic.Image = Properties.Resources.Выход3;
            await Task.Delay(20);
            workerBottomExit_exitPic.Image = Properties.Resources.Выход2;
            await Task.Delay(20);
            workerBottomExit_exitPic.Image = Properties.Resources.Выход1;
        }

        private void mapTopMap_Pnl_Click_1(object sender, EventArgs e)
        {
            issue_Pnl.Visible = false;
            map_Pnl.Visible = true;
            worker_Pnl.Visible = false;
            this.SuspendLayout();
            this.ResumeLayout();
        }

        private async void mapTopMap_Pnl_MouseEnter_1(object sender, EventArgs e)
        {
            mapTopMap_mapPic.Image = Properties.Resources.Карта1;
            await Task.Delay(20);
            mapTopMap_mapPic.Image = Properties.Resources.Карта2;
            await Task.Delay(20);
            mapTopMap_mapPic.Image = Properties.Resources.Карта3;
            await Task.Delay(20);
            mapTopMap_mapPic.Image = Properties.Resources.Карта4;
            await Task.Delay(20);
            mapTopMap_mapPic.Image = Properties.Resources.Карта5;
        }

        private async void mapTopMap_Pnl_MouseLeave_1(object sender, EventArgs e)
        {
            mapTopMap_mapPic.Image = Properties.Resources.Карта5;
            await Task.Delay(20);
            mapTopMap_mapPic.Image = Properties.Resources.Карта4;
            await Task.Delay(20);
            mapTopMap_mapPic.Image = Properties.Resources.Карта3;
            await Task.Delay(20);
            mapTopMap_mapPic.Image = Properties.Resources.Карта2;
            await Task.Delay(20);
            mapTopMap_mapPic.Image = Properties.Resources.Карта1;
        }

        private void cuiButton7_Click(object sender, EventArgs e)
        {
            mapRoute = !mapRoute;
            mapInfo_animationTmr.Start();
        }
    }
}
