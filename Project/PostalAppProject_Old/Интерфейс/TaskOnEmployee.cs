using CuoreUI.Controls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Интерфейс
{
    public static class TaskOnEmployee
    {
        private static int _timeUpdateTasks = 5000;
        private static bool _internetConnection = true;

        public static List<TaskViewModel> _tasks = new List<TaskViewModel>();
        public static TaskViewModel _selectedTask;
        public static string[] _taskStatus = { "Новое", "Принято", "В процессе", "Выполнено" };
        public static string[] _taskMessageStatus = { "Задание в начато!", "Задание выполнено!" };

        private static async Task AutoUpdateTasks(cuiPictureBox cuiPictureBox, cuiPictureBox pictureBox)
        {
            try
            {
                var response = await DataBase._client.From<DataBase.Tasks>().Where(x => x.IdEmployee == UserData.CurrentUser.Employee.Id && x.Status == "Новое").Get();
                Bitmap originalBitmap = response.Models.Count > 0 ? Properties.Resources.задачи_с_воскл : Properties.Resources.задачи;
                cuiPictureBox.BackgroundImage = originalBitmap;
                pictureBox.Visible = false;
                if (response.Models.Count > 0)
                {
                    if(!_internetConnection)
                        Logger.Info($"Подключение к интернету востановлено");
                    Logger.Info($"Обнаружено {response.Models.Count} новых заданий");
                }
            }
            catch 
            {
                pictureBox.Visible = true;
                _internetConnection = false;
                Logger.Info($"Проблемы с подключением к интернету");
            }
        }

        public static async Task UpdateSelectedTask(DataBase.Tasks item, DataGridView dataGridView = null)
        {
            var task = await DataBase._client.From<DataBase.Tasks>().Where(x => x.Id == item.Id).Single();
            var employee = await DataBase._client.From<DataBase.Employees>().Where(x => x.Id == item.IdEmployee).Single();

            if (task == null || employee == null)
                return;

            _selectedTask = new TaskViewModel
            {
                Task = task,
                Employee = employee
            };

            if (dataGridView != null)
            {
                foreach (DataGridViewRow row in dataGridView.Rows)
                {
                    if (row.Cells["Id"].Value != null && row.Cells["Статус"].Value.ToString() == _selectedTask.Task.Status)
                    {
                        row.Cells["Статус"].Value = _selectedTask.Task.Status;
                        break;
                    }
                }
            }
        }

        public static void InitializeTaskComboBox(cuiComboBox cuiComboBox, DataGridView dataGridView)
        {
            cuiComboBox.Items = new string[0];
            for (int i = 0; i < dataGridView.ColumnCount; i++)
            {
                if (!dataGridView.Columns[i].Visible)
                    continue;
                cuiComboBox.AddItem(dataGridView.Columns[i].HeaderText);
            }

            cuiComboBox.AddItem("Показывать всё");
            cuiComboBox.SelectedIndex = cuiComboBox.Items.Length - 1;
        }

        public static void VisualChangedStatus(cuiButton tasksButton, cuiTextBox tasksTextBox, cuiLabel tasksLabelStatus, cuiCalendarDatePicker tasksDatePickerIssue, cuiCalendarDatePicker tasksDatePickerDelivery, DataBase.Tasks item)
        {
            if (item.Status == _taskStatus[0] || item.Status == _taskStatus[1])
            {
                tasksButton.NormalBackground = Color.FromArgb(242, 243, 250);
                tasksButton.NormalOutline = Color.FromArgb(26, 52, 232);
                tasksButton.NormalForeColor = Color.FromArgb(26, 52, 232);
                tasksButton.HoverBackground = Color.FromArgb(235, 235, 244);
                tasksButton.HoverOutline = Color.FromArgb(26, 52, 232);
                tasksButton.HoverForeColor = Color.FromArgb(26, 52, 232);
                tasksButton.PressedBackground = Color.FromArgb(242, 243, 250);
                tasksButton.PressedOutline = Color.FromArgb(26, 52, 232);
                tasksButton.PressedForeColor = Color.FromArgb(26, 52, 232);
                tasksButton.Content = "Начать задание";
                tasksButton.Enabled = true;
            }
            else if (item.Status == _taskStatus[2])
            {
                tasksButton.NormalBackground = Color.FromArgb(26, 52, 232);
                tasksButton.NormalOutline = Color.FromArgb(26, 52, 232);
                tasksButton.NormalForeColor = Color.White;
                tasksButton.HoverBackground = Color.FromArgb(23, 46, 208);
                tasksButton.HoverOutline = Color.FromArgb(23, 46, 208);
                tasksButton.HoverForeColor = Color.White;
                tasksButton.PressedBackground = Color.FromArgb(23, 46, 208);
                tasksButton.PressedOutline = Color.FromArgb(23, 46, 208);
                tasksButton.Content = "Выполнить задание";
                tasksButton.PressedForeColor = Color.White;
                tasksButton.Enabled = true;
            }
            else if (item.Status == _taskStatus[3])
            {
                tasksButton.Content = "Задание выполнено";
                tasksButton.Enabled = false;
            }

            tasksTextBox.Content = item.Text;
            tasksLabelStatus.Content = $"Статус задания: {item.Status}";
            tasksDatePickerIssue.Content = item.DateIssue;
            tasksDatePickerDelivery.Content = item.DateDelivery;
        }

        public static async void ClickTasksButton(cuiButton tasksButton, DataGridView dataGridView, cuiTextBox tasksTextBox, cuiLabel tasksLabelStatus, cuiCalendarDatePicker tasksDatePickerIssue, cuiCalendarDatePicker tasksDatePickerDelivery)
        {
            if (_selectedTask == null)
                return;

            string newStatus = string.Empty;
            if(_selectedTask.Task.Status == _taskStatus[0] || _selectedTask.Task.Status == _taskStatus[1])
                newStatus = _taskStatus[2];
            else if(_selectedTask.Task.Status == _taskStatus[2])
                newStatus = _taskStatus[3];

            try
            {
                string text = tasksButton.Content;
                tasksButton.Content = "Подождите";
                tasksButton.Enabled = false;

                // 1. Обновляем статус в базе данных и в таблице
                await DataBase._client.From<DataBase.Tasks>().Where(t => t.Id == _selectedTask.Task.Id).Set(x => x.Status, newStatus).Update();
                await UpdateSelectedTask(_selectedTask.Task, dataGridView);

                // 2. Обновляем кнопку
                VisualChangedStatus(tasksButton, tasksTextBox, tasksLabelStatus, tasksDatePickerIssue, tasksDatePickerDelivery, _selectedTask.Task);
                tasksButton.Content = text;
                tasksButton.Enabled = true;

                // 3. Показываем уведомление
                string message = string.Empty;
                if (newStatus == _taskStatus[0] || newStatus == _taskStatus[1])
                    message = _taskMessageStatus[0];
                else if (newStatus == _taskStatus[0])
                    message = _taskMessageStatus[1];

                Logger.Info(message);
                Logger.ShowInfo(message);
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка при выполнении задания", ex);
                Logger.ShowError("Ошибка при выполнении задания");
            }
        }

        public static Timer UpdateTasksTimer(cuiPictureBox cuiPictureBox, cuiPictureBox pictureBox)
        {
            var timer = new Timer();
            timer.Interval = _timeUpdateTasks;
            timer.Tick += async (s, e) => await AutoUpdateTasks(cuiPictureBox, pictureBox);
            timer.Start();
            return timer;
        }

        public static async Task MarkAsAcceptedIfNew(DataBase.Tasks item, DataGridView dataGridView, cuiPictureBox cuiPictureBox)
        {
            if (item.Status != _taskStatus[0])
                return;

            // Обновляем статус в БД
            await DataBase._client.From<DataBase.Tasks>().Where(x => x.Id == item.Id).Set(x => x.Status, _taskStatus[1]).Update();

            // Обновляем в таблице
            foreach (DataGridViewRow row in dataGridView.Rows)
            {
                if (row.Cells["Id"].Value != null && Guid.Parse(row.Cells["Id"].Value.ToString()) == item.Id)
                {
                    row.Cells["Status"].Value = _taskStatus[1];
                    break;
                }
            }

            // Проверяем, остались ли ещё "Новые" задания
            var response = await DataBase._client.From<DataBase.Tasks>().Where(x => x.IdEmployee == UserData.CurrentUser.Employee.Id && x.Status == _taskStatus[0]).Get();

            if (response.Models.Count == 0)
                cuiPictureBox.BackgroundImage = Properties.Resources.задачи;
        }

        public class TaskViewModel
        {
            public DataBase.Tasks Task { get; set; }
            public DataBase.Employees Employee { get; set; }
        }
    }
}
