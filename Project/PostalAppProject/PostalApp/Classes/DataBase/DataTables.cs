using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PostalApp
{
    public static class DataTables
    {
        public static void InitializeEditionsTable(DataGridView dataGridView)
        {
            // Настройка столбцов
            dataGridView.AutoGenerateColumns = false;
            dataGridView.Columns.Clear();

            // Добавляем только нужные столбцы
            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Index",
                HeaderText = "Индекс",
                DataPropertyName = "Index",
            });

            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Name",
                HeaderText = "Название",
                DataPropertyName = "Name",
            });

            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "TypeEdition",
                HeaderText = "Тип издания",
                DataPropertyName = "TypeEdition",
            });

            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "MinTermSubscription",
                HeaderText = "Минимальный срок подписки",
                DataPropertyName = "MinTermSubscription",
            });

            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "MinTermHousePrice",
                HeaderText = "Минимальная цена подписки на дом",
                DataPropertyName = "MinTermHousePrice",
            });

            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "MinTermPricePerMailbox",
                HeaderText = "Минимальная цена подписки на почтовый ящик",
                DataPropertyName = "MinTermPricePerMailbox",
            });

            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "MaxTermSubscription",
                HeaderText = "Максимальный срок подписки",
                DataPropertyName = "MaxTermSubscription",
            });

            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "MaxTermHousePrice",
                HeaderText = "Максимальная цена подписки на дом",
                DataPropertyName = "MaxTermHousePrice",
            });

            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "MaxTermPricePerMailbox",
                HeaderText = "Максимальная цена подписки на почтовый ящик",
                DataPropertyName = "MaxTermPricePerMailbox",
            });

            // Скрытые столбцы
            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Id",
                DataPropertyName = "Id",
                Visible = false
            });

            dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        public static void InitializeSubscriptionsTable(DataGridView dataGridView)
        {
            // Настройка столбцов
            dataGridView.AutoGenerateColumns = false;
            dataGridView.Columns.Clear();

            // Добавляем только нужные столбцы
            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "TermSubscription",
                HeaderText = "Срок подписки",
                DataPropertyName = "TermSubscription",
            });

            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "PriceSubscription",
                HeaderText = "Цена подписки",
                DataPropertyName = "PriceSubscription",
            });

            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Kit",
                HeaderText = "Количество комплектов",
                DataPropertyName = "Kit",
            });

            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "DateRegistred",
                HeaderText = "Дата оформления",
                DataPropertyName = "DateRegistred",
            });

            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Edition",
                HeaderText = "Название издания",
                DataPropertyName = "Edition",
            });

            // Скрытые столбцы
            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Id",
                DataPropertyName = "Id",
                Visible = false
            });

            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "IndexEdition",
                DataPropertyName = "IndexEdition",
                Visible = false,
            });

            dataGridView.Columns["DateRegistred"].DefaultCellStyle.Format = "dd.MM.yyyy";

            dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        public static void InitializeReadersTable(DataGridView dataGridView)
        {
            dataGridView.AutoGenerateColumns = false;
            dataGridView.Columns.Clear();

            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "FIO",
                HeaderText = "ФИО",
                DataPropertyName = "FIO",
            });

            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Address",
                HeaderText = "Адрес",
                DataPropertyName = "Address",
            });

            // Скрытые
            dataGridView.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", Visible = false });
            dataGridView.Columns.Add(new DataGridViewTextBoxColumn { Name = "IdMarker", Visible = false });
            dataGridView.Columns.Add(new DataGridViewTextBoxColumn { Name = "IdActiveSubscriptions", Visible = false });

            dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        public static void InitializeTasksTable(DataGridView dataGridView, bool isHideCountMarkers = true)
        {
            // Настройка столбцов
            dataGridView.AutoGenerateColumns = false;
            dataGridView.Columns.Clear();

            // Добавляем только нужные столбцы
            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Number",
                HeaderText = "№",
                DataPropertyName = "Number",
            });

            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "FIO",
                HeaderText = "ФИО сотрудника",
                DataPropertyName = "FIO",
            });

            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Status",
                HeaderText = "Статус",
                DataPropertyName = "Status",
            });

            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "DateIssue",
                HeaderText = "Дата выдачи",
                DataPropertyName = "DateIssue",
            });

            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "DateDelivery",
                HeaderText = "Дата сдачи",
                DataPropertyName = "DateDelivery",
            });

            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "CountMarkers",
                HeaderText = "Меток на карте",
                DataPropertyName = "CountMarkers",
                Visible = !isHideCountMarkers
            });

            // Скрытые столбцы
            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Id",
                DataPropertyName = "Id",
                Visible = false
            });

            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "IdEmployee",
                DataPropertyName = "IdEmployee",
                Visible = false
            });

            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Text",
                DataPropertyName = "Text",
                Visible = false
            });

            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "IdAttachedMarkers",
                DataPropertyName = "IdAttachedMarkers",
                Visible = false
            });

            // Настройка отображения дат
            dataGridView.Columns["DateIssue"].DefaultCellStyle.Format = "dd.MM.yyyy";
            dataGridView.Columns["DateDelivery"].DefaultCellStyle.Format = "dd.MM.yyyy";

            dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        public static void InitializeEmployeesTable(DataGridView dataGridView)
        {
            // Настройка столбцов
            dataGridView.AutoGenerateColumns = false;
            dataGridView.Columns.Clear();

            // Добавляем только нужные столбцы
            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "FIO",
                HeaderText = "ФИО сотрудника",
                DataPropertyName = "FIO",
            });

            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Role",
                HeaderText = "Роль",
                DataPropertyName = "Role",
            });

            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Login",
                HeaderText = "Логин",
                DataPropertyName = "Login",
            });

            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "CreatedAt",
                HeaderText = "Дата регистрации",
                DataPropertyName = "CreatedAt",
            });

            // Скрытые столбцы
            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Id",
                DataPropertyName = "Id",
                Visible = false
            });

            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "IdLogin",
                DataPropertyName = "IdLogin",
                Visible = false
            });

            // Настройка отображения дат
            dataGridView.Columns["CreatedAt"].DefaultCellStyle.Format = "dd.MM.yyyy";

            dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        public static void InitializeAddressTable(DataGridView dataGridView)
        {
            // Настройка столбцов
            dataGridView.AutoGenerateColumns = false;
            dataGridView.Columns.Clear();

            // Добавляем только нужные столбцы
            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Address",
                HeaderText = "Адрес",
                DataPropertyName = "Address",
            });

            // Скрытые столбцы
            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Id",
                DataPropertyName = "Id",
                Visible = false
            });

            dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        public static void AddEditionRow(DataGridView dataGridView, DataBase.Editions item)
        {
            int rowIndex = dataGridView.Rows.Add();
            var row = dataGridView.Rows[rowIndex];

            row.Cells["Id"].Value = item.Id;
            row.Cells["Index"].Value = item.Index;
            row.Cells["Name"].Value = item.Name;
            row.Cells["TypeEdition"].Value = item.TypeEdition;
            row.Cells["MinTermSubscription"].Value = item.MinTermSubscription;
            row.Cells["MinTermHousePrice"].Value = item.MinTermHousePrice;
            row.Cells["MinTermPricePerMailbox"].Value = item.MinTermPricePerMailbox;
            row.Cells["MaxTermSubscription"].Value = item.MaxTermSubscription;
            row.Cells["MaxTermHousePrice"].Value = item.MaxTermHousePrice;
            row.Cells["MaxTermPricePerMailbox"].Value = item.MaxTermPricePerMailbox;
        }

        public static void AddSubscriptionRow(DataGridView dataGridView, DataBase.Subscriptions item)
        {
            int rowIndex = dataGridView.Rows.Add();
            var row = dataGridView.Rows[rowIndex];

            row.Cells["TermSubscription"].Value = item.TermSubscription;
            row.Cells["PriceSubscription"].Value = item.PriceSubscription;
            row.Cells["Kit"].Value = item.Kit;
            row.Cells["DateRegistred"].Value = item.DateRegistred;
            row.Cells["IndexEdition"].Value = item.IndexEdition;
            row.Cells["Id"].Value = item.Id;
        }

        public static void AddReaderTableRow(DataGridView dataGridView, DataBase.Readers item, DataBase.Markers marker = null)
        {
            int rowIndex = dataGridView.Rows.Add();
            var row = dataGridView.Rows[rowIndex];

            row.Cells["FIO"].Value = item.FIO;
            row.Cells["IdActiveSubscriptions"].Value = item.IdActiveSubscriptions;
            row.Cells["Id"].Value = item.Id;

            if (marker != null)
            {
                row.Cells["IdMarker"].Value = marker.Id;

                string address;
                if (!string.IsNullOrWhiteSpace(marker.House))
                    address = $"{marker.Street} {marker.House}".Trim();
                else
                {
                    string extra = "";
                    if (!string.IsNullOrWhiteSpace(marker.Building)) extra += $"к{marker.Building}";
                    if (!string.IsNullOrWhiteSpace(marker.Apartment)) extra += $"/{marker.Apartment}";
                    address = $"{marker.Street} {extra}".Trim();
                }
                row.Cells["Address"].Value = address;
            }
            else
            {
                row.Cells["IdMarker"].Value = "";
                row.Cells["Address"].Value = "—";
            }
        }

        public static void AddStreetRow(DataGridView dataGridView, DataBase.Markers item)
        {
            int rowIndex = dataGridView.Rows.Add();
            var row = dataGridView.Rows[rowIndex];

            if (item.Apartment != string.Empty)
                row.Cells["Address"].Value = $"{item.Street} {item.Apartment}";
            else if (item.Building != string.Empty)
                row.Cells["Address"].Value = $"{item.Street} {item.Building}";
            else if (item.House != string.Empty)
                row.Cells["Address"].Value = $"{item.Street} {item.House}";
            else
                row.Cells["Address"].Value = $"{item.Street} {item.TypeBuilding}";
            row.Cells["Id"].Value = item.Id;
        }

        public static async Task AddTaskRow(DataGridView dataGridView, DataBase.Tasks item, int num, int countMarkers = -1, bool isSkipTwoViewStatus = true)
        {
            int rowIndex = dataGridView.Rows.Add();
            var row = dataGridView.Rows[rowIndex];

            var employee = await DataBase._client.From<DataBase.Employees>().Where(x => x.Id == item.IdEmployee).Single();
            if (employee == null)
                return;

            row.Cells["Id"].Value = item.Id;
            row.Cells["IdEmployee"].Value = employee.Id;
            row.Cells["Number"].Value = $"№{num}";
            row.Cells["FIO"].Value = employee.FIO;
            if (isSkipTwoViewStatus && item.Status == TaskOnEmployee._taskStatus[2])
                row.Cells["Status"].Value = TaskOnEmployee._taskStatus[1];
            else
                row.Cells["Status"].Value = item.Status;
            if (countMarkers != -1)
                row.Cells["CountMarkers"].Value = $"{countMarkers} {PostmanForm.GetForm(countMarkers, "точка", "точки", "точек")}";
            row.Cells["Text"].Value = item.Text;
            row.Cells["DateIssue"].Value = item.DateIssue;
            row.Cells["DateDelivery"].Value = item.DateDelivery;
        }

        public static async Task AddEmployeeRow(DataGridView dataGridView, DataBase.Employees item)
        {
            int rowIndex = dataGridView.Rows.Add();
            var row = dataGridView.Rows[rowIndex];

            var login = await DataBase._client.From<DataBase.Login>().Where(x => x.Id == item.IdLogin).Single();
            if (login == null)
                return;

            row.Cells["Id"].Value = item.Id;
            row.Cells["FIO"].Value = item.FIO;
            row.Cells["Role"].Value = item.Role;
            row.Cells["Login"].Value = login.Email;
            row.Cells["CreatedAt"].Value = item.CreatedAt;
            row.Cells["IdLogin"].Value = item.IdLogin;
        }
    }
}
