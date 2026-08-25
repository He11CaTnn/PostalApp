using CuoreUI.Controls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PostalApp
{
    public partial class PostmanForm : Form
    {
        // Константы для перемещения окна
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HTCAPTION = 0x2;

        // Панель карты
        bool mapStyle = false;
        bool mouseOpenPanel = false;
        int mapStyleUp = -267;
        int mapStyleDown = 64;

        bool mapSwitch = false;
        int mapSwitchRight = 90;
        int mapSwitchLeft = -6;

        bool mapStyleSettings_polygons = true;
        bool mapStyleSettings_borders = true;
        bool mapStyleSettings_markers = true;
        bool mapStyleSettings_check = true;

        bool mapStyleSettings_homes = true;
        bool mapStyleSettings_apartments = true;
        bool mapStyleSettings_organizations = true;
        bool mapStyleSettings_post = true;

        bool mapStyle2_animation = false;
        bool _closingForHide = false;

        bool mapRoute = false;
        bool mapRouteBtn = false;

        bool mapRouteTravel = true;
        bool mapRouteWalk = true;
        bool mapRouteBike = false;
        private readonly int _gap = 5;
        bool _openWalk = true;

        bool mapRouteWay = true;

        bool mapTags_animation = false;

        // Панель заданий
        bool taskWatchOpen = false;

        bool taskUpperFilter_animation = false;
        bool taskUpperFilterArrow = false;

        bool mapRouteTaskTabel_animation1 = false;
        bool mapRouteTaskTabel_animation2 = false;

        //private int routeMarkers = 0;
        private Timer _scrollDebounceTimer;
        private SearchFilter<DataBase.Tasks> _searchTasks;
        private LazyLoader<DataBase.Tasks> _loaderTasks;
        private Timer _autoUpdateTasksTimer;
        private readonly HashSet<string> _excludedStatuses = new HashSet<string>();

        // --- Маршрут по заданию ---
        private Timer _routeTaskUpdateTimer;
        private List<DataBase.Tasks> _routeTaskItems = new List<DataBase.Tasks>();
        private List<DataBase.Markers> _currentTaskMarkers = new List<DataBase.Markers>();
        private bool _isUpdatingTaskCmb = false;

        private bool _isDoubleCheckInternet = false;


        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateRoundRectRgn(int x1, int y1, int x2, int y2, int cx, int cy);
        public PostmanForm()
        {
            InitializeComponent();
            OpenPanel();
            UpdateLabelText();
            RoundedCorners();
        }
        private void OpenPanel()
        {
            mapRoute_Pnl.Location = new Point(79, this.ClientSize.Height + 10);
            taskTabelInsert_Pnl.Size = new Size(0, 278);
            taskWatch_Pnl.Location = new Point(1200, 83);

            mapRouteTaskTabel_Pnl.Location = new Point(10, 230);
            mapRouteTaskTabel_Pnl.Size = new Size(311, 0);

            map_Pnl.Visible = true;
            task_Pnl.Visible = false;
            task_Pnl.Dock = DockStyle.Fill;
            map_Pnl.Dock = DockStyle.Fill;

            mapRouteTask_taskCmb.SelectedIndex = -1;
        }
        //Скругление панелей и т.д
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
            SetRoundedCorners(mapRoute_Pnl, 15);
            SetRoundedCorners(mapLocation_Pnl, 23);
            SetRoundedCorners(mapZoom_Pnl, 17);
            SetRoundedCorners(mapRouteTags_Pnl, 17);
            SetRoundedCorners(mapTools_Pnl, 17);
            SetRoundedCorners(mapStyleSwitchBlock_Pnl, 10);
            SetRoundedCorners(taskTop_Pnl, 23);
            SetRoundedCorners(taskTabelInsert_Pnl, 24);
            SetRoundedCorners(taskBottom_Pnl, 23);
            SetRoundedCorners(taskWatch_Pnl, 24);
            SetRoundedCorners(taskTabel_Pnl, 24);
        }
        //------------------------------Панель карты----------------------------------------
        //upperPanel Вверхняя панель
        //Скрыть форму
        private void upper_Pnl_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
                SetRoundedCorners(this, 15);
            }
        }

        //Минимальный размер формы
        private void upper_minimizeBtn_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        //Перемещение формы
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

        //Закрытие формы
        private void upper_closeBtn_Click(object sender, EventArgs e)
        {
            Program.AppExit();
        }

        //mapTools_Pnl + mapStyle_Pnl Панель настроек карты
        //Настройка выезда панели mapStyle_Pnl
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

        private void mapStyle_Pnl_MouseLeave(object sender, EventArgs e)
        {
            mouseOpenPanel = false;
            mapStyle_checkTmr.Start();
        }

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

        private bool IsMouseOverPanelHierarchy()
        {
            Point cursorPos = Cursor.Position;
            if (IsMouseOverControl(mapStyle_Pnl, cursorPos))
            {
                return true;
            }
            return false;
        }

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

        //Переключатель спутник/схема
        private void mapStyleSwitchScheme_Pnl_Click(object sender, EventArgs e)
        {
            if (map_gmapCnl.MapProvider == Map.satelliteProvider && mapSwitch)
            {
                mapStyleSwitch_animationTmr.Start();
                mapSwitch = false;
                map_gmapCnl.MapProvider = Map.streetProvider;
            }
        }

        private void mapStyleSwitchSatellite_Pnl_Click(object sender, EventArgs e)
        {
            if (map_gmapCnl.MapProvider == Map.streetProvider && !mapSwitch)
            {
                mapStyleSwitch_animationTmr.Start();
                mapSwitch = true;
                map_gmapCnl.MapProvider = Map.satelliteProvider;
            }
        }

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

        //Кнопка + Переключатель меток
        private async void mapToolsTags_Pnl_Click(object sender, EventArgs e)
        {
            mapStyleSettingsTags_Pnl_Click(sender, e);
        }

        //Кнопка + Переключаитель границ
        private async void mapToolsBorder_Pnl_Click(object sender, EventArgs e)
        {
            mapStyleSettingsBorder_Pnl_Click(sender, e);
        }

        //Кнопка + Переключаитель полигонов
        private async void mapToolsPolygon_Pnl_Click(object sender, EventArgs e)
        {
            mapStyleSettingsPolygon_Pnl_Click(sender, e);
        }

        //mapRoutePanel Панель постройки маршрутов на карте
        private async void mapRouteButton_Click(object sender, EventArgs e)
        {
            mapRoute = !mapRoute;
            mapRouteTimer.Start();
            if (mapRouteBtn == false)
            {
                mapRouteBtn = true;
                mapRoutePictureBox.Image = Properties.Resources.Маршрут1;
                await Task.Delay(20);
                mapRoutePictureBox.Image = Properties.Resources.Маршрут2;
                await Task.Delay(20);
                mapRoutePictureBox.Image = Properties.Resources.Маршрут3;
                await Task.Delay(20);
                mapRoutePictureBox.Image = Properties.Resources.Маршрут4;
                await Task.Delay(20);
                mapRoutePictureBox.Image = Properties.Resources.Маршрут5;
            }
            else if (mapRouteBtn == true)
            {
                mapRouteBtn = false;
                mapRoutePictureBox.Image = Properties.Resources.Маршрут5;
                await Task.Delay(20);
                mapRoutePictureBox.Image = Properties.Resources.Маршрут4;
                await Task.Delay(20);
                mapRoutePictureBox.Image = Properties.Resources.Маршрут3;
                await Task.Delay(20);
                mapRoutePictureBox.Image = Properties.Resources.Маршрут2;
                await Task.Delay(20);
                mapRoutePictureBox.Image = Properties.Resources.Маршрут1;
            }
        }

        private void mapRouteTimer_Tick(object sender, EventArgs e)
        {
            int targetYDown2 = this.ClientSize.Height - mapRoute_Pnl.Height - 43;
            int targetYUp2 = this.ClientSize.Height + 10;
            int target = mapRoute ? targetYDown2 : targetYUp2;
            int distance = target - mapRoute_Pnl.Top;
            int step = (int)(distance * 0.1f);
            if (Math.Abs(distance) < 1)
            {
                mapRoute_Pnl.Top = target;
                mapRouteTimer.Stop();
                return;
            }
            mapRoute_Pnl.Top += step;
        }

        private void mapRoutePanelButton1_1_Click(object sender, EventArgs e)
        {
            mapRouteTask_taskCmb.SelectedIndex = -1;
        }

        //Проверка на нажатии кнопки пешком/велосипед
        private void mapRoureTravel_animationTmr_Tick(object sender, EventArgs e)
        {
            int walkTarget = _openWalk ? mapRouteTaskWalk_Pnl.MaximumSize.Width
                                       : mapRouteTaskWalk_Pnl.MinimumSize.Width;
            int bikeTarget = _openWalk ? mapRouteTaskBike_Pnl.MinimumSize.Width
                                       : mapRouteTaskBike_Pnl.MaximumSize.Width;
            AnimateWidth(mapRouteTaskWalk_Pnl, walkTarget);
            AnimateWidth(mapRouteTaskBike_Pnl, bikeTarget);
            mapRouteTaskBike_Pnl.Left = mapRouteTaskWalk_Pnl.Left + mapRouteTaskWalk_Pnl.Width + _gap;
            if (mapRouteTaskWalk_Pnl.Width == walkTarget && mapRouteTaskBike_Pnl.Width == bikeTarget)
                mapRoureTravel_animationTmr.Stop();
        }

        private static void AnimateWidth(Control c, int target)
        {
            int diff = Math.Abs(target - c.Width);
            int step = Math.Max(2, diff / 5);
            int dir = target > c.Width ? 1 : -1;
            int next = c.Width + dir * step;
            c.Width = (dir > 0) ? Math.Min(next, target) : Math.Max(next, target);
        }

        //Кнопка пешком
        private void mapRouteTaskWalk_Pnl_Click(object sender, EventArgs e)
        {
            _openWalk = true;
            mapRoureTravel_animationTmr.Start();
            mapRouteWalk = true;
            mapRouteBike = false;
            mapRouteTravel = true;
            mapRouteTaskWalk_Pnl.PanelColor = Color.FromArgb(25, 55, 255);
            mapRouteTaskWalk_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
            mapRouteTaskBike_Pnl.PanelColor = Color.White;
            mapRouteTaskBike_Pnl.PanelOutlineColor = Color.FromArgb(214, 216, 235);
            mapRouteTaskWalk_walkLbl.Visible = true;
            mapRouteTaskBike_bikeLbl.Visible = false;
            mapRouteTaskWalk_walkPic.Image = Properties.Resources.ЧеловекИдёт1;
            mapRouteTaskBike_bikePic.Image = Properties.Resources.ВелосипедистЕдет2;
            mapRouteTaskBike_Pnl.Cursor = Cursors.Hand;
            mapRouteTaskWalk_Pnl.Cursor = Cursors.Arrow;
        }

        private async void mapRouteTaskWalk_Pnl_MouseEnter(object sender, EventArgs e)
        {
            if (mapRouteWalk == false)
            {
                mapRouteTaskWalk_Pnl.PanelColor = Color.FromArgb(255, 255, 255); //1
                mapRouteTaskWalk_Pnl.PanelOutlineColor = Color.FromArgb(214, 216, 235);
                await Task.Delay(20);
                mapRouteTaskWalk_Pnl.PanelColor = Color.FromArgb(250, 251, 255); //2
                await Task.Delay(20);
                mapRouteTaskWalk_Pnl.PanelColor = Color.FromArgb(244, 248, 254); //3
                await Task.Delay(20);
                mapRouteTaskWalk_Pnl.PanelColor = Color.FromArgb(239, 244, 254); //4
            }
            else
            {
                mapRouteTaskWalk_Pnl.PanelColor = Color.FromArgb(25, 55, 255); //1
                mapRouteTaskWalk_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
                await Task.Delay(20);
                mapRouteTaskWalk_Pnl.PanelColor = Color.FromArgb(26, 54, 244); //2
                mapRouteTaskWalk_Pnl.PanelOutlineColor = Color.FromArgb(26, 54, 244);
                await Task.Delay(20);
                mapRouteTaskWalk_Pnl.PanelColor = Color.FromArgb(26, 52, 232); //3
                mapRouteTaskWalk_Pnl.PanelOutlineColor = Color.FromArgb(26, 52, 232);
            }
        }

        private async void mapRouteTaskWalk_Pnl_MouseLeave(object sender, EventArgs e)
        {
            if (mapRouteWalk == false)
            {
                mapRouteTaskWalk_Pnl.PanelColor = Color.FromArgb(239, 244, 254); //4
                mapRouteTaskWalk_Pnl.PanelOutlineColor = Color.FromArgb(214, 216, 235);
                await Task.Delay(20);
                mapRouteTaskWalk_Pnl.PanelColor = Color.FromArgb(244, 248, 254); //3
                await Task.Delay(20);
                mapRouteTaskWalk_Pnl.PanelColor = Color.FromArgb(250, 251, 255); //2
                await Task.Delay(20);
                mapRouteTaskWalk_Pnl.PanelColor = Color.FromArgb(255, 255, 255); //1
            }
            else
            {
                mapRouteTaskWalk_Pnl.PanelColor = Color.FromArgb(26, 52, 232); //3
                mapRouteTaskWalk_Pnl.PanelOutlineColor = Color.FromArgb(26, 52, 232);
                await Task.Delay(20);
                mapRouteTaskWalk_Pnl.PanelColor = Color.FromArgb(26, 54, 244); //2
                mapRouteTaskWalk_Pnl.PanelOutlineColor = Color.FromArgb(26, 54, 244);
                await Task.Delay(20);
                mapRouteTaskWalk_Pnl.PanelColor = Color.FromArgb(25, 55, 255); //1
                mapRouteTaskWalk_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
            }
        }

        //Кнопка велосипеда
        private void mapRouteTaskBike_Pnl_Click(object sender, EventArgs e)
        {
            _openWalk = false;
            mapRoureTravel_animationTmr.Start();
            mapRouteWalk = false;
            mapRouteBike = true;
            mapRouteTravel = false;
            mapRouteTaskBike_Pnl.PanelColor = Color.FromArgb(25, 55, 255);
            mapRouteTaskBike_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
            mapRouteTaskWalk_Pnl.PanelColor = Color.White;
            mapRouteTaskWalk_Pnl.PanelOutlineColor = Color.FromArgb(214, 216, 235);
            mapRouteTaskWalk_walkLbl.Visible = false;
            mapRouteTaskBike_bikeLbl.Visible = true;
            mapRouteTaskWalk_walkPic.Image = Properties.Resources.ЧеловекИдёт2;
            mapRouteTaskBike_bikePic.Image = Properties.Resources.ВелосипедистЕдет1;
            mapRouteTaskBike_Pnl.Cursor = Cursors.Arrow;
            mapRouteTaskWalk_Pnl.Cursor = Cursors.Hand;
        }

        private async void mapRouteTaskBike_Pnl_MouseEnter(object sender, EventArgs e)
        {
            if (mapRouteBike == false)
            {
                mapRouteTaskBike_Pnl.PanelColor = Color.FromArgb(255, 255, 255); //1
                mapRouteTaskBike_Pnl.PanelOutlineColor = Color.FromArgb(214, 216, 235);
                await Task.Delay(20);
                mapRouteTaskBike_Pnl.PanelColor = Color.FromArgb(250, 251, 255); //2
                await Task.Delay(20);
                mapRouteTaskBike_Pnl.PanelColor = Color.FromArgb(244, 248, 254); //3
                await Task.Delay(20);
                mapRouteTaskBike_Pnl.PanelColor = Color.FromArgb(239, 244, 254); //4
            }
            else
            {
                mapRouteTaskBike_Pnl.PanelColor = Color.FromArgb(25, 55, 255); //1
                mapRouteTaskBike_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
                await Task.Delay(20);
                mapRouteTaskBike_Pnl.PanelColor = Color.FromArgb(26, 54, 244); //2
                mapRouteTaskBike_Pnl.PanelOutlineColor = Color.FromArgb(26, 54, 244);
                await Task.Delay(20);
                mapRouteTaskBike_Pnl.PanelColor = Color.FromArgb(26, 52, 232); //3
                mapRouteTaskBike_Pnl.PanelOutlineColor = Color.FromArgb(26, 52, 232);
            }
        }

        private async void mapRouteTaskBike_Pnl_MouseLeave(object sender, EventArgs e)
        {
            if (mapRouteBike == false)
            {
                mapRouteTaskBike_Pnl.PanelColor = Color.FromArgb(239, 244, 254); //4
                mapRouteTaskBike_Pnl.PanelOutlineColor = Color.FromArgb(214, 216, 235);
                await Task.Delay(20);
                mapRouteTaskBike_Pnl.PanelColor = Color.FromArgb(244, 248, 254); //3
                await Task.Delay(20);
                mapRouteTaskBike_Pnl.PanelColor = Color.FromArgb(250, 251, 255); //2
                await Task.Delay(20);
                mapRouteTaskBike_Pnl.PanelColor = Color.FromArgb(255, 255, 255); //1
            }
            else
            {
                mapRouteTaskBike_Pnl.PanelColor = Color.FromArgb(26, 52, 232); //3
                mapRouteTaskBike_Pnl.PanelOutlineColor = Color.FromArgb(26, 52, 232);
                await Task.Delay(20);
                mapRouteTaskBike_Pnl.PanelColor = Color.FromArgb(26, 54, 244); //2
                mapRouteTaskBike_Pnl.PanelOutlineColor = Color.FromArgb(26, 54, 244);
                await Task.Delay(20);
                mapRouteTaskBike_Pnl.PanelColor = Color.FromArgb(25, 55, 255); //1
                mapRouteTaskBike_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
            }
        }

        //Кнопки полный путь
        private async void mapRouteTaskFull_Pnl_Click(object sender, EventArgs e)
        {
            mapRouteWay = true;
            mapRouteTaskFull_stLbl_1_1.ForeColor = Color.FromArgb(25, 55, 255);
            mapRouteTaskShort_stLbl_1_1.ForeColor = Color.FromArgb(49, 50, 60);
            mapRouteTaskFull_stLbl_1_2.ForeColor = Color.FromArgb(96, 125, 250);
            mapRouteTaskShort_stLbl_1_2.ForeColor = Color.FromArgb(126, 128, 143);
            mapRouteTaskFull_Pnl.PanelColor = Color.FromArgb(242, 243, 250);
            mapRouteTaskFull_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
            mapRouteTaskShort_Pnl.PanelColor = Color.FromArgb(242, 243, 250);
            mapRouteTaskShort_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
            mapRouteTaskFull_stPic.Image = Properties.Resources.ПолныйМаршрут2;
            mapRouteTaskShort_stPic.Image = Properties.Resources.КороткийМаршрут1;
            mapRouteTaskFull_Pnl.Cursor = Cursors.Arrow;
            mapRouteTaskShort_Pnl.Cursor = Cursors.Hand;
            mapRouteTaskShort_Pnl.Enabled = true;
            mapRouteTaskFull_Pnl.Enabled = false;
            mapRouteTaskShort_Pnl.Rounding = new Padding(12, 12, 0, 0);
            await Task.Delay(05);
            mapRouteTaskShort_Pnl.Rounding = new Padding(12, 12, 2, 2);
            await Task.Delay(05);
            mapRouteTaskShort_Pnl.Rounding = new Padding(12, 12, 4, 4);
            await Task.Delay(05);
            mapRouteTaskShort_Pnl.Rounding = new Padding(12, 12, 6, 6);
            await Task.Delay(05);
            mapRouteTaskShort_Pnl.Rounding = new Padding(12, 12, 8, 8);
            await Task.Delay(05);
            mapRouteTaskShort_Pnl.Rounding = new Padding(12, 12, 10, 10);
            await Task.Delay(05);
            mapRouteTaskShort_Pnl.Rounding = new Padding(12, 12, 12, 12);
            mapRouteTask_animationTmr.Start();
            mapRouteTaskTabel_animationTmr.Start();

        }
        private async void mapRouteTaskFull_Pnl_MouseEnter(object sender, EventArgs e)
        {
            if (mapRouteWay == false)
            {
                mapRouteTaskFull_Pnl.PanelColor = Color.FromArgb(242, 243, 250); //1
                mapRouteTaskFull_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
                await Task.Delay(20);
                mapRouteTaskFull_Pnl.PanelColor = Color.FromArgb(240, 240, 248); //2
                mapRouteTaskFull_Pnl.PanelOutlineColor = Color.FromArgb(240, 240, 248);
                await Task.Delay(20);
                mapRouteTaskFull_Pnl.PanelColor = Color.FromArgb(237, 238, 246); //3
                mapRouteTaskFull_Pnl.PanelOutlineColor = Color.FromArgb(237, 238, 246);
                await Task.Delay(20);
                mapRouteTaskFull_Pnl.PanelColor = Color.FromArgb(235, 235, 244); //4
                mapRouteTaskFull_Pnl.PanelOutlineColor = Color.FromArgb(235, 235, 244);
            }
        }

        private async void mapRouteTaskFull_Pnl_MouseLeave(object sender, EventArgs e)
        {
            if (mapRouteWay == false)
            {
                mapRouteTaskFull_Pnl.PanelColor = Color.FromArgb(235, 235, 244); //4
                mapRouteTaskFull_Pnl.PanelOutlineColor = Color.FromArgb(235, 235, 244);
                await Task.Delay(20);
                mapRouteTaskFull_Pnl.PanelColor = Color.FromArgb(237, 238, 246); //3
                mapRouteTaskFull_Pnl.PanelOutlineColor = Color.FromArgb(237, 238, 246);
                await Task.Delay(20);
                mapRouteTaskFull_Pnl.PanelColor = Color.FromArgb(240, 240, 248); //2
                mapRouteTaskFull_Pnl.PanelOutlineColor = Color.FromArgb(240, 240, 248);
                await Task.Delay(20);
                mapRouteTaskFull_Pnl.PanelOutlineColor = Color.FromArgb(235, 235, 244);
                mapRouteTaskFull_Pnl.PanelColor = Color.FromArgb(242, 243, 250); //1
                mapRouteTaskFull_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
            }
        }

        //Кнопка короткий путь
        private async void mapRouteTaskShort_Pnl_Click(object sender, EventArgs e)
        {
            mapRouteWay = false;
            mapRouteTaskFull_stLbl_1_1.ForeColor = Color.FromArgb(49, 50, 60);
            mapRouteTaskShort_stLbl_1_1.ForeColor = Color.FromArgb(25, 55, 255);

            mapRouteTaskFull_stLbl_1_2.ForeColor = Color.FromArgb(126, 128, 143);
            mapRouteTaskShort_stLbl_1_2.ForeColor = Color.FromArgb(96, 125, 250);

            mapRouteTaskFull_Pnl.PanelColor = Color.FromArgb(242, 243, 250);
            mapRouteTaskFull_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);

            mapRouteTaskShort_Pnl.PanelColor = Color.FromArgb(242, 243, 250);
            mapRouteTaskShort_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);

            mapRouteTaskFull_stPic.Image = Properties.Resources.ПолныйМаршрут1;
            mapRouteTaskShort_stPic.Image = Properties.Resources.КороткийМаршрут2;
            mapRouteTaskFull_Pnl.Cursor = Cursors.Hand;
            mapRouteTaskShort_Pnl.Cursor = Cursors.Arrow;
            mapRouteTaskShort_Pnl.Enabled = false;
            mapRouteTaskFull_Pnl.Enabled = true;

            mapRouteTaskShort_Pnl.Rounding = new Padding(12, 12, 12, 12);
            await Task.Delay(05);
            mapRouteTaskShort_Pnl.Rounding = new Padding(12, 12, 10, 10);
            await Task.Delay(05);
            mapRouteTaskShort_Pnl.Rounding = new Padding(12, 12, 8, 8);
            await Task.Delay(05);
            mapRouteTaskShort_Pnl.Rounding = new Padding(12, 12, 6, 6);
            await Task.Delay(05);
            mapRouteTaskShort_Pnl.Rounding = new Padding(12, 12, 4, 4);
            await Task.Delay(05);
            mapRouteTaskShort_Pnl.Rounding = new Padding(12, 12, 2, 2);
            await Task.Delay(05);
            mapRouteTaskShort_Pnl.Rounding = new Padding(12, 12, 0, 0);
            mapRouteTask_animationTmr.Start();
            mapRouteTaskTabel_animationTmr.Start();
        }

        private async void mapRouteTaskShort_Pnl_MouseEnter(object sender, EventArgs e)
        {
            if (mapRouteWay == true)
            {
                mapRouteTaskShort_Pnl.PanelColor = Color.FromArgb(242, 243, 250); //1
                mapRouteTaskShort_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
                await Task.Delay(20);
                mapRouteTaskShort_Pnl.PanelColor = Color.FromArgb(240, 240, 248); //2
                mapRouteTaskShort_Pnl.PanelOutlineColor = Color.FromArgb(240, 240, 248);
                await Task.Delay(20);
                mapRouteTaskShort_Pnl.PanelColor = Color.FromArgb(237, 238, 246); //3
                mapRouteTaskShort_Pnl.PanelOutlineColor = Color.FromArgb(237, 238, 246);
                await Task.Delay(20);
                mapRouteTaskShort_Pnl.PanelColor = Color.FromArgb(235, 235, 244); //4
                mapRouteTaskShort_Pnl.PanelOutlineColor = Color.FromArgb(235, 235, 244);
            }
        }

        private async void mapRouteTaskShort_Pnl_MouseLeave(object sender, EventArgs e)
        {
            if (mapRouteWay == true)
            {
                mapRouteTaskShort_Pnl.PanelColor = Color.FromArgb(235, 235, 244); //4
                mapRouteTaskShort_Pnl.PanelOutlineColor = Color.FromArgb(235, 235, 244);
                await Task.Delay(20);
                mapRouteTaskShort_Pnl.PanelColor = Color.FromArgb(237, 238, 246); //3
                mapRouteTaskShort_Pnl.PanelOutlineColor = Color.FromArgb(237, 238, 246);
                await Task.Delay(20);
                mapRouteTaskShort_Pnl.PanelColor = Color.FromArgb(240, 240, 248); //2
                mapRouteTaskShort_Pnl.PanelOutlineColor = Color.FromArgb(240, 240, 248);
                await Task.Delay(20);
                mapRouteTaskShort_Pnl.PanelColor = Color.FromArgb(242, 243, 250); //1
                mapRouteTaskShort_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
            }
        }

        //Кнопка начать маршрут
        private async void mapRouteStart_Pnl_MouseEnter(object sender, EventArgs e)
        {
            mapRouteStart_Pnl.PanelColor = Color.FromArgb(25, 55, 255); //1
            mapRouteStart_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
            await Task.Delay(20);
            mapRouteStart_Pnl.PanelColor = Color.FromArgb(26, 54, 244); //2
            mapRouteStart_Pnl.PanelOutlineColor = Color.FromArgb(26, 54, 244);
            await Task.Delay(20);
            mapRouteStart_Pnl.PanelColor = Color.FromArgb(26, 52, 232); //3
            mapRouteStart_Pnl.PanelOutlineColor = Color.FromArgb(26, 52, 232);
        }

        private async void mapRouteStart_Pnl_MouseLeave(object sender, EventArgs e)
        {
            await Task.Delay(100);
            mapRouteStart_Pnl.PanelColor = Color.FromArgb(26, 52, 232); //3
            mapRouteStart_Pnl.PanelOutlineColor = Color.FromArgb(26, 52, 232);
            await Task.Delay(20);
            mapRouteStart_Pnl.PanelColor = Color.FromArgb(26, 54, 244); //2
            mapRouteStart_Pnl.PanelOutlineColor = Color.FromArgb(26, 54, 244);
            await Task.Delay(20);
            mapRouteStart_Pnl.PanelColor = Color.FromArgb(25, 55, 255); //1
            mapRouteStart_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
        }

        //Кнопка сбросить маршрут
        private async void mapRouteReset_Pnl_MouseEnter(object sender, EventArgs e)
        {
            mapRouteReset_Pnl.PanelColor = Color.FromArgb(242, 243, 250); //1
            mapRouteReset_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
            mapRouteReset_resetLbl.ForeColor = Color.FromArgb(49, 50, 60);
            await Task.Delay(20);
            mapRouteReset_Pnl.PanelColor = Color.FromArgb(240, 195, 204); //2
            mapRouteReset_Pnl.PanelOutlineColor = Color.FromArgb(240, 195, 204);
            mapRouteReset_resetLbl.ForeColor = Color.FromArgb(118, 118, 125);
            await Task.Delay(20);
            mapRouteReset_Pnl.PanelColor = Color.FromArgb(237, 147, 158); //3
            mapRouteReset_Pnl.PanelOutlineColor = Color.FromArgb(237, 147, 158);
            mapRouteReset_resetLbl.ForeColor = Color.FromArgb(186, 187, 190);
            await Task.Delay(20);
            mapRouteReset_Pnl.PanelColor = Color.FromArgb(235, 98, 111);  //4
            mapRouteReset_Pnl.PanelOutlineColor = Color.FromArgb(235, 98, 111);
            mapRouteReset_resetLbl.ForeColor = Color.FromArgb(255, 255, 255);
            await Task.Delay(20);
            mapRouteReset_Pnl.PanelColor = Color.FromArgb(232, 50, 65);  //5
            mapRouteReset_Pnl.PanelOutlineColor = Color.FromArgb(232, 50, 65);
        }

        private async void mapRouteReset_Pnl_MouseLeave(object sender, EventArgs e)
        {
            mapRouteReset_Pnl.PanelColor = Color.FromArgb(232, 50, 65);  //5
            mapRouteReset_Pnl.PanelOutlineColor = Color.FromArgb(232, 50, 65);
            await Task.Delay(20);
            mapRouteReset_Pnl.PanelColor = Color.FromArgb(235, 98, 111); //4
            mapRouteReset_Pnl.PanelOutlineColor = Color.FromArgb(235, 98, 111);
            mapRouteReset_resetLbl.ForeColor = Color.FromArgb(255, 255, 255);
            await Task.Delay(20);
            mapRouteReset_Pnl.PanelColor = Color.FromArgb(237, 147, 158); //3
            mapRouteReset_Pnl.PanelOutlineColor = Color.FromArgb(237, 147, 158);
            mapRouteReset_resetLbl.ForeColor = Color.FromArgb(186, 187, 190);
            await Task.Delay(20);
            mapRouteReset_Pnl.PanelColor = Color.FromArgb(240, 195, 204); //2
            mapRouteReset_Pnl.PanelOutlineColor = Color.FromArgb(240, 195, 204);
            mapRouteReset_resetLbl.ForeColor = Color.FromArgb(118, 118, 125);
            await Task.Delay(20);
            mapRouteReset_Pnl.PanelColor = Color.FromArgb(242, 243, 250); //1
            mapRouteReset_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
            mapRouteReset_resetLbl.ForeColor = Color.FromArgb(49, 50, 60);
        }

        // закрытие панели машрута
        private void mapRoute_Pnl_Click(object sender, EventArgs e)
        {
            mapRoute = !mapRoute;
            mapRouteTimer.Start();
        }

        //mapTop_Pnl + mapBottom_Pnl навигационные кнопки
        //Кнопка карта в панели карта
        private void mapTopMap_Pnl_Click(object sender, EventArgs e)
        {
            map_Pnl.Visible = true;
            task_Pnl.Visible = false;
        }

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

        //Кнопка задания в панели карта
        private void mapTopTask_Pnl_Click(object sender, EventArgs e)
        {
            this.SuspendLayout();
            task_Pnl.Visible = true;
            map_Pnl.Visible = false;
            this.ResumeLayout();
        }

        private async void mapTopTask_Pnl_MouseEnter(object sender, EventArgs e)
        {
            mapTopTask_taskPic.Image = Properties.Resources.Задание1;
            await Task.Delay(20);
            mapTopTask_taskPic.Image = Properties.Resources.Задание2;
            await Task.Delay(20);
            mapTopTask_taskPic.Image = Properties.Resources.Задание3;
            await Task.Delay(20);
            mapTopTask_taskPic.Image = Properties.Resources.Задание4;
            await Task.Delay(20);
            mapTopTask_taskPic.Image = Properties.Resources.Задание5;
        }

        private async void mapTopTask_Pnl_MouseLeave(object sender, EventArgs e)
        {
            mapTopTask_taskPic.Image = Properties.Resources.Задание5;
            await Task.Delay(20);
            mapTopTask_taskPic.Image = Properties.Resources.Задание4;
            await Task.Delay(20);
            mapTopTask_taskPic.Image = Properties.Resources.Задание3;
            await Task.Delay(20);
            mapTopTask_taskPic.Image = Properties.Resources.Задание2;
            await Task.Delay(20);
            mapTopTask_taskPic.Image = Properties.Resources.Задание1;
        }

        //Кнопка настройки в панели карта
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

        //Кнопка выйти в панели карта
        private async void mapBottomExit_Pnl_Click(object sender, EventArgs e)
        {
            await UserData.LogoutAndExit(this);
        }

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

        //mapZoom_Pnl + mapLocation_Pnl Кнопки зума + Где я
        //Кнопка плюс
        private void mapZoomPlus_Pnl_Click(object sender, EventArgs e)
        {
            map_gmapCnl.Zoom++;
        }

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

        //Кнопка минус
        private void mapZoomMinus_Pnl_Click(object sender, EventArgs e)
        {
            map_gmapCnl.Zoom--;
        }

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

        //Кнопка где я
        private void mapLocation_Pnl_Click(object sender, EventArgs e)
        {
            map_gmapCnl.Position = Map.startPosition;
        }

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

        //taskWatch_Pnl панель информации о задании
        //Выезд панели информации
        private void taskWatch_animationTmr_Tick(object sender, EventArgs e)
        {
            int targetXVisible = this.ClientSize.Width - taskWatch_Pnl.Width - 14;
            int targetXHidden = this.ClientSize.Width + 5;
            int target = taskWatchOpen ? targetXVisible : targetXHidden;
            int distance = target - taskWatch_Pnl.Left;
            int step = (int)(distance * 0.2f);
            int targetJobWidth = taskWatch_Pnl.Left - taskTabel_Pnl.Left - 10;
            int jobWidthDistance = targetJobWidth - taskTabel_Pnl.Width;
            int jobWidthStep = (int)(jobWidthDistance * 0.5f);

            if (Math.Abs(distance) < 1)
            {
                taskWatch_Pnl.Left = target;
                taskTabel_Pnl.Width = targetJobWidth;
                taskWatch_animationTmr.Stop();
                return;
            }

            taskWatch_Pnl.Left += step;
            taskTabel_Pnl.Width += jobWidthStep;
        }

        private void taskWatchsTransitionClose_Pnl_Click(object sender, EventArgs e)
        {
            taskWatchOpen = !taskWatchOpen;
            taskWatch_animationTmr.Start();
        }

        //Кнопка закрыть панель информацию о задании
        private async void taskWatchsTransitionClose_Pnl_MouseEnter(object sender, EventArgs e)
        {
            taskWatchsTransitionClose_Pnl.PanelColor = Color.FromArgb(255, 255, 255);
            taskWatchsTransitionClose_Pnl.PanelOutlineColor = Color.FromArgb(255, 255, 255);
            taskWatchsTransitionClose_closeLbl.ForeColor = Color.FromArgb(49, 50, 60);
            await Task.Delay(20);
            taskWatchsTransitionClose_Pnl.PanelColor = Color.FromArgb(249, 204, 208);
            taskWatchsTransitionClose_Pnl.PanelOutlineColor = Color.FromArgb(249, 204, 208);
            taskWatchsTransitionClose_closeLbl.ForeColor = Color.FromArgb(118, 118, 125);
            await Task.Delay(20);
            taskWatchsTransitionClose_Pnl.PanelColor = Color.FromArgb(244, 153, 160);
            taskWatchsTransitionClose_Pnl.PanelOutlineColor = Color.FromArgb(244, 153, 160);
            taskWatchsTransitionClose_closeLbl.ForeColor = Color.FromArgb(186, 187, 190);
            await Task.Delay(20);
            taskWatchsTransitionClose_Pnl.PanelColor = Color.FromArgb(238, 101, 113);
            taskWatchsTransitionClose_Pnl.PanelOutlineColor = Color.FromArgb(238, 101, 113);
            taskWatchsTransitionClose_closeLbl.ForeColor = Color.FromArgb(255, 255, 255);
            await Task.Delay(20);
            taskWatchsTransitionClose_Pnl.PanelColor = Color.FromArgb(232, 50, 65);
            taskWatchsTransitionClose_Pnl.PanelOutlineColor = Color.FromArgb(232, 50, 65);
        }

        private async void taskWatchsTransitionClose_Pnl_MouseLeave(object sender, EventArgs e)
        {
            taskWatchsTransitionClose_Pnl.PanelColor = Color.FromArgb(232, 50, 65);
            taskWatchsTransitionClose_Pnl.PanelOutlineColor = Color.FromArgb(232, 50, 65);
            await Task.Delay(20);
            taskWatchsTransitionClose_Pnl.PanelColor = Color.FromArgb(238, 101, 113);
            taskWatchsTransitionClose_Pnl.PanelOutlineColor = Color.FromArgb(238, 101, 113);
            taskWatchsTransitionClose_closeLbl.ForeColor = Color.FromArgb(255, 255, 255);
            await Task.Delay(20);
            taskWatchsTransitionClose_Pnl.PanelColor = Color.FromArgb(244, 153, 160);
            taskWatchsTransitionClose_Pnl.PanelOutlineColor = Color.FromArgb(244, 153, 160);
            taskWatchsTransitionClose_closeLbl.ForeColor = Color.FromArgb(186, 187, 190);
            await Task.Delay(20);
            taskWatchsTransitionClose_Pnl.PanelColor = Color.FromArgb(249, 204, 208);
            taskWatchsTransitionClose_Pnl.PanelOutlineColor = Color.FromArgb(249, 204, 208);
            taskWatchsTransitionClose_closeLbl.ForeColor = Color.FromArgb(118, 118, 125);
            await Task.Delay(20);
            taskWatchsTransitionClose_Pnl.PanelColor = Color.FromArgb(255, 255, 255);
            taskWatchsTransitionClose_Pnl.PanelOutlineColor = Color.FromArgb(255, 255, 255);
            taskWatchsTransitionClose_closeLbl.ForeColor = Color.FromArgb(49, 50, 60);
        }

        //Кнопка готово панель информацию о задании
        private async void taskWatchsTransitionStart_Pnl_MouseEnter(object sender, EventArgs e)
        {
            taskWatchsTransitionStart_Pnl.PanelColor = Color.FromArgb(25, 55, 255); //1
            taskWatchsTransitionStart_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
            await Task.Delay(20);
            taskWatchsTransitionStart_Pnl.PanelColor = Color.FromArgb(26, 54, 244); //2
            taskWatchsTransitionStart_Pnl.PanelOutlineColor = Color.FromArgb(26, 54, 244);
            await Task.Delay(20);
            taskWatchsTransitionStart_Pnl.PanelColor = Color.FromArgb(26, 52, 232); //3
            taskWatchsTransitionStart_Pnl.PanelOutlineColor = Color.FromArgb(26, 52, 232);
        }

        private async void taskWatchsTransitionStart_Pnl_MouseLeave(object sender, EventArgs e)
        {
            taskWatchsTransitionStart_Pnl.PanelColor = Color.FromArgb(26, 52, 232); //3
            taskWatchsTransitionStart_Pnl.PanelOutlineColor = Color.FromArgb(26, 52, 232);
            await Task.Delay(20);
            taskWatchsTransitionStart_Pnl.PanelColor = Color.FromArgb(26, 54, 244); //2
            taskWatchsTransitionStart_Pnl.PanelOutlineColor = Color.FromArgb(26, 54, 244);
            await Task.Delay(20);
            taskWatchsTransitionStart_Pnl.PanelColor = Color.FromArgb(25, 55, 255); //1
            taskWatchsTransitionStart_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
        }

        //taskTabelFilter_Pnl панель фильтра для таблицы
        //Выезд панели фильтрации
        private void taskTabelUpperFilter_Pnl_Click(object sender, EventArgs e)
        {
            taskUpperFilter_animation = !taskUpperFilter_animation;
            taskTabelFilter_animationTmr.Start();
            if (taskUpperFilterArrow == false)
            {
                taskUpperFilterArrow = true;
                taskTabelUpperFilter_arrowPic.Image = Properties.Resources.КомбоБокс1;
            }
            else
            {
                taskUpperFilterArrow = false;
                taskTabelUpperFilter_arrowPic.Image = Properties.Resources.КомбоБокс2;
            }
        }

        private void taskTabelFilter_animationTmr_Tick(object sender, EventArgs e)
        {
            int step;
            int diff;

            if (taskUpperFilter_animation)
            {
                diff = taskTabelInsert_Pnl.Height - taskTabelInsert_Pnl.MinimumSize.Height;
                step = Math.Max(2, diff / 5);

                taskTabelInsert_Pnl.Height -= step;

                if (taskTabelInsert_Pnl.Height <= taskTabelInsert_Pnl.MinimumSize.Height)
                {
                    taskTabelInsert_Pnl.Height = taskTabelInsert_Pnl.MinimumSize.Height;
                    taskUpperFilter_animation = false;
                    taskTabelFilter_animationTmr.Stop();
                }
            }
            else
            {
                diff = taskTabelInsert_Pnl.MaximumSize.Height - taskTabelInsert_Pnl.Height;
                step = Math.Max(2, diff / 5);

                taskTabelInsert_Pnl.Top -= step;
                taskTabelInsert_Pnl.Height += step;

                if (taskTabelInsert_Pnl.Height >= taskTabelInsert_Pnl.MaximumSize.Height)
                {
                    taskTabelInsert_Pnl.Height = taskTabelInsert_Pnl.MaximumSize.Height;
                    taskUpperFilter_animation = true;
                    taskTabelFilter_animationTmr.Stop();
                }
            }
        }

        //Кнопка готово панель фильтрации таблицы в задании
        private async void taskTabelFilterDone_Pnl_Click(object sender, EventArgs e)
        {
            _excludedStatuses.Clear();
            taskTabel_Dgw.Rows.Clear();

            if (!taskTabelInsertFilterStatus_newCkb.Checked)
            {
                _excludedStatuses.Add(TaskOnEmployee._taskStatus[1]);
                _excludedStatuses.Add(TaskOnEmployee._taskStatus[2]);
            }
            if (!taskTabelInsertFilterStatus_processCkb.Checked)
                _excludedStatuses.Add(TaskOnEmployee._taskStatus[3]);

            if (!taskTabelInsertFilterStatus_doneCkb.Checked)
                _excludedStatuses.Add(TaskOnEmployee._taskStatus[4]);

            if (taskUpperFilter_animation)
                taskTabelUpperFilter_Pnl_Click(sender, e);

            await LoadDataAsync();
        }

        private async void taskTabelFilterDone_Pnl_MouseEnter(object sender, EventArgs e)
        {
            taskTabelInsertFilterDone_Pnl.PanelColor = Color.FromArgb(25, 55, 255); //1
            taskTabelInsertFilterDone_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
            await Task.Delay(20);
            taskTabelInsertFilterDone_Pnl.PanelColor = Color.FromArgb(26, 54, 244); //2
            taskTabelInsertFilterDone_Pnl.PanelOutlineColor = Color.FromArgb(26, 54, 244);
            await Task.Delay(20);
            taskTabelInsertFilterDone_Pnl.PanelColor = Color.FromArgb(26, 52, 232); //3
            taskTabelInsertFilterDone_Pnl.PanelOutlineColor = Color.FromArgb(26, 52, 232);
        }

        private async void taskTabelFilterDone_Pnl_MouseLeave(object sender, EventArgs e)
        {
            taskTabelInsertFilterDone_Pnl.PanelColor = Color.FromArgb(26, 52, 232); //3
            taskTabelInsertFilterDone_Pnl.PanelOutlineColor = Color.FromArgb(26, 52, 232);
            await Task.Delay(20);
            taskTabelInsertFilterDone_Pnl.PanelColor = Color.FromArgb(26, 54, 244); //2
            taskTabelInsertFilterDone_Pnl.PanelOutlineColor = Color.FromArgb(26, 54, 244);
            await Task.Delay(20);
            taskTabelInsertFilterDone_Pnl.PanelColor = Color.FromArgb(25, 55, 255); //1
            taskTabelInsertFilterDone_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
        }

        //Кнопка сбросить панель фильтрации таблицы в задании
        private async void taskTabelFilterClose_Pnl_Click(object sender, EventArgs e)
        {
            taskTabelInsertFilterStatus_newCkb.Checked = true;
            taskTabelInsertFilterStatus_processCkb.Checked = true;
            taskTabelInsertFilterStatus_doneCkb.Checked = true;

            _excludedStatuses.Clear();
            taskTabel_Dgw.Rows.Clear();

            if (taskUpperFilter_animation)
                taskTabelUpperFilter_Pnl_Click(sender, e);

            await LoadDataAsync();
        }

        private async void taskTabelFilterClose_Pnl_MouseEnter(object sender, EventArgs e)
        {
            taskTabelInsertFilterClose_Pnl.PanelColor = Color.FromArgb(242, 243, 250); //1
            taskTabelInsertFilterClose_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
            taskTabelInsertFilterClose_closePic.Image = Properties.Resources.Сброс1;
            await Task.Delay(20);
            taskTabelInsertFilterClose_Pnl.PanelColor = Color.FromArgb(240, 195, 204); //2
            taskTabelInsertFilterClose_Pnl.PanelOutlineColor = Color.FromArgb(240, 195, 204);
            taskTabelInsertFilterClose_closePic.Image = Properties.Resources.Сброс2;
            await Task.Delay(20);
            taskTabelInsertFilterClose_Pnl.PanelColor = Color.FromArgb(237, 147, 158); //3
            taskTabelInsertFilterClose_Pnl.PanelOutlineColor = Color.FromArgb(237, 147, 158);
            taskTabelInsertFilterClose_closePic.Image = Properties.Resources.Сброс3;
            await Task.Delay(20);
            taskTabelInsertFilterClose_Pnl.PanelColor = Color.FromArgb(235, 98, 111);  //4
            taskTabelInsertFilterClose_Pnl.PanelOutlineColor = Color.FromArgb(235, 98, 111);
            taskTabelInsertFilterClose_closePic.Image = Properties.Resources.Сброс4;
            await Task.Delay(20);
            taskTabelInsertFilterClose_Pnl.PanelColor = Color.FromArgb(232, 50, 65);  //5
            taskTabelInsertFilterClose_Pnl.PanelOutlineColor = Color.FromArgb(232, 50, 65);
            taskTabelInsertFilterClose_closePic.Image = Properties.Resources.Сброс5;
        }

        private async void taskTabelFilterClose_Pnl_MouseLeave(object sender, EventArgs e)
        {
            taskTabelInsertFilterClose_Pnl.PanelColor = Color.FromArgb(232, 50, 65);  //5
            taskTabelInsertFilterClose_Pnl.PanelOutlineColor = Color.FromArgb(232, 50, 65);
            taskTabelInsertFilterClose_closePic.Image = Properties.Resources.Сброс5;
            await Task.Delay(20);
            taskTabelInsertFilterClose_Pnl.PanelColor = Color.FromArgb(235, 98, 111);  //4
            taskTabelInsertFilterClose_Pnl.PanelOutlineColor = Color.FromArgb(235, 98, 111);
            taskTabelInsertFilterClose_closePic.Image = Properties.Resources.Сброс4;
            await Task.Delay(20);
            taskTabelInsertFilterClose_Pnl.PanelColor = Color.FromArgb(237, 147, 158); //3
            taskTabelInsertFilterClose_Pnl.PanelOutlineColor = Color.FromArgb(237, 147, 158);
            taskTabelInsertFilterClose_closePic.Image = Properties.Resources.Сброс3;
            await Task.Delay(20);
            taskTabelInsertFilterClose_Pnl.PanelColor = Color.FromArgb(240, 195, 204); //2
            taskTabelInsertFilterClose_Pnl.PanelOutlineColor = Color.FromArgb(240, 195, 204);
            taskTabelInsertFilterClose_closePic.Image = Properties.Resources.Сброс2;
            await Task.Delay(20);
            taskTabelInsertFilterClose_Pnl.PanelColor = Color.FromArgb(242, 243, 250); //1
            taskTabelInsertFilterClose_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
            taskTabelInsertFilterClose_closePic.Image = Properties.Resources.Сброс1;
        }

        //taskTop_Pnl + taskBottom_Pnl навигационные кнопки
        //Кнопка карта в панели задания
        private void taskTopMap_Pnl_Click(object sender, EventArgs e)
        {
            map_Pnl.Visible = true;
            task_Pnl.Visible = false;
        }

        private async void taskTopMap_Pnl_MouseEnter(object sender, EventArgs e)
        {
            taskTopMap_mapPic.Image = Properties.Resources.Карта1;
            await Task.Delay(20);
            taskTopMap_mapPic.Image = Properties.Resources.Карта2;
            await Task.Delay(20);
            taskTopMap_mapPic.Image = Properties.Resources.Карта3;
            await Task.Delay(20);
            taskTopMap_mapPic.Image = Properties.Resources.Карта4;
            await Task.Delay(20);
            taskTopMap_mapPic.Image = Properties.Resources.Карта5;
        }

        private async void taskTopMap_Pnl_MouseLeave(object sender, EventArgs e)
        {
            taskTopMap_mapPic.Image = Properties.Resources.Карта5;
            await Task.Delay(20);
            taskTopMap_mapPic.Image = Properties.Resources.Карта4;
            await Task.Delay(20);
            taskTopMap_mapPic.Image = Properties.Resources.Карта3;
            await Task.Delay(20);
            taskTopMap_mapPic.Image = Properties.Resources.Карта2;
            await Task.Delay(20);
            taskTopMap_mapPic.Image = Properties.Resources.Карта1;
        }

        //Кнопка задания в панели задания
        private async void taskTopTask_Pnl_Click(object sender, EventArgs e)
        {
            this.SuspendLayout();
            task_Pnl.Visible = true;
            map_Pnl.Visible = false;
            this.ResumeLayout();
        }

        private async void taskTopTask_Pnl_MouseEnter(object sender, EventArgs e)
        {
            taskTopTask_taskPic.Image = Properties.Resources.Задание1;
            await Task.Delay(20);
            taskTopTask_taskPic.Image = Properties.Resources.Задание2;
            await Task.Delay(20);
            taskTopTask_taskPic.Image = Properties.Resources.Задание3;
            await Task.Delay(20);
            taskTopTask_taskPic.Image = Properties.Resources.Задание4;
            await Task.Delay(20);
            taskTopTask_taskPic.Image = Properties.Resources.Задание5;
        }

        private async void taskTopTask_Pnl_MouseLeave(object sender, EventArgs e)
        {
            taskTopTask_taskPic.Image = Properties.Resources.Задание5;
            await Task.Delay(20);
            taskTopTask_taskPic.Image = Properties.Resources.Задание4;
            await Task.Delay(20);
            taskTopTask_taskPic.Image = Properties.Resources.Задание3;
            await Task.Delay(20);
            taskTopTask_taskPic.Image = Properties.Resources.Задание2;
            await Task.Delay(20);
            taskTopTask_taskPic.Image = Properties.Resources.Задание1;
        }

        private async void taskBottomSettings_Pnl_MouseEnter(object sender, EventArgs e)
        {
            taskBottomSettings_settingsPic.Image = Properties.Resources.Настройки1;
            await Task.Delay(20);
            taskBottomSettings_settingsPic.Image = Properties.Resources.Настройки2;
            await Task.Delay(20);
            taskBottomSettings_settingsPic.Image = Properties.Resources.Настройки3;
            await Task.Delay(20);
            taskBottomSettings_settingsPic.Image = Properties.Resources.Настройки4;
            await Task.Delay(20);
            taskBottomSettings_settingsPic.Image = Properties.Resources.Настройки5;
        }

        private async void taskBottomSettings_Pnl_MouseLeave(object sender, EventArgs e)
        {
            taskBottomSettings_settingsPic.Image = Properties.Resources.Настройки5;
            await Task.Delay(20);
            taskBottomSettings_settingsPic.Image = Properties.Resources.Настройки4;
            await Task.Delay(20);
            taskBottomSettings_settingsPic.Image = Properties.Resources.Настройки3;
            await Task.Delay(20);
            taskBottomSettings_settingsPic.Image = Properties.Resources.Настройки2;
            await Task.Delay(20);
            taskBottomSettings_settingsPic.Image = Properties.Resources.Настройки1;
        }

        //Кнопка выйти в панели задания
        private async void taskBottomExit_Pnl_Click(object sender, EventArgs e)
        {
            await UserData.LogoutAndExit(this);
        }

        private async void taskBottomExit_Pnl_MouseEnter(object sender, EventArgs e)
        {
            taskBottomExit_exitPic.Image = Properties.Resources.Выход1;
            await Task.Delay(20);
            taskBottomExit_exitPic.Image = Properties.Resources.Выход2;
            await Task.Delay(20);
            taskBottomExit_exitPic.Image = Properties.Resources.Выход3;
            await Task.Delay(20);
            taskBottomExit_exitPic.Image = Properties.Resources.Выход4;
            await Task.Delay(20);
            taskBottomExit_exitPic.Image = Properties.Resources.Выход5;
        }

        private async void taskBottomExit_Pnl_MouseLeave(object sender, EventArgs e)
        {
            taskBottomExit_exitPic.Image = Properties.Resources.Выход5;
            await Task.Delay(20);
            taskBottomExit_exitPic.Image = Properties.Resources.Выход4;
            await Task.Delay(20);
            taskBottomExit_exitPic.Image = Properties.Resources.Выход3;
            await Task.Delay(20);
            taskBottomExit_exitPic.Image = Properties.Resources.Выход2;
            await Task.Delay(20);
            taskBottomExit_exitPic.Image = Properties.Resources.Выход1;
        }

        private async void PostmanForm_Load(object sender, EventArgs e)
        {
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
        }

        private void InitializeTimerUpdateRouteTaskComboBox()
        {
            _routeTaskUpdateTimer = new Timer { Interval = 5000 };
            _routeTaskUpdateTimer.Tick += async (s, ev) => await UpdateRouteTaskComboBox();
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
            taskTabel_Dgw.Scroll += (s, t) => ResetTimer();
            taskTabel_Dgw.MouseWheel += (s, t) => ResetTimer();
            _searchTasks = new SearchFilter<DataBase.Tasks>();
            _loaderTasks = new LazyLoader<DataBase.Tasks>(_searchTasks);
        }

        private async Task CheckScrollAndLoad()
        {
            // Проверка, не грузим ли мы уже
            int firstVisible = taskTabel_Dgw.FirstDisplayedScrollingRowIndex;
            if (firstVisible < 0)
                return;

            // Если прокрутили вниз
            if (firstVisible + taskTabel_Dgw.DisplayedRowCount(false) >= taskTabel_Dgw.RowCount - 10)
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

                        await DataTables.AddTaskRow(taskTabel_Dgw, item, taskTabel_Dgw.RowCount + 1, numMarkers);
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

        private async void taskTabel_Dgw_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && e.RowIndex < taskTabel_Dgw.Rows.Count - 1)
                {
                    var row = taskTabel_Dgw.Rows[e.RowIndex];
                    var id = Guid.Parse(row.Cells["Id"].Value.ToString());

                    var task = await DataBase._client.From<DataBase.Tasks>().Where(x => x.Id == id).Single();
                    if (task == null)
                        return;

                    await TaskOnEmployee.UpdateSelectedTask(task);
                    TaskOnEmployee.VisualChangedStatus(taskWatchsTransitionStart_Pnl, taskWatchsTransitionClose_startLbl, task,
                        taskWatchTransitionLayerRack_progressPth, taskWatch_nameLbl, taskWatch_deliveryLbl,
                        taskWatch_endingLbl, taskWatchsTransitionReadertxt_readerTxt, taskWatch_tagsLbl);
                }
            }
            catch { }

            if (!taskWatchOpen)
                taskWatch_animationTmr.Start();
        }

        private void taskWatchsTransitionStart_Pnl_Click(object sender, EventArgs e)
        {
            TaskOnEmployee.ClickTasksButton(taskWatchsTransitionStart_Pnl, taskWatchsTransitionClose_startLbl, taskTabel_Dgw,
                taskWatchTransitionLayerRack_progressPth, taskWatch_nameLbl, taskWatch_deliveryLbl,
                taskWatch_endingLbl, taskWatchsTransitionReadertxt_readerTxt, taskWatch_tagsLbl);
        }

        private void mapRouteTaskTabel_animationTmr_Tick(object sender, EventArgs e)
        {
            int step;
            int diff;
            if (mapRouteTaskTabel_animation1)
            {
                diff = mapRouteTaskTabel_Pnl.Height - mapRouteTaskTabel_Pnl.MinimumSize.Height;
                step = Math.Max(1, diff / 4);
                mapRouteTaskTabel_Pnl.Height -= step;
                mapRouteInfo_Pnl.Top -= step;
                mapRouteReset_Pnl.Top -= step;
                mapRouteStart_Pnl.Top -= step;
                if (mapRouteTaskTabel_Pnl.Height <= mapRouteTaskTabel_Pnl.MinimumSize.Height)
                {
                    mapRouteTaskTabel_Pnl.Height = mapRouteTaskTabel_Pnl.MinimumSize.Height;
                    mapRouteTaskTabel_animation1 = false;
                    mapRouteTaskTabel_animationTmr.Stop();
                }
            }
            else
            {
                diff = mapRouteTaskTabel_Pnl.MaximumSize.Height - mapRouteTaskTabel_Pnl.Height;
                step = Math.Max(1, diff / 4);
                mapRouteTaskTabel_Pnl.Height += step;
                mapRouteInfo_Pnl.Top += step;
                mapRouteReset_Pnl.Top += step;
                mapRouteStart_Pnl.Top += step;
                if (mapRouteTaskTabel_Pnl.Height >= mapRouteTaskTabel_Pnl.MaximumSize.Height)
                {
                    mapRouteTaskTabel_Pnl.Height = mapRouteTaskTabel_Pnl.MaximumSize.Height;

                    mapRouteTaskTabel_animation1 = true;
                    mapRouteTaskTabel_animationTmr.Stop();
                }
            }
        }
        private void mapRouteTask_animationTmr_Tick(object sender, EventArgs e)
        {
            int step;
            int diff;

            if (mapRouteTaskTabel_animation2)
            {
                diff = mapRoute_Pnl.Height - mapRoute_Pnl.MinimumSize.Height;
                step = Math.Max(2, diff / 5);

                mapRoute_Pnl.Height -= step;

                if (mapRoute_Pnl.Height <= mapRoute_Pnl.MinimumSize.Height)
                {
                    mapRoute_Pnl.Height = mapRoute_Pnl.MinimumSize.Height;
                    mapRouteTaskTabel_animation2 = false;
                    mapRouteTask_animationTmr.Stop();
                }
            }
            else
            {
                diff = mapRoute_Pnl.MaximumSize.Height - mapRoute_Pnl.Height;
                step = Math.Max(2, diff / 5);

                mapRoute_Pnl.Top -= step;
                mapRoute_Pnl.Height += step;

                if (mapRoute_Pnl.Height >= mapRoute_Pnl.MaximumSize.Height)
                {
                    mapRoute_Pnl.Height = mapRoute_Pnl.MaximumSize.Height;
                    mapRouteTaskTabel_animation2 = true;
                    mapRouteTask_animationTmr.Stop();
                }
            }
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

        // ================== Маршрут по заданию ==================

        /// <summary>
        /// Каждые 5 секунд проверяет изменения в заданиях со статусом "В работе".
        /// Добавляет новые, убирает исчезнувшие — без полного сброса списка.
        /// Задания без прикреплённых меток игнорируются.
        /// </summary>
        private async Task UpdateRouteTaskComboBox()
        {
            try
            {
                var response = await DataBase._client.From<DataBase.Tasks>()
                    .Where(x => x.IdEmployee == UserData.CurrentUser.Employee.Id
                             && x.Status == TaskOnEmployee._taskStatus[3])
                    .Get();

                // Фильтруем: только задания с хотя бы одной меткой
                var activeTasks = response.Models
                    .Where(t => !string.IsNullOrWhiteSpace(t.AttachedMarkers))
                    .ToList();

                var currentIds = new HashSet<Guid>(_routeTaskItems.Select(t => t.Id));
                var newIds = new HashSet<Guid>(activeTasks.Select(t => t.Id));

                // Если ничего не изменилось — не трогаем комбо-бокс
                if (currentIds.SetEquals(newIds))
                    return;

                // Запоминаем ID выбранного задания, чтобы восстановить выбор
                Guid? selectedId = null;
                int prevIdx = mapRouteTask_taskCmb.SelectedIndex;
                if (prevIdx >= 0 && prevIdx < _routeTaskItems.Count)
                    selectedId = _routeTaskItems[prevIdx].Id;

                _isUpdatingTaskCmb = true;

                _routeTaskItems = activeTasks;

                mapRouteTask_taskCmb.Items = _routeTaskItems
                    .Select(t => $"Задание {t.DateIssue:dd.MM.yy}")
                    .ToArray();

                // Восстанавливаем выбор если задание осталось в работе
                if (selectedId.HasValue)
                    mapRouteTask_taskCmb.SelectedIndex = _routeTaskItems.FindIndex(t => t.Id == selectedId.Value);
                else
                    mapRouteTask_taskCmb.SelectedIndex = -1;

                _isUpdatingTaskCmb = false;

                _isDoubleCheckInternet = false;
            }
            catch (Exception ex)
            {
                if (!DataBase._internetConnection && _isDoubleCheckInternet)
                {
                    Logger.Error("Ошибка обновления заданий маршрута", ex);
                    _isUpdatingTaskCmb = false;
                }
                else if (!DataBase._internetConnection && !_isDoubleCheckInternet)
                    _isDoubleCheckInternet = true;
            }
        }

        /// <summary>
        /// Срабатывает при выборе задания в комбо-боксе.
        /// Загружает метки задания и строит панель улиц.
        /// </summary>
        private async void mapRouteTask_taskCmb_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isUpdatingTaskCmb) return;

            int idx = mapRouteTask_taskCmb.SelectedIndex;
            if (idx < 0 || idx >= _routeTaskItems.Count)
            {
                _currentTaskMarkers.Clear();
                mapRouteInfo_tagsLbl.Content = "0 штук";
                mapRouteTaskTabelSelect_Pnl.Controls.Clear();
                return;
            }

            await LoadTaskMarkersAsync(_routeTaskItems[idx]);
        }

        /// <summary>
        /// Загружает из БД все метки задания одним IN-запросом.
        /// Обновляет счётчик меток и перестраивает панель улиц.
        /// </summary>
        private async Task LoadTaskMarkersAsync(DataBase.Tasks task)
        {
            try
            {
                // Парсим UUID из строки вида "uuid1,uuid2,..."
                var markerIds = task.AttachedMarkers
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .Select(s => { Guid g; return Guid.TryParse(s, out g) ? (Guid?)g : null; })
                    .Where(g => g.HasValue)
                    .Select(g => g.Value)
                    .Distinct()
                    .ToList();

                if (markerIds.Count == 0)
                {
                    _currentTaskMarkers.Clear();
                    mapRouteInfo_tagsLbl.Content = "0 штук";
                    mapRouteTaskTabelSelect_Pnl.Controls.Clear();
                    return;
                }

                // Один запрос для всех меток сразу
                var response = await DataBase._client.From<DataBase.Markers>()
                    .Filter("id", "IN", markerIds.ToArray())
                    .Get();

                _currentTaskMarkers = response.Models;

                mapRouteInfo_tagsLbl.Content = $"{_currentTaskMarkers.Count} {GetForm(_currentTaskMarkers.Count, "штука", "штуки", "штук")}";

                BuildStreetCheckBoxes(mapRouteTaskTabelText_textTxt.Content);
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка загрузки меток задания", ex);
                Logger.ShowError("Ошибка загрузки меток задания");
            }
        }

        private void mapRouteTaskTabelText_textTxt_ContentChanged(object sender, EventArgs e)
        {
            BuildStreetCheckBoxes(mapRouteTaskTabelText_textTxt.Content);
        }

        /// <summary>
        /// Заполняет mapRouteTaskTabelSelect_Pnl чекбоксами улиц.
        /// Каждая строка: [CheckBox с названием улицы] [Label с количеством точек].
        /// Параметр filter — текст из поля поиска, фильтрует по названию улицы.
        /// По умолчанию все чекбоксы отмечены.
        /// </summary>
        private void BuildStreetCheckBoxes(string filter = "")
        {
            mapRouteTaskTabelSelect_Pnl.SuspendLayout();
            mapRouteTaskTabelSelect_Pnl.Controls.Clear();

            var streetGroups = _currentTaskMarkers
                .GroupBy(m => m.Street ?? string.Empty)
                .OrderBy(g => g.Key)
                .ToList();

            int panelWidth = mapRouteTaskTabelSelect_Pnl.ClientSize.Width > 4
                ? mapRouteTaskTabelSelect_Pnl.ClientSize.Width - 4
                : 290;

            int y = 2;
            foreach (var group in streetGroups)
            {
                string street = group.Key;
                int count = group.Count();

                if (!string.IsNullOrWhiteSpace(filter) &&
                    !street.ToLower().Contains(filter.ToLower()))
                    continue;

                var row = new Panel
                {
                    Width = panelWidth - 20,
                    Height = 30,
                    BackColor = Color.White,
                    Tag = street,
                    Location = new Point((panelWidth - (panelWidth - 10)) / 2, y)
                };

                int checkBoxWidth = row.Width * 4 / 5;
                int labelWidth = row.Width - checkBoxWidth;

                var checkBox = new cuiCheckbox
                {
                    Checked = true,
                    AutoSize = false,
                    Size = new Size(checkBoxWidth, 18),
                    Location = new Point(0, (row.Height - 18) / 2),
                    Content = street,
                    ForeColor = Color.FromArgb(49, 50, 60),
                    Font = new Font("Montserrat SemiBold", 9f, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                    CheckedForeground = Color.FromArgb(25, 55, 255),
                    CheckedOutlineColor = Color.FromArgb(25, 55, 255),
                    CheckedSymbolColor = Color.White,
                };

                var countLbl = new cuiLabel
                {
                    Content = $"{count} {GetForm(count, "точка", "точки", "точек")}",
                    AutoSize = false,
                    Size = new Size(labelWidth, 18),
                    Location = new Point(checkBoxWidth, (row.Height - 18) / 2),
                    ForeColor = Color.FromArgb(126, 128, 143),
                    Font = new Font("Montserrat SemiBold", 9f, FontStyle.Bold),
                    VerticalAlignment = StringAlignment.Center,
                    HorizontalAlignment = StringAlignment.Far,
                };

                row.Controls.Add(checkBox);
                row.Controls.Add(countLbl);
                mapRouteTaskTabelSelect_Pnl.Controls.Add(row);
                y += row.Height;
            }

            mapRouteTaskTabelSelect_Pnl.ResumeLayout();
        }

        public static string GetForm(int number, string one, string two, string five)
        {
            int n = number % 100;
            if (n >= 11 && n <= 19) return five;
            n = n % 10;
            if (n == 1) return one;
            if (n >= 2 && n <= 4) return two;
            return five;
        }

        /// <summary>
        /// Сбрасывает маршрут: очищает линии на карте, отмечает все улицы,
        /// обнуляет поля времени и расстояния.
        /// </summary>
        private void mapRouteReset_Pnl_Click(object sender, EventArgs e)
        {
            Map._routesOverlay.Clear();
            map_gmapCnl.Refresh();

            // Отмечаем все чекбоксы улиц
            foreach (Control row in mapRouteTaskTabelSelect_Pnl.Controls)
            {
                foreach (Control c in row.Controls)
                {
                    if (c is CheckBox cb)
                        cb.Checked = true;
                }
            }

            mapRouteInfo_timeLbl.Content = "—-";
            mapRouteInfo_kmLbl.Content = "—-";
        }

        private async void mapRouteStart_Pnl_Click(object sender, EventArgs e)
        {
            if (_currentTaskMarkers.Count == 0)
                return;

            string text = mapRouteStart_startLbl.Content;
            try
            {
                mapRouteStart_Pnl.Enabled = false;
                mapRouteStart_startLbl.Content = "Строим маршрут...";
                List<DataBase.Markers> markersToRoute;

                if (mapRouteWay) // true = весь маршрут
                {
                    markersToRoute = _currentTaskMarkers;
                }
                else // false = свой маршрут (только выбранные улицы)
                {
                    var selectedStreets = new HashSet<string>();
                    foreach (Control row in mapRouteTaskTabelSelect_Pnl.Controls)
                    {
                        if (row.Tag is string street)
                        {
                            foreach (Control c in row.Controls)
                            {
                                if (c is CheckBox cb && cb.Checked)
                                    selectedStreets.Add(street);
                            }
                        }
                    }
                    markersToRoute = _currentTaskMarkers
                        .Where(m => selectedStreets.Contains(m.Street ?? string.Empty))
                        .ToList();
                }

                if (markersToRoute.Count == 0)
                    return;

                var gMapMarkers = markersToRoute
                    .Select(m =>
                    {
                        var pt = new GMap.NET.PointLatLng(m.Latitude, m.Longitude);
                        var gm = new GMap.NET.WindowsForms.Markers.GMarkerGoogle(
                            pt, GMap.NET.WindowsForms.Markers.GMarkerGoogleType.blue_dot);
                        gm.Tag = m.Id;
                        return (GMap.NET.WindowsForms.GMapMarker)gm;
                    })
                    .ToList();

                if (_openWalk || mapRouteBike)
                    Route.metreSeconds = Route._walkSpeed;
                else
                    Route.metreSeconds = Route._bikeSpeed;

                await Route.BuildRoute(map_gmapCnl, gMapMarkers);
                map_gmapCnl.Refresh();
                mapRouteStart_Pnl.Enabled = true;
                mapRouteStart_startLbl.Content = text;

                mapRouteInfo_timeLbl.Content = Route.FormatTime(Route.estimatedTimeMinutes);
                mapRouteInfo_kmLbl.Content = Route.FormatDistance(Route.routeDistance);
            }
            catch (Exception ex)
            {
                mapRouteStart_Pnl.Enabled = true;
                mapRouteStart_startLbl.Content = text;
                Logger.Error("Ошибка построения маршрута по заданию", ex);
                Logger.ShowError("Ошибка построения маршрута");
            }
        }
    }
}
