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
    public partial class OperatorForm : Form
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
        // Кнопки месяцев, во вкладке [Оформление подписок]
        bool subCreatureGive_jan = false;
        bool subCreatureGive_feb = false;
        bool subCreatureGive_mar = false;
        bool subCreatureGive_apr = false;
        bool subCreatureGive_may = false;
        bool subCreatureGive_jun = false;
        bool subCreatureGive_jul = false;
        bool subCreatureGive_aug = false;
        bool subCreatureGive_sep = false;
        bool subCreatureGive_oct = false;
        bool subCreatureGive_nov = false;
        bool subCreatureGive_dec = false;
        // Кнопки доставки, во вкладке [Оформление подписок]
        bool subCreatureGive_delivery = false;
        // Cттепер, во вкладке [Оформление подписок]
        int subCreatureGive_number = 1;
        // Панель с оформления подписки, во вкладке [Оформление подписок]
        bool subCreature_animation = true;
        // Панель информации о подписки, во вкладке [Оформление подписок]
        bool subCreatureTitle_animation = true;
        // Комбо бокс фильтра, во вкладке [Оформление подписок]
        bool subFilter_change = true;
        bool subTabel_arrow = false;
        // Панели фильтра изданий, во вкладке [Оформление подписок]
        bool subTabelFilter1_animation = false;
        int subTabelFilter1_up = -270;
        int subTabelFilter1_down = 53;
        // Панели фильтра подписок, во вкладке [Оформление подписок]
        bool subTabelFilter2_animation = false;
        int subTabelFilter2_up = -270;
        int subTabelFilter2_down = 53;
        // Панель поиска улицы/дома/квартиры, во вкладке [Оформление подписок]
        bool subRegistrationSearch_animtaion = false;
        // Панель поиска улицы/дома/квартиры, во вкладке [Регистрация читателей]
        bool readerRegistrationSearch1_animtaion = false;
        // Панель поиска открытия информации о задание, во вкладке [Задание]
        bool taskWatchOpen = false;
        // Панель поиска фильтра, во вкладке [Задание]
        bool taskUpperFilter = false;
        // Картинка комбо бокса фильтра, во вкладке [Задание]
        bool taskUpperFilterArrow = false;


        private Timer _scrollDebounceTimer;
        private TableType _currentTable = TableType.Subscriptions;
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
        private readonly HashSet<string> _excludedStatuses = new HashSet<string>();

        private readonly HashSet<Guid> _locallyAddedSubscriptionIds = new HashSet<Guid>();
        private readonly HashSet<Guid> _locallyAddedReaderIds = new HashSet<Guid>();

        private DataBase.Readers _selectedReader = null;
        private DataBase.Markers _selectedMarker = null;
        private List<DataBase.Markers> _allMarkers = new List<DataBase.Markers>();
        private List<DataBase.Markers> _cachedMarkers = new List<DataBase.Markers>();
        private Guid _selectedSubscriptionReaderId = Guid.Empty;
        private DataBase.Editions _selectedEdition;
        private DataBase.Subscriptions _selectedSubscription;
        private List<string> _editionTypeFilters = new List<string>(); // активные типы изданий

        public enum TableType
        {
            Editions,
            Subscriptions,
            Readers,
            Tasks
        }

        public OperatorForm()
        {
            InitializeComponent();
            OpenPanel();
            RoundedCorners();
            monthCheck();
        }
        // Метод который выполняют всякую чушь
        private void OpenPanel()
        {
            subTabelEdition_Pnl.Location = new Point(5, -260);
            subTabelSubscription_Pnl.Location = new Point(5, -260);

            readerRegistrationSearch_Pnl.Size = new Size(360, 0);
            subCreatureGiveSearch_Pnl.Size = new Size(460, 0);

            sub_Pnl.Visible = false;
            sub_Pnl.Dock = DockStyle.Fill;
            reader_Pnl.Visible = false;
            reader_Pnl.Dock = DockStyle.Fill;
            task_Pnl.Visible = true;
            task_Pnl.Dock = DockStyle.Fill;

            subCreatureGive_stLbl_1_5.Content = DateTime.Now.Year.ToString();
        }
        // Метод скругляющий элементы на форме
        private void RoundedCorners()
        {
            SetRoundedCorners(subTabel_Pnl, 24);
            SetRoundedCorners(subTabelEdition_Pnl, 24);
            SetRoundedCorners(subTabelSubscription_Pnl, 24);
            SetRoundedCorners(subTop_Pnl, 23);
            SetRoundedCorners(subBottom_Pnl, 23);

            SetRoundedCorners(readerTabel_Pnl, 24);

            SetRoundedCorners(taskTabel_Pnl, 24);
        }
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
        // upper_Pnl Вверхняя панель формы
        // Кнопка закрытие формы в верхней части панели
        private void upper_closeBtn_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        // Кнопка скрыть форму в верхней части панели
        private void upper_Pnl_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
                SetRoundedCorners(this, 15);
            }
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
        // Кнопка минимальный размер формы в верхней части панели
        private void upper_minimizeBtn_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }
        // subCreature_Pnl панель с информацией о подписке/издании, во вкладке [Оформление подписок]
        // Таймер открытия панели информацией о подписке/издании, во вкладке [Оформление подписок]
        private void subCreature_animationTmr_Tick(object sender, EventArgs e)
        {
            int targetXVisible = this.ClientSize.Width - subCreature_Pnl.Width - 15;
            int targetXHidden = this.ClientSize.Width + 5;
            int target = subCreature_animation ? targetXVisible : targetXHidden;
            int distance = target - subCreature_Pnl.Left;
            int step = (int)(distance * 0.3f);
            int targetJobWidth = subCreature_Pnl.Left - subTabel_Pnl.Left - 10;
            int jobWidthDistance = targetJobWidth - subTabel_Pnl.Width;
            int jobWidthStep = (int)(jobWidthDistance * 0.4f);
            if (Math.Abs(distance) < 1)
            {
                subCreature_Pnl.Left = target;
                subTabel_Pnl.Width = targetJobWidth;
                subCreature_animationTmr.Stop();
                return;
            }
            subCreature_Pnl.Left += step;
            subTabel_Pnl.Width += jobWidthStep;
        }
        // subCreatureTitle_Pnl Панели с информацией, во вкладке [Оформление подписок]
        // Таймер открывающий панель с информацией, во вкладке [Оформление подписок]
        private void subCreatureTitle_Pnl_Tick(object sender, EventArgs e)
        {
            int step;
            int diff;
            if (subCreatureTitle_animation)
            {
                diff = subCreatureGive_Pnl.Height - subCreatureGive_Pnl.MinimumSize.Height;
                step = Math.Max(2, diff / 5);
                subCreatureGive_Pnl.Height -= step;
                subCreatureGive_Pnl.Top += step;
                if (subCreatureGive_Pnl.Height <= subCreatureGive_Pnl.MinimumSize.Height)
                {
                    subCreatureGive_Pnl.Height = subCreatureGive_Pnl.MinimumSize.Height;
                    subCreatureTitle_animation = false;
                    subCreatureTitle_Pnl_animationTmr.Stop();
                }
            }
            else
            {
                diff = subCreatureGive_Pnl.MaximumSize.Height - subCreatureGive_Pnl.Height;
                step = Math.Max(2, diff / 5);
                subCreatureGive_Pnl.Height += step;
                subCreatureGive_Pnl.Top -= step;
                if (subCreatureGive_Pnl.Height >= subCreatureGive_Pnl.MaximumSize.Height)
                {
                    subCreatureGive_Pnl.Height = subCreatureGive_Pnl.MaximumSize.Height;
                    subCreatureTitle_animation = true;
                    subCreatureTitle_Pnl_animationTmr.Stop();
                }
            }
        }
        // Верхняя часть панели открывающая и закрывающая панель с информацией, во вкладке [Оформление подписок]
        private void subCreatureTitle_Pnl_Click(object sender, EventArgs e)
        {
            subCreatureTitle_Pnl_animationTmr.Start();
            if (subCreatureTitle_animation == false)
            {
                subCreatureTitleOpen_arrowPic.Image = Properties.Resources.КомбоБокс2;

            }
            else if (subCreatureTitle_animation == true)
            {
                subCreatureTitleOpen_arrowPic.Image = Properties.Resources.КомбоБокс1;
            }
        }
        // subCreatureGiveCounter_Pnl Степпер количество комплектов в панели с оформлением, во вкладке [Оформление подписок]
        // Кнопка плюс в панели с оформлением, во вкладке [Оформление подписок]
        private void subCreatureGiveCounterPlus_Pnl_Click(object sender, EventArgs e)
        {
            if (subCreatureGive_number == 20)
            {
                subCreatureGiveCounter_numberLbl_1_1.Content = ("20");
            }
            else
            {
                subCreatureGive_number = subCreatureGive_number + 1;
                subCreatureGiveCounter_numberLbl_1_1.Content = Convert.ToString(subCreatureGive_number);
            }
        }
        private async void subCreatureGiveCounterPlus_Pnl_MouseEnter(object sender, EventArgs e)
        {
            subCreatureGiveCounterPlus_plusPic.Image = Properties.Resources.Плюс1; //1
            await Task.Delay(20);
            subCreatureGiveCounterPlus_plusPic.Image = Properties.Resources.Плюс2; //2
            await Task.Delay(20);
            subCreatureGiveCounterPlus_plusPic.Image = Properties.Resources.Плюс3; //3
            await Task.Delay(20);
            subCreatureGiveCounterPlus_plusPic.Image = Properties.Resources.Плюс4; //4
            await Task.Delay(20);
            subCreatureGiveCounterPlus_plusPic.Image = Properties.Resources.Плюс5; //5
        }
        private async void subCreatureGiveCounterPlus_Pnl_MouseLeave(object sender, EventArgs e)
        {
            await Task.Delay(100);
            subCreatureGiveCounterPlus_plusPic.Image = Properties.Resources.Плюс5; //5
            await Task.Delay(20);
            subCreatureGiveCounterPlus_plusPic.Image = Properties.Resources.Плюс4; //4
            await Task.Delay(20);
            subCreatureGiveCounterPlus_plusPic.Image = Properties.Resources.Плюс3; //3
            await Task.Delay(20);
            subCreatureGiveCounterPlus_plusPic.Image = Properties.Resources.Плюс2; //2
            await Task.Delay(20);
            subCreatureGiveCounterPlus_plusPic.Image = Properties.Resources.Плюс1; //1
        }
        // Кнопка минус в панели с оформлением, во вкладке [Оформление подписок]
        private void subCreatureGiveCounterMinus_Pnl_Click(object sender, EventArgs e)
        {
            if (subCreatureGive_number == 1)
            {
                subCreatureGiveCounter_numberLbl_1_1.Content = ("1");
            }
            else
            {
                subCreatureGive_number = subCreatureGive_number - 1;
                subCreatureGiveCounter_numberLbl_1_1.Content = Convert.ToString(subCreatureGive_number);
            }
        }
        private async void subCreatureGiveCounterMinus_Pnl_MouseEnter(object sender, EventArgs e)
        {
            subCreatureGiveCounterMinus_minusPic.Image = Properties.Resources.Минус1; //1
            await Task.Delay(20);
            subCreatureGiveCounterMinus_minusPic.Image = Properties.Resources.Минус2; //2
            await Task.Delay(20);
            subCreatureGiveCounterMinus_minusPic.Image = Properties.Resources.Минус3; //3
            await Task.Delay(20);
            subCreatureGiveCounterMinus_minusPic.Image = Properties.Resources.Минус4; //4
            await Task.Delay(20);
            subCreatureGiveCounterMinus_minusPic.Image = Properties.Resources.Минус5; //5
        }
        private async void subCreatureGiveCounterMinus_Pnl_MouseLeave(object sender, EventArgs e)
        {
            await Task.Delay(100);
            subCreatureGiveCounterMinus_minusPic.Image = Properties.Resources.Минус5; //5
            await Task.Delay(20);
            subCreatureGiveCounterMinus_minusPic.Image = Properties.Resources.Минус4; //4
            await Task.Delay(20);
            subCreatureGiveCounterMinus_minusPic.Image = Properties.Resources.Минус3; //3
            await Task.Delay(20);
            subCreatureGiveCounterMinus_minusPic.Image = Properties.Resources.Минус2; //2
            await Task.Delay(20);
            subCreatureGiveCounterMinus_minusPic.Image = Properties.Resources.Минус1; //1
        }
        // subCreatureGive_Pnl Кнопки выбора месяца в панеле с оформлением, во вкладке [Оформление подписок]
        // Проверка на прошлые месяца в панеле с оформлением, во вкладке [Оформление подписок]
        public void monthCheck()
        {
            int currentMonth = DateTime.Now.Month;
            if (currentMonth >= 1) { subCreatureGiveJan_Pnl.Enabled = false; subCreatureGiveJan_Pnl.PanelColor = Color.FromArgb(245, 245, 249); subCreatureGiveJan_Pnl.PanelOutlineColor = Color.FromArgb(245, 245, 249); subCreatureGiveJan_stLbl.ForeColor = Color.FromArgb(202, 203, 207); }
            if (currentMonth >= 2) { subCreatureGiveFeb_Pnl.Enabled = false; subCreatureGiveFeb_Pnl.PanelColor = Color.FromArgb(245, 245, 249); subCreatureGiveFeb_Pnl.PanelOutlineColor = Color.FromArgb(245, 245, 249); subCreatureGiveFeb_stLbl.ForeColor = Color.FromArgb(202, 203, 207); }
            if (currentMonth >= 3) { subCreatureGiveMar_Pnl.Enabled = false; subCreatureGiveMar_Pnl.PanelColor = Color.FromArgb(245, 245, 249); subCreatureGiveMar_Pnl.PanelOutlineColor = Color.FromArgb(245, 245, 249); subCreatureGiveMar_stLbl.ForeColor = Color.FromArgb(202, 203, 207); }
            if (currentMonth >= 4) { subCreatureGiveApr_Pnl.Enabled = false; subCreatureGiveApr_Pnl.PanelColor = Color.FromArgb(245, 245, 249); subCreatureGiveApr_Pnl.PanelOutlineColor = Color.FromArgb(245, 245, 249); subCreatureGiveApr_stLbl.ForeColor = Color.FromArgb(202, 203, 207); }
            if (currentMonth >= 5) { subCreatureGiveMay_Pnl.Enabled = false; subCreatureGiveMay_Pnl.PanelColor = Color.FromArgb(245, 245, 249); subCreatureGiveMay_Pnl.PanelOutlineColor = Color.FromArgb(245, 245, 249); subCreatureGiveMay_stLbl.ForeColor = Color.FromArgb(202, 203, 207); }
            if (currentMonth >= 6) { subCreatureGiveJul_Pnl.Enabled = false; subCreatureGiveJul_Pnl.PanelColor = Color.FromArgb(245, 245, 249); subCreatureGiveJul_Pnl.PanelOutlineColor = Color.FromArgb(245, 245, 249); subCreatureGiveJun_stLbl.ForeColor = Color.FromArgb(202, 203, 207); }
            if (currentMonth >= 7) { subCreatureGiveJun_Pnl.Enabled = false; subCreatureGiveJun_Pnl.PanelColor = Color.FromArgb(245, 245, 249); subCreatureGiveJun_Pnl.PanelOutlineColor = Color.FromArgb(245, 245, 249); subCreatureGiveJul_stLbl.ForeColor = Color.FromArgb(202, 203, 207); }
            if (currentMonth >= 8) { subCreatureGiveAug_Pnl.Enabled = false; subCreatureGiveAug_Pnl.PanelColor = Color.FromArgb(245, 245, 249); subCreatureGiveAug_Pnl.PanelOutlineColor = Color.FromArgb(245, 245, 249); subCreatureGiveAug_stLbl.ForeColor = Color.FromArgb(202, 203, 207); }
            if (currentMonth >= 9) { subCreatureGiveSep_Pnl.Enabled = false; subCreatureGiveSep_Pnl.PanelColor = Color.FromArgb(245, 245, 249); subCreatureGiveSep_Pnl.PanelOutlineColor = Color.FromArgb(245, 245, 249); subCreatureGiveSep_stLbl.ForeColor = Color.FromArgb(202, 203, 207); }
            if (currentMonth >= 10) { subCreatureGiveOct_Pnl.Enabled = false; subCreatureGiveOct_Pnl.PanelColor = Color.FromArgb(245, 245, 249); subCreatureGiveOct_Pnl.PanelOutlineColor = Color.FromArgb(245, 245, 249); subCreatureGiveOct_stLbl.ForeColor = Color.FromArgb(202, 203, 207); }
            if (currentMonth >= 11) { subCreatureGiveNov_Pnl.Enabled = false; subCreatureGiveNov_Pnl.PanelColor = Color.FromArgb(245, 245, 249); subCreatureGiveNov_Pnl.PanelOutlineColor = Color.FromArgb(245, 245, 249); subCreatureGiveNov_stLbl.ForeColor = Color.FromArgb(202, 203, 207); }
            if (currentMonth >= 12) { subCreatureGiveDec_Pnl.Enabled = false; subCreatureGiveDec_Pnl.PanelColor = Color.FromArgb(245, 245, 249); subCreatureGiveDec_Pnl.PanelOutlineColor = Color.FromArgb(245, 245, 249); subCreatureGiveDec_stLbl.ForeColor = Color.FromArgb(202, 203, 207); }
        }
        // Кнопка месяца январь в панеле с оформлением, во вкладке [Оформление подписок]
        private void subCreatureGiveJan_Pnl_Click(object sender, EventArgs e)
        {
            if (subCreatureGive_jan == true)
            {
                subCreatureGive_jan = false;
                subCreatureGiveJan_Pnl.PanelColor = Color.FromArgb(25, 55, 255);
                subCreatureGiveJan_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
                subCreatureGiveJan_stLbl.ForeColor = Color.White;
            }
            else
            {
                subCreatureGive_jan = true;
                subCreatureGiveJan_Pnl.PanelColor = Color.FromArgb(242, 243, 250);
                subCreatureGiveJan_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
                subCreatureGiveJan_stLbl.ForeColor = Color.FromArgb(49, 50, 60);
            }
            PanelsMounthUpdate();
        }
        private async void subCreatureGiveJan_Pnl_MouseEnter(object sender, EventArgs e)
        {
            if (subCreatureGive_jan == true)
            {
                subCreatureGiveJan_Pnl.PanelColor = Color.FromArgb(242, 243, 250); //1
                subCreatureGiveJan_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
                subCreatureGiveJan_stLbl.ForeColor = Color.FromArgb(49, 50, 60);
                await Task.Delay(20);
                subCreatureGiveJan_Pnl.PanelColor = Color.FromArgb(240, 240, 248); //2
                subCreatureGiveJan_Pnl.PanelOutlineColor = Color.FromArgb(240, 240, 248);
                await Task.Delay(20);
                subCreatureGiveJan_Pnl.PanelColor = Color.FromArgb(237, 238, 246); //3
                subCreatureGiveJan_Pnl.PanelOutlineColor = Color.FromArgb(237, 238, 246);
                await Task.Delay(20);
                subCreatureGiveJan_Pnl.PanelColor = Color.FromArgb(235, 235, 244); //4
                subCreatureGiveJan_Pnl.PanelOutlineColor = Color.FromArgb(235, 235, 244);
            }
            else
            {
                await Task.Delay(20);
                subCreatureGiveJan_Pnl.PanelColor = Color.FromArgb(25, 55, 255); //1
                subCreatureGiveJan_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
                subCreatureGiveJan_stLbl.ForeColor = Color.White;
                await Task.Delay(20);
                subCreatureGiveJan_Pnl.PanelColor = Color.FromArgb(26, 54, 244); //2
                subCreatureGiveJan_Pnl.PanelOutlineColor = Color.FromArgb(26, 54, 244);
                await Task.Delay(20);
                subCreatureGiveJan_Pnl.PanelColor = Color.FromArgb(26, 52, 232); //3
                subCreatureGiveJan_Pnl.PanelOutlineColor = Color.FromArgb(26, 52, 232);
            }
        }
        private async void subCreatureGiveJan_Pnl_MouseLeave(object sender, EventArgs e)
        {
            if (subCreatureGive_jan == true)
            {
                subCreatureGiveJan_Pnl.PanelColor = Color.FromArgb(235, 235, 244); //4
                subCreatureGiveJan_Pnl.PanelOutlineColor = Color.FromArgb(235, 235, 244);
                subCreatureGiveJan_stLbl.ForeColor = Color.FromArgb(49, 50, 60);
                await Task.Delay(20);
                subCreatureGiveJan_Pnl.PanelColor = Color.FromArgb(237, 238, 246); //3
                subCreatureGiveJan_Pnl.PanelOutlineColor = Color.FromArgb(237, 238, 246);
                await Task.Delay(20);
                subCreatureGiveJan_Pnl.PanelColor = Color.FromArgb(240, 240, 248); //2
                subCreatureGiveJan_Pnl.PanelOutlineColor = Color.FromArgb(240, 240, 248);
                await Task.Delay(20);
                subCreatureGiveJan_Pnl.PanelColor = Color.FromArgb(242, 243, 250); //1
                subCreatureGiveJan_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
            }
            else
            {
                subCreatureGiveJan_Pnl.PanelColor = Color.FromArgb(26, 52, 232); //3
                subCreatureGiveJan_Pnl.PanelOutlineColor = Color.FromArgb(26, 52, 232);
                subCreatureGiveJan_stLbl.ForeColor = Color.White;
                await Task.Delay(20);
                subCreatureGiveJan_Pnl.PanelColor = Color.FromArgb(26, 54, 244); //2
                subCreatureGiveJan_Pnl.PanelOutlineColor = Color.FromArgb(26, 54, 244);
                await Task.Delay(20);
                subCreatureGiveJan_Pnl.PanelColor = Color.FromArgb(25, 55, 255); //1
                subCreatureGiveJan_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
            }
        }
        // Кнопка месяца февраль в панеле с оформлением, во вкладке [Оформление подписок]
        private void subCreatureGiveFeb_Pnl_Click(object sender, EventArgs e)
        {
            if (subCreatureGive_feb == true)
            {
                subCreatureGive_feb = false;
                subCreatureGiveFeb_Pnl.PanelColor = Color.FromArgb(25, 55, 255);
                subCreatureGiveFeb_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
                subCreatureGiveFeb_stLbl.ForeColor = Color.White;
            }
            else
            {
                subCreatureGive_feb = true;
                subCreatureGiveFeb_Pnl.PanelColor = Color.FromArgb(242, 243, 250);
                subCreatureGiveFeb_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
                subCreatureGiveFeb_stLbl.ForeColor = Color.FromArgb(49, 50, 60);
            }
            PanelsMounthUpdate();
        }
        private async void subCreatureGiveFeb_Pnl_MouseEnter(object sender, EventArgs e)
        {
            if (subCreatureGive_feb == true)
            {
                subCreatureGiveFeb_Pnl.PanelColor = Color.FromArgb(242, 243, 250); //1
                subCreatureGiveFeb_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
                subCreatureGiveFeb_stLbl.ForeColor = Color.FromArgb(49, 50, 60);
                await Task.Delay(20);
                subCreatureGiveFeb_Pnl.PanelColor = Color.FromArgb(240, 240, 248); //2
                subCreatureGiveFeb_Pnl.PanelOutlineColor = Color.FromArgb(240, 240, 248);
                await Task.Delay(20);
                subCreatureGiveFeb_Pnl.PanelColor = Color.FromArgb(237, 238, 246); //3
                subCreatureGiveFeb_Pnl.PanelOutlineColor = Color.FromArgb(237, 238, 246);
                await Task.Delay(20);
                subCreatureGiveFeb_Pnl.PanelColor = Color.FromArgb(235, 235, 244); //4
                subCreatureGiveFeb_Pnl.PanelOutlineColor = Color.FromArgb(235, 235, 244);
            }
            else
            {
                await Task.Delay(20);
                subCreatureGiveFeb_Pnl.PanelColor = Color.FromArgb(25, 55, 255); //1
                subCreatureGiveFeb_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
                subCreatureGiveFeb_stLbl.ForeColor = Color.White;
                await Task.Delay(20);
                subCreatureGiveFeb_Pnl.PanelColor = Color.FromArgb(26, 54, 244); //2
                subCreatureGiveFeb_Pnl.PanelOutlineColor = Color.FromArgb(26, 54, 244);
                await Task.Delay(20);
                subCreatureGiveFeb_Pnl.PanelColor = Color.FromArgb(26, 52, 232); //3
                subCreatureGiveFeb_Pnl.PanelOutlineColor = Color.FromArgb(26, 52, 232);
            }
        }
        private async void subCreatureGiveFeb_Pnl_MouseLeave(object sender, EventArgs e)
        {
            if (subCreatureGive_feb == true)
            {
                subCreatureGiveFeb_Pnl.PanelColor = Color.FromArgb(235, 235, 244); //4
                subCreatureGiveFeb_Pnl.PanelOutlineColor = Color.FromArgb(235, 235, 244);
                subCreatureGiveFeb_stLbl.ForeColor = Color.FromArgb(49, 50, 60);
                await Task.Delay(20);
                subCreatureGiveFeb_Pnl.PanelColor = Color.FromArgb(237, 238, 246); //3
                subCreatureGiveFeb_Pnl.PanelOutlineColor = Color.FromArgb(237, 238, 246);
                await Task.Delay(20);
                subCreatureGiveFeb_Pnl.PanelColor = Color.FromArgb(240, 240, 248); //2
                subCreatureGiveFeb_Pnl.PanelOutlineColor = Color.FromArgb(240, 240, 248);
                await Task.Delay(20);
                subCreatureGiveFeb_Pnl.PanelColor = Color.FromArgb(242, 243, 250); //1
                subCreatureGiveFeb_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
            }
            else
            {
                subCreatureGiveFeb_Pnl.PanelColor = Color.FromArgb(26, 52, 232); //3
                subCreatureGiveFeb_Pnl.PanelOutlineColor = Color.FromArgb(26, 52, 232);
                subCreatureGiveFeb_stLbl.ForeColor = Color.White;
                await Task.Delay(20);
                subCreatureGiveFeb_Pnl.PanelColor = Color.FromArgb(26, 54, 244); //2
                subCreatureGiveFeb_Pnl.PanelOutlineColor = Color.FromArgb(26, 54, 244);
                await Task.Delay(20);
                subCreatureGiveFeb_Pnl.PanelColor = Color.FromArgb(25, 55, 255); //1
                subCreatureGiveFeb_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
            }
        }
        // Кнопка месяца март в панеле с оформлением, во вкладке [Оформление подписок]
        private void subCreatureGiveMar_Pnl_Click(object sender, EventArgs e)
        {
            if (subCreatureGive_mar == true)
            {
                subCreatureGive_mar = false;
                subCreatureGiveMar_Pnl.PanelColor = Color.FromArgb(25, 55, 255);
                subCreatureGiveMar_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
                subCreatureGiveMar_stLbl.ForeColor = Color.White;
            }
            else
            {
                subCreatureGive_mar = true;
                subCreatureGiveMar_Pnl.PanelColor = Color.FromArgb(242, 243, 250);
                subCreatureGiveMar_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
                subCreatureGiveMar_stLbl.ForeColor = Color.FromArgb(49, 50, 60);
            }
            PanelsMounthUpdate();
        }
        private async void subCreatureGiveMar_Pnl_MouseEnter(object sender, EventArgs e)
        {
            if (subCreatureGive_mar == true)
            {
                subCreatureGiveMar_Pnl.PanelColor = Color.FromArgb(242, 243, 250); //1
                subCreatureGiveMar_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
                subCreatureGiveMar_stLbl.ForeColor = Color.FromArgb(49, 50, 60);
                await Task.Delay(20);
                subCreatureGiveMar_Pnl.PanelColor = Color.FromArgb(240, 240, 248); //2
                subCreatureGiveMar_Pnl.PanelOutlineColor = Color.FromArgb(240, 240, 248);
                await Task.Delay(20);
                subCreatureGiveMar_Pnl.PanelColor = Color.FromArgb(237, 238, 246); //3
                subCreatureGiveMar_Pnl.PanelOutlineColor = Color.FromArgb(237, 238, 246);
                await Task.Delay(20);
                subCreatureGiveMar_Pnl.PanelColor = Color.FromArgb(235, 235, 244); //4
                subCreatureGiveMar_Pnl.PanelOutlineColor = Color.FromArgb(235, 235, 244);
            }
            else
            {
                await Task.Delay(20);
                subCreatureGiveMar_Pnl.PanelColor = Color.FromArgb(25, 55, 255); //1
                subCreatureGiveMar_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
                subCreatureGiveMar_stLbl.ForeColor = Color.White;
                await Task.Delay(20);
                subCreatureGiveMar_Pnl.PanelColor = Color.FromArgb(26, 54, 244); //2
                subCreatureGiveMar_Pnl.PanelOutlineColor = Color.FromArgb(26, 54, 244);
                await Task.Delay(20);
                subCreatureGiveMar_Pnl.PanelColor = Color.FromArgb(26, 52, 232); //3
                subCreatureGiveMar_Pnl.PanelOutlineColor = Color.FromArgb(26, 52, 232);
            }
        }
        private async void subCreatureGiveMar_Pnl_MouseLeave(object sender, EventArgs e)
        {
            if (subCreatureGive_mar == true)
            {
                subCreatureGiveMar_Pnl.PanelColor = Color.FromArgb(235, 235, 244); //4
                subCreatureGiveMar_Pnl.PanelOutlineColor = Color.FromArgb(235, 235, 244);
                subCreatureGiveMar_stLbl.ForeColor = Color.FromArgb(49, 50, 60);
                await Task.Delay(20);
                subCreatureGiveMar_Pnl.PanelColor = Color.FromArgb(237, 238, 246); //3
                subCreatureGiveMar_Pnl.PanelOutlineColor = Color.FromArgb(237, 238, 246);
                await Task.Delay(20);
                subCreatureGiveMar_Pnl.PanelColor = Color.FromArgb(240, 240, 248); //2
                subCreatureGiveMar_Pnl.PanelOutlineColor = Color.FromArgb(240, 240, 248);
                await Task.Delay(20);
                subCreatureGiveMar_Pnl.PanelColor = Color.FromArgb(242, 243, 250); //1
                subCreatureGiveMar_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
            }
            else
            {
                subCreatureGiveMar_Pnl.PanelColor = Color.FromArgb(26, 52, 232); //3
                subCreatureGiveMar_Pnl.PanelOutlineColor = Color.FromArgb(26, 52, 232);
                subCreatureGiveMar_stLbl.ForeColor = Color.White;
                await Task.Delay(20);
                subCreatureGiveMar_Pnl.PanelColor = Color.FromArgb(26, 54, 244); //2
                subCreatureGiveMar_Pnl.PanelOutlineColor = Color.FromArgb(26, 54, 244);
                await Task.Delay(20);
                subCreatureGiveMar_Pnl.PanelColor = Color.FromArgb(25, 55, 255); //1
                subCreatureGiveMar_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
            }
        }
        // Кнопка месяца апрель в панеле с оформлением, во вкладке [Оформление подписок]
        private void subCreatureGiveApr_Pnl_Click(object sender, EventArgs e)
        {
            if (subCreatureGive_apr == true)
            {
                subCreatureGive_apr = false;
                subCreatureGiveApr_Pnl.PanelColor = Color.FromArgb(25, 55, 255);
                subCreatureGiveApr_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
                subCreatureGiveApr_stLbl.ForeColor = Color.White;
            }
            else
            {
                subCreatureGive_apr = true;
                subCreatureGiveApr_Pnl.PanelColor = Color.FromArgb(242, 243, 250);
                subCreatureGiveApr_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
                subCreatureGiveApr_stLbl.ForeColor = Color.FromArgb(49, 50, 60);
            }
            PanelsMounthUpdate();
        }
        private async void subCreatureGiveApr_Pnl_MouseEnter(object sender, EventArgs e)
        {
            if (subCreatureGive_apr == true)
            {
                subCreatureGiveApr_Pnl.PanelColor = Color.FromArgb(242, 243, 250); //1
                subCreatureGiveApr_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
                subCreatureGiveApr_stLbl.ForeColor = Color.FromArgb(49, 50, 60);
                await Task.Delay(20);
                subCreatureGiveApr_Pnl.PanelColor = Color.FromArgb(240, 240, 248); //2
                subCreatureGiveApr_Pnl.PanelOutlineColor = Color.FromArgb(240, 240, 248);
                await Task.Delay(20);
                subCreatureGiveApr_Pnl.PanelColor = Color.FromArgb(237, 238, 246); //3
                subCreatureGiveApr_Pnl.PanelOutlineColor = Color.FromArgb(237, 238, 246);
                await Task.Delay(20);
                subCreatureGiveApr_Pnl.PanelColor = Color.FromArgb(235, 235, 244); //4
                subCreatureGiveApr_Pnl.PanelOutlineColor = Color.FromArgb(235, 235, 244);
            }
            else
            {
                await Task.Delay(20);
                subCreatureGiveApr_Pnl.PanelColor = Color.FromArgb(25, 55, 255); //1
                subCreatureGiveApr_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
                subCreatureGiveApr_stLbl.ForeColor = Color.White;
                await Task.Delay(20);
                subCreatureGiveApr_Pnl.PanelColor = Color.FromArgb(26, 54, 244); //2
                subCreatureGiveApr_Pnl.PanelOutlineColor = Color.FromArgb(26, 54, 244);
                await Task.Delay(20);
                subCreatureGiveApr_Pnl.PanelColor = Color.FromArgb(26, 52, 232); //3
                subCreatureGiveApr_Pnl.PanelOutlineColor = Color.FromArgb(26, 52, 232);
            }
        }
        private async void subCreatureGiveApr_Pnl_MouseLeave(object sender, EventArgs e)
        {
            if (subCreatureGive_apr == true)
            {
                subCreatureGiveApr_Pnl.PanelColor = Color.FromArgb(235, 235, 244); //4
                subCreatureGiveApr_Pnl.PanelOutlineColor = Color.FromArgb(235, 235, 244);
                subCreatureGiveApr_stLbl.ForeColor = Color.FromArgb(49, 50, 60);
                await Task.Delay(20);
                subCreatureGiveApr_Pnl.PanelColor = Color.FromArgb(237, 238, 246); //3
                subCreatureGiveApr_Pnl.PanelOutlineColor = Color.FromArgb(237, 238, 246);
                await Task.Delay(20);
                subCreatureGiveApr_Pnl.PanelColor = Color.FromArgb(240, 240, 248); //2
                subCreatureGiveApr_Pnl.PanelOutlineColor = Color.FromArgb(240, 240, 248);
                await Task.Delay(20);
                subCreatureGiveApr_Pnl.PanelColor = Color.FromArgb(242, 243, 250); //1
                subCreatureGiveApr_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
            }
            else
            {
                subCreatureGiveApr_Pnl.PanelColor = Color.FromArgb(26, 52, 232); //3
                subCreatureGiveApr_Pnl.PanelOutlineColor = Color.FromArgb(26, 52, 232);
                subCreatureGiveApr_stLbl.ForeColor = Color.White;
                await Task.Delay(20);
                subCreatureGiveApr_Pnl.PanelColor = Color.FromArgb(26, 54, 244); //2
                subCreatureGiveApr_Pnl.PanelOutlineColor = Color.FromArgb(26, 54, 244);
                await Task.Delay(20);
                subCreatureGiveApr_Pnl.PanelColor = Color.FromArgb(25, 55, 255); //1
                subCreatureGiveApr_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
            }
        }
        // Кнопка месяца май в панеле с оформлением, во вкладке [Оформление подписок]
        private void subCreatureGiveMay_Pnl_Click(object sender, EventArgs e)
        {
            if (subCreatureGive_may == true)
            {
                subCreatureGive_may = false;
                subCreatureGiveMay_Pnl.PanelColor = Color.FromArgb(25, 55, 255);
                subCreatureGiveMay_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
                subCreatureGiveMay_stLbl.ForeColor = Color.White;
            }
            else
            {
                subCreatureGive_may = true;
                subCreatureGiveMay_Pnl.PanelColor = Color.FromArgb(242, 243, 250);
                subCreatureGiveMay_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
                subCreatureGiveMay_stLbl.ForeColor = Color.FromArgb(49, 50, 60);
            }
            PanelsMounthUpdate();
        }
        private async void subCreatureGiveMay_Pnl_MouseEnter(object sender, EventArgs e)
        {
            if (subCreatureGive_may == true)
            {
                subCreatureGiveMay_Pnl.PanelColor = Color.FromArgb(242, 243, 250);
                subCreatureGiveMay_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
                subCreatureGiveMay_stLbl.ForeColor = Color.FromArgb(49, 50, 60);
                await Task.Delay(20);
                subCreatureGiveMay_Pnl.PanelColor = Color.FromArgb(240, 240, 248);
                subCreatureGiveMay_Pnl.PanelOutlineColor = Color.FromArgb(240, 240, 248);
                await Task.Delay(20);
                subCreatureGiveMay_Pnl.PanelColor = Color.FromArgb(237, 238, 246);
                subCreatureGiveMay_Pnl.PanelOutlineColor = Color.FromArgb(237, 238, 246);
                await Task.Delay(20);
                subCreatureGiveMay_Pnl.PanelColor = Color.FromArgb(235, 235, 244);
                subCreatureGiveMay_Pnl.PanelOutlineColor = Color.FromArgb(235, 235, 244);
            }
            else
            {
                await Task.Delay(20);
                subCreatureGiveMay_Pnl.PanelColor = Color.FromArgb(25, 55, 255);
                subCreatureGiveMay_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
                subCreatureGiveMay_stLbl.ForeColor = Color.White;
                await Task.Delay(20);
                subCreatureGiveMay_Pnl.PanelColor = Color.FromArgb(26, 54, 244);
                subCreatureGiveMay_Pnl.PanelOutlineColor = Color.FromArgb(26, 54, 244);
                await Task.Delay(20);
                subCreatureGiveMay_Pnl.PanelColor = Color.FromArgb(26, 52, 232);
                subCreatureGiveMay_Pnl.PanelOutlineColor = Color.FromArgb(26, 52, 232);
            }
        }
        private async void subCreatureGiveMay_Pnl_MouseLeave(object sender, EventArgs e)
        {
            if (subCreatureGive_may == true)
            {
                subCreatureGiveMay_Pnl.PanelColor = Color.FromArgb(235, 235, 244);
                subCreatureGiveMay_Pnl.PanelOutlineColor = Color.FromArgb(235, 235, 244);
                subCreatureGiveMay_stLbl.ForeColor = Color.FromArgb(49, 50, 60);
                await Task.Delay(20);
                subCreatureGiveMay_Pnl.PanelColor = Color.FromArgb(237, 238, 246);
                subCreatureGiveMay_Pnl.PanelOutlineColor = Color.FromArgb(237, 238, 246);
                await Task.Delay(20);
                subCreatureGiveMay_Pnl.PanelColor = Color.FromArgb(240, 240, 248);
                subCreatureGiveMay_Pnl.PanelOutlineColor = Color.FromArgb(240, 240, 248);
                await Task.Delay(20);
                subCreatureGiveMay_Pnl.PanelColor = Color.FromArgb(242, 243, 250);
                subCreatureGiveMay_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
            }
            else
            {
                subCreatureGiveMay_Pnl.PanelColor = Color.FromArgb(26, 52, 232);
                subCreatureGiveMay_Pnl.PanelOutlineColor = Color.FromArgb(26, 52, 232);
                subCreatureGiveMay_stLbl.ForeColor = Color.White;
                await Task.Delay(20);
                subCreatureGiveMay_Pnl.PanelColor = Color.FromArgb(26, 54, 244);
                subCreatureGiveMay_Pnl.PanelOutlineColor = Color.FromArgb(26, 54, 244);
                await Task.Delay(20);
                subCreatureGiveMay_Pnl.PanelColor = Color.FromArgb(25, 55, 255);
                subCreatureGiveMay_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
            }
        }
        // Кнопка месяца июнь в панеле с оформлением, во вкладке [Оформление подписок]
        private void subCreatureGiveJun_Pnl_Click(object sender, EventArgs e)
        {
            if (subCreatureGive_jun == true)
            {
                subCreatureGive_jun = false;
                subCreatureGiveJun_Pnl.PanelColor = Color.FromArgb(25, 55, 255);
                subCreatureGiveJun_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
                subCreatureGiveJun_stLbl.ForeColor = Color.White;
            }
            else
            {
                subCreatureGive_jun = true;
                subCreatureGiveJun_Pnl.PanelColor = Color.FromArgb(242, 243, 250);
                subCreatureGiveJun_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
                subCreatureGiveJun_stLbl.ForeColor = Color.FromArgb(49, 50, 60);
            }
            PanelsMounthUpdate();
        }
        private async void subCreatureGiveJun_Pnl_MouseEnter(object sender, EventArgs e)
        {
            if (subCreatureGive_jun == true)
            {
                subCreatureGiveJun_Pnl.PanelColor = Color.FromArgb(242, 243, 250);
                subCreatureGiveJun_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
                subCreatureGiveJun_stLbl.ForeColor = Color.FromArgb(49, 50, 60);
                await Task.Delay(20);
                subCreatureGiveJun_Pnl.PanelColor = Color.FromArgb(240, 240, 248);
                subCreatureGiveJun_Pnl.PanelOutlineColor = Color.FromArgb(240, 240, 248);
                await Task.Delay(20);
                subCreatureGiveJun_Pnl.PanelColor = Color.FromArgb(237, 238, 246);
                subCreatureGiveJun_Pnl.PanelOutlineColor = Color.FromArgb(237, 238, 246);
                await Task.Delay(20);
                subCreatureGiveJun_Pnl.PanelColor = Color.FromArgb(235, 235, 244);
                subCreatureGiveJun_Pnl.PanelOutlineColor = Color.FromArgb(235, 235, 244);
            }
            else
            {
                await Task.Delay(20);
                subCreatureGiveJun_Pnl.PanelColor = Color.FromArgb(25, 55, 255);
                subCreatureGiveJun_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
                subCreatureGiveJun_stLbl.ForeColor = Color.White;
                await Task.Delay(20);
                subCreatureGiveJun_Pnl.PanelColor = Color.FromArgb(26, 54, 244);
                subCreatureGiveJun_Pnl.PanelOutlineColor = Color.FromArgb(26, 54, 244);
                await Task.Delay(20);
                subCreatureGiveJun_Pnl.PanelColor = Color.FromArgb(26, 52, 232);
                subCreatureGiveJun_Pnl.PanelOutlineColor = Color.FromArgb(26, 52, 232);
            }
        }
        private async void subCreatureGiveJun_Pnl_MouseLeave(object sender, EventArgs e)
        {
            if (subCreatureGive_jun == true)
            {
                subCreatureGiveJun_Pnl.PanelColor = Color.FromArgb(235, 235, 244);
                subCreatureGiveJun_Pnl.PanelOutlineColor = Color.FromArgb(235, 235, 244);
                subCreatureGiveJun_stLbl.ForeColor = Color.FromArgb(49, 50, 60);
                await Task.Delay(20);
                subCreatureGiveJun_Pnl.PanelColor = Color.FromArgb(237, 238, 246);
                subCreatureGiveJun_Pnl.PanelOutlineColor = Color.FromArgb(237, 238, 246);
                await Task.Delay(20);
                subCreatureGiveJun_Pnl.PanelColor = Color.FromArgb(240, 240, 248);
                subCreatureGiveJun_Pnl.PanelOutlineColor = Color.FromArgb(240, 240, 248);
                await Task.Delay(20);
                subCreatureGiveJun_Pnl.PanelColor = Color.FromArgb(242, 243, 250);
                subCreatureGiveJun_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
            }
            else
            {
                subCreatureGiveJun_Pnl.PanelColor = Color.FromArgb(26, 52, 232);
                subCreatureGiveJun_Pnl.PanelOutlineColor = Color.FromArgb(26, 52, 232);
                subCreatureGiveJun_stLbl.ForeColor = Color.White;
                await Task.Delay(20);
                subCreatureGiveJun_Pnl.PanelColor = Color.FromArgb(26, 54, 244);
                subCreatureGiveJun_Pnl.PanelOutlineColor = Color.FromArgb(26, 54, 244);
                await Task.Delay(20);
                subCreatureGiveJun_Pnl.PanelColor = Color.FromArgb(25, 55, 255);
                subCreatureGiveJun_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
            }
        }
        // Кнопка месяца июль в панеле с оформлением, во вкладке [Оформление подписок]
        private void subCreatureGiveJul_Pnl_Click(object sender, EventArgs e)
        {
            if (subCreatureGive_jul == true)
            {
                subCreatureGive_jul = false;
                subCreatureGiveJul_Pnl.PanelColor = Color.FromArgb(25, 55, 255);
                subCreatureGiveJul_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
                subCreatureGiveJul_stLbl.ForeColor = Color.White;
            }
            else
            {
                subCreatureGive_jul = true;
                subCreatureGiveJul_Pnl.PanelColor = Color.FromArgb(242, 243, 250);
                subCreatureGiveJul_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
                subCreatureGiveJul_stLbl.ForeColor = Color.FromArgb(49, 50, 60);
            }
            PanelsMounthUpdate();
        }
        private async void subCreatureGiveJul_Pnl_MouseEnter(object sender, EventArgs e)
        {
            if (subCreatureGive_jul == true)
            {
                subCreatureGiveJul_Pnl.PanelColor = Color.FromArgb(242, 243, 250);
                subCreatureGiveJul_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
                subCreatureGiveJul_stLbl.ForeColor = Color.FromArgb(49, 50, 60);
                await Task.Delay(20);
                subCreatureGiveJul_Pnl.PanelColor = Color.FromArgb(240, 240, 248);
                subCreatureGiveJul_Pnl.PanelOutlineColor = Color.FromArgb(240, 240, 248);
                await Task.Delay(20);
                subCreatureGiveJul_Pnl.PanelColor = Color.FromArgb(237, 238, 246);
                subCreatureGiveJul_Pnl.PanelOutlineColor = Color.FromArgb(237, 238, 246);
                await Task.Delay(20);
                subCreatureGiveJul_Pnl.PanelColor = Color.FromArgb(235, 235, 244);
                subCreatureGiveJul_Pnl.PanelOutlineColor = Color.FromArgb(235, 235, 244);
            }
            else
            {
                await Task.Delay(20);
                subCreatureGiveJul_Pnl.PanelColor = Color.FromArgb(25, 55, 255);
                subCreatureGiveJul_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
                subCreatureGiveJul_stLbl.ForeColor = Color.White;
                await Task.Delay(20);
                subCreatureGiveJul_Pnl.PanelColor = Color.FromArgb(26, 54, 244);
                subCreatureGiveJul_Pnl.PanelOutlineColor = Color.FromArgb(26, 54, 244);
                await Task.Delay(20);
                subCreatureGiveJul_Pnl.PanelColor = Color.FromArgb(26, 52, 232);
                subCreatureGiveJul_Pnl.PanelOutlineColor = Color.FromArgb(26, 52, 232);
            }
        }
        private async void subCreatureGiveJul_Pnl_MouseLeave(object sender, EventArgs e)
        {
            if (subCreatureGive_jul == true)
            {
                subCreatureGiveJul_Pnl.PanelColor = Color.FromArgb(235, 235, 244);
                subCreatureGiveJul_Pnl.PanelOutlineColor = Color.FromArgb(235, 235, 244);
                subCreatureGiveJul_stLbl.ForeColor = Color.FromArgb(49, 50, 60);
                await Task.Delay(20);
                subCreatureGiveJul_Pnl.PanelColor = Color.FromArgb(237, 238, 246);
                subCreatureGiveJul_Pnl.PanelOutlineColor = Color.FromArgb(237, 238, 246);
                await Task.Delay(20);
                subCreatureGiveJul_Pnl.PanelColor = Color.FromArgb(240, 240, 248);
                subCreatureGiveJul_Pnl.PanelOutlineColor = Color.FromArgb(240, 240, 248);
                await Task.Delay(20);
                subCreatureGiveJul_Pnl.PanelColor = Color.FromArgb(242, 243, 250);
                subCreatureGiveJul_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
            }
            else
            {
                subCreatureGiveJul_Pnl.PanelColor = Color.FromArgb(26, 52, 232);
                subCreatureGiveJul_Pnl.PanelOutlineColor = Color.FromArgb(26, 52, 232);
                subCreatureGiveJul_stLbl.ForeColor = Color.White;
                await Task.Delay(20);
                subCreatureGiveJul_Pnl.PanelColor = Color.FromArgb(26, 54, 244);
                subCreatureGiveJul_Pnl.PanelOutlineColor = Color.FromArgb(26, 54, 244);
                await Task.Delay(20);
                subCreatureGiveJul_Pnl.PanelColor = Color.FromArgb(25, 55, 255);
                subCreatureGiveJul_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
            }
        }
        // Кнопка месяца август в панеле с оформлением, во вкладке [Оформление подписок]
        private void subCreatureGiveAug_Pnl_Click(object sender, EventArgs e)
        {
            if (subCreatureGive_aug == true)
            {
                subCreatureGive_aug = false;
                subCreatureGiveAug_Pnl.PanelColor = Color.FromArgb(25, 55, 255);
                subCreatureGiveAug_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
                subCreatureGiveAug_stLbl.ForeColor = Color.White;
            }
            else
            {
                subCreatureGive_aug = true;
                subCreatureGiveAug_Pnl.PanelColor = Color.FromArgb(242, 243, 250);
                subCreatureGiveAug_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
                subCreatureGiveAug_stLbl.ForeColor = Color.FromArgb(49, 50, 60);
            }
            PanelsMounthUpdate();
        }
        private async void subCreatureGiveAug_Pnl_MouseEnter(object sender, EventArgs e)
        {
            if (subCreatureGive_aug == true)
            {
                subCreatureGiveAug_Pnl.PanelColor = Color.FromArgb(242, 243, 250);
                subCreatureGiveAug_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
                subCreatureGiveAug_stLbl.ForeColor = Color.FromArgb(49, 50, 60);
                await Task.Delay(20);
                subCreatureGiveAug_Pnl.PanelColor = Color.FromArgb(240, 240, 248);
                subCreatureGiveAug_Pnl.PanelOutlineColor = Color.FromArgb(240, 240, 248);
                await Task.Delay(20);
                subCreatureGiveAug_Pnl.PanelColor = Color.FromArgb(237, 238, 246);
                subCreatureGiveAug_Pnl.PanelOutlineColor = Color.FromArgb(237, 238, 246);
                await Task.Delay(20);
                subCreatureGiveAug_Pnl.PanelColor = Color.FromArgb(235, 235, 244);
                subCreatureGiveAug_Pnl.PanelOutlineColor = Color.FromArgb(235, 235, 244);
            }
            else
            {
                await Task.Delay(20);
                subCreatureGiveAug_Pnl.PanelColor = Color.FromArgb(25, 55, 255);
                subCreatureGiveAug_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
                subCreatureGiveAug_stLbl.ForeColor = Color.White;
                await Task.Delay(20);
                subCreatureGiveAug_Pnl.PanelColor = Color.FromArgb(26, 54, 244);
                subCreatureGiveAug_Pnl.PanelOutlineColor = Color.FromArgb(26, 54, 244);
                await Task.Delay(20);
                subCreatureGiveAug_Pnl.PanelColor = Color.FromArgb(26, 52, 232);
                subCreatureGiveAug_Pnl.PanelOutlineColor = Color.FromArgb(26, 52, 232);
            }
        }
        private async void subCreatureGiveAug_Pnl_MouseLeave(object sender, EventArgs e)
        {
            if (subCreatureGive_aug == true)
            {
                subCreatureGiveAug_Pnl.PanelColor = Color.FromArgb(235, 235, 244);
                subCreatureGiveAug_Pnl.PanelOutlineColor = Color.FromArgb(235, 235, 244);
                subCreatureGiveAug_stLbl.ForeColor = Color.FromArgb(49, 50, 60);
                await Task.Delay(20);
                subCreatureGiveAug_Pnl.PanelColor = Color.FromArgb(237, 238, 246);
                subCreatureGiveAug_Pnl.PanelOutlineColor = Color.FromArgb(237, 238, 246);
                await Task.Delay(20);
                subCreatureGiveAug_Pnl.PanelColor = Color.FromArgb(240, 240, 248);
                subCreatureGiveAug_Pnl.PanelOutlineColor = Color.FromArgb(240, 240, 248);
                await Task.Delay(20);
                subCreatureGiveAug_Pnl.PanelColor = Color.FromArgb(242, 243, 250);
                subCreatureGiveAug_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
            }
            else
            {
                subCreatureGiveAug_Pnl.PanelColor = Color.FromArgb(26, 52, 232);
                subCreatureGiveAug_Pnl.PanelOutlineColor = Color.FromArgb(26, 52, 232);
                subCreatureGiveAug_stLbl.ForeColor = Color.White;
                await Task.Delay(20);
                subCreatureGiveAug_Pnl.PanelColor = Color.FromArgb(26, 54, 244);
                subCreatureGiveAug_Pnl.PanelOutlineColor = Color.FromArgb(26, 54, 244);
                await Task.Delay(20);
                subCreatureGiveAug_Pnl.PanelColor = Color.FromArgb(25, 55, 255);
                subCreatureGiveAug_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
            }
        }
        // Кнопка месяца сентября в панеле с оформлением, во вкладке [Оформление подписок]
        private void subCreatureGiveSep_Pnl_Click(object sender, EventArgs e)
        {
            if (subCreatureGive_sep == true)
            {
                subCreatureGive_sep = false;
                subCreatureGiveSep_Pnl.PanelColor = Color.FromArgb(25, 55, 255);
                subCreatureGiveSep_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
                subCreatureGiveSep_stLbl.ForeColor = Color.White;
            }
            else
            {
                subCreatureGive_sep = true;
                subCreatureGiveSep_Pnl.PanelColor = Color.FromArgb(242, 243, 250);
                subCreatureGiveSep_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
                subCreatureGiveSep_stLbl.ForeColor = Color.FromArgb(49, 50, 60);
            }
            PanelsMounthUpdate();
        }
        private async void subCreatureGiveSep_Pnl_MouseEnter(object sender, EventArgs e)
        {
            if (subCreatureGive_sep == true)
            {
                subCreatureGiveSep_Pnl.PanelColor = Color.FromArgb(242, 243, 250);
                subCreatureGiveSep_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
                subCreatureGiveSep_stLbl.ForeColor = Color.FromArgb(49, 50, 60);
                await Task.Delay(20);
                subCreatureGiveSep_Pnl.PanelColor = Color.FromArgb(240, 240, 248);
                subCreatureGiveSep_Pnl.PanelOutlineColor = Color.FromArgb(240, 240, 248);
                await Task.Delay(20);
                subCreatureGiveSep_Pnl.PanelColor = Color.FromArgb(237, 238, 246);
                subCreatureGiveSep_Pnl.PanelOutlineColor = Color.FromArgb(237, 238, 246);
                await Task.Delay(20);
                subCreatureGiveSep_Pnl.PanelColor = Color.FromArgb(235, 235, 244);
                subCreatureGiveSep_Pnl.PanelOutlineColor = Color.FromArgb(235, 235, 244);
            }
            else
            {
                await Task.Delay(20);
                subCreatureGiveSep_Pnl.PanelColor = Color.FromArgb(25, 55, 255);
                subCreatureGiveSep_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
                subCreatureGiveSep_stLbl.ForeColor = Color.White;
                await Task.Delay(20);
                subCreatureGiveSep_Pnl.PanelColor = Color.FromArgb(26, 54, 244);
                subCreatureGiveSep_Pnl.PanelOutlineColor = Color.FromArgb(26, 54, 244);
                await Task.Delay(20);
                subCreatureGiveSep_Pnl.PanelColor = Color.FromArgb(26, 52, 232);
                subCreatureGiveSep_Pnl.PanelOutlineColor = Color.FromArgb(26, 52, 232);
            }
        }
        private async void subCreatureGiveSep_Pnl_MouseLeave(object sender, EventArgs e)
        {
            if (subCreatureGive_sep == true)
            {
                subCreatureGiveSep_Pnl.PanelColor = Color.FromArgb(235, 235, 244);
                subCreatureGiveSep_Pnl.PanelOutlineColor = Color.FromArgb(235, 235, 244);
                subCreatureGiveSep_stLbl.ForeColor = Color.FromArgb(49, 50, 60);
                await Task.Delay(20);
                subCreatureGiveSep_Pnl.PanelColor = Color.FromArgb(237, 238, 246);
                subCreatureGiveSep_Pnl.PanelOutlineColor = Color.FromArgb(237, 238, 246);
                await Task.Delay(20);
                subCreatureGiveSep_Pnl.PanelColor = Color.FromArgb(240, 240, 248);
                subCreatureGiveSep_Pnl.PanelOutlineColor = Color.FromArgb(240, 240, 248);
                await Task.Delay(20);
                subCreatureGiveSep_Pnl.PanelColor = Color.FromArgb(242, 243, 250);
                subCreatureGiveSep_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
            }
            else
            {
                subCreatureGiveSep_Pnl.PanelColor = Color.FromArgb(26, 52, 232);
                subCreatureGiveSep_Pnl.PanelOutlineColor = Color.FromArgb(26, 52, 232);
                subCreatureGiveSep_stLbl.ForeColor = Color.White;
                await Task.Delay(20);
                subCreatureGiveSep_Pnl.PanelColor = Color.FromArgb(26, 54, 244);
                subCreatureGiveSep_Pnl.PanelOutlineColor = Color.FromArgb(26, 54, 244);
                await Task.Delay(20);
                subCreatureGiveSep_Pnl.PanelColor = Color.FromArgb(25, 55, 255);
                subCreatureGiveSep_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
            }
        }
        // Кнопка месяца октября в панеле с оформлением, во вкладке [Оформление подписок]
        private void subCreatureGiveOct_Pnl_Click(object sender, EventArgs e)
        {
            if (subCreatureGive_oct == true)
            {
                subCreatureGive_oct = false;
                subCreatureGiveOct_Pnl.PanelColor = Color.FromArgb(25, 55, 255);
                subCreatureGiveOct_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
                subCreatureGiveOct_stLbl.ForeColor = Color.White;
            }
            else
            {
                subCreatureGive_oct = true;
                subCreatureGiveOct_Pnl.PanelColor = Color.FromArgb(242, 243, 250);
                subCreatureGiveOct_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
                subCreatureGiveOct_stLbl.ForeColor = Color.FromArgb(49, 50, 60);
            }
            PanelsMounthUpdate();
        }
        private async void subCreatureGiveOct_Pnl_MouseEnter(object sender, EventArgs e)
        {
            if (subCreatureGive_oct == true)
            {
                subCreatureGiveOct_Pnl.PanelColor = Color.FromArgb(242, 243, 250);
                subCreatureGiveOct_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
                subCreatureGiveOct_stLbl.ForeColor = Color.FromArgb(49, 50, 60);
                await Task.Delay(20);
                subCreatureGiveOct_Pnl.PanelColor = Color.FromArgb(240, 240, 248);
                subCreatureGiveOct_Pnl.PanelOutlineColor = Color.FromArgb(240, 240, 248);
                await Task.Delay(20);
                subCreatureGiveOct_Pnl.PanelColor = Color.FromArgb(237, 238, 246);
                subCreatureGiveOct_Pnl.PanelOutlineColor = Color.FromArgb(237, 238, 246);
                await Task.Delay(20);
                subCreatureGiveOct_Pnl.PanelColor = Color.FromArgb(235, 235, 244);
                subCreatureGiveOct_Pnl.PanelOutlineColor = Color.FromArgb(235, 235, 244);
            }
            else
            {
                await Task.Delay(20);
                subCreatureGiveOct_Pnl.PanelColor = Color.FromArgb(25, 55, 255);
                subCreatureGiveOct_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
                subCreatureGiveOct_stLbl.ForeColor = Color.White;
                await Task.Delay(20);
                subCreatureGiveOct_Pnl.PanelColor = Color.FromArgb(26, 54, 244);
                subCreatureGiveOct_Pnl.PanelOutlineColor = Color.FromArgb(26, 54, 244);
                await Task.Delay(20);
                subCreatureGiveOct_Pnl.PanelColor = Color.FromArgb(26, 52, 232);
                subCreatureGiveOct_Pnl.PanelOutlineColor = Color.FromArgb(26, 52, 232);
            }
        }
        private async void subCreatureGiveOct_Pnl_MouseLeave(object sender, EventArgs e)
        {
            if (subCreatureGive_oct == true)
            {
                subCreatureGiveOct_Pnl.PanelColor = Color.FromArgb(235, 235, 244);
                subCreatureGiveOct_Pnl.PanelOutlineColor = Color.FromArgb(235, 235, 244);
                subCreatureGiveOct_stLbl.ForeColor = Color.FromArgb(49, 50, 60);
                await Task.Delay(20);
                subCreatureGiveOct_Pnl.PanelColor = Color.FromArgb(237, 238, 246);
                subCreatureGiveOct_Pnl.PanelOutlineColor = Color.FromArgb(237, 238, 246);
                await Task.Delay(20);
                subCreatureGiveOct_Pnl.PanelColor = Color.FromArgb(240, 240, 248);
                subCreatureGiveOct_Pnl.PanelOutlineColor = Color.FromArgb(240, 240, 248);
                await Task.Delay(20);
                subCreatureGiveOct_Pnl.PanelColor = Color.FromArgb(242, 243, 250);
                subCreatureGiveOct_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
            }
            else
            {
                subCreatureGiveOct_Pnl.PanelColor = Color.FromArgb(26, 52, 232);
                subCreatureGiveOct_Pnl.PanelOutlineColor = Color.FromArgb(26, 52, 232);
                subCreatureGiveOct_stLbl.ForeColor = Color.White;
                await Task.Delay(20);
                subCreatureGiveOct_Pnl.PanelColor = Color.FromArgb(26, 54, 244);
                subCreatureGiveOct_Pnl.PanelOutlineColor = Color.FromArgb(26, 54, 244);
                await Task.Delay(20);
                subCreatureGiveOct_Pnl.PanelColor = Color.FromArgb(25, 55, 255);
                subCreatureGiveOct_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
            }
        }
        // Кнопка месяца ноября в панеле с оформлением, во вкладке [Оформление подписок]
        private void subCreatureGiveNov_Pnl_Click(object sender, EventArgs e)
        {
            if (subCreatureGive_nov == true)
            {
                subCreatureGive_nov = false;
                subCreatureGiveNov_Pnl.PanelColor = Color.FromArgb(25, 55, 255);
                subCreatureGiveNov_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
                subCreatureGiveNov_stLbl.ForeColor = Color.White;
            }
            else
            {
                subCreatureGive_nov = true;
                subCreatureGiveNov_Pnl.PanelColor = Color.FromArgb(242, 243, 250);
                subCreatureGiveNov_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
                subCreatureGiveNov_stLbl.ForeColor = Color.FromArgb(49, 50, 60);
            }
            PanelsMounthUpdate();
        }
        private async void subCreatureGiveNov_Pnl_MouseEnter(object sender, EventArgs e)
        {
            if (subCreatureGive_nov == true)
            {
                subCreatureGiveNov_Pnl.PanelColor = Color.FromArgb(242, 243, 250);
                subCreatureGiveNov_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
                subCreatureGiveNov_stLbl.ForeColor = Color.FromArgb(49, 50, 60);
                await Task.Delay(20);
                subCreatureGiveNov_Pnl.PanelColor = Color.FromArgb(240, 240, 248);
                subCreatureGiveNov_Pnl.PanelOutlineColor = Color.FromArgb(240, 240, 248);
                await Task.Delay(20);
                subCreatureGiveNov_Pnl.PanelColor = Color.FromArgb(237, 238, 246);
                subCreatureGiveNov_Pnl.PanelOutlineColor = Color.FromArgb(237, 238, 246);
                await Task.Delay(20);
                subCreatureGiveNov_Pnl.PanelColor = Color.FromArgb(235, 235, 244);
                subCreatureGiveNov_Pnl.PanelOutlineColor = Color.FromArgb(235, 235, 244);
            }
            else
            {
                await Task.Delay(20);
                subCreatureGiveNov_Pnl.PanelColor = Color.FromArgb(25, 55, 255);
                subCreatureGiveNov_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
                subCreatureGiveNov_stLbl.ForeColor = Color.White;
                await Task.Delay(20);
                subCreatureGiveNov_Pnl.PanelColor = Color.FromArgb(26, 54, 244);
                subCreatureGiveNov_Pnl.PanelOutlineColor = Color.FromArgb(26, 54, 244);
                await Task.Delay(20);
                subCreatureGiveNov_Pnl.PanelColor = Color.FromArgb(26, 52, 232);
                subCreatureGiveNov_Pnl.PanelOutlineColor = Color.FromArgb(26, 52, 232);
            }
        }
        private async void subCreatureGiveNov_Pnl_MouseLeave(object sender, EventArgs e)
        {
            if (subCreatureGive_nov == true)
            {
                subCreatureGiveNov_Pnl.PanelColor = Color.FromArgb(235, 235, 244);
                subCreatureGiveNov_Pnl.PanelOutlineColor = Color.FromArgb(235, 235, 244);
                subCreatureGiveNov_stLbl.ForeColor = Color.FromArgb(49, 50, 60);
                await Task.Delay(20);
                subCreatureGiveNov_Pnl.PanelColor = Color.FromArgb(237, 238, 246);
                subCreatureGiveNov_Pnl.PanelOutlineColor = Color.FromArgb(237, 238, 246);
                await Task.Delay(20);
                subCreatureGiveNov_Pnl.PanelColor = Color.FromArgb(240, 240, 248);
                subCreatureGiveNov_Pnl.PanelOutlineColor = Color.FromArgb(240, 240, 248);
                await Task.Delay(20);
                subCreatureGiveNov_Pnl.PanelColor = Color.FromArgb(242, 243, 250);
                subCreatureGiveNov_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
            }
            else
            {
                subCreatureGiveNov_Pnl.PanelColor = Color.FromArgb(26, 52, 232);
                subCreatureGiveNov_Pnl.PanelOutlineColor = Color.FromArgb(26, 52, 232);
                subCreatureGiveNov_stLbl.ForeColor = Color.White;
                await Task.Delay(20);
                subCreatureGiveNov_Pnl.PanelColor = Color.FromArgb(26, 54, 244);
                subCreatureGiveNov_Pnl.PanelOutlineColor = Color.FromArgb(26, 54, 244);
                await Task.Delay(20);
                subCreatureGiveNov_Pnl.PanelColor = Color.FromArgb(25, 55, 255);
                subCreatureGiveNov_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
            }
        }
        // Кнопка месяца декабрь в панеле с оформлением, во вкладке [Оформление подписок]
        private void subCreatureGiveDec_Pnl_Click(object sender, EventArgs e)
        {
            if (subCreatureGive_dec == true)
            {
                subCreatureGive_dec = false;
                subCreatureGiveDec_Pnl.PanelColor = Color.FromArgb(25, 55, 255);
                subCreatureGiveDec_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
                subCreatureGiveDec_stLbl.ForeColor = Color.White;
            }
            else
            {
                subCreatureGive_dec = true;
                subCreatureGiveDec_Pnl.PanelColor = Color.FromArgb(242, 243, 250);
                subCreatureGiveDec_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
                subCreatureGiveDec_stLbl.ForeColor = Color.FromArgb(49, 50, 60);
            }
            PanelsMounthUpdate();
        }
        private async void subCreatureGiveDec_Pnl_MouseEnter(object sender, EventArgs e)
        {
            if (subCreatureGive_dec == true)
            {
                subCreatureGiveDec_Pnl.PanelColor = Color.FromArgb(242, 243, 250);
                subCreatureGiveDec_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
                subCreatureGiveDec_stLbl.ForeColor = Color.FromArgb(49, 50, 60);
                await Task.Delay(20);
                subCreatureGiveDec_Pnl.PanelColor = Color.FromArgb(240, 240, 248);
                subCreatureGiveDec_Pnl.PanelOutlineColor = Color.FromArgb(240, 240, 248);
                await Task.Delay(20);
                subCreatureGiveDec_Pnl.PanelColor = Color.FromArgb(237, 238, 246);
                subCreatureGiveDec_Pnl.PanelOutlineColor = Color.FromArgb(237, 238, 246);
                await Task.Delay(20);
                subCreatureGiveDec_Pnl.PanelColor = Color.FromArgb(235, 235, 244);
                subCreatureGiveDec_Pnl.PanelOutlineColor = Color.FromArgb(235, 235, 244);
            }
            else
            {
                await Task.Delay(20);
                subCreatureGiveDec_Pnl.PanelColor = Color.FromArgb(25, 55, 255);
                subCreatureGiveDec_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
                subCreatureGiveDec_stLbl.ForeColor = Color.White;
                await Task.Delay(20);
                subCreatureGiveDec_Pnl.PanelColor = Color.FromArgb(26, 54, 244);
                subCreatureGiveDec_Pnl.PanelOutlineColor = Color.FromArgb(26, 54, 244);
                await Task.Delay(20);
                subCreatureGiveDec_Pnl.PanelColor = Color.FromArgb(26, 52, 232);
                subCreatureGiveDec_Pnl.PanelOutlineColor = Color.FromArgb(26, 52, 232);
            }
        }
        private async void subCreatureGiveDec_Pnl_MouseLeave(object sender, EventArgs e)
        {
            if (subCreatureGive_dec == true)
            {
                subCreatureGiveDec_Pnl.PanelColor = Color.FromArgb(235, 235, 244);
                subCreatureGiveDec_Pnl.PanelOutlineColor = Color.FromArgb(235, 235, 244);
                subCreatureGiveDec_stLbl.ForeColor = Color.FromArgb(49, 50, 60);
                await Task.Delay(20);
                subCreatureGiveDec_Pnl.PanelColor = Color.FromArgb(237, 238, 246);
                subCreatureGiveDec_Pnl.PanelOutlineColor = Color.FromArgb(237, 238, 246);
                await Task.Delay(20);
                subCreatureGiveDec_Pnl.PanelColor = Color.FromArgb(240, 240, 248);
                subCreatureGiveDec_Pnl.PanelOutlineColor = Color.FromArgb(240, 240, 248);
                await Task.Delay(20);
                subCreatureGiveDec_Pnl.PanelColor = Color.FromArgb(242, 243, 250);
                subCreatureGiveDec_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
            }
            else
            {
                subCreatureGiveDec_Pnl.PanelColor = Color.FromArgb(26, 52, 232);
                subCreatureGiveDec_Pnl.PanelOutlineColor = Color.FromArgb(26, 52, 232);
                subCreatureGiveDec_stLbl.ForeColor = Color.White;
                await Task.Delay(20);
                subCreatureGiveDec_Pnl.PanelColor = Color.FromArgb(26, 54, 244);
                subCreatureGiveDec_Pnl.PanelOutlineColor = Color.FromArgb(26, 54, 244);
                await Task.Delay(20);
                subCreatureGiveDec_Pnl.PanelColor = Color.FromArgb(25, 55, 255);
                subCreatureGiveDec_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
            }
        }
        // Кнопка доставки оформить в панеле с оформлением, во вкладке [Оформление подписок]
        private async void subCreatureGiveBuy_Pnl_Click(object sender, EventArgs e)
        {
            if (_currentTable == TableType.Subscriptions)
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
                    term += TermCalculate(subCreatureGiveJan_Pnl);
                    term += TermCalculate(subCreatureGiveFeb_Pnl);
                    term += TermCalculate(subCreatureGiveMar_Pnl);
                    term += TermCalculate(subCreatureGiveApr_Pnl);
                    term += TermCalculate(subCreatureGiveMay_Pnl);
                    term += TermCalculate(subCreatureGiveJun_Pnl);
                    term += TermCalculate(subCreatureGiveJul_Pnl);
                    term += TermCalculate(subCreatureGiveAug_Pnl);
                    term += TermCalculate(subCreatureGiveSep_Pnl);
                    term += TermCalculate(subCreatureGiveOct_Pnl);
                    term += TermCalculate(subCreatureGiveNov_Pnl);
                    term += TermCalculate(subCreatureGiveDec_Pnl);

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
                    int.TryParse(subCreatureGiveCounter_numberLbl_1_1.Content, out kits);

                    float minTermPrice = -1;
                    if (!subCreatureGive_delivery)
                        minTermPrice = _selectedEdition.MinTermHousePrice;
                    else if (subCreatureGive_delivery)
                        minTermPrice = _selectedEdition.MinTermPricePerMailbox;

                    float priceMonth = minTermPrice / _selectedEdition.MinTermSubscription;
                    var newSubscription = new DataBase.Subscriptions
                    {
                        Id = _selectedSubscription.Id,
                        TermSubscription = term,
                        PriceSubscription = $"{priceMonth * count * kits} ₽",
                        Kit = kits,
                        DateRegistred = _selectedSubscription.DateRegistred,
                        IndexEdition = _selectedEdition.Index
                    };

                    // Сохраняем в БД
                    await DataBase._client.From<DataBase.Subscriptions>().Upsert(newSubscription);

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

                    // Обновляем в таблице
                    foreach (DataGridViewRow row in subTabel_Dgw.Rows)
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
            else if (_currentTable == TableType.Editions)
            {
                try
                {
                    if (_selectedSubscriptionReaderId == Guid.Empty)
                    {
                        Logger.ShowWarning("Выберите читателя");
                        return;
                    }
                    if (_selectedEdition == null)
                    {
                        Logger.ShowWarning("Выберите издание");
                        return;
                    }

                    string term = string.Empty;
                    int kits = 1;
                    term += TermCalculate(subCreatureGiveJan_Pnl);
                    term += TermCalculate(subCreatureGiveFeb_Pnl);
                    term += TermCalculate(subCreatureGiveMar_Pnl);
                    term += TermCalculate(subCreatureGiveApr_Pnl);
                    term += TermCalculate(subCreatureGiveMay_Pnl);
                    term += TermCalculate(subCreatureGiveJun_Pnl);
                    term += TermCalculate(subCreatureGiveJul_Pnl);
                    term += TermCalculate(subCreatureGiveAug_Pnl);
                    term += TermCalculate(subCreatureGiveSep_Pnl);
                    term += TermCalculate(subCreatureGiveOct_Pnl);
                    term += TermCalculate(subCreatureGiveNov_Pnl);
                    term += TermCalculate(subCreatureGiveDec_Pnl);

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
                    int.TryParse(subCreatureGiveCounter_numberLbl_1_1.Content, out kits);

                    float minTermPrice = -1;
                    if (!subCreatureGive_delivery)
                        minTermPrice = _selectedEdition.MinTermHousePrice;
                    else if (subCreatureGive_delivery)
                        minTermPrice = _selectedEdition.MinTermPricePerMailbox;

                    float priceMonth = minTermPrice / _selectedEdition.MinTermSubscription;
                    var newSubscription = new DataBase.Subscriptions
                    {
                        Id = Guid.NewGuid(),
                        TermSubscription = term,
                        PriceSubscription = $"{priceMonth * count * kits} ₽",
                        Kit = kits,
                        DateRegistred = DateTime.UtcNow,
                        IndexEdition = _selectedEdition.Index
                    };

                    // Сохраняем в БД
                    await DataBase._client.From<DataBase.Subscriptions>().Insert(newSubscription);
                    var readers = await DataBase._client.From<DataBase.Readers>().Where(r => r.Id == _selectedSubscriptionReaderId).Get();
                    var reader = readers.Model;

                    string idSubscription = string.Empty;
                    if (reader.IdActiveSubscriptions == string.Empty)
                        idSubscription = newSubscription.Id.ToString();
                    else
                        idSubscription = $"{reader.IdActiveSubscriptions},{newSubscription.Id.ToString()}";

                    var updatedReader = new DataBase.Readers
                    {
                        Id = reader.Id,
                        FIO = reader.FIO,
                        IdActiveSubscriptions = idSubscription
                    };

                    // Обновляем в БД
                    await DataBase._client.From<DataBase.Readers>().Upsert(updatedReader);

                    Logger.Info($"Подписка {idSubscription} успешно оформлена");
                    Logger.ShowInfo("Подписка успешно оформлена");
                }
                catch (Exception ex)
                {
                    Logger.Error("Ошибка при оформлении подписки", ex);
                    Logger.ShowError("Ошибка при оформлении подписки");
                }
            }
        }

        private string TermCalculate(cuiPanel panel)
        {
            if (panel == subCreatureGiveJan_Pnl)
            {
                if (subCreatureGive_jan)
                    return "1";
                else if (!subCreatureGive_jan)
                    return "0";
            }
            else if (panel == subCreatureGiveFeb_Pnl)
            {
                if (subCreatureGive_feb)
                    return "1";
                else if (!subCreatureGive_feb)
                    return "0";
            }
            else if (panel == subCreatureGiveMar_Pnl)
            {
                if (subCreatureGive_mar)
                    return "1";
                else if (!subCreatureGive_mar)
                    return "0";
            }
            else if (panel == subCreatureGiveApr_Pnl)
            {
                if (subCreatureGive_apr)
                    return "1";
                else if (!subCreatureGive_apr)
                    return "0";
            }
            else if (panel == subCreatureGiveMay_Pnl)
            {
                if (subCreatureGive_may)
                    return "1";
                else if (!subCreatureGive_may)
                    return "0";
            }
            else if (panel == subCreatureGiveJun_Pnl)
            {
                if (subCreatureGive_jun)
                    return "1";
                else if (!subCreatureGive_jun)
                    return "0";
            }
            else if (panel == subCreatureGiveJul_Pnl)
            {
                if (subCreatureGive_jul)
                    return "1";
                else if (!subCreatureGive_jul)
                    return "0";
            }
            else if (panel == subCreatureGiveAug_Pnl)
            {
                if (subCreatureGive_aug)
                    return "1";
                else if (!subCreatureGive_aug)
                    return "0";
            }
            else if (panel == subCreatureGiveSep_Pnl)
            {
                if (subCreatureGive_sep)
                    return "1";
                else if (!subCreatureGive_sep)
                    return "0";
            }
            else if (panel == subCreatureGiveOct_Pnl)
            {
                if (subCreatureGive_oct)
                    return "1";
                else if (!subCreatureGive_oct)
                    return "0";
            }
            else if (panel == subCreatureGiveNov_Pnl)
            {
                if (subCreatureGive_nov)
                    return "1";
                else if (!subCreatureGive_nov)
                    return "0";
            }
            else if (panel == subCreatureGiveDec_Pnl)
            {
                if (subCreatureGive_dec)
                    return "1";
                else if (!subCreatureGive_dec)
                    return "0";
            }
            return "-1";
        }

        private async void subCreatureGiveBuy_Pnl_MouseEnter(object sender, EventArgs e)
        {
            subCreatureGiveBuy_Pnl.PanelColor = Color.FromArgb(25, 55, 255); //1
            subCreatureGiveBuy_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
            await Task.Delay(20);
            subCreatureGiveBuy_Pnl.PanelColor = Color.FromArgb(26, 54, 244); //2
            subCreatureGiveBuy_Pnl.PanelOutlineColor = Color.FromArgb(26, 54, 244);
            await Task.Delay(20);
            subCreatureGiveBuy_Pnl.PanelColor = Color.FromArgb(26, 52, 232); //3
            subCreatureGiveBuy_Pnl.PanelOutlineColor = Color.FromArgb(26, 52, 232);
        }
        private async void subCreatureGiveBuy_Pnl_MouseLeave(object sender, EventArgs e)
        {
            await Task.Delay(100);
            subCreatureGiveBuy_Pnl.PanelColor = Color.FromArgb(26, 52, 232); //3
            subCreatureGiveBuy_Pnl.PanelOutlineColor = Color.FromArgb(26, 52, 232);
            await Task.Delay(20);
            subCreatureGiveBuy_Pnl.PanelColor = Color.FromArgb(26, 54, 244); //2
            subCreatureGiveBuy_Pnl.PanelOutlineColor = Color.FromArgb(26, 54, 244);
            await Task.Delay(20);
            subCreatureGiveBuy_Pnl.PanelColor = Color.FromArgb(25, 55, 255); //1
            subCreatureGiveBuy_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
        }
        // Кнопка доставки на дом в панеле с оформлением, во вкладке [Оформление подписок]
        private void subCreatureGiveHome_Pnl_Click(object sender, EventArgs e)
        {
            subCreatureGive_delivery = false;
            subCreatureGiveHome_titleLbl.ForeColor = Color.FromArgb(26, 52, 232);
            subCreatureGiveBox_titleLbl.ForeColor = Color.FromArgb(49, 50, 60);
            subCreatureGiveHome_Pnl.PanelColor = Color.FromArgb(242, 243, 250);
            subCreatureGiveHome_Pnl.PanelOutlineColor = Color.FromArgb(26, 52, 232);
            subCreatureGiveBox_Pnl.PanelColor = Color.FromArgb(242, 243, 250);
            subCreatureGiveBox_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
            subCreatureGiveHome_homePic.Image = Properties.Resources.Дом2;
            subCreatureGiveBox_boxPic.Image = Properties.Resources.ПочтовыйЯщик1;
            subCreatureGiveHome_Pnl.Cursor = Cursors.Arrow;
            subCreatureGiveBox_Pnl.Cursor = Cursors.Hand;
            subCreatureGiveHome_Pnl.Enabled = false;
            subCreatureGiveBox_Pnl.Enabled = true;
            PanelsMounthUpdate();
        }
        private async void subCreatureGiveHome_Pnl_MouseEnter(object sender, EventArgs e)
        {
            if (subCreatureGive_delivery == true)
            {
                subCreatureGiveHome_Pnl.PanelColor = Color.FromArgb(242, 243, 250); //1
                subCreatureGiveHome_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
                await Task.Delay(20);
                subCreatureGiveHome_Pnl.PanelColor = Color.FromArgb(240, 240, 248); //2
                subCreatureGiveHome_Pnl.PanelOutlineColor = Color.FromArgb(240, 240, 248);
                await Task.Delay(20);
                subCreatureGiveHome_Pnl.PanelColor = Color.FromArgb(237, 238, 246); //3
                subCreatureGiveHome_Pnl.PanelOutlineColor = Color.FromArgb(237, 238, 246);
                await Task.Delay(20);
                subCreatureGiveHome_Pnl.PanelColor = Color.FromArgb(235, 235, 244); //4
                subCreatureGiveHome_Pnl.PanelOutlineColor = Color.FromArgb(235, 235, 244);
            }
        }

        private async void PanelsMounthUpdate()
        {
            if (_selectedEdition != null)
            {
                await Task.Delay(delay);
                string term = string.Empty;
                term += TermCalculate(subCreatureGiveJan_Pnl);
                term += TermCalculate(subCreatureGiveFeb_Pnl);
                term += TermCalculate(subCreatureGiveMar_Pnl);
                term += TermCalculate(subCreatureGiveApr_Pnl);
                term += TermCalculate(subCreatureGiveMay_Pnl);
                term += TermCalculate(subCreatureGiveJun_Pnl);
                term += TermCalculate(subCreatureGiveJul_Pnl);
                term += TermCalculate(subCreatureGiveAug_Pnl);
                term += TermCalculate(subCreatureGiveSep_Pnl);
                term += TermCalculate(subCreatureGiveOct_Pnl);
                term += TermCalculate(subCreatureGiveNov_Pnl);
                term += TermCalculate(subCreatureGiveDec_Pnl);

                int count = 0;
                for (int i = 0; i < term.Length; i++)
                {
                    if (term[i] == '1')
                        count++;
                }

                float priceMonth = _selectedEdition.MinTermHousePrice / _selectedEdition.MinTermSubscription;
                subCreatureGive_priceLbl_2_1.Content = $"{priceMonth * count * int.Parse(subCreatureGiveCounter_numberLbl_1_1.Content)} ₽";

                if (!subCreatureGive_delivery)
                    cuiLabel1.Content = $"{count} месяца(а/ев) ● на дом";
                else if (subCreatureGive_delivery)
                    cuiLabel1.Content = $"{count} месяц(а/ев) ● на почтовый ящик";

                if (count == 0 && subCreatureGiveBuy_Pnl.Enabled)
                    subCreatureGiveBuy_Pnl.Enabled = false;
                else if (count != 0 && !subCreatureGiveBuy_Pnl.Enabled)
                    subCreatureGiveBuy_Pnl.Enabled = true;
            }
            else
            {
                subCreatureGive_priceLbl_2_1.Content = "0 ₽";
                cuiLabel1.Content = "0 месяца(а/ев)";
            }
        }
        private async void subCreatureGiveHome_Pnl_MouseLeave(object sender, EventArgs e)
        {
            if (subCreatureGive_delivery == true)
            {
                await Task.Delay(100);
                subCreatureGiveHome_Pnl.PanelColor = Color.FromArgb(235, 235, 244); //4
                subCreatureGiveHome_Pnl.PanelOutlineColor = Color.FromArgb(235, 235, 244);
                await Task.Delay(20);
                subCreatureGiveHome_Pnl.PanelColor = Color.FromArgb(237, 238, 246); //3
                subCreatureGiveHome_Pnl.PanelOutlineColor = Color.FromArgb(237, 238, 246);
                await Task.Delay(20);
                subCreatureGiveHome_Pnl.PanelColor = Color.FromArgb(240, 240, 248); //2
                subCreatureGiveHome_Pnl.PanelOutlineColor = Color.FromArgb(240, 240, 248);
                await Task.Delay(20);
                subCreatureGiveHome_Pnl.PanelColor = Color.FromArgb(242, 243, 250); //1
                subCreatureGiveHome_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
            }
        }
        // Кнопка доставки на ящик в панеле с оформлением, во вкладке [Оформление подписок]
        private void subCreatureGiveBox_Pnl_Click(object sender, EventArgs e)
        {
            subCreatureGive_delivery = true;
            subCreatureGiveHome_titleLbl.ForeColor = Color.FromArgb(49, 50, 60);
            subCreatureGiveBox_titleLbl.ForeColor = Color.FromArgb(26, 52, 232);
            subCreatureGiveHome_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
            subCreatureGiveHome_Pnl.PanelColor = Color.FromArgb(242, 243, 250);
            subCreatureGiveBox_Pnl.PanelColor = Color.FromArgb(242, 243, 250);
            subCreatureGiveBox_Pnl.PanelOutlineColor = Color.FromArgb(26, 52, 232);
            subCreatureGiveHome_homePic.Image = Properties.Resources.Дом1;
            subCreatureGiveBox_boxPic.Image = Properties.Resources.ПочтовыйЯщик2;
            subCreatureGiveHome_Pnl.Cursor = Cursors.Hand;
            subCreatureGiveBox_Pnl.Cursor = Cursors.Arrow;
            subCreatureGiveHome_Pnl.Enabled = true;
            subCreatureGiveBox_Pnl.Enabled = false;
            PanelsMounthUpdate();
        }
        private async void subCreatureGiveBox_Pnl_MouseEnter(object sender, EventArgs e)
        {
            if (subCreatureGive_delivery == false)
            {
                subCreatureGiveBox_Pnl.PanelColor = Color.FromArgb(242, 243, 250); //1
                subCreatureGiveBox_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
                await Task.Delay(20);
                subCreatureGiveBox_Pnl.PanelColor = Color.FromArgb(240, 240, 248); //2
                subCreatureGiveBox_Pnl.PanelOutlineColor = Color.FromArgb(240, 240, 248);
                await Task.Delay(20);
                subCreatureGiveBox_Pnl.PanelColor = Color.FromArgb(237, 238, 246); //3
                subCreatureGiveBox_Pnl.PanelOutlineColor = Color.FromArgb(237, 238, 246);
                await Task.Delay(20);
                subCreatureGiveBox_Pnl.PanelColor = Color.FromArgb(235, 235, 244); //4
                subCreatureGiveBox_Pnl.PanelOutlineColor = Color.FromArgb(235, 235, 244);
            }
        }
        private async void subCreatureGiveBox_Pnl_MouseLeave(object sender, EventArgs e)
        {
            if (subCreatureGive_delivery == false)
            {
                await Task.Delay(100);
                subCreatureGiveBox_Pnl.PanelColor = Color.FromArgb(235, 235, 244); //4
                subCreatureGiveBox_Pnl.PanelOutlineColor = Color.FromArgb(235, 235, 244);
                await Task.Delay(20);
                subCreatureGiveBox_Pnl.PanelColor = Color.FromArgb(237, 238, 246); //3
                subCreatureGiveBox_Pnl.PanelOutlineColor = Color.FromArgb(237, 238, 246);
                await Task.Delay(20);
                subCreatureGiveBox_Pnl.PanelColor = Color.FromArgb(240, 240, 248); //2
                subCreatureGiveBox_Pnl.PanelOutlineColor = Color.FromArgb(240, 240, 248);
                await Task.Delay(20);
                subCreatureGiveBox_Pnl.PanelColor = Color.FromArgb(242, 243, 250); //1
                subCreatureGiveBox_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
            }
        }
        // subEdit_Pnl + subSubs_Pnl Кнопки переключение таблицы, во вкладке [Оформление подписок]
        // Кнопка переключение таблицы на издания, во вкладке [Оформление подписок]
        private void subEdit1_Pnl_Click(object sender, EventArgs e)
        {
            subEdit2_Pnl.Visible = true;
            subSubs2_Pnl.Visible = false;
            subHalf1_Pnl.Rounding = new Padding(0, 0, 10, 500);
            subHalf2_Pnl.Visible = false;
            subCorner_Pnl.Rounding = new Padding(18, 0, 18, 18);

            SwitchMainTables(TableType.Editions);
        }
        private async void subEdit1_Pnl_MouseEnter(object sender, EventArgs e)
        {
            subEdit1_Pnl.PanelColor = Color.FromArgb(240, 240, 250); //1
            subEdit1_Pnl.PanelOutlineColor = Color.FromArgb(240, 240, 250);
            await Task.Delay(20);
            subEdit1_Pnl.PanelColor = Color.FromArgb(230, 236, 251); //2
            subEdit1_Pnl.PanelOutlineColor = Color.FromArgb(230, 236, 251);
            await Task.Delay(20);
            subEdit1_Pnl.PanelColor = Color.FromArgb(221, 231, 252); //3
            subEdit1_Pnl.PanelOutlineColor = Color.FromArgb(221, 231, 252);
            await Task.Delay(20);
            subEdit1_Pnl.PanelColor = Color.FromArgb(211, 227, 253); //4
            subEdit1_Pnl.PanelOutlineColor = Color.FromArgb(211, 227, 253);
        }
        private async void subEdit1_Pnl_MouseLeave(object sender, EventArgs e)
        {
            await Task.Delay(100);
            subEdit1_Pnl.PanelColor = Color.FromArgb(211, 227, 253); //4
            subEdit1_Pnl.PanelOutlineColor = Color.FromArgb(211, 227, 253);
            await Task.Delay(20);
            subEdit1_Pnl.PanelColor = Color.FromArgb(221, 231, 252); //3
            subEdit1_Pnl.PanelOutlineColor = Color.FromArgb(221, 231, 252);
            await Task.Delay(20);
            subEdit1_Pnl.PanelColor = Color.FromArgb(230, 236, 251); //2
            subEdit1_Pnl.PanelOutlineColor = Color.FromArgb(230, 236, 251);
            await Task.Delay(20);
            subEdit1_Pnl.PanelColor = Color.FromArgb(240, 240, 250); //1
            subEdit1_Pnl.PanelOutlineColor = Color.FromArgb(240, 240, 250);
        }
        // Кнопка переключение таблицы на подписки, во вкладке [Оформление подписок]
        private void subSubs1_Pnl_Click(object sender, EventArgs e)
        {
            subEdit2_Pnl.Visible = false;
            subSubs2_Pnl.Visible = true;
            subHalf1_Pnl.Rounding = new Padding(0, 0, 500, 10);
            subHalf2_Pnl.Visible = true;
            subCorner_Pnl.Rounding = new Padding(18, 18, 18, 18);

            SwitchMainTables(TableType.Subscriptions);
        }

        private async void SwitchMainTables(TableType table)
        {
            if (table == TableType.Subscriptions)
            {
                _currentTable = TableType.Subscriptions;
                subTabel_Dgw.Rows.Clear();
                subTabel_Dgw.Columns.Clear();
                _loaderEditions.Reset();
                DataTables.InitializeSubscriptionsTable(subTabel_Dgw);
                await LoadDataAsync(TableType.Subscriptions);
            }
            else if (table == TableType.Editions)
            {
                _currentTable = TableType.Editions;
                subTabel_Dgw.Rows.Clear();
                subTabel_Dgw.Columns.Clear();
                _loaderSubs.Reset();
                DataTables.InitializeEditionsTable(subTabel_Dgw);
                await LoadDataAsync(TableType.Editions);
            }
        }

        private async void subSubs1_Pnl_MouseEnter(object sender, EventArgs e)
        {
            subSubs1_Pnl.PanelColor = Color.FromArgb(240, 240, 250); //1
            subSubs1_Pnl.PanelOutlineColor = Color.FromArgb(240, 240, 250);
            await Task.Delay(20);
            subSubs1_Pnl.PanelColor = Color.FromArgb(230, 236, 251); //2
            subSubs1_Pnl.PanelOutlineColor = Color.FromArgb(230, 236, 251);
            await Task.Delay(20);
            subSubs1_Pnl.PanelColor = Color.FromArgb(221, 231, 252); //3
            subSubs1_Pnl.PanelOutlineColor = Color.FromArgb(221, 231, 252);
            await Task.Delay(20);
            subSubs1_Pnl.PanelColor = Color.FromArgb(211, 227, 253); //4
            subSubs1_Pnl.PanelOutlineColor = Color.FromArgb(211, 227, 253);
        }
        private async void subSubs1_Pnl_MouseLeave(object sender, EventArgs e)
        {
            await Task.Delay(100);
            subSubs1_Pnl.PanelColor = Color.FromArgb(211, 227, 253); //4
            subSubs1_Pnl.PanelOutlineColor = Color.FromArgb(211, 227, 253);
            await Task.Delay(20);
            subSubs1_Pnl.PanelColor = Color.FromArgb(221, 231, 252); //3
            subSubs1_Pnl.PanelOutlineColor = Color.FromArgb(221, 231, 252);
            await Task.Delay(20);
            subSubs1_Pnl.PanelColor = Color.FromArgb(230, 236, 251); //2
            subSubs1_Pnl.PanelOutlineColor = Color.FromArgb(230, 236, 251);
            await Task.Delay(20);
            subSubs1_Pnl.PanelColor = Color.FromArgb(240, 240, 250); //1
            subSubs1_Pnl.PanelOutlineColor = Color.FromArgb(240, 240, 250);
        }
        // subTabel_Dgw Таблицы тестовые, для проверки (Удалить), во вкладке [Оформление подписок]
        // Данные подписки, во вкладке [Оформление подписок]
        private void AddSampleData3()
        {
            subTabel_Dgw.Columns.Clear();
            subTabel_Dgw.Rows.Clear();
            subTabel_Dgw.Columns.Add("TermSubscription", "Срок подписки");
            subTabel_Dgw.Columns.Add("PriceSubscription", "Цена подписки");
            subTabel_Dgw.Columns.Add("Kit", "Количество комплектов");
            subTabel_Dgw.Columns.Add("DateRegistred", "Дата оформления");
            subTabel_Dgw.Columns.Add("Edition", "Название издания");
            subTabel_Dgw.Columns.Add("edit", "редактировать");
            subTabel_Dgw.Columns.Add("delete", "удалить");

            subTabel_Dgw.Rows.Add("1 месяц", "500 ₽", "1", "10.01.2025", "Компьютерра");
            subTabel_Dgw.Rows.Add("3 месяца", "1350 ₽", "2", "15.02.2025", "Наука и жизнь");
            subTabel_Dgw.Rows.Add("6 месяцев", "2500 ₽", "1", "20.03.2025", "Вокруг света");
            subTabel_Dgw.Rows.Add("1 год", "4800 ₽", "3", "01.04.2025", "Maxim");
            subTabel_Dgw.Rows.Add("1 месяц", "450 ₽", "1", "05.05.2025", "Playboy");
            subTabel_Dgw.Rows.Add("3 месяца", "1200 ₽", "2", "12.06.2025", "Forbes");
            subTabel_Dgw.Rows.Add("6 месяцев", "2300 ₽", "1", "18.07.2025", "National Geographic");
            subTabel_Dgw.Rows.Add("1 год", "5000 ₽", "4", "22.08.2025", "Cosmopolitan");
            subTabel_Dgw.Rows.Add("1 месяц", "600 ₽", "1", "30.09.2025", "GQ");
            subTabel_Dgw.Rows.Add("3 месяца", "1600 ₽", "2", "10.10.2025", "Vogue");
            subTabel_Dgw.Rows.Add("6 месяцев", "2700 ₽", "3", "15.11.2025", "Тинькофф Журнал");
            subTabel_Dgw.Rows.Add("1 год", "5200 ₽", "1", "20.12.2025", "Кот Шрёдингера");
            subTabel_Dgw.Rows.Add("1 месяц", "400 ₽", "2", "05.01.2026", "Популярная механика");
            subTabel_Dgw.Rows.Add("3 месяца", "1100 ₽", "1", "10.02.2026", "Men's Health");
            subTabel_Dgw.Rows.Add("6 месяцев", "2400 ₽", "2", "18.03.2026", "Harvard Business Review");
        }
        // Данные изданий, во вкладке [Оформление подписок]
        private void AddSampleData2()
        {
            subTabel_Dgw.Columns.Clear();
            subTabel_Dgw.Rows.Clear();
            subTabel_Dgw.Columns.Add("Индекс", "Индекс");
            subTabel_Dgw.Columns.Add("Наименование издания", "Наименование издания");
            subTabel_Dgw.Columns.Add("Вид изд.", "Вид изд.");
            subTabel_Dgw.Columns.Add("Мин. срок подписки (мес.)", "Мин. срок подписки (мес.)");
            subTabel_Dgw.Columns.Add("Подписная цена (на дом) мин. срок", "(на дом) мин. срок");
            subTabel_Dgw.Columns.Add("Подписная цена (в аб. ящик) мин. срок", "(в аб. ящик) мин. срок");
            subTabel_Dgw.Columns.Add("Макс. срок подписки (мес.)", "Макс. срок подписки (мес.)");
            subTabel_Dgw.Columns.Add("Подписная цена (на дом) макс. срок", "на дом) макс. срок");
            subTabel_Dgw.Columns.Add("Подписная цена (в аб. ящик) макс. срок", "(в аб. ящик) макс. срок");

            subTabel_Dgw.Rows.Add("1", "Компьютерра", "Журнал", "1", "500", "450", "12", "4800", "4320");
            subTabel_Dgw.Rows.Add("2", "Наука и жизнь", "Журнал", "1", "450", "405", "12", "1350", "1215");
            subTabel_Dgw.Rows.Add("3", "Вокруг света", "Журнал", "1", "417", "375", "6", "2500", "2250");
            subTabel_Dgw.Rows.Add("4", "Maxim", "Журнал", "1", "400", "360", "12", "4800", "4320");
            subTabel_Dgw.Rows.Add("5", "Playboy", "Журнал", "1", "450", "405", "12", "5400", "4860");
            subTabel_Dgw.Rows.Add("6", "Forbes", "Журнал", "1", "400", "360", "3", "1200", "1080");
            subTabel_Dgw.Rows.Add("7", "National Geographic", "Журнал", "1", "383", "345", "6", "2300", "2070");
            subTabel_Dgw.Rows.Add("8", "Cosmopolitan", "Журнал", "1", "417", "375", "12", "5000", "4500");
            subTabel_Dgw.Rows.Add("9", "GQ", "Журнал", "1", "600", "540", "12", "7200", "6480");
            subTabel_Dgw.Rows.Add("10", "Vogue", "Журнал", "1", "533", "480", "3", "1600", "1440");
            subTabel_Dgw.Rows.Add("11", "Тинькофф Журнал", "Журнал", "1", "450", "405", "6", "2700", "2430");
            subTabel_Dgw.Rows.Add("12", "Кот Шрёдингера", "Журнал", "1", "433", "390", "12", "5200", "4680");
            subTabel_Dgw.Rows.Add("13", "Популярная механика", "Журнал", "1", "400", "360", "12", "4800", "4320");
            subTabel_Dgw.Rows.Add("14", "Men's Health", "Журнал", "1", "367", "330", "3", "1100", "990");
            subTabel_Dgw.Rows.Add("15", "Harvard Business Review", "Журнал", "1", "400", "360", "6", "2400", "2160");

        }
        // subTabelUpperFilter_Pnl_Click Комбо бокс фильтр отвечающий за выезд панелей фильтра, во вкладке [Оформление подписок]
        private void subTabelUpperFilter_Pnl_Click(object sender, EventArgs e)
        {
            if (subTabel_arrow == false)
            {
                subTabel_arrow = true;
                subTabelUpperFilter_arrowPic.Image = Properties.Resources.КомбоБокс2;
            }
            else
            {
                subTabel_arrow = false;
                subTabelUpperFilter_arrowPic.Image = Properties.Resources.КомбоБокс1;
            }
            if (subFilter_change == true)
            {
                subTabelFilter2_animation = !subTabelFilter2_animation;
                subTabelFilter2_animationTmr.Start();
            }
            else
            {
                subTabelFilter1_animation = !subTabelFilter1_animation;
                subTabelFilter1_animationTmr.Start();
            }
        }
        // subTabelSubscriptionFilter_Pnl Панель фильтра изданий, во вкладке [Оформление подписок]
        // Таймер с функции выезда панели фильтра изданий, во вкладке [Оформление подписок]
        private void subTabelFilter1_animationTmr_Tick(object sender, EventArgs e)
        {
            int target = subTabelFilter1_animation ? subTabelFilter1_down : subTabelFilter1_up;
            int distance = target - subTabelEdition_Pnl.Top;
            int step = (int)(distance * 0.1f);
            if (Math.Abs(distance) < 1)
            {
                subTabelEdition_Pnl.Top = target;
                subTabelFilter1_animationTmr.Stop();
                return;
            }
            subTabelEdition_Pnl.Top += step;
        }
        // Ползунок срока месяца в фильтра изданий, во вкладке [Оформление подписок]
        private void subTabelEditionFilterTerm_termSdr_ValueChanged(object sender, EventArgs e)
        {
            subTabelEditionFilterTerm_monthLbl.Content = subTabelEditionFilterTerm_termSdr.Value.ToString() + " мес.";
        }

        //Кнопка применить фильтра издании, во вкладке [Оформление подписок]
        private async void subTabelEditionFilterDone_donePnl_Click(object sender, EventArgs e)
        {
            if (_currentTable != TableType.Editions) return;

            try
            {
                // Собираем включённые типы
                var activeTypes = subTabelEditionFilterEdit_Pnl.Controls
                    .OfType<cuiCheckbox>()
                    .Where(cb => cb.Checked)
                    .Select(cb => cb.Tag.ToString())
                    .ToList();

                int minTerm = (int)subTabelEditionFilterTerm_termSdr.Value;

                var all = await DataBase._client.From<DataBase.Editions>().Get();

                var filtered = all.Models.Where(ed =>
                {
                    if (!activeTypes.Contains(ed.TypeEdition)) return false;
                    if (minTerm > 0 && ed.MinTermSubscription < minTerm) return false;
                    return true;
                }).ToList();

                subTabel_Dgw.Rows.Clear();
                foreach (var item in filtered)
                    DataTables.AddEditionRow(subTabel_Dgw, item);
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка фильтрации изданий", ex);
            }
        }

        private async void subTabelEditionFilterDone_donePnl_MouseEnter(object sender, EventArgs e)
        {
            subTabelEditionFilterDone_donePnl.PanelColor = Color.FromArgb(25, 55, 255); //1
            subTabelEditionFilterDone_donePnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
            await Task.Delay(20);
            subTabelEditionFilterDone_donePnl.PanelColor = Color.FromArgb(26, 54, 244); //2
            subTabelEditionFilterDone_donePnl.PanelOutlineColor = Color.FromArgb(26, 54, 244);
            await Task.Delay(20);
            subTabelEditionFilterDone_donePnl.PanelColor = Color.FromArgb(26, 52, 232); //3
            subTabelEditionFilterDone_donePnl.PanelOutlineColor = Color.FromArgb(26, 52, 232);
        }
        private async void subTabelEditionFilterDone_donePnl_MouseLeave(object sender, EventArgs e)
        {
            subTabelEditionFilterDone_donePnl.PanelColor = Color.FromArgb(26, 52, 232); //3
            subTabelEditionFilterDone_donePnl.PanelOutlineColor = Color.FromArgb(26, 52, 232);
            await Task.Delay(20);
            subTabelEditionFilterDone_donePnl.PanelColor = Color.FromArgb(26, 54, 244); //2
            subTabelEditionFilterDone_donePnl.PanelOutlineColor = Color.FromArgb(26, 54, 244);
            await Task.Delay(20);
            subTabelEditionFilterDone_donePnl.PanelColor = Color.FromArgb(25, 55, 255); //1
            subTabelEditionFilterDone_donePnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
        }

        // Кнопка сбросить фильтра изданий, во вкладке [Оформление подписок]
        private async void subTabelEditionFilterReset_Pnl_Click(object sender, EventArgs e)
        {
            subTabelEditionFilterTerm_termSdr.Value = 0;

            // Включаем все чекбоксы обратно
            foreach (var cb in subTabelEditionFilterEdit_Pnl.Controls.OfType<cuiCheckbox>())
                cb.Checked = true;

            if (_currentTable == TableType.Editions)
            {
                subTabel_Dgw.Rows.Clear();
                _loaderEditions.Reset();
                await LoadDataAsync(TableType.Editions);
            }
        }

        private async void subTabelEditionFilterReset_Pnl_MouseEnter(object sender, EventArgs e)
        {
            subTabelEditionFilterReset_Pnl.PanelColor = Color.FromArgb(242, 243, 250); //1
            subTabelEditionFilterReset_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
            subTabelEditionFilterReset_resetPic.Image = Properties.Resources.Сброс1;
            await Task.Delay(20);
            subTabelEditionFilterReset_Pnl.PanelColor = Color.FromArgb(240, 195, 204); //2
            subTabelEditionFilterReset_Pnl.PanelOutlineColor = Color.FromArgb(240, 195, 204);
            subTabelEditionFilterReset_resetPic.Image = Properties.Resources.Сброс2;
            await Task.Delay(20);
            subTabelEditionFilterReset_Pnl.PanelColor = Color.FromArgb(237, 147, 158); //3
            subTabelEditionFilterReset_Pnl.PanelOutlineColor = Color.FromArgb(237, 147, 158);
            subTabelEditionFilterReset_resetPic.Image = Properties.Resources.Сброс3;
            await Task.Delay(20);
            subTabelEditionFilterReset_Pnl.PanelColor = Color.FromArgb(235, 98, 111);  //4
            subTabelEditionFilterReset_Pnl.PanelOutlineColor = Color.FromArgb(235, 98, 111);
            subTabelEditionFilterReset_resetPic.Image = Properties.Resources.Сброс4;
            await Task.Delay(20);
            subTabelEditionFilterReset_Pnl.PanelColor = Color.FromArgb(232, 50, 65);  //5
            subTabelEditionFilterReset_Pnl.PanelOutlineColor = Color.FromArgb(232, 50, 65);
            subTabelEditionFilterReset_resetPic.Image = Properties.Resources.Сброс5;
        }
        private async void subTabelEditionFilterReset_Pnl_MouseLeave(object sender, EventArgs e)
        {
            subTabelEditionFilterReset_Pnl.PanelColor = Color.FromArgb(232, 50, 65);  //5
            subTabelEditionFilterReset_Pnl.PanelOutlineColor = Color.FromArgb(232, 50, 65);
            subTabelEditionFilterReset_resetPic.Image = Properties.Resources.Сброс5;
            await Task.Delay(20);
            subTabelEditionFilterReset_Pnl.PanelColor = Color.FromArgb(235, 98, 111);  //4
            subTabelEditionFilterReset_Pnl.PanelOutlineColor = Color.FromArgb(235, 98, 111);
            subTabelEditionFilterReset_resetPic.Image = Properties.Resources.Сброс4;
            await Task.Delay(20);
            subTabelEditionFilterReset_Pnl.PanelColor = Color.FromArgb(237, 147, 158); //3
            subTabelEditionFilterReset_Pnl.PanelOutlineColor = Color.FromArgb(237, 147, 158);
            subTabelEditionFilterReset_resetPic.Image = Properties.Resources.Сброс3;
            await Task.Delay(20);
            subTabelEditionFilterReset_Pnl.PanelColor = Color.FromArgb(240, 195, 204); //2
            subTabelEditionFilterReset_Pnl.PanelOutlineColor = Color.FromArgb(240, 195, 204);
            subTabelEditionFilterReset_resetPic.Image = Properties.Resources.Сброс2;
            await Task.Delay(20);
            subTabelEditionFilterReset_Pnl.PanelColor = Color.FromArgb(242, 243, 250); //1
            subTabelEditionFilterReset_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
            subTabelEditionFilterReset_resetPic.Image = Properties.Resources.Сброс1;
        }
        // subTabelSubscriptionFilter_Pnl Панель фильтра подписок, во вкладке [Оформление подписок]
        // Таймер с функции выезда панели фильтра подписок, во вкладке [Оформление подписок]
        private void subTabelFilter2_animationTmr_Tick(object sender, EventArgs e)
        {
            int target = subTabelFilter2_animation ? subTabelFilter2_down : subTabelFilter2_up;
            int distance = target - subTabelSubscription_Pnl.Top;
            int step = (int)(distance * 0.1f);
            if (Math.Abs(distance) < 1)
            {
                subTabelSubscription_Pnl.Top = target;
                subTabelFilter2_animationTmr.Stop();
                return;
            }
            subTabelSubscription_Pnl.Top += step;
        }
        // Ползунок срока месяца в фильтра подписок, во вкладке [Оформление подписок]
        private void subTabelSubscriptionFilterTerm_termSdr_ValueChanged(object sender, EventArgs e)
        {
            subTabelSubscriptionFilterTerm_monthLbl.Content = subTabelSubscriptionFilterTerm_termSdr.Value.ToString() + " мес.";
        }

        // Кнопка применить, фильтра подписок, во вкладке [Оформление подписок]
        private async void subTabelSubscriptionFilterDone_donePnl_Click(object sender, EventArgs e)
        {
            if (_currentTable != TableType.Subscriptions) return;

            try
            {
                DateTime from = (DateTime)subTabelSubscriptionFilterDate_fromCdp.Content;
                DateTime to = (DateTime)subTabelSubscriptionFilterDate_toCdp.Content;
                int minTermCount = (int)subTabelSubscriptionFilterTerm_termSdr.Value;

                var all = await DataBase._client.From<DataBase.Subscriptions>().Get();

                var filtered = all.Models.Where(s =>
                {
                    // Фильтр по дате
                    if (s.DateRegistred < from || s.DateRegistred > to) return false;

                    // Фильтр по сроку (количество '1' в строке)
                    if (minTermCount > 0)
                    {
                        int count = string.IsNullOrEmpty(s.TermSubscription)
                            ? 0
                            : s.TermSubscription.Count(c => c == '1');
                        if (count < minTermCount) return false;
                    }

                    return true;
                }).ToList();

                subTabel_Dgw.Rows.Clear();
                foreach (var item in filtered)
                    DataTables.AddSubscriptionRow(subTabel_Dgw, item);
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка фильтрации подписок", ex);
            }
        }

        private async void subTabelSubscriptionFilterDone_donePnl_MouseEnter(object sender, EventArgs e)
        {
            subTabelSubscriptionFilterDone_donePnl.PanelColor = Color.FromArgb(25, 55, 255); //1
            subTabelSubscriptionFilterDone_donePnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
            await Task.Delay(20);
            subTabelSubscriptionFilterDone_donePnl.PanelColor = Color.FromArgb(26, 54, 244); //2
            subTabelSubscriptionFilterDone_donePnl.PanelOutlineColor = Color.FromArgb(26, 54, 244);
            await Task.Delay(20);
            subTabelSubscriptionFilterDone_donePnl.PanelColor = Color.FromArgb(26, 52, 232); //3
            subTabelSubscriptionFilterDone_donePnl.PanelOutlineColor = Color.FromArgb(26, 52, 232);
        }

        private async void subTabelSubscriptionFilterDone_donePnl_MouseLeave(object sender, EventArgs e)
        {
            subTabelSubscriptionFilterDone_donePnl.PanelColor = Color.FromArgb(26, 52, 232); //3
            subTabelSubscriptionFilterDone_donePnl.PanelOutlineColor = Color.FromArgb(26, 52, 232);
            await Task.Delay(20);
            subTabelSubscriptionFilterDone_donePnl.PanelColor = Color.FromArgb(26, 54, 244); //2
            subTabelSubscriptionFilterDone_donePnl.PanelOutlineColor = Color.FromArgb(26, 54, 244);
            await Task.Delay(20);
            subTabelSubscriptionFilterDone_donePnl.PanelColor = Color.FromArgb(25, 55, 255); //1
            subTabelSubscriptionFilterDone_donePnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
        }

        //Кнопка сбросить фильтра подписок, во вкладке [Оформление подписок]
        private async void subTabelSubscriptionFilterReset_Pnl_Click(object sender, EventArgs e)
        {
            subTabelSubscriptionFilterTerm_termSdr.Value = 0;
            await LoadSubscriptionDateRangeAndReset();

            if (_currentTable == TableType.Subscriptions)
            {
                subTabel_Dgw.Rows.Clear();
                _loaderSubs.Reset();
                await LoadDataAsync(TableType.Subscriptions);
            }
        }

        private async Task LoadSubscriptionDateRangeAndReset()
        {
            try
            {
                var subs = await DataBase._client.From<DataBase.Subscriptions>().Get();
                if (subs.Models.Count == 0) return;
                subTabelSubscriptionFilterDate_fromCdp.Content = subs.Models.Min(s => s.DateRegistred);
                subTabelSubscriptionFilterDate_toCdp.Content = subs.Models.Max(s => s.DateRegistred);
            }
            catch { }
        }

        private async void subTabelSubscriptionFilterReset_Pnl_MouseEnter(object sender, EventArgs e)
        {
            subTabelSubscriptionFilterReset_Pnl.PanelColor = Color.FromArgb(242, 243, 250); //1
            subTabelSubscriptionFilterReset_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
            subTabelSubscriptionFilterReset_resetPic.Image = Properties.Resources.Сброс1;
            await Task.Delay(20);
            subTabelSubscriptionFilterReset_Pnl.PanelColor = Color.FromArgb(240, 195, 204); //2
            subTabelSubscriptionFilterReset_Pnl.PanelOutlineColor = Color.FromArgb(240, 195, 204);
            subTabelSubscriptionFilterReset_resetPic.Image = Properties.Resources.Сброс2;
            await Task.Delay(20);
            subTabelSubscriptionFilterReset_Pnl.PanelColor = Color.FromArgb(237, 147, 158); //3
            subTabelSubscriptionFilterReset_Pnl.PanelOutlineColor = Color.FromArgb(237, 147, 158);
            subTabelSubscriptionFilterReset_resetPic.Image = Properties.Resources.Сброс3;
            await Task.Delay(20);
            subTabelSubscriptionFilterReset_Pnl.PanelColor = Color.FromArgb(235, 98, 111);  //4
            subTabelSubscriptionFilterReset_Pnl.PanelOutlineColor = Color.FromArgb(235, 98, 111);
            subTabelSubscriptionFilterReset_resetPic.Image = Properties.Resources.Сброс4;
            await Task.Delay(20);
            subTabelSubscriptionFilterReset_Pnl.PanelColor = Color.FromArgb(232, 50, 65);  //5
            subTabelSubscriptionFilterReset_Pnl.PanelOutlineColor = Color.FromArgb(232, 50, 65);
            subTabelSubscriptionFilterReset_resetPic.Image = Properties.Resources.Сброс5;
        }
        private async void subTabelSubscriptionFilterReset_Pnl_MouseLeave(object sender, EventArgs e)
        {
            subTabelSubscriptionFilterReset_Pnl.PanelColor = Color.FromArgb(232, 50, 65);  //5
            subTabelSubscriptionFilterReset_Pnl.PanelOutlineColor = Color.FromArgb(232, 50, 65);
            subTabelSubscriptionFilterReset_resetPic.Image = Properties.Resources.Сброс5;
            await Task.Delay(20);
            subTabelSubscriptionFilterReset_Pnl.PanelColor = Color.FromArgb(235, 98, 111);  //4
            subTabelSubscriptionFilterReset_Pnl.PanelOutlineColor = Color.FromArgb(235, 98, 111);
            subTabelSubscriptionFilterReset_resetPic.Image = Properties.Resources.Сброс4;
            await Task.Delay(20);
            subTabelSubscriptionFilterReset_Pnl.PanelColor = Color.FromArgb(237, 147, 158); //3
            subTabelSubscriptionFilterReset_Pnl.PanelOutlineColor = Color.FromArgb(237, 147, 158);
            subTabelSubscriptionFilterReset_resetPic.Image = Properties.Resources.Сброс3;
            await Task.Delay(20);
            subTabelSubscriptionFilterReset_Pnl.PanelColor = Color.FromArgb(240, 195, 204); //2
            subTabelSubscriptionFilterReset_Pnl.PanelOutlineColor = Color.FromArgb(240, 195, 204);
            subTabelSubscriptionFilterReset_resetPic.Image = Properties.Resources.Сброс2;
            await Task.Delay(20);
            subTabelSubscriptionFilterReset_Pnl.PanelColor = Color.FromArgb(242, 243, 250); //1
            subTabelSubscriptionFilterReset_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
            subTabelSubscriptionFilterReset_resetPic.Image = Properties.Resources.Сброс1;
        }
        // subTop_Pnl Верхняя левая панель меню, во вкладке [Оформление подписок]
        // Кнопка задания, во вкладке [Оформление подписок]
        private void subTopTask_Pnl_Click(object sender, EventArgs e)
        {
            sub_Pnl.Visible = false;
            reader_Pnl.Visible = false;
            task_Pnl.Visible = true;
            this.SuspendLayout();
            this.ResumeLayout();
        }
        private async void subTopTask_Pnl_MouseEnter(object sender, EventArgs e)
        {
            subTopTask_taskPic.Image = Properties.Resources.Задание1;
            await Task.Delay(20);
            subTopTask_taskPic.Image = Properties.Resources.Задание2;
            await Task.Delay(20);
            subTopTask_taskPic.Image = Properties.Resources.Задание3;
            await Task.Delay(20);
            subTopTask_taskPic.Image = Properties.Resources.Задание4;
            await Task.Delay(20);
            subTopTask_taskPic.Image = Properties.Resources.Задание5;
        }
        private async void subTopTask_Pnl_MouseLeave(object sender, EventArgs e)
        {
            subTopTask_taskPic.Image = Properties.Resources.Задание5;
            await Task.Delay(20);
            subTopTask_taskPic.Image = Properties.Resources.Задание4;
            await Task.Delay(20);
            subTopTask_taskPic.Image = Properties.Resources.Задание3;
            await Task.Delay(20);
            subTopTask_taskPic.Image = Properties.Resources.Задание2;
            await Task.Delay(20);
            subTopTask_taskPic.Image = Properties.Resources.Задание1;
        }
        // Кнопка оформление подписок, во вкладке [Оформление подписок]
        private void subTopSub_Pnl_Click(object sender, EventArgs e)
        {
            sub_Pnl.Visible = true;
            reader_Pnl.Visible = false;
            task_Pnl.Visible = false;
            this.SuspendLayout();
            this.ResumeLayout();
        }
        private async void subTopSub_Pnl_MouseEnter(object sender, EventArgs e)
        {
            subTopSub_subPic.Image = Properties.Resources.Подписки1;
            await Task.Delay(20);
            subTopSub_subPic.Image = Properties.Resources.Подписки2;
            await Task.Delay(20);
            subTopSub_subPic.Image = Properties.Resources.Подписки3;
            await Task.Delay(20);
            subTopSub_subPic.Image = Properties.Resources.Подписки4;
            await Task.Delay(20);
            subTopSub_subPic.Image = Properties.Resources.Подписки5;
        }
        private async void subTopSub_Pnl_MouseLeave(object sender, EventArgs e)
        {
            subTopSub_subPic.Image = Properties.Resources.Подписки5;
            await Task.Delay(20);
            subTopSub_subPic.Image = Properties.Resources.Подписки4;
            await Task.Delay(20);
            subTopSub_subPic.Image = Properties.Resources.Подписки3;
            await Task.Delay(20);
            subTopSub_subPic.Image = Properties.Resources.Подписки2;
            await Task.Delay(20);
            subTopSub_subPic.Image = Properties.Resources.Подписки1;
        }
        // Кнопка регистрация читателей, во вкладке [Оформление подписок]
        private void subTopReg_Pnl_Click(object sender, EventArgs e)
        {
            sub_Pnl.Visible = false;
            reader_Pnl.Visible = true;
            task_Pnl.Visible = false;
            this.SuspendLayout();
            this.ResumeLayout();
        }
        private async void subTopReg_Pnl_MouseEnter(object sender, EventArgs e)
        {
            subTopReg_regPic.Image = Properties.Resources.Регистрация1;
            await Task.Delay(20);
            subTopReg_regPic.Image = Properties.Resources.Регистрация2;
            await Task.Delay(20);
            subTopReg_regPic.Image = Properties.Resources.Регистрация3;
            await Task.Delay(20);
            subTopReg_regPic.Image = Properties.Resources.Регистрация4;
            await Task.Delay(20);
            subTopReg_regPic.Image = Properties.Resources.Регистрация5;
        }
        private async void subTopReg_Pnl_MouseLeave(object sender, EventArgs e)
        {
            subTopReg_regPic.Image = Properties.Resources.Регистрация5;
            await Task.Delay(20);
            subTopReg_regPic.Image = Properties.Resources.Регистрация4;
            await Task.Delay(20);
            subTopReg_regPic.Image = Properties.Resources.Регистрация3;
            await Task.Delay(20);
            subTopReg_regPic.Image = Properties.Resources.Регистрация2;
            await Task.Delay(20);
            subTopReg_regPic.Image = Properties.Resources.Регистрация1;
        }
        // subBottom_Pnl Нижняя левая панель меню, во вкладке [Оформление подписок]
        // Кнопка настройки, во вкладке [Оформление подписок]
        private void subBottomSettings_Pnl_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Настройки");
        }
        private async void subBottomSettings_Pnl_MouseEnter(object sender, EventArgs e)
        {
            subBottomSettings_settingsPic.Image = Properties.Resources.Настройки1;
            await Task.Delay(20);
            subBottomSettings_settingsPic.Image = Properties.Resources.Настройки2;
            await Task.Delay(20);
            subBottomSettings_settingsPic.Image = Properties.Resources.Настройки3;
            await Task.Delay(20);
            subBottomSettings_settingsPic.Image = Properties.Resources.Настройки4;
            await Task.Delay(20);
            subBottomSettings_settingsPic.Image = Properties.Resources.Настройки5;
        }
        private async void subBottomSettings_Pnl_MouseLeave(object sender, EventArgs e)
        {
            subBottomSettings_settingsPic.Image = Properties.Resources.Настройки5;
            await Task.Delay(20);
            subBottomSettings_settingsPic.Image = Properties.Resources.Настройки4;
            await Task.Delay(20);
            subBottomSettings_settingsPic.Image = Properties.Resources.Настройки3;
            await Task.Delay(20);
            subBottomSettings_settingsPic.Image = Properties.Resources.Настройки2;
            await Task.Delay(20);
            subBottomSettings_settingsPic.Image = Properties.Resources.Настройки1;
        }
        // Кнопка выхода, во вкладке [Оформление подписок]
        private async void subBottomExit_Pnl_Click(object sender, EventArgs e)
        {
            await UserData.LogoutAndExit(this);
        }
        private async void subBottomExit_Pnl_MouseEnter(object sender, EventArgs e)
        {
            subBottomExit_exitPic.Image = Properties.Resources.Выход1;
            await Task.Delay(20);
            subBottomExit_exitPic.Image = Properties.Resources.Выход2;
            await Task.Delay(20);
            subBottomExit_exitPic.Image = Properties.Resources.Выход3;
            await Task.Delay(20);
            subBottomExit_exitPic.Image = Properties.Resources.Выход4;
            await Task.Delay(20);
            subBottomExit_exitPic.Image = Properties.Resources.Выход5;
        }
        private async void subBottomExit_Pnl_MouseLeave(object sender, EventArgs e)
        {
            subBottomExit_exitPic.Image = Properties.Resources.Выход5;
            await Task.Delay(20);
            subBottomExit_exitPic.Image = Properties.Resources.Выход4;
            await Task.Delay(20);
            subBottomExit_exitPic.Image = Properties.Resources.Выход3;
            await Task.Delay(20);
            subBottomExit_exitPic.Image = Properties.Resources.Выход2;
            await Task.Delay(20);
            subBottomExit_exitPic.Image = Properties.Resources.Выход1;
        }
        // Данные читателей, во вкладке [Регистрация читателей]
        private void AddSampleData4()
        {
            readerTabel_Dgw.Columns.Clear();
            readerTabel_Dgw.Rows.Clear();
            readerTabel_Dgw.Columns.Add("LastName", "Фамилия");
            readerTabel_Dgw.Columns.Add("FirstName", "Имя");
            readerTabel_Dgw.Columns.Add("MiddleName", "Отчество");
            readerTabel_Dgw.Columns.Add("Street", "Улица");
            readerTabel_Dgw.Columns.Add("HouseNumber", "Номер дома");
            readerTabel_Dgw.Columns.Add("ApartmentNumber", "Номер квартиры");

            readerTabel_Dgw.Rows.Add("Иванов", "Иван", "Иванович", "Ленина", "12", "45");
            readerTabel_Dgw.Rows.Add("Петрова", "Мария", "Сергеевна", "Советская", "7", "18");
            readerTabel_Dgw.Rows.Add("Сидоров", "Алексей", "Петрович", "Гагарина", "24", "7");
            readerTabel_Dgw.Rows.Add("Кузнецова", "Елена", "Андреевна", "Пушкина", "5", "32");
            readerTabel_Dgw.Rows.Add("Смирнов", "Дмитрий", "Николаевич", "Лермонтова", "18", "9");
            readerTabel_Dgw.Rows.Add("Васильева", "Анна", "Владимировна", "Садовый переулок", "3", "56");
            readerTabel_Dgw.Rows.Add("Попов", "Сергей", "Михайлович", "Комсомольская", "42", "8");
            readerTabel_Dgw.Rows.Add("Новикова", "Татьяна", "Павловна", "Мира", "11", "123");
            readerTabel_Dgw.Rows.Add("Морозов", "Андрей", "Викторович", "Строителей", "8", "67");
            readerTabel_Dgw.Rows.Add("Волкова", "Ольга", "Дмитриевна", "Кирова", "21", "4");
            readerTabel_Dgw.Rows.Add("Зайцев", "Максим", "Александрович", "Октябрьская", "33", "88");
            readerTabel_Dgw.Rows.Add("Соколова", "Ирина", "Егоровна", "Парковая", "6", "15");
            readerTabel_Dgw.Rows.Add("Лебедев", "Павел", "Романович", "Чапаева", "47", "3");
            readerTabel_Dgw.Rows.Add("Козлова", "Наталья", "Аркадьевна", "Луговая", "9", "71");
            readerTabel_Dgw.Rows.Add("Орлов", "Владимир", "Семёнович", "Юбилейная", "15", "24");
        }
        // readerRegistration_Pnl панель с регистрацией читателей, во вкладке [Регистрация читателей]
        // Комбо бокс панеле с регистрацией, во вкладке [Регистрация читателей]
        private void readerRegistrationStreet_streetTxt_Click(object sender, EventArgs e)
        {
            readerRegistrationSearch1_animationTmr.Start();
        }
        // Таймер открытия панели комбо бокса, во вкладке [Регистрация читателей]
        private void readerRegistrationSearch1_animationTmr_Tick(object sender, EventArgs e)
        {
            int step;
            int diff;
            if (readerRegistrationSearch1_animtaion)
            {
                diff = readerRegistrationSearch_Pnl.Height - readerRegistrationSearch_Pnl.MinimumSize.Height;
                step = Math.Max(2, diff / 5);
                readerRegistrationSearch_Pnl.Height -= step;
                if (readerRegistrationSearch_Pnl.Height <= readerRegistrationSearch_Pnl.MinimumSize.Height)
                {
                    readerRegistrationSearch_Pnl.Height = readerRegistrationSearch_Pnl.MinimumSize.Height;
                    readerRegistrationSearch1_animtaion = false;
                    readerRegistrationSearch1_animationTmr.Stop();
                }
            }
            else
            {
                diff = readerRegistrationSearch_Pnl.MaximumSize.Height - readerRegistrationSearch_Pnl.Height;
                step = Math.Max(2, diff / 5);
                readerRegistrationSearch_Pnl.Height += step;
                if (readerRegistrationSearch_Pnl.Height >= readerRegistrationSearch_Pnl.MaximumSize.Height)
                {
                    readerRegistrationSearch_Pnl.Height = readerRegistrationSearch_Pnl.MaximumSize.Height;
                    readerRegistrationSearch1_animtaion = true;
                    readerRegistrationSearch1_animationTmr.Stop();
                }
            }
        }
        // Кнопка зарегистрировать в панеле с регистрацией, во вкладке [Регистрация читателей]
        private async void readerRegistrationDone_Pnl_Click(object sender, EventArgs e)
        {
            if (readerRegistrationSurname_surnameTxt.Content == string.Empty && cuiTextBox2.Content == string.Empty)
            {
                Logger.ShowWarning("Введите хотя бы Имя и Фамилию");
                return;
            }

            if (_selectedMarker == null)
            {
                Logger.ShowWarning("Выберите улицу");
                return;
            }

            if (_selectedReader == null)
            {
                try
                {
                    Guid id = Guid.NewGuid();
                    var newReader = new DataBase.Readers
                    {
                        Id = id,
                        FIO = $"{readerRegistrationSurname_surnameTxt.Content} {cuiTextBox2.Content} {readerRegistrationPatronymic_patronymicTxt.Content}",
                        IdActiveSubscriptions = string.Empty,
                    };

                    string readerIds = string.IsNullOrEmpty(_selectedMarker.IdReaders)
                        ? id.ToString()
                        : $"{_selectedMarker.IdReaders},{id}";

                    await DataBase._client.From<DataBase.Readers>().Upsert(newReader);
                    await DataBase._client.From<DataBase.Markers>().Where(x => x.Id == _selectedMarker.Id).Set(x => x.IdReaders, readerIds).Update();

                    // Обновляем кэш маркеров
                    var cached = _cachedMarkers.FirstOrDefault(m => m.Id == _selectedMarker.Id);
                    if (cached != null) cached.IdReaders = readerIds;

                    DataTables.AddReaderTableRow(readerTabel_Dgw, newReader, _selectedMarker);
                    _locallyAddedReaderIds.Add(newReader.Id);

                    _selectedMarker = null;
                    readerRegistrationSurname_surnameTxt.Content = string.Empty;
                    cuiTextBox2.Content = string.Empty;
                    readerRegistrationPatronymic_patronymicTxt.Content = string.Empty;
                    readerRegistrationReset_Pnl_Click(sender, e);

                    Logger.Info($"Читатель {newReader.FIO} успешно зарегистрирован");
                    Logger.ShowInfo("Читатель успешно зарегистрирован");
                }
                catch (Exception ex)
                {
                    Logger.Error("Ошибка при регистрации читателя", ex);
                    Logger.ShowError("Ошибка при регистрации читателя");
                }
            }
            else
            {
                try
                {
                    Guid id = _selectedReader.Id;
                    string idStr = id.ToString();

                    // Получаем старый маркер из строки таблицы
                    DataGridViewRow tableRow = null;
                    foreach (DataGridViewRow r in readerTabel_Dgw.Rows)
                    {
                        if (r.Cells["Id"].Value != null && r.Cells["Id"].Value.ToString() == idStr)
                        {
                            tableRow = r;
                            break;
                        }
                    }

                    // Убираем читателя из старого маркера если маркер сменился
                    if (tableRow != null && tableRow.Cells["IdMarker"].Value != null &&
                        !string.IsNullOrEmpty(tableRow.Cells["IdMarker"].Value.ToString()))
                    {
                        Guid oldMarkerId = Guid.Parse(tableRow.Cells["IdMarker"].Value.ToString());
                        if (oldMarkerId != _selectedMarker.Id)
                        {
                            var oldMarker = await DataBase._client.From<DataBase.Markers>().Where(x => x.Id == oldMarkerId).Single();
                            if (oldMarker != null)
                            {
                                var ids = oldMarker.IdReaders
                                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(s => s.Trim())
                                    .Where(s => s != idStr)
                                    .ToArray();
                                string updatedOldIds = string.Join(",", ids);
                                await DataBase._client.From<DataBase.Markers>().Where(x => x.Id == oldMarkerId).Set(x => x.IdReaders, updatedOldIds).Update();

                                var cachedOld = _cachedMarkers.FirstOrDefault(m => m.Id == oldMarkerId);
                                if (cachedOld != null) cachedOld.IdReaders = updatedOldIds;
                            }
                        }
                    }

                    var updatedReader = new DataBase.Readers
                    {
                        Id = id,
                        FIO = $"{readerRegistrationSurname_surnameTxt.Content} {cuiTextBox2.Content} {readerRegistrationPatronymic_patronymicTxt.Content}",
                        IdActiveSubscriptions = _selectedReader.IdActiveSubscriptions
                    };

                    string newReaderIds = string.IsNullOrEmpty(_selectedMarker.IdReaders) || !_selectedMarker.IdReaders.Contains(idStr)
                        ? (string.IsNullOrEmpty(_selectedMarker.IdReaders) ? idStr : $"{_selectedMarker.IdReaders},{idStr}")
                        : _selectedMarker.IdReaders;

                    await DataBase._client.From<DataBase.Readers>().Upsert(updatedReader);
                    await DataBase._client.From<DataBase.Markers>().Where(x => x.Id == _selectedMarker.Id).Set(x => x.IdReaders, newReaderIds).Update();

                    var cachedNew = _cachedMarkers.FirstOrDefault(m => m.Id == _selectedMarker.Id);
                    if (cachedNew != null) cachedNew.IdReaders = newReaderIds;

                    // Обновляем строку в таблице на месте
                    if (tableRow != null)
                    {
                        tableRow.Cells["FIO"].Value = updatedReader.FIO;
                        tableRow.Cells["IdMarker"].Value = _selectedMarker.Id;

                        string address;
                        if (!string.IsNullOrWhiteSpace(_selectedMarker.House))
                            address = $"{_selectedMarker.Street} {_selectedMarker.House}".Trim();
                        else
                        {
                            string extra = "";
                            if (!string.IsNullOrWhiteSpace(_selectedMarker.Building)) extra += $"к{_selectedMarker.Building}";
                            if (!string.IsNullOrWhiteSpace(_selectedMarker.Apartment)) extra += $"/{_selectedMarker.Apartment}";
                            address = $"{_selectedMarker.Street} {extra}".Trim();
                        }
                        tableRow.Cells["Address"].Value = address;
                    }

                    Logger.Info($"Читатель {updatedReader.FIO} успешно изменён");
                    Logger.ShowInfo("Читатель успешно изменён");
                }
                catch (Exception ex)
                {
                    Logger.Error("Ошибка при изменении читателя", ex);
                    Logger.ShowError("Ошибка при изменении читателя");
                }
            }
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
        // Кнопка закрыть в панеле с регистрацией, во вкладке [Регистрация читателей]
        private void readerRegistrationReset_Pnl_Click(object sender, EventArgs e)
        {
            readerRegistrationSurname_surnameTxt.Content = string.Empty;
            cuiTextBox2.Content = string.Empty;
            readerRegistrationPatronymic_patronymicTxt.Content = string.Empty;
            readerRegistrationStreet_streetTxt.Content = string.Empty;
            _selectedMarker = null;
            _selectedReader = null;
            readerRegistration_fioLbl.Content = string.Empty;
            readerRegistration_streetLbl.Content = string.Empty;
            readerRegistrationDone_stLbl.Content = "Зарегистрировать";
        }
        private async void readerRegistrationReset_Pnl_MouseEnter(object sender, EventArgs e)
        {
            readerRegistrationReset_Pnl.PanelColor = Color.FromArgb(242, 243, 250); //1
            readerRegistrationReset_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
            await Task.Delay(20);
            readerRegistrationReset_Pnl.PanelColor = Color.FromArgb(240, 240, 248); //2
            readerRegistrationReset_Pnl.PanelOutlineColor = Color.FromArgb(240, 240, 248);
            await Task.Delay(20);
            readerRegistrationReset_Pnl.PanelColor = Color.FromArgb(237, 238, 246); //3
            readerRegistrationReset_Pnl.PanelOutlineColor = Color.FromArgb(237, 238, 246);
            await Task.Delay(20);
            readerRegistrationReset_Pnl.PanelColor = Color.FromArgb(235, 235, 244);  //4
            readerRegistrationReset_Pnl.PanelOutlineColor = Color.FromArgb(235, 235, 244);
        }
        private async void readerRegistrationReset_Pnl_MouseLeave(object sender, EventArgs e)
        {
            readerRegistrationReset_Pnl.PanelColor = Color.FromArgb(235, 235, 244);  //4
            readerRegistrationReset_Pnl.PanelOutlineColor = Color.FromArgb(235, 235, 244);
            await Task.Delay(20);
            readerRegistrationReset_Pnl.PanelColor = Color.FromArgb(237, 238, 246); //3
            readerRegistrationReset_Pnl.PanelOutlineColor = Color.FromArgb(237, 238, 246);
            await Task.Delay(20);
            readerRegistrationReset_Pnl.PanelColor = Color.FromArgb(240, 240, 248); //2
            readerRegistrationReset_Pnl.PanelOutlineColor = Color.FromArgb(240, 240, 248);
            await Task.Delay(20);
            readerRegistrationReset_Pnl.PanelColor = Color.FromArgb(242, 243, 250); //1
            readerRegistrationReset_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
        }
        // readerTop_Pnl Верхняя панель меню, во вкладке [Регистрация читателей]
        // Кнопка задания, во вкладке [Регистрация читателей]
        private void readerTopTask_Pnl_Click(object sender, EventArgs e)
        {
            sub_Pnl.Visible = false;
            reader_Pnl.Visible = false;
            task_Pnl.Visible = true;
            this.SuspendLayout();
            this.ResumeLayout();
        }
        private async void readerTopTask_Pnl_MouseEnter(object sender, EventArgs e)
        {
            readerTopTask_taskPic.Image = Properties.Resources.Задание1;
            await Task.Delay(20);
            readerTopTask_taskPic.Image = Properties.Resources.Задание2;
            await Task.Delay(20);
            readerTopTask_taskPic.Image = Properties.Resources.Задание3;
            await Task.Delay(20);
            readerTopTask_taskPic.Image = Properties.Resources.Задание4;
            await Task.Delay(20);
            readerTopTask_taskPic.Image = Properties.Resources.Задание5;
        }
        private async void readerTopTask_Pnl_MouseLeave(object sender, EventArgs e)
        {
            readerTopTask_taskPic.Image = Properties.Resources.Задание5;
            await Task.Delay(20);
            readerTopTask_taskPic.Image = Properties.Resources.Задание4;
            await Task.Delay(20);
            readerTopTask_taskPic.Image = Properties.Resources.Задание3;
            await Task.Delay(20);
            readerTopTask_taskPic.Image = Properties.Resources.Задание2;
            await Task.Delay(20);
            readerTopTask_taskPic.Image = Properties.Resources.Задание1;
        }
        // Кнопка оформление подписки, во вкладке [Регистрация читателей]
        private void readerTopSub_Pnl_Click(object sender, EventArgs e)
        {
            sub_Pnl.Visible = true;
            reader_Pnl.Visible = false;
            task_Pnl.Visible = false;
            this.SuspendLayout();
            this.ResumeLayout();
        }
        private async void readerTopSub_Pnl_MouseEnter(object sender, EventArgs e)
        {
            readerTopSub_subPic.Image = Properties.Resources.Подписки1;
            await Task.Delay(20);
            readerTopSub_subPic.Image = Properties.Resources.Подписки2;
            await Task.Delay(20);
            readerTopSub_subPic.Image = Properties.Resources.Подписки3;
            await Task.Delay(20);
            readerTopSub_subPic.Image = Properties.Resources.Подписки4;
            await Task.Delay(20);
            readerTopSub_subPic.Image = Properties.Resources.Подписки5;
        }
        private async void readerTopSub_Pnl_MouseLeave(object sender, EventArgs e)
        {
            readerTopSub_subPic.Image = Properties.Resources.Подписки5;
            await Task.Delay(20);
            readerTopSub_subPic.Image = Properties.Resources.Подписки4;
            await Task.Delay(20);
            readerTopSub_subPic.Image = Properties.Resources.Подписки3;
            await Task.Delay(20);
            readerTopSub_subPic.Image = Properties.Resources.Подписки2;
            await Task.Delay(20);
            readerTopSub_subPic.Image = Properties.Resources.Подписки1;
        }
        // Кнопка регистрация читателей, во вкладке [Регистрация читателей]
        private void readerTopReg_Pnl_Click(object sender, EventArgs e)
        {
            sub_Pnl.Visible = false;
            reader_Pnl.Visible = true;
            task_Pnl.Visible = false;
            this.SuspendLayout();
            this.ResumeLayout();
        }
        private async void readerTopReg_Pnl_MouseEnter(object sender, EventArgs e)
        {
            readerTopReg_regPic.Image = Properties.Resources.Регистрация1;
            await Task.Delay(20);
            readerTopReg_regPic.Image = Properties.Resources.Регистрация2;
            await Task.Delay(20);
            readerTopReg_regPic.Image = Properties.Resources.Регистрация3;
            await Task.Delay(20);
            readerTopReg_regPic.Image = Properties.Resources.Регистрация4;
            await Task.Delay(20);
            readerTopReg_regPic.Image = Properties.Resources.Регистрация5;
        }
        private async void readerTopReg_Pnl_MouseLeave(object sender, EventArgs e)
        {
            readerTopReg_regPic.Image = Properties.Resources.Регистрация5;
            await Task.Delay(20);
            readerTopReg_regPic.Image = Properties.Resources.Регистрация4;
            await Task.Delay(20);
            readerTopReg_regPic.Image = Properties.Resources.Регистрация3;
            await Task.Delay(20);
            readerTopReg_regPic.Image = Properties.Resources.Регистрация2;
            await Task.Delay(20);
            readerTopReg_regPic.Image = Properties.Resources.Регистрация1;
        }
        // readerBottom_Pnl Ниджняя панель меню, во вкладке [Регистрация читателей]
        // Кнопка настрйки, во вкладке [Регистрация читателей]
        private void readerBottomSettings_Pnl_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Настройки");
        }
        private async void readerBottomSettings_Pnl_MouseEnter(object sender, EventArgs e)
        {
            readerBottomSettings_settingsPic.Image = Properties.Resources.Настройки1;
            await Task.Delay(20);
            readerBottomSettings_settingsPic.Image = Properties.Resources.Настройки2;
            await Task.Delay(20);
            readerBottomSettings_settingsPic.Image = Properties.Resources.Настройки3;
            await Task.Delay(20);
            readerBottomSettings_settingsPic.Image = Properties.Resources.Настройки4;
            await Task.Delay(20);
            readerBottomSettings_settingsPic.Image = Properties.Resources.Настройки5;
        }
        private async void readerBottomSettings_Pnl_MouseLeave(object sender, EventArgs e)
        {
            readerBottomSettings_settingsPic.Image = Properties.Resources.Настройки5;
            await Task.Delay(20);
            readerBottomSettings_settingsPic.Image = Properties.Resources.Настройки4;
            await Task.Delay(20);
            readerBottomSettings_settingsPic.Image = Properties.Resources.Настройки3;
            await Task.Delay(20);
            readerBottomSettings_settingsPic.Image = Properties.Resources.Настройки2;
            await Task.Delay(20);
            readerBottomSettings_settingsPic.Image = Properties.Resources.Настройки1;
        }
        // Кнопка выхода, во вкладке [Регистрация читателей]
        private async void readerBottomExit_Pnl_Click(object sender, EventArgs e)
        {
            await UserData.LogoutAndExit(this);
        }
        private async void readerBottomExit_Pnl_MouseEnter(object sender, EventArgs e)
        {
            readerBottomExit_exitPic.Image = Properties.Resources.Выход1;
            await Task.Delay(20);
            readerBottomExit_exitPic.Image = Properties.Resources.Выход2;
            await Task.Delay(20);
            readerBottomExit_exitPic.Image = Properties.Resources.Выход3;
            await Task.Delay(20);
            readerBottomExit_exitPic.Image = Properties.Resources.Выход4;
            await Task.Delay(20);
            readerBottomExit_exitPic.Image = Properties.Resources.Выход5;
        }
        private async void readerBottomExit_Pnl_MouseLeave(object sender, EventArgs e)
        {
            readerBottomExit_exitPic.Image = Properties.Resources.Выход5;
            await Task.Delay(20);
            readerBottomExit_exitPic.Image = Properties.Resources.Выход4;
            await Task.Delay(20);
            readerBottomExit_exitPic.Image = Properties.Resources.Выход3;
            await Task.Delay(20);
            readerBottomExit_exitPic.Image = Properties.Resources.Выход2;
            await Task.Delay(20);
            readerBottomExit_exitPic.Image = Properties.Resources.Выход1;
        }
        //Загрузка таблицы для проверки. Удали когда нужно
        private void AddSampleData5()
        {
            taskTabel_Dgw.Columns.Add("Number", "№");
            taskTabel_Dgw.Columns.Add("FIO", "ФИО сотрудника");
            taskTabel_Dgw.Columns.Add("Status", "Статус");
            taskTabel_Dgw.Columns.Add("Tags", "Меток");
            taskTabel_Dgw.Columns.Add("DateIssue", "Дата выдачи");
            taskTabel_Dgw.Columns.Add("DateDelivery", "Дата сдачи");

            taskTabel_Dgw.Columns["Number"].Width = 60;
            taskTabel_Dgw.Columns["Number"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            taskTabel_Dgw.Columns["Number"].DefaultCellStyle.Font = new Font("Montserrat", 9F, FontStyle.Bold);

            taskTabel_Dgw.Rows.Add("№1", "Иванов Иван Петрович", "Готово", "231 штук", "21.05.26", "25.05.26");
            taskTabel_Dgw.Rows.Add("№2", "Иванов Иван Петрович", "Готово", "231 штук", "21.05.26", "25.05.26");
            taskTabel_Dgw.Rows.Add("№3", "Иванов Иван Петрович", "Готово", "231 штук", "21.05.26", "25.05.26");
            taskTabel_Dgw.Rows.Add("№4", "Иванов Иван Петрович", "Готово", "231 штук", "21.05.26", "25.05.26");
            taskTabel_Dgw.Rows.Add("№5", "Иванов Иван Петрович", "Готово", "231 штук", "21.05.26", "25.05.26");
            taskTabel_Dgw.Rows.Add("№6", "Иванов Иван Петрович", "Готово", "231 штук", "21.05.26", "25.05.26");
            taskTabel_Dgw.Rows.Add("№7", "Иванов Иван Петрович", "Готово", "231 штук", "21.05.26", "25.05.26");
            taskTabel_Dgw.Rows.Add("№8", "Иванов Иван Петрович", "Готово", "231 штук", "21.05.26", "25.05.26");
            taskTabel_Dgw.Rows.Add("№9", "Иванов Иван Петрович", "Готово", "231 штук", "21.05.26", "25.05.26");
            taskTabel_Dgw.Rows.Add("№10", "Иванов Иван Петрович", "Готово", "231 штук", "21.05.26", "25.05.26");

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
        private async void taskWatchsTransitionStart_Pnl_Click(object sender, EventArgs e)
        {
            TaskOnEmployee.ClickTasksButton(taskWatchsTransitionStart_Pnl, taskWatchsTransitionClose_startLbl, taskTabel_Dgw,
                taskWatchTransitionLayerRack_progressPth, taskWatch_nameLbl, taskWatch_deliveryLbl,
                taskWatch_endingLbl, taskWatchsTransitionReadertxt_readerTxt, taskWatch_tagsLbl);
        }
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
            taskUpperFilter = !taskUpperFilter;
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
        }
        //
        // Кнопка применить, во вкладке [Задание]
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

            if (taskUpperFilter)
                taskTabelUpperFilter_Pnl_Click(sender, e);

            await LoadDataAsync(TableType.Tasks);
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
        // Кнопка сбросить фильтра, во вкладке [Задание]
        private async void taskTabelFilterClose_Pnl_Click(object sender, EventArgs e)
        {
            taskTabelInsertFilterStatus_newCkb.Checked = true;
            taskTabelInsertFilterStatus_processCkb.Checked = true;
            taskTabelInsertFilterStatus_doneCkb.Checked = true;

            _excludedStatuses.Clear();
            taskTabel_Dgw.Rows.Clear();

            if (taskUpperFilter)
                taskTabelUpperFilter_Pnl_Click(sender, e);

            await LoadDataAsync(TableType.Tasks);
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
        //taskTop_Pnl Верхняя панель меню, во вкладке [Задания]
        //Кнопка карта в панели задания
        private void taskTopTask_Pnl_Click(object sender, EventArgs e)
        {
            this.SuspendLayout();
            task_Pnl.Visible = true;
            reader_Pnl.Visible = false;
            sub_Pnl.Visible = false;
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
        // Кнопка оформление подписки, во вкладке [Задание]
        private void taskTopSub_Pnl_Click(object sender, EventArgs e)
        {
            this.SuspendLayout();
            task_Pnl.Visible = false;
            reader_Pnl.Visible = false;
            sub_Pnl.Visible = true;
            this.ResumeLayout();
        }
        private async void taskTopSub_Pnl_MouseEnter(object sender, EventArgs e)
        {
            taskTopSub_subPic.Image = Properties.Resources.Подписки1;
            await Task.Delay(20);
            taskTopSub_subPic.Image = Properties.Resources.Подписки2;
            await Task.Delay(20);
            taskTopSub_subPic.Image = Properties.Resources.Подписки3;
            await Task.Delay(20);
            taskTopSub_subPic.Image = Properties.Resources.Подписки4;
            await Task.Delay(20);
            taskTopSub_subPic.Image = Properties.Resources.Подписки5;
        }
        private async void taskTopSub_Pnl_MouseLeave(object sender, EventArgs e)
        {
            taskTopSub_subPic.Image = Properties.Resources.Подписки5;
            await Task.Delay(20);
            taskTopSub_subPic.Image = Properties.Resources.Подписки4;
            await Task.Delay(20);
            taskTopSub_subPic.Image = Properties.Resources.Подписки3;
            await Task.Delay(20);
            taskTopSub_subPic.Image = Properties.Resources.Подписки2;
            await Task.Delay(20);
            taskTopSub_subPic.Image = Properties.Resources.Подписки1;
        }
        // Кнопка регистрации читателей, во вкладке [Задание]
        private void taskTopReg_Pnl_Click(object sender, EventArgs e)
        {
            this.SuspendLayout();
            task_Pnl.Visible = false;
            reader_Pnl.Visible = true;
            sub_Pnl.Visible = false;
            this.ResumeLayout();
        }
        private async void taskTopReg_Pnl_MouseEnter(object sender, EventArgs e)
        {
            taskTopReg_regPic.Image = Properties.Resources.Регистрация1;
            await Task.Delay(20);
            taskTopReg_regPic.Image = Properties.Resources.Регистрация2;
            await Task.Delay(20);
            taskTopReg_regPic.Image = Properties.Resources.Регистрация3;
            await Task.Delay(20);
            taskTopReg_regPic.Image = Properties.Resources.Регистрация4;
            await Task.Delay(20);
            taskTopReg_regPic.Image = Properties.Resources.Регистрация5;
        }
        private async void taskTopReg_Pnl_MouseLeave(object sender, EventArgs e)
        {
            taskTopReg_regPic.Image = Properties.Resources.Регистрация5;
            await Task.Delay(20);
            taskTopReg_regPic.Image = Properties.Resources.Регистрация4;
            await Task.Delay(20);
            taskTopReg_regPic.Image = Properties.Resources.Регистрация3;
            await Task.Delay(20);
            taskTopReg_regPic.Image = Properties.Resources.Регистрация2;
            await Task.Delay(20);
            taskTopReg_regPic.Image = Properties.Resources.Регистрация1;
        }
        //taskBottom_Pnl Нижняя панель меню, во вкладке [Задания]
        // Кнопка настройки, во вкладке [Задание]
        private void taskBottomSettings_Pnl_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Настройка мана");
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
        // Кнопка выйти, во вкладке [Задание]
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

        private void subCreatureGiveSearch_animationTmr_Tick(object sender, EventArgs e)
        {
            int step;
            int diff;
            if (subRegistrationSearch_animtaion)
            {
                diff = subCreatureGiveSearch_Pnl.Height - subCreatureGiveSearch_Pnl.MinimumSize.Height;
                step = Math.Max(2, diff / 5);
                subCreatureGiveSearch_Pnl.Height -= step;
                if (subCreatureGiveSearch_Pnl.Height <= subCreatureGiveSearch_Pnl.MinimumSize.Height)
                {
                    subCreatureGiveSearch_Pnl.Height = subCreatureGiveSearch_Pnl.MinimumSize.Height;
                    subRegistrationSearch_animtaion = false;
                    subCreatureGiveSearch_animationTmr.Stop();
                }
            }
            else
            {
                diff = subCreatureGiveSearch_Pnl.MaximumSize.Height - subCreatureGiveSearch_Pnl.Height;
                step = Math.Max(2, diff / 5);
                subCreatureGiveSearch_Pnl.Height += step;
                if (subCreatureGiveSearch_Pnl.Height >= subCreatureGiveSearch_Pnl.MaximumSize.Height)
                {
                    subCreatureGiveSearch_Pnl.Height = subCreatureGiveSearch_Pnl.MaximumSize.Height;
                    subRegistrationSearch_animtaion = true;
                    subCreatureGiveSearch_animationTmr.Stop();
                }
            }
        }

        private void subCreatureGiveStreettxt_streetTxt_Click(object sender, EventArgs e)
        {
            subCreatureGiveSearch_animationTmr.Start();
        }

        private async void OperatorForm_Load(object sender, EventArgs e)
        {
            InitializeTimer();
            SubscriptionEvents();

            DataTables.InitializeReadersTable(readerTabel_Dgw);
            DataTables.InitializeTasksTable(taskTabel_Dgw);

            SwitchMainTables(TableType.Editions);
            await LoadDataAsync(TableType.Tasks);
            await LoadDataAsync(TableType.Readers);

            TaskOnEmployee.AssignTaskDashboard(taskData_waitLbl, taskData_doneLbl, taskData_newLbl,
                taskData_failedLbl, taskData_progressBarCpb, taskData_percentLbl,
                _searchTasks, subTabelInsertFilterDate_fromCdp, subTabelInsertFilterDate_toCdp);
            TaskOnEmployee.UpdateTasksTimer(_autoUpdateTasksTimer);

            ReloadMarkersForAddress();
            LoadEditionTypeFilters();
            LoadSubscriptionDateRange();
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
            subTabel_Dgw.Scroll += (s, t) => ResetTimer();
            subTabel_Dgw.MouseWheel += (s, t) => ResetTimer();
            readerTabel_Dgw.Scroll += (s, t) => ResetTimer();
            readerTabel_Dgw.MouseWheel += (s, t) => ResetTimer();

            _searchTasks = new SearchFilter<DataBase.Tasks>();
            _loaderTasks = new LazyLoader<DataBase.Tasks>(_searchTasks);
            _searchEditions = new SearchFilter<DataBase.Editions>();
            _loaderEditions = new LazyLoader<DataBase.Editions>(_searchEditions);
            _searchSubs = new SearchFilter<DataBase.Subscriptions>();
            _loaderSubs = new LazyLoader<DataBase.Subscriptions>(_searchSubs);
            _searchReds = new SearchFilter<DataBase.Readers>();
            _loaderReds = new LazyLoader<DataBase.Readers>(_searchReds);
        }

        private async Task CheckScrollAndLoad()
        {
            // Проверка, не грузим ли мы уже
            int firstVisible = taskTabel_Dgw.FirstDisplayedScrollingRowIndex;
            if (firstVisible < 0)
                return;

            // Если прокрутили вниз
            if (firstVisible + taskTabel_Dgw.DisplayedRowCount(false) >= taskTabel_Dgw.RowCount - 10)
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
                        DataTables.AddEditionRow(subTabel_Dgw, item);
                }
                else if (tableType == TableType.Subscriptions)
                {
                    var data = await _loaderSubs.LoadNextBatchAsync();

                    foreach (var item in data)
                    {
                        if (_locallyAddedSubscriptionIds.Contains(item.Id))
                            continue;

                        DataTables.AddSubscriptionRow(subTabel_Dgw, item);
                    }
                }
                else if (tableType == TableType.Readers)
                {
                    // Загружаем маркеры один раз
                    if (_cachedMarkers.Count == 0)
                    {
                        var markersResult = await DataBase._client.From<DataBase.Markers>().Get();
                        _cachedMarkers = markersResult.Models;
                    }

                    var data = await _loaderReds.LoadNextBatchAsync();

                    foreach (var item in data)
                    {
                        if (_locallyAddedReaderIds.Contains(item.Id))
                            continue;

                        var marker = _cachedMarkers.FirstOrDefault(m =>
                            !string.IsNullOrEmpty(m.IdReaders) &&
                            m.IdReaders.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                       .Select(s => s.Trim())
                                       .Contains(item.Id.ToString()));

                        DataTables.AddReaderTableRow(readerTabel_Dgw, item, marker);
                    }
                }
                else if (tableType == TableType.Tasks)
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

        private void readerRegistrationStreet_streetTxt_ContentChanged(object sender, EventArgs e)
        {
            BuildAddressPanels(readerRegistrationStreet_streetTxt.Content);
        }

        private async void LoadMarkersForAddress()
        {
            try
            {
                var response = await DataBase._client.From<DataBase.Markers>().Get();
                _allMarkers = response.Models;
                BuildAddressPanels(readerRegistrationStreet_streetTxt.Content);
                BuildSubscriptionReaderPanels(subCreatureGiveStreettxt_streetTxt.Content);
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка загрузки меток", ex);
            }
        }

        // Панели адресов для регистрации читателя
        private void BuildAddressPanels(string filter = "")
        {
            readerRegistrationSearch_stFlp.SuspendLayout();
            readerRegistrationSearch_stFlp.Controls.Clear();

            var filtered = _allMarkers.Where(m =>
            {
                string address = BuildAddress(m);
                return string.IsNullOrWhiteSpace(filter) ||
                       address.ToLower().Contains(filter.ToLower());
            }).ToList();

            foreach (var marker in filtered)
            {
                string address = BuildAddress(marker);

                var row = new cuiPanel
                {
                    Width = readerRegistrationSearch_stFlp.ClientSize.Width - 8,
                    Height = 32,
                    Cursor = Cursors.Hand,
                    Tag = marker
                };

                var lbl = new cuiLabel
                {
                    Content = address,
                    AutoSize = false,
                    Size = new Size(row.Width - 8, row.Height),
                    Location = new Point(4, 0),
                    Font = new Font("Montserrat SemiBold", 9f, FontStyle.Bold),
                    VerticalAlignment = StringAlignment.Center,
                    HorizontalAlignment = StringAlignment.Near,
                };

                row.Click += (s, e) =>
                {
                    _selectedMarker = (DataBase.Markers)((cuiPanel)s).Tag;
                    readerRegistrationStreet_streetTxt.Content = address;
                    readerRegistrationSearch_stFlp.Controls.Clear();
                };
                lbl.Click += (s, e) =>
                {
                    _selectedMarker = (DataBase.Markers)row.Tag;
                    readerRegistrationStreet_streetTxt.Content = address;
                    readerRegistrationSearch_stFlp.Controls.Clear();
                };

                row.Controls.Add(lbl);
                readerRegistrationSearch_stFlp.Controls.Add(row);
            }

            readerRegistrationSearch_stFlp.ResumeLayout();
        }

        // Панели читателей по адресам для оформления подписки
        private void BuildSubscriptionReaderPanels(string filter = "")
        {
            subCreatureGiveSearch_stFlp.SuspendLayout();
            subCreatureGiveSearch_stFlp.Controls.Clear();

            foreach (var marker in _allMarkers)
            {
                if (string.IsNullOrEmpty(marker.IdReaders)) continue;

                string address = BuildAddress(marker);
                var readerIds = marker.IdReaders
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .ToList();

                foreach (string readerIdStr in readerIds)
                {
                    if (!Guid.TryParse(readerIdStr, out Guid readerId)) continue;

                    // Ищем читателя в кэше
                    var cachedRow = FindReaderInTable(readerId);
                    string fio = cachedRow ?? readerIdStr;

                    string displayText = $"{address}, {fio}";

                    if (!string.IsNullOrWhiteSpace(filter) &&
                        !address.ToLower().Contains(filter.ToLower()) &&
                        !fio.ToLower().Contains(filter.ToLower()))
                        continue;

                    var capturedReaderId = readerId;
                    var capturedDisplay = displayText;

                    var row = new cuiPanel
                    {
                        Width = subCreatureGiveSearch_stFlp.ClientSize.Width - 8,
                        Height = 32,
                        Cursor = Cursors.Hand,
                        Tag = capturedReaderId
                    };

                    var lbl = new cuiLabel
                    {
                        Content = displayText,
                        AutoSize = false,
                        Size = new Size(row.Width - 8, row.Height),
                        Location = new Point(4, 0),
                        Font = new Font("Montserrat SemiBold", 9f, FontStyle.Bold),
                        VerticalAlignment = StringAlignment.Center,
                        HorizontalAlignment = StringAlignment.Near,
                    };

                    row.Click += (s, e) =>
                    {
                        _selectedSubscriptionReaderId = (Guid)((cuiPanel)s).Tag;
                        subCreatureGiveStreettxt_streetTxt.Content = capturedDisplay;
                        subCreatureGiveSearch_stFlp.Controls.Clear();
                    };
                    lbl.Click += (s, e) =>
                    {
                        _selectedSubscriptionReaderId = capturedReaderId;
                        subCreatureGiveStreettxt_streetTxt.Content = capturedDisplay;
                        subCreatureGiveSearch_stFlp.Controls.Clear();
                    };

                    row.Controls.Add(lbl);
                    subCreatureGiveSearch_stFlp.Controls.Add(row);
                }
            }

            subCreatureGiveSearch_stFlp.ResumeLayout();
        }

        // Ищет ФИО читателя по id в таблице readerTabel_Dgw
        private string FindReaderInTable(Guid readerId)
        {
            foreach (DataGridViewRow row in readerTabel_Dgw.Rows)
            {
                if (row.Cells["Id"].Value != null &&
                    row.Cells["Id"].Value.ToString() == readerId.ToString())
                    return row.Cells["FIO"].Value?.ToString() ?? readerId.ToString();
            }
            return null;
        }

        private string BuildAddress(DataBase.Markers m)
        {
            if (!string.IsNullOrWhiteSpace(m.House))
                return $"{m.Street} {m.House}".Trim();

            string extra = "";
            if (!string.IsNullOrWhiteSpace(m.Building)) extra += $"к{m.Building}";
            if (!string.IsNullOrWhiteSpace(m.Apartment)) extra += $"/{m.Apartment}";
            return $"{m.Street} {extra}".Trim();
        }

        private void ReloadMarkersForAddress()
        {
            _allMarkers.Clear();
            readerRegistrationSearch_stFlp.Controls.Clear();
            subCreatureGiveSearch_stFlp.Controls.Clear();
            readerRegistrationStreet_streetTxt.Content = string.Empty;
            subCreatureGiveStreettxt_streetTxt.Content = string.Empty;
            LoadMarkersForAddress();
        }

        private async void readerTabel_Dgw_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex < 0 || e.RowIndex >= readerTabel_Dgw.Rows.Count - 1) return;

                var row = readerTabel_Dgw.Rows[e.RowIndex];
                if (row.Cells["Id"].Value == null || string.IsNullOrEmpty(row.Cells["Id"].Value.ToString())) return;

                Guid id = Guid.Parse(row.Cells["Id"].Value.ToString());
                var reader = await DataBase._client.From<DataBase.Readers>().Where(x => x.Id == id).Single();
                if (reader == null) return;

                // Берём маркер из кэша по IdMarker из строки таблицы
                DataBase.Markers marker = null;
                if (row.Cells["IdMarker"].Value != null && !string.IsNullOrEmpty(row.Cells["IdMarker"].Value.ToString()))
                {
                    Guid markerId = Guid.Parse(row.Cells["IdMarker"].Value.ToString());
                    marker = _cachedMarkers.FirstOrDefault(m => m.Id == markerId);
                    // Если кэш устарел — тянем из БД
                    if (marker == null)
                        marker = await DataBase._client.From<DataBase.Markers>().Where(x => x.Id == markerId).Single();
                }

                readerRegistrationSurname_surnameTxt.Content = string.Empty;
                cuiTextBox2.Content = string.Empty;
                readerRegistrationPatronymic_patronymicTxt.Content = string.Empty;

                readerRegistration_fioLbl.Content = reader.FIO;

                if (marker != null)
                {
                    string address = !string.IsNullOrWhiteSpace(marker.House)
                        ? $"Ул. {marker.Street} {marker.House}"
                        : $"Ул. {marker.Street} {marker.Building}/{marker.Apartment}";

                    readerRegistration_streetLbl.Content = address;
                    readerRegistrationStreet_streetTxt.Content = address;
                    _selectedMarker = marker;
                }

                _selectedReader = reader;

                int logic = 0;
                foreach (char c in reader.FIO)
                {
                    if (c == ' ') logic++;
                    else if (logic == 0) readerRegistrationSurname_surnameTxt.Content += c;
                    else if (logic == 1) cuiTextBox2.Content += c;
                    else if (logic == 2) readerRegistrationPatronymic_patronymicTxt.Content += c;
                }

                readerRegistrationDone_stLbl.Content = "Редактировать";
            }
            catch { }
        }

        private async void cuiPanel2_Click(object sender, EventArgs e)
        {
            string query = readerTabelUpperSearch_searchTxt.Content?.Trim().ToLower() ?? "";

            readerTabel_Dgw.Rows.Clear();
            _loaderReds.Reset();
            _cachedMarkers.Clear();

            // Загружаем маркеры
            var markersResult = await DataBase._client.From<DataBase.Markers>().Get();
            _cachedMarkers = markersResult.Models;

            // Загружаем всех читателей
            var readersResult = await DataBase._client.From<DataBase.Readers>().Get();
            var readers = readersResult.Models;

            foreach (var item in readers)
            {
                var marker = _cachedMarkers.FirstOrDefault(m =>
                    !string.IsNullOrEmpty(m.IdReaders) &&
                    m.IdReaders.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                               .Select(s => s.Trim())
                               .Contains(item.Id.ToString()));

                string address = "";
                if (marker != null)
                {
                    if (!string.IsNullOrWhiteSpace(marker.House))
                        address = $"{marker.Street} {marker.House}".Trim();
                    else
                    {
                        string extra = "";
                        if (!string.IsNullOrWhiteSpace(marker.Building)) extra += $"к{marker.Building}";
                        if (!string.IsNullOrWhiteSpace(marker.Apartment)) extra += $"/{marker.Apartment}";
                        address = $"{marker.Street} {extra}".Trim();
                    }
                }

                // Фильтрация по ФИО или адресу
                if (string.IsNullOrWhiteSpace(query) ||
                    (item.FIO?.ToLower().Contains(query) ?? false) ||
                    address.ToLower().Contains(query))
                {
                    DataTables.AddReaderTableRow(readerTabel_Dgw, item, marker);
                }
            }
        }

        private void TermResult(cuiPanel panel, string term)
        {
            if (panel == subCreatureGiveJan_Pnl)
            {
                if (subCreatureGive_jan && term[0] == '0')
                    subCreatureGiveJan_Pnl_Click(null, null);
                else if (!subCreatureGive_jan && term[0] == '1')
                    subCreatureGiveJan_Pnl_Click(null, null);
            }
            else if (panel == subCreatureGiveFeb_Pnl)
            {
                if (subCreatureGive_feb && term[1] == '0')
                    subCreatureGiveFeb_Pnl_Click(null, null);
                else if (!subCreatureGive_feb && term[1] == '1')
                    subCreatureGiveFeb_Pnl_Click(null, null);
            }
            else if (panel == subCreatureGiveMar_Pnl)
            {
                if (subCreatureGive_mar && term[2] == '0')
                    subCreatureGiveMar_Pnl_Click(null, null);
                else if (!subCreatureGive_mar && term[2] == '1')
                    subCreatureGiveMar_Pnl_Click(null, null);
            }
            else if (panel == subCreatureGiveApr_Pnl)
            {
                if (subCreatureGive_apr && term[3] == '0')
                    subCreatureGiveApr_Pnl_Click(null, null);
                else if (!subCreatureGive_apr && term[3] == '1')
                    subCreatureGiveApr_Pnl_Click(null, null);
            }
            else if (panel == subCreatureGiveMay_Pnl)
            {
                if (subCreatureGive_may && term[4] == '0')
                    subCreatureGiveMay_Pnl_Click(null, null);
                else if (!subCreatureGive_may && term[4] == '1')
                    subCreatureGiveMay_Pnl_Click(null, null);
            }
            else if (panel == subCreatureGiveJun_Pnl)
            {
                if (subCreatureGive_jun && term[5] == '0')
                    subCreatureGiveJun_Pnl_Click(null, null);
                else if (!subCreatureGive_jun && term[5] == '1')
                    subCreatureGiveJun_Pnl_Click(null, null);
            }
            else if (panel == subCreatureGiveJul_Pnl)
            {
                if (subCreatureGive_jul && term[6] == '0')
                    subCreatureGiveJul_Pnl_Click(null, null);
                else if (!subCreatureGive_jul && term[6] == '1')
                    subCreatureGiveJul_Pnl_Click(null, null);
            }
            else if (panel == subCreatureGiveAug_Pnl)
            {
                if (subCreatureGive_aug && term[7] == '0')
                    subCreatureGiveAug_Pnl_Click(null, null);
                else if (!subCreatureGive_aug && term[7] == '1')
                    subCreatureGiveAug_Pnl_Click(null, null);
            }
            else if (panel == subCreatureGiveSep_Pnl)
            {
                if (subCreatureGive_sep && term[8] == '0')
                    subCreatureGiveSep_Pnl_Click(null, null);
                else if (!subCreatureGive_sep && term[8] == '1')
                    subCreatureGiveSep_Pnl_Click(null, null);
            }
            else if (panel == subCreatureGiveOct_Pnl)
            {
                if (subCreatureGive_oct && term[9] == '0')
                    subCreatureGiveOct_Pnl_Click(null, null);
                else if (!subCreatureGive_oct && term[9] == '1')
                    subCreatureGiveOct_Pnl_Click(null, null);
            }
            else if (panel == subCreatureGiveNov_Pnl)
            {
                if (subCreatureGive_nov && term[10] == '0')
                    subCreatureGiveNov_Pnl_Click(null, null);
                else if (!subCreatureGive_nov && term[10] == '1')
                    subCreatureGiveNov_Pnl_Click(null, null);
            }
            else if (panel == subCreatureGiveDec_Pnl)
            {
                if (subCreatureGive_dec && term[11] == '0')
                    subCreatureGiveDec_Pnl_Click(null, null);
                else if (!subCreatureGive_dec && term[11] == '1')
                    subCreatureGiveDec_Pnl_Click(null, null);
            }
        }

        private async void subTabel_Dgw_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (!subCreature_animation)
                    subCreature_animationTmr.Start();

                if (_currentTable == TableType.Subscriptions)
                {
                    if (subCreatureGiveBuy_stLbl.Content != "Редактировать подписку")
                        subCreatureGiveBuy_stLbl.Content = "Редактировать подписку";

                    if (_cachedMarkers.Count == 0)
                    {
                        var markersRes = await DataBase._client.From<DataBase.Markers>().Get();
                        _cachedMarkers = markersRes.Models;
                    }

                    if (e.RowIndex >= 0 && e.RowIndex < subTabel_Dgw.Rows.Count - 1)
                    {
                        // Получаем строку
                        var row = subTabel_Dgw.Rows[e.RowIndex];
                        Guid id = Guid.Parse(row.Cells["Id"].Value.ToString());

                        var subscriptionMain = await DataBase._client.From<DataBase.Subscriptions>().Where(x => x.Id == id).Single();
                        if (row.Cells["Id"].Value == null || string.IsNullOrEmpty(row.Cells["Id"].Value.ToString()))
                            return;

                        _selectedSubscription = subscriptionMain;
                        var editionMain = await DataBase._client.From<DataBase.Editions>().Where(x => x.Index != null && x.Index == subscriptionMain.IndexEdition).Single();
                        _selectedEdition = editionMain;

                        subCreatureGiveCounter_numberLbl_1_1.Content = subscriptionMain.Kit.ToString();
                        // Ищем читателя у которого есть id этой подписки в IdActiveSubscriptions
                        var allReaders = await DataBase._client.From<DataBase.Readers>().Get();
                        var readerSub = allReaders.Models.FirstOrDefault(r =>
                            !string.IsNullOrEmpty(r.IdActiveSubscriptions) &&
                            r.IdActiveSubscriptions.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(s => s.Trim())
                                .Contains(id.ToString()));

                        if (readerSub != null)
                        {
                            _selectedSubscriptionReaderId = readerSub.Id;

                            // Ищем маркер где есть этот читатель
                            var markerSub = _cachedMarkers.FirstOrDefault(m =>
                                !string.IsNullOrEmpty(m.IdReaders) &&
                                m.IdReaders.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(s => s.Trim())
                                    .Contains(readerSub.Id.ToString()));

                            if (markerSub != null)
                            {
                                string addressSub = BuildAddress(markerSub);
                                subCreatureGiveStreettxt_streetTxt.Content = $"{addressSub}, {readerSub.FIO}";
                            }
                            else
                                subCreatureGiveStreettxt_streetTxt.Content = readerSub.FIO;
                        }
                        else
                        {
                            _selectedSubscriptionReaderId = Guid.Empty;
                            subCreatureGiveStreettxt_streetTxt.Content = string.Empty;
                        }

                        string term = subscriptionMain.TermSubscription;
                        TermResult(subCreatureGiveJan_Pnl, term);
                        TermResult(subCreatureGiveFeb_Pnl, term);
                        TermResult(subCreatureGiveMar_Pnl, term);
                        TermResult(subCreatureGiveApr_Pnl, term);
                        TermResult(subCreatureGiveMay_Pnl, term);
                        TermResult(subCreatureGiveJun_Pnl, term);
                        TermResult(subCreatureGiveJul_Pnl, term);
                        TermResult(subCreatureGiveAug_Pnl, term);
                        TermResult(subCreatureGiveSep_Pnl, term);
                        TermResult(subCreatureGiveOct_Pnl, term);
                        TermResult(subCreatureGiveNov_Pnl, term);
                        TermResult(subCreatureGiveDec_Pnl, term);

                        // Автоматически вызываем метод для расчета цен
                        PanelsMounthUpdate();
                    }
                }
                else if (_currentTable == TableType.Editions)
                {
                    if (subCreatureGiveBuy_stLbl.Content != "Оформить подписку")
                        subCreatureGiveBuy_stLbl.Content = "Оформить подписку";

                    if (e.RowIndex >= 0 && e.RowIndex < subTabel_Dgw.Rows.Count - 1)
                    {
                        // Получаем строку
                        var row = subTabel_Dgw.Rows[e.RowIndex];
                        Guid id = Guid.Parse(row.Cells["Id"].Value.ToString());

                        _selectedSubscription = null;
                        var editionMain = await DataBase._client.From<DataBase.Editions>().Where(x => x.Id == id).Single();
                        if (row.Cells["Id"].Value == null || string.IsNullOrEmpty(row.Cells["Id"].Value.ToString()))
                            return;

                        _selectedEdition = editionMain;

                        subCreatureTitle_nameLbl.Content = _selectedEdition.Name;
                        subCreatureTitle_numberLbl.Content = _selectedEdition.Index;
                        subCreatureTitle_editionsLbl.Content = _selectedEdition.TypeEdition;

                        // Автоматически вызываем методы для расчета цен
                        PanelsMounthUpdate();
                    }
                }
            }
            catch { }
        }

        private void subCreatureGiveStreettxt_streetTxt_ContentChanged(object sender, EventArgs e)
        {
            BuildSubscriptionReaderPanels(subCreatureGiveStreettxt_streetTxt.Content);
        }

        private async void LoadEditionTypeFilters()
        {
            try
            {
                var editions = await DataBase._client.From<DataBase.Editions>().Get();
                var types = editions.Models
                    .Select(e => e.TypeEdition)
                    .Where(t => !string.IsNullOrEmpty(t))
                    .Distinct()
                    .ToList();

                _editionTypeFilters = new List<string>(types);
                subTabelEditionFilterEdit_Pnl.Controls.Clear();

                foreach (var type in types)
                {
                    var cb = new cuiCheckbox
                    {
                        Content = type,
                        Checked = true,
                        Width = subTabelEditionFilterEdit_Pnl.ClientSize.Width - 8,
                        Height = 28,
                        Tag = type
                    };
                    subTabelEditionFilterEdit_Pnl.Controls.Add(cb);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка загрузки типов изданий", ex);
            }
        }

        private async void LoadSubscriptionDateRange()
        {
            try
            {
                var subs = await DataBase._client.From<DataBase.Subscriptions>().Get();
                if (subs.Models.Count == 0) return;

                var minDate = subs.Models.Min(s => s.DateRegistred);
                var maxDate = subs.Models.Max(s => s.DateRegistred);

                subTabelSubscriptionFilterDate_fromCdp.Content = minDate;
                subTabelSubscriptionFilterDate_toCdp.Content = maxDate;
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка загрузки дат подписок", ex);
            }
        }
    }
}
