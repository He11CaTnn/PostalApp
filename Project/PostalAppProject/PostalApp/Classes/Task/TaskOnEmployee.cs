using CuoreUI.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PostalApp
{
    public static class TaskOnEmployee
    {
        private static int _timeUpdateTasks = 5000;
        private static bool _isTaskDashboardAssign = false;

        public static List<TaskViewModel> _tasks = new List<TaskViewModel>();
        public static TaskViewModel _selectedTask;
        public static string[] _taskStatus = { "Не выполнено", "Новое", "Просмотренное", "В работе", "Готово" };
        public static string[] _taskMessageStatus = { "Задание в начато!", "Задание выполнено!" };

        private static cuiLabel taskLabelCompleteThis;
        private static cuiLabel taskLabelNewThis;
        private static cuiLabel taskLabelFailThis;
        private static cuiLabel taskLabelWorkThis;
        private static cuiCircleProgressBar taskProgressBarNewThis;
        private static cuiLabel taskLabelPercentNewThis;
        private static SearchFilter<DataBase.Tasks> taskSearchFilterThis;
        private static cuiCalendarDatePicker taskDatePickerIssueMinThis;
        private static cuiCalendarDatePicker taskDatePickerIssueMaxThis;

        private static List<string> _newTasks = new List<string>();

        private static async Task AutoUpdateTasks()
        {
            try
            {
                var response = await DataBase._client.From<DataBase.Tasks>().Where(x => x.IdEmployee == UserData.CurrentUser.Employee.Id && x.Status == _taskStatus[1]).Get();

                if (_isTaskDashboardAssign)
                    await UpdateTaskDashboard(taskLabelCompleteThis, taskLabelNewThis, taskLabelFailThis,
                        taskLabelWorkThis, taskProgressBarNewThis, taskLabelPercentNewThis,
                        taskSearchFilterThis, taskDatePickerIssueMinThis, taskDatePickerIssueMaxThis);

                /*Bitmap originalBitmap = response.Models.Count > 0 ? Properties.Resources.НовыеЗадания : Properties.Resources.ВыполненныеЗадания;
                cuiPictureBox.BackgroundImage = originalBitmap;
                pictureBox.Visible = false;*/

                if (!DataBase._internetConnection)
                {
                    Logger.Info($"Подключение к интернету востановлено");
                    DataBase._internetConnection = true;
                }
                if (response.Models.Count > 0)
                {
                    int count = 0;

                    for (int i = 0; i < _newTasks.Count; i++)
                    {
                        if (response.Models[i].Id.ToString() == _newTasks[i])
                        {
                            count++;
                            _newTasks.Add(response.Models[i].Id.ToString());
                        }
                    }

                    Logger.Info($"Обнаружено {count} новых заданий");
                }
            }
            catch
            {
                //pictureBox.Visible = true;
                DataBase._internetConnection = false;
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

        private static async Task UpdateTaskDashboard(cuiLabel taskLabelComplete, cuiLabel taskLabelWork, cuiLabel taskLabelNew, 
            cuiLabel taskLabelFail, cuiCircleProgressBar taskProgressBarNew, cuiLabel taskLabelPercentNew,
            SearchFilter<DataBase.Tasks> taskSearchFilter, cuiCalendarDatePicker taskDatePickerIssueMin, cuiCalendarDatePicker taskDatePickerIssueMax)
        {
            var allTasksResponse = await DataBase._client.From<DataBase.Tasks>().Where(x => x.IdEmployee == UserData.CurrentUser.Employee.Id).Get();

            var allTasks = allTasksResponse.Models;

            var completeTasks = allTasks.Where(x => x.Status == _taskStatus[4]).ToList();
            var newTasks = allTasks.Where(x => x.Status == _taskStatus[1]).ToList();
            var viewTasks = allTasks.Where(x => x.Status == _taskStatus[2]).ToList();
            var workTasks = allTasks.Where(x => x.Status == _taskStatus[3]).ToList();
            var failTasks = allTasks.Where(x => x.Status == _taskStatus[0]).ToList();

            string newCompleteText = completeTasks.Count.ToString();
            if (!Equals(taskLabelComplete.Content, newCompleteText))
                taskLabelComplete.Content = newCompleteText;

            string newNewText = (newTasks.Count + viewTasks.Count).ToString();
            if (!Equals(taskLabelNew.Content, newNewText))
                taskLabelNew.Content = newNewText;

            string newWorkText = workTasks.Count.ToString();
            if (!Equals(taskLabelWork.Content, newWorkText))
                taskLabelWork.Content = newWorkText;

            string newFailText = failTasks.Count.ToString();
            if (!Equals(taskLabelFail.Content, newFailText))
                taskLabelFail.Content = newFailText;

            int totalForPercent = completeTasks.Count + failTasks.Count;
            int percent = totalForPercent > 0 ? (completeTasks.Count * 100) / totalForPercent : 0;

            if (taskProgressBarNew.ProgressValue != percent)
                taskProgressBarNew.ProgressValue = percent;

            string newPercentText = $"{percent}%";
            if (!Equals(taskLabelPercentNew.Content, newPercentText))
                taskLabelPercentNew.Content = newPercentText;

            if (!taskSearchFilter.IsActive && allTasks.Count > 0)
            {
                var newMinDate = allTasks.Min(x => x.DateIssue);
                var newMaxDate = allTasks.Max(x => x.DateIssue);

                if (!Equals(taskDatePickerIssueMin.Content, newMinDate))
                    taskDatePickerIssueMin.Content = newMinDate;

                if (!Equals(taskDatePickerIssueMax.Content, newMaxDate))
                    taskDatePickerIssueMax.Content = newMaxDate;
            }
        }

        public static void AssignTaskDashboard(cuiLabel taskLabelComplete, cuiLabel taskLabelWork, cuiLabel taskLabelNew,
            cuiLabel taskLabelFail, cuiCircleProgressBar taskProgressBarNew, cuiLabel taskLabelPercentNew,
            SearchFilter<DataBase.Tasks> taskSearchFilter, cuiCalendarDatePicker taskDatePickerIssueMin, cuiCalendarDatePicker taskDatePickerIssueMax)
        {
            if (taskLabelComplete != null && taskLabelNew != null && taskLabelFail != null &&
                taskLabelWork != null && taskProgressBarNew != null && taskLabelPercentNew != null &&
                taskSearchFilter != null && taskDatePickerIssueMin != null && taskDatePickerIssueMax != null)
            {
                taskLabelCompleteThis = taskLabelComplete;
                taskLabelNewThis = taskLabelNew;
                taskLabelFailThis = taskLabelFail;
                taskLabelWorkThis = taskLabelWork;
                taskProgressBarNewThis = taskProgressBarNew;
                taskLabelPercentNewThis = taskLabelPercentNew;
                taskSearchFilterThis = taskSearchFilter;
                taskDatePickerIssueMinThis = taskDatePickerIssueMin;
                taskDatePickerIssueMaxThis = taskDatePickerIssueMax;
            }

            _isTaskDashboardAssign = true;
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

        public static void VisualChangedStatus(cuiPanel tasksButton, cuiLabel taskLabelButton, DataBase.Tasks item,
            cuiProgressTrackerHorizontal tasksProgressStatus, cuiLabel taskLabelFio, cuiLabel tasksLabelDateIssue,
            cuiLabel tasksLabelDateDelivery, cuiTextBox tasksTextBox, cuiLabel tsakLabelCountMarkers = null)
        {
            if (item.Status == _taskStatus[1] || item.Status == _taskStatus[2])
            {
                taskLabelButton.Content = "Начать";
                tasksButton.Enabled = true;
            }
            else if (item.Status == _taskStatus[3])
            {
                taskLabelButton.Content = "Завершить";
                tasksButton.Enabled = true;
            }
            else if (item.Status == _taskStatus[4])
            {
                taskLabelButton.Content = "Выполнено";
                tasksButton.Enabled = false;
            }

            for (int i = 1; i < _taskStatus.Length; i++)
            {
                if (item.Status == _taskStatus[i + 1])
                {
                    tasksProgressStatus.TasksProgress = i;
                    break;
                }
            }
            taskLabelFio.Content = taskLabelFio.Text;
            if (tsakLabelCountMarkers != null)
                tsakLabelCountMarkers.Content = $"{item.AttachedMarkers.Split(',').Length} {PostmanForm.GetForm(item.AttachedMarkers.Split(',').Length, "точка", "точки", "точек")}";
            tasksLabelDateIssue.Content = item.DateIssue.ToString();
            tasksLabelDateDelivery.Content = item.DateDelivery.ToString();
            tasksTextBox.Content = item.Text;
        }

        public static async void ClickTasksButton(cuiPanel tasksButton, cuiLabel taskLabelButton, DataGridView taskDataGridView,
            cuiProgressTrackerHorizontal tasksProgressStatus, cuiLabel taskLabelFio, cuiLabel tasksLabelDateIssue,
            cuiLabel tasksLabelDateDelivery, cuiTextBox tasksTextBox, cuiLabel tsakLabelCountMarkers = null)
        {
            if (_selectedTask == null)
                return;

            string newStatus = string.Empty;
            if (_selectedTask.Task.Status == _taskStatus[1] || _selectedTask.Task.Status == _taskStatus[2])
                newStatus = _taskStatus[3];
            else if (_selectedTask.Task.Status == _taskStatus[3])
                newStatus = _taskStatus[4];

            try
            {
                string text = taskLabelButton.Content;
                tasksButton.Enabled = false;
                taskLabelButton.Content = "Подождите";

                // 1. Обновляем статус в базе данных и в таблице
                await DataBase._client.From<DataBase.Tasks>().Where(t => t.Id == _selectedTask.Task.Id).Set(x => x.Status, newStatus).Update();
                await UpdateSelectedTask(_selectedTask.Task, taskDataGridView);

                // 2. Обновляем кнопку
                VisualChangedStatus(tasksButton, taskLabelButton, _selectedTask.Task,
                    tasksProgressStatus, taskLabelFio, tasksLabelDateIssue,
                    tasksLabelDateDelivery, tasksTextBox, tsakLabelCountMarkers);
                tasksButton.Enabled = true;
                taskLabelButton.Content = text;

                // 3. Показываем уведомление
                string message = string.Empty;
                if (newStatus == _taskStatus[1] || newStatus == _taskStatus[2])
                    message = _taskMessageStatus[0];
                else if (newStatus == _taskStatus[3])
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

        public static void UpdateTasksTimer(Timer timer)
        {
            timer = new Timer();
            timer.Interval = _timeUpdateTasks;
            timer.Tick += async (s, e) => await AutoUpdateTasks();
            timer.Start();
        }

        public static async Task MarkAsAcceptedIfNew(DataBase.Tasks item)
        {
            if (item.Status != _taskStatus[1])
                return;

            // Обновляем статус в БД
            await DataBase._client.From<DataBase.Tasks>().Where(x => x.Id == item.Id).Set(x => x.Status, _taskStatus[2]).Update();

            // Проверяем, остались ли ещё "Новые" задания
            var response = await DataBase._client.From<DataBase.Tasks>().Where(x => x.IdEmployee == UserData.CurrentUser.Employee.Id && x.Status == _taskStatus[1]).Get();

            /*if (response.Models.Count == 0)
                cuiPictureBox.BackgroundImage = Properties.Resources.ВыполненныеЗадания;*/
        }

        public class TaskViewModel
        {
            public DataBase.Tasks Task { get; set; }
            public DataBase.Employees Employee { get; set; }
        }
    }
}
