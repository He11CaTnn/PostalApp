using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PostalApp
{
    public partial class ManagerForm : Form
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
        // Панель информации, во вкладке [Задание]
        bool taskWatchOpen = false;
        // Панель фильтра, во вкладке [Задание]
        bool taskUpperFilter_animation = false;
        // Комбо бокс фильтра, во вкладке [Задание]
        bool taskUpperFilterArrow = false;

        private System.Windows.Forms.Timer _scrollDebounceTimer;
        private int delay = 100;
        private System.Windows.Forms.Timer _autoUpdateTasksTimer;

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

        private string currentExcelFilePath = null;
        private List<Dictionary<string, object>> validatedData = new List<Dictionary<string, object>>();
        private CancellationTokenSource validationCancellation = null;
        private ExcelPackage excelPackage = null;

        public enum TableType
        {
            Editions,
            Tasks
        }

        public ManagerForm()
        {
            InitializeComponent();
            OpenPanel();
            RoundedCorners();
        }
        // Метод который выполняют всякую чушь
        private void OpenPanel()
        {
            taskWatch_Pnl.Location = new Point(1200, 83);
            task_Pnl.Visible = true;
            task_Pnl.Dock = DockStyle.Fill;
            editions_Pnl.Visible = false;
            editions_Pnl.Dock = DockStyle.Fill;
        }
        // Метод скругляющий элементы на форме
        private void RoundedCorners()
        {
            SetRoundedCorners(taskTop_Pnl, 23);
            SetRoundedCorners(taskBottom_Pnl, 23);
            SetRoundedCorners(taskWatch_Pnl, 24);
            SetRoundedCorners(taskTabel_Pnl, 24);

            SetRoundedCorners(editionsTabel_Pnl, 24);
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
        // Кнопка минимальный размер формы в верхней части панели
        private void upper_minimizeBtn_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }
        // Перемещение формы зажатием по верхней части панели
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
        // taskWatch_Pnl панель с информацией о задании, во вкладке [Задании]
        // Таймер открытия панели информацией о задании, во вкладке [Задании]
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
        // taskWatch_Pnl Комбо бокс фильтр отвечающий за выезд панелей фильтра, во вкладке [Задании]
        // Таймер с функции выезда панели фильтра изданий, во вкладке [Оформление подписок]
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
        // taskTabelUpperFilter_Pnl Комбо бокс фильтр отвечающий за выезд панелей фильтра, во вкладке [Задании]
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

        // taskTop_Pnl Нижняя левая панель меню, во вкладке [Задании]
        // Кнопка задании, во вкладке [Задании]
        private void taskTopTask_Pnl_Click(object sender, EventArgs e)
        {
            this.SuspendLayout();
            task_Pnl.Visible = true;
            editions_Pnl.Visible = false;
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
        // Кнопка загрузка журнала издания, во вкладке [Задании]
        private void taskTopEdit_Pnl_Click(object sender, EventArgs e)
        {
            this.SuspendLayout();
            task_Pnl.Visible = false;
            editions_Pnl.Visible = true;
            this.ResumeLayout();
        }
        private async void taskTopEdit_Pnl_MouseEnter(object sender, EventArgs e)
        {
            taskTopEdit_editPic.Image = Properties.Resources.Издания1;
            await Task.Delay(20);
            taskTopEdit_editPic.Image = Properties.Resources.Издания2;
            await Task.Delay(20);
            taskTopEdit_editPic.Image = Properties.Resources.Издания3;
            await Task.Delay(20);
            taskTopEdit_editPic.Image = Properties.Resources.Издания4;
            await Task.Delay(20);
            taskTopEdit_editPic.Image = Properties.Resources.Издания5;
        }
        private async void taskTopEdit_Pnl_MouseLeave(object sender, EventArgs e)
        {
            taskTopEdit_editPic.Image = Properties.Resources.Издания5;
            await Task.Delay(20);
            taskTopEdit_editPic.Image = Properties.Resources.Издания4;
            await Task.Delay(20);
            taskTopEdit_editPic.Image = Properties.Resources.Издания3;
            await Task.Delay(20);
            taskTopEdit_editPic.Image = Properties.Resources.Издания2;
            await Task.Delay(20);
            taskTopEdit_editPic.Image = Properties.Resources.Издания1;
        }
        // taskBottom_Pnl Нижняя левая панель меню, во вкладке [Задании]
        // Кнопка настройки, во вкладке [Задании]
        private void taskBottomSettings_Pnl_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Настройка");
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
        // Кнопка выйти, во вкладке [Задании]
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

        private void RollbackValidatedData()
        {
            validatedData.Clear();
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

        // Кнопка загрузить данные, во вкладке [Загрузка журнала издания]
        private async void editionsMoveEditDone_Pnl_Click(object sender, EventArgs e)
        {
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
                editionsMoveEditTabInfo_nameLbl.Content = "Нет загруженного файла";
                editionsMoveEditTabInfo_wtLbl.Content = "0кб";
                editionsMoveEditTab_stLbl_1_1.Content = "0 строк";
                editionsMoveEditTab_stLbl_1_2.Content = "0 столбцов";

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
                editionsTabel_Dgw.Rows.Clear();
                await LoadDataAsync(TableType.Editions);
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

        private async void editionsMoveEditDone_Pnl_MouseEnter(object sender, EventArgs e)
        {
            editionsMoveEditDone_Pnl.PanelColor = Color.FromArgb(25, 55, 255); //1
            editionsMoveEditDone_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
            await Task.Delay(20);
            editionsMoveEditDone_Pnl.PanelColor = Color.FromArgb(26, 54, 244); //2
            editionsMoveEditDone_Pnl.PanelOutlineColor = Color.FromArgb(26, 54, 244);
            await Task.Delay(20);
            editionsMoveEditDone_Pnl.PanelColor = Color.FromArgb(26, 52, 232); //3
            editionsMoveEditDone_Pnl.PanelOutlineColor = Color.FromArgb(26, 52, 232);
        }
        private async void editionsMoveEditDone_Pnl_MouseLeave(object sender, EventArgs e)
        {
            editionsMoveEditDone_Pnl.PanelColor = Color.FromArgb(26, 52, 232); //3
            editionsMoveEditDone_Pnl.PanelOutlineColor = Color.FromArgb(26, 52, 232);
            await Task.Delay(20);
            editionsMoveEditDone_Pnl.PanelColor = Color.FromArgb(26, 54, 244); //2
            editionsMoveEditDone_Pnl.PanelOutlineColor = Color.FromArgb(26, 54, 244);
            await Task.Delay(20);
            editionsMoveEditDone_Pnl.PanelColor = Color.FromArgb(25, 55, 255); //1
            editionsMoveEditDone_Pnl.PanelOutlineColor = Color.FromArgb(25, 55, 255);
        }
        // Кнопка очистить, во вкладке [Загрузка журнала издания]
        private async void editionsMoveEditClean_Pnl_Click(object sender, EventArgs e)
        {
            try
            {
                var result = Logger.ShowYesNo("Вы уверены, что хотите удалить все данные из таблицы «Издания»?\nОни будут безвозвратно удалены как из таблицы, так и с сервера.");
                if (result == DialogResult.Yes)
                {
                    try
                    {
                        await DataBase._client.From<DataBase.Editions>().Where(x => x.Id != null).Delete();
                        editionsTabel_Dgw.Rows.Clear();
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
            catch (Exception ex)
            {
                Logger.Error("Ошибка при очистки таблицы «Издания»", ex);
                Logger.ShowError("Ошибка при очистки таблицы «Издания»");
            }
        }
        private async void editionsMoveEditClean_Pnl_MouseEnter(object sender, EventArgs e)
        {
            editionsMoveEditClean_Pnl.PanelColor = Color.FromArgb(242, 243, 250); //1
            editionsMoveEditClean_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
            await Task.Delay(20);
            editionsMoveEditClean_Pnl.PanelColor = Color.FromArgb(240, 240, 248); //2
            editionsMoveEditClean_Pnl.PanelOutlineColor = Color.FromArgb(240, 240, 248);
            await Task.Delay(20);
            editionsMoveEditClean_Pnl.PanelColor = Color.FromArgb(237, 238, 246); //3
            editionsMoveEditClean_Pnl.PanelOutlineColor = Color.FromArgb(237, 238, 246);
            await Task.Delay(20);
            editionsMoveEditClean_Pnl.PanelColor = Color.FromArgb(235, 235, 244); //4
            editionsMoveEditClean_Pnl.PanelOutlineColor = Color.FromArgb(235, 235, 244);
        }
        private async void editionsMoveEditClean_Pnl_MouseLeave(object sender, EventArgs e)
        {
            editionsMoveEditClean_Pnl.PanelColor = Color.FromArgb(235, 235, 244); //4
            editionsMoveEditClean_Pnl.PanelOutlineColor = Color.FromArgb(235, 235, 244);
            await Task.Delay(20);
            editionsMoveEditClean_Pnl.PanelColor = Color.FromArgb(237, 238, 246); //3
            editionsMoveEditClean_Pnl.PanelOutlineColor = Color.FromArgb(237, 238, 246);
            await Task.Delay(20);
            editionsMoveEditClean_Pnl.PanelColor = Color.FromArgb(240, 240, 248); //2
            editionsMoveEditClean_Pnl.PanelOutlineColor = Color.FromArgb(240, 240, 248);
            await Task.Delay(20);
            editionsMoveEditClean_Pnl.PanelColor = Color.FromArgb(242, 243, 250); //1
            editionsMoveEditClean_Pnl.PanelOutlineColor = Color.FromArgb(242, 243, 250);
        }
        // Кнопка удалить файл, во вкладке [Загрузка журнала издания]
        private void editionsMoveEditDelete_Pnl_Click(object sender, EventArgs e)
        {
            // Очищаем путь к файлу
            currentExcelFilePath = null;

            // Очищаем лейблы
            editionsMoveEditTabInfo_nameLbl.Content = "Нет загруженного файла";
            editionsMoveEditTabInfo_wtLbl.Content = "0 кб";
            editionsMoveEditTab_stLbl_1_1.Content = "0 строк";
            editionsMoveEditTab_stLbl_1_2.Content = "0 столбцов";
        }
        private async void editionsMoveEditDelete_Pnl_MouseEnter(object sender, EventArgs e)
        {
            editionsMoveEditDelete_Pnl.PanelColor = Color.FromArgb(255, 255, 255); //1
            editionsMoveEditDelete_deleteLbl.ForeColor = Color.FromArgb(232, 50, 65);
            await Task.Delay(20);
            editionsMoveEditDelete_Pnl.PanelColor = Color.FromArgb(247, 187, 192); //2
            editionsMoveEditDelete_deleteLbl.ForeColor = Color.FromArgb(240, 118, 128);
            await Task.Delay(20);
            editionsMoveEditDelete_Pnl.PanelColor = Color.FromArgb(240, 118, 128); //3
            editionsMoveEditDelete_deleteLbl.ForeColor = Color.FromArgb(247, 187, 192);
            await Task.Delay(20);
            editionsMoveEditDelete_Pnl.PanelColor = Color.FromArgb(232, 50, 65); //4
            editionsMoveEditDelete_deleteLbl.ForeColor = Color.FromArgb(255, 255, 255);
        }
        private async void editionsMoveEditDelete_Pnl_MouseLeave(object sender, EventArgs e)
        {
            editionsMoveEditDelete_Pnl.PanelColor = Color.FromArgb(232, 50, 65); //4
            editionsMoveEditDelete_deleteLbl.ForeColor = Color.FromArgb(255, 255, 255);
            await Task.Delay(20);
            editionsMoveEditDelete_Pnl.PanelColor = Color.FromArgb(240, 118, 128); //3
            editionsMoveEditDelete_deleteLbl.ForeColor = Color.FromArgb(247, 187, 192);
            await Task.Delay(20);
            editionsMoveEditDelete_Pnl.PanelColor = Color.FromArgb(247, 187, 192); //2
            editionsMoveEditDelete_deleteLbl.ForeColor = Color.FromArgb(240, 118, 128);
            await Task.Delay(20);
            editionsMoveEditDelete_Pnl.PanelColor = Color.FromArgb(255, 255, 255); //1
            editionsMoveEditDelete_deleteLbl.ForeColor = Color.FromArgb(232, 50, 65);
        }
        // editionsTop_Pnl Нижняя левая панель меню, во вкладке [Загрузка журнала издания]
        // Кнопка задании, во вкладке [Загрузка журнала издания]
        private void editionsTopTask_Pnl_Click(object sender, EventArgs e)
        {
            this.SuspendLayout();
            task_Pnl.Visible = true;
            editions_Pnl.Visible = false;
            this.ResumeLayout();
        }
        private async void editionsTopTask_Pnl_MouseEnter(object sender, EventArgs e)
        {
            editionsTopTask_taskPic.Image = Properties.Resources.Задание1;
            await Task.Delay(20);
            editionsTopTask_taskPic.Image = Properties.Resources.Задание2;
            await Task.Delay(20);
            editionsTopTask_taskPic.Image = Properties.Resources.Задание3;
            await Task.Delay(20);
            editionsTopTask_taskPic.Image = Properties.Resources.Задание4;
            await Task.Delay(20);
            editionsTopTask_taskPic.Image = Properties.Resources.Задание5;
        }
        private async void editionsTopTask_Pnl_MouseLeave(object sender, EventArgs e)
        {
            editionsTopTask_taskPic.Image = Properties.Resources.Задание5;
            await Task.Delay(20);
            editionsTopTask_taskPic.Image = Properties.Resources.Задание4;
            await Task.Delay(20);
            editionsTopTask_taskPic.Image = Properties.Resources.Задание3;
            await Task.Delay(20);
            editionsTopTask_taskPic.Image = Properties.Resources.Задание2;
            await Task.Delay(20);
            editionsTopTask_taskPic.Image = Properties.Resources.Задание1;
        }
        // Кнопка загрузка журнала издания, во вкладке [Загрузка журнала издания]
        private void editionsTopEdit_Pnl_Click(object sender, EventArgs e)
        {
            this.SuspendLayout();
            task_Pnl.Visible = false;
            editions_Pnl.Visible = true;
            this.ResumeLayout();
        }
        private async void editionsTopEdit_Pnl_MouseEnter(object sender, EventArgs e)
        {
            editionsTopEdit_editPic.Image = Properties.Resources.Издания1;
            await Task.Delay(20);
            editionsTopEdit_editPic.Image = Properties.Resources.Издания2;
            await Task.Delay(20);
            editionsTopEdit_editPic.Image = Properties.Resources.Издания3;
            await Task.Delay(20);
            editionsTopEdit_editPic.Image = Properties.Resources.Издания4;
            await Task.Delay(20);
            editionsTopEdit_editPic.Image = Properties.Resources.Издания5;
        }
        private async void editionsTopEdit_Pnl_MouseLeave(object sender, EventArgs e)
        {
            editionsTopEdit_editPic.Image = Properties.Resources.Издания5;
            await Task.Delay(20);
            editionsTopEdit_editPic.Image = Properties.Resources.Издания4;
            await Task.Delay(20);
            editionsTopEdit_editPic.Image = Properties.Resources.Издания3;
            await Task.Delay(20);
            editionsTopEdit_editPic.Image = Properties.Resources.Издания2;
            await Task.Delay(20);
            editionsTopEdit_editPic.Image = Properties.Resources.Издания1;
        }
        // editionsBottom_Pnl Нижняя левая панель меню, во вкладке [Загрузка журнала издания]
        // Кнопка настройка, во вкладке [Загрузка журнала издания]
        private void editionsBottomSettings_Pnl_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Настройка");
        }
        private async void editionsBottomSettings_Pnl_MouseEnter(object sender, EventArgs e)
        {
            editionsBottomSettings_settingsPic.Image = Properties.Resources.Настройки1;
            await Task.Delay(20);
            editionsBottomSettings_settingsPic.Image = Properties.Resources.Настройки2;
            await Task.Delay(20);
            editionsBottomSettings_settingsPic.Image = Properties.Resources.Настройки3;
            await Task.Delay(20);
            editionsBottomSettings_settingsPic.Image = Properties.Resources.Настройки4;
            await Task.Delay(20);
            editionsBottomSettings_settingsPic.Image = Properties.Resources.Настройки5;
        }
        private async void editionsBottomSettings_Pnl_MouseLeave(object sender, EventArgs e)
        {
            editionsBottomSettings_settingsPic.Image = Properties.Resources.Настройки5;
            await Task.Delay(20);
            editionsBottomSettings_settingsPic.Image = Properties.Resources.Настройки4;
            await Task.Delay(20);
            editionsBottomSettings_settingsPic.Image = Properties.Resources.Настройки3;
            await Task.Delay(20);
            editionsBottomSettings_settingsPic.Image = Properties.Resources.Настройки2;
            await Task.Delay(20);
            editionsBottomSettings_settingsPic.Image = Properties.Resources.Настройки1;
        }
        // Кнопка выйти, во вкладке [Загрузка журнала издания]
        private async void editionsBottomExit_Pnl_Click(object sender, EventArgs e)
        {
            await UserData.LogoutAndExit(this);
        }
        private async void editionsBottomExit_Pnl_MouseEnter(object sender, EventArgs e)
        {
            editionsBottomExit_exitPic.Image = Properties.Resources.Выход1;
            await Task.Delay(20);
            editionsBottomExit_exitPic.Image = Properties.Resources.Выход2;
            await Task.Delay(20);
            editionsBottomExit_exitPic.Image = Properties.Resources.Выход3;
            await Task.Delay(20);
            editionsBottomExit_exitPic.Image = Properties.Resources.Выход4;
            await Task.Delay(20);
            editionsBottomExit_exitPic.Image = Properties.Resources.Выход5;
        }
        private async void editionsBottomExit_Pnl_MouseLeave(object sender, EventArgs e)
        {
            editionsBottomExit_exitPic.Image = Properties.Resources.Выход5;
            await Task.Delay(20);
            editionsBottomExit_exitPic.Image = Properties.Resources.Выход4;
            await Task.Delay(20);
            editionsBottomExit_exitPic.Image = Properties.Resources.Выход3;
            await Task.Delay(20);
            editionsBottomExit_exitPic.Image = Properties.Resources.Выход2;
            await Task.Delay(20);
            editionsBottomExit_exitPic.Image = Properties.Resources.Выход1;
        }

        private async void ManagerForm_Load(object sender, EventArgs e)
        {
            InitializeTimer();
            SubscriptionEvents();

            DataTables.InitializeTasksTable(taskTabel_Dgw);
            DataTables.InitializeEditionsTable(editionsTabel_Dgw);
            await LoadDataAsync(TableType.Tasks);
            await LoadDataAsync(TableType.Editions);

            TaskOnEmployee.AssignTaskDashboard(taskData_waitLbl, taskData_doneLbl, taskData_newLbl,
                taskData_failedLbl, taskData_progressBarCpb, taskData_percentLbl,
                _searchTasks, subTabelInsertFilterDate_fromCdp, subTabelInsertFilterDate_toCdp);
            TaskOnEmployee.UpdateTasksTimer(_autoUpdateTasksTimer);

            ExcelPackage.License.SetNonCommercialPersonal(UserData.CurrentUser.Employee.FIO);
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

        private void ResetTimer()
        {
            _scrollDebounceTimer.Stop();
            _scrollDebounceTimer.Start();
        }

        private void SubscriptionEvents()
        {
            taskTabel_Dgw.Scroll += (s, t) => ResetTimer();
            taskTabel_Dgw.MouseWheel += (s, t) => ResetTimer();
            editionsTabel_Dgw.Scroll += (s, t) => ResetTimer();
            editionsTabel_Dgw.MouseWheel += (s, t) => ResetTimer();

            _searchTasks = new SearchFilter<DataBase.Tasks>();
            _loaderTasks = new LazyLoader<DataBase.Tasks>(_searchTasks);
            _searchEditions = new SearchFilter<DataBase.Editions>();
            _loaderEditions = new LazyLoader<DataBase.Editions>(_searchEditions);
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
                        DataTables.AddEditionRow(editionsTabel_Dgw, item);
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

        private void taskWatchsTransitionStart_Pnl_Click(object sender, EventArgs e)
        {
            TaskOnEmployee.ClickTasksButton(taskWatchsTransitionStart_Pnl, taskWatchsTransitionClose_startLbl, taskTabel_Dgw,
                taskWatchTransitionLayerRack_progressPth, taskWatch_nameLbl, taskWatch_deliveryLbl,
                taskWatch_endingLbl, taskWatchsTransitionReadertxt_readerTxt, taskWatch_tagsLbl);
        }

        private void taskWatchsTransitionClose_Pnl_Click(object sender, EventArgs e)
        {
            taskWatchOpen = !taskWatchOpen;
            taskWatch_animationTmr.Start();
        }

        private async void taskTabelInsertFilterDone_Pnl_Click(object sender, EventArgs e)
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

            await LoadDataAsync(TableType.Tasks);
        }

        private async void taskTabelInsertFilterClose_Pnl_Click(object sender, EventArgs e)
        {
            taskTabelInsertFilterStatus_newCkb.Checked = true;
            taskTabelInsertFilterStatus_processCkb.Checked = true;
            taskTabelInsertFilterStatus_doneCkb.Checked = true;

            _excludedStatuses.Clear();
            taskTabel_Dgw.Rows.Clear();

            if (taskUpperFilter_animation)
                taskTabelUpperFilter_Pnl_Click(sender, e);

            await LoadDataAsync(TableType.Tasks);
        }

        private void editionsMoveDropper_fileFdp_FileDropped(object sender, CuoreUI.Controls.FileDroppedEventArgs e)
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
            editionsMoveEditTabInfo_nameLbl.Content = Path.GetFileNameWithoutExtension(filePath);

            // Вычисляем и устанавливаем размер файла
            FileInfo fileInfo = new FileInfo(filePath);
            long fileSizeBytes = fileInfo.Length;
            string fileSizeText = GetFileSizeString(fileSizeBytes);
            GetExcelDimensions(filePath);
            editionsMoveEditTabInfo_wtLbl.Content = fileSizeText;
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

        private void GetExcelDimensions(string filePath)
        {
            try
            {
                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    var sheet = package.Workbook.Worksheets[0];
                    int rowCount = sheet.Dimension?.Rows ?? 0;
                    int colCount = sheet.Dimension?.Columns ?? 0;

                    editionsMoveEditTab_stLbl_1_1.Content = $"{rowCount} строк(-а, -и)";
                    editionsMoveEditTab_stLbl_1_2.Content = $"{colCount} столбцов(-а, -ец)";
                }
            }
            catch { }
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
    }
}
