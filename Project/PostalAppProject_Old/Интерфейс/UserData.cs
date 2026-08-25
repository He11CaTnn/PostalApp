using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;

namespace Интерфейс
{
    public static class UserData
    {
        public static DataBase.Employees _selectedEmployee;
        public static DataBase.Login _selectedLogin;
        public static string _originalLogin = "";
        public static List<string> _allRoles = new List<string>()
            { "Почтальон", "Оператор", "Руководитель подписок", "Директор" };

        // URL сервиса для получения внешнего IP и местоположения
        private const string IpApiUrl = "http://ip-api.com/json";

        // ===================================================================
        // ПРОВЕРКА ПОЛЬЗОВАТЕЛЯ В БД (ручной вход)
        // ===================================================================

        /// <summary>
        /// Проверяет email и пароль по таблице логинов.
        /// Загружает данные сотрудника в CurrentUser.
        /// НЕ открывает никаких форм — только авторизует.
        /// Возвращает true при успехе, false при неверных данных.
        /// </summary>
        public static async Task<bool> VerifyUser(string email, string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                    return false;

                var loginResponse = await DataBase._client.From<DataBase.Login>()
                    .Where(x => x.Email == email).Get();
                var loginData = loginResponse.Models.FirstOrDefault();

                if (loginData == null)
                {
                    Logger.Warning("Проверка пользователя: email не найден");
                    return false;
                }

                if (!PasswordHasher.VerifyPassword(password, loginData.Password))
                {
                    Logger.Warning("Проверка пользователя: неверный пароль");
                    return false;
                }

                var employeeResponse = await DataBase._client.From<DataBase.Employees>()
                    .Where(x => x.IdLogin == loginData.Id).Get();
                var employee = employeeResponse.Models.FirstOrDefault();

                if (employee == null)
                {
                    Logger.Warning("Проверка пользователя: сотрудник не найден");
                    return false;
                }

                CurrentUser.Employee = employee;
                CurrentUser.Login = loginData;

                var regionsResponse = await DataBase._client.From<DataBase.Regions>()
                    .Where(x => x.IdEmployee == employee.Id).Get();
                CurrentUser.RegionIds = regionsResponse.Models.Select(r => r.Id).ToList();

                Logger.Info($"Проверка пользователя: успех — {employee.FIO} ({employee.Role})");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка при проверке пользователя", ex);
                return false;
            }
        }

        // ===================================================================
        // АВТОВХОД ПО ID МАТЕРИНСКОЙ ПЛАТЫ
        // ===================================================================

        /// <summary>
        /// Загружает данные сотрудника по ID материнской платы из таблицы Устройства.
        /// Ищет только записи с PermanentAccess = true.
        /// Возвращает true при успехе.
        /// </summary>
        public static async Task<bool> LoadUserByMotherboardId(string motherboardId)
        {
            try
            {
                if (string.IsNullOrEmpty(motherboardId))
                    return false;

                var devResponse = await DataBase._client.From<DataBase.Devaices>()
                    .Where(x => x.IdMotherboard == motherboardId && x.PermanentAccess == true)
                    .Get();
                var device = devResponse.Models?.FirstOrDefault();

                if (device == null)
                {
                    Logger.Warning("LoadUserByMotherboardId: устройство не найдено или PermanentAccess = false");
                    return false;
                }

                if (!Guid.TryParse(device.IdEmployee, out Guid employeeId))
                {
                    Logger.Error("LoadUserByMotherboardId: некорректный IdEmployee: " + device.IdEmployee);
                    return false;
                }

                var empResponse = await DataBase._client.From<DataBase.Employees>()
                    .Where(x => x.Id == employeeId).Get();
                var employee = empResponse.Models?.FirstOrDefault();

                if (employee == null)
                {
                    Logger.Warning("LoadUserByMotherboardId: сотрудник не найден");
                    return false;
                }

                var loginResponse = await DataBase._client.From<DataBase.Login>()
                    .Where(x => x.Id == employee.IdLogin).Get();
                var login = loginResponse.Models?.FirstOrDefault();

                CurrentUser.Employee = employee;
                CurrentUser.Login = login;

                var regionsResponse = await DataBase._client.From<DataBase.Regions>()
                    .Where(x => x.IdEmployee == employee.Id).Get();
                CurrentUser.RegionIds = regionsResponse.Models.Select(r => r.Id).ToList();

                Logger.Info($"Автовход по устройству: {employee.FIO} ({employee.Role})");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка при загрузке пользователя по ID устройства", ex);
                return false;
            }
        }

        // ===================================================================
        // РЕГИСТРАЦИЯ / ОБНОВЛЕНИЕ УСТРОЙСТВА
        // Вызывается при КАЖДОМ ручном входе, независимо от галочки RememberMe.
        // PermanentAccess = true если галочка стоит, false если нет.
        // ===================================================================

        /// <summary>
        /// Создаёт или обновляет запись об устройстве в таблице Устройства.
        /// Проверяет наличие записи по motherboardId:
        ///   — если есть → обновляет IdEmployee, Ip, NameDevaice, PermanentAccess, LastEntry
        ///   — если нет  → создаёт новую запись
        /// LastEntry записывается с точностью до секунды.
        /// </summary>
        public static async Task RegisterOrUpdateDevice(string motherboardId, bool permanentAccess)
        {
            try
            {
                if (string.IsNullOrEmpty(motherboardId))
                {
                    Logger.Warning("RegisterOrUpdateDevice: ID материнской платы не получен");
                    return;
                }

                var employee = CurrentUser.Employee;
                if (employee == null) return;

                var (externalIp, location) = await GetExternalIpInfoAsync();
                string hostname = Dns.GetHostName();
                DateTime now = TruncateToSeconds(DateTime.UtcNow);

                var existingResponse = await DataBase._client.From<DataBase.Devaices>()
                    .Where(x => x.IdMotherboard == motherboardId)
                    .Get();
                var existing = existingResponse.Models?.FirstOrDefault();

                if (existing == null)
                {
                    // Новое устройство
                    var newDevice = new DataBase.Devaices
                    {
                        Id = Guid.NewGuid(),
                        IdEmployee = employee.Id.ToString(),
                        Ip = externalIp,
                        IdMotherboard = motherboardId,
                        NameDevaice = hostname,
                        Location = location,
                        PermanentAccess = permanentAccess,
                        LastEntry = now
                    };
                    await DataBase._client.From<DataBase.Devaices>().Insert(newDevice);
                    Logger.Info($"Устройство '{hostname}' зарегистрировано " +
                                $"(IP={externalIp}, Location={location}, PermanentAccess={permanentAccess}, LastEntry={now:yyyy-MM-dd HH:mm:ss})");
                }
                else
                {
                    // Обновляем существующую запись
                    await DataBase._client.From<DataBase.Devaices>()
                        .Where(x => x.IdMotherboard == motherboardId)
                        .Set(x => x.IdEmployee, employee.Id.ToString())
                        .Set(x => x.Ip, externalIp)
                        .Set(x => x.NameDevaice, hostname)
                        .Set(x => x.Location, location)
                        .Set(x => x.PermanentAccess, permanentAccess)
                        .Set(x => x.LastEntry, now)
                        .Update();
                    Logger.Info($"Устройство '{hostname}' обновлено " +
                                $"(IP={externalIp}, Location={location}, PermanentAccess={permanentAccess}, LastEntry={now:yyyy-MM-dd HH:mm:ss})");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка при регистрации/обновлении устройства", ex);
            }
        }

        // ===================================================================
        // ОБНОВЛЕНИЕ ДАННЫХ УСТРОЙСТВА (без изменения PermanentAccess)
        // Вызывается после автовхода — фиксирует факт входа.
        // ===================================================================

        /// <summary>
        /// Обновляет Ip, NameDevaice, Location и LastEntry для устройства с данным motherboardId.
        /// PermanentAccess НЕ изменяет. Вызывается после успешного автовхода.
        /// LastEntry записывается с точностью до секунды.
        /// </summary>
        public static async Task UpdateDeviceInfo(string motherboardId)
        {
            try
            {
                if (string.IsNullOrEmpty(motherboardId))
                    return;

                var (externalIp, location) = await GetExternalIpInfoAsync();
                string hostname = Dns.GetHostName();
                DateTime now = TruncateToSeconds(DateTime.UtcNow);

                await DataBase._client.From<DataBase.Devaices>()
                    .Where(x => x.IdMotherboard == motherboardId)
                    .Set(x => x.Ip, externalIp)
                    .Set(x => x.NameDevaice, hostname)
                    .Set(x => x.Location, location)
                    .Set(x => x.LastEntry, now)
                    .Update();

                Logger.Info($"Устройство '{hostname}': данные обновлены (IP={externalIp}, Location={location}, LastEntry={now:yyyy-MM-dd HH:mm:ss})");
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка при обновлении данных устройства", ex);
            }
        }

        // ===================================================================
        // ПРОВЕРКА: ЗАРЕГИСТРИРОВАНО ЛИ ТЕКУЩЕЕ УСТРОЙСТВО
        // ===================================================================

        /// <summary>
        /// Проверяет наличие записи с указанным motherboardId в таблице Устройства.
        /// Не смотрит на PermanentAccess — только факт наличия записи.
        /// Возвращает true если запись найдена, false если нет.
        /// Требует активного подключения DataBase._client.
        /// </summary>
        public static async Task<bool> IsDeviceRegistered(string motherboardId)
        {
            try
            {
                if (string.IsNullOrEmpty(motherboardId))
                    return false;

                var response = await DataBase._client.From<DataBase.Devaices>()
                    .Where(x => x.IdMotherboard == motherboardId)
                    .Get();

                bool found = response.Models != null && response.Models.Count > 0;
                Logger.Debug($"IsDeviceRegistered ({motherboardId}): {found}");
                return found;
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка при проверке регистрации устройства", ex);
                return false;
            }
        }

        // ===================================================================
        // ОТКРЫТИЕ ФОРМЫ ПО РОЛИ
        // ===================================================================

        public static void OpenRoleForm(DataBase.Employees employee, Form callerForm)
        {
            switch (employee.Role.ToLower())
            {
                case "почтальон":
                    var pf = new postmanForm();
                    Logger.Info($"Переход сотрудника {employee.FIO} на форму почтальона");
                    pf.Show();
                    callerForm.Hide();
                    break;

                case "оператор":
                    var of = new operatorForm();
                    Logger.Info($"Переход сотрудника {employee.FIO} на форму оператора");
                    of.Show();
                    callerForm.Hide();
                    break;

                case "руководитель подписок":
                    var mf = new managerForm();
                    Logger.Info($"Переход сотрудника {employee.FIO} на форму руководителя подписок");
                    mf.Show();
                    callerForm.Hide();
                    break;

                case "директор":
                    var df = new directorForm();
                    Logger.Info($"Переход сотрудника {employee.FIO} на форму директора");
                    df.Show();
                    callerForm.Hide();
                    break;

                default:
                    Logger.Warning($"Неизвестная роль: {employee.Role} у {employee.FIO}");
                    Logger.ShowWarning($"У вас указана неизвестная роль: {employee.Role}");
                    break;
            }
        }

        // ===================================================================
        // ВЫХОД ИЗ АККАУНТА
        // ===================================================================

        public static async Task LogoutAndExit(Form form)
        {
            CurrentUser.Clear();

            var startup = new StartupForm();
            startup.Show();
            form.Close();

            Logger.Info("Выход из аккаунта, переход на стартовый экран");
            await Task.CompletedTask;
        }

        // ===================================================================
        // РЕГИСТРАЦИЯ НОВОГО СОТРУДНИКА
        // ===================================================================

        public static async void Register(
            string email, string password, string fio, string role,
            DataGridView dataGridView = null,
            System.Collections.Generic.HashSet<Guid> locallyAddedEmployeesIds = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email) ||
                    string.IsNullOrWhiteSpace(password) ||
                    string.IsNullOrWhiteSpace(fio) ||
                    string.IsNullOrWhiteSpace(role))
                {
                    Logger.ShowWarning("Заполните все поля");
                    return;
                }

                var existingUser = await DataBase._client.From<DataBase.Login>()
                    .Where(x => x.Email == email).Get();

                if (existingUser.Models != null && existingUser.Models.Any())
                {
                    Logger.ShowWarning("Пользователь с таким email уже существует");
                    return;
                }

                string hashedPassword = PasswordHasher.HashPassword(password);

                var loginRecord = new DataBase.Login { Email = email, Password = hashedPassword };
                var loginResult = await DataBase._client.From<DataBase.Login>().Insert(loginRecord);
                var createdLogin = loginResult.Models?.FirstOrDefault();

                if (createdLogin == null)
                {
                    Logger.Error("Ошибка при создании учётной записи");
                    Logger.ShowError("Ошибка при создании учётной записи");
                    return;
                }

                var employeeRecord = new DataBase.Employees
                {
                    Id = Guid.NewGuid(),
                    FIO = fio,
                    Role = role,
                    IdLogin = createdLogin.Id,
                    CreatedAt = DateTime.UtcNow
                };

                var employeeResult = await DataBase._client.From<DataBase.Employees>()
                    .Insert(employeeRecord);

                if (employeeResult.Models == null || !employeeResult.Models.Any())
                {
                    await DataBase._client.From<DataBase.Login>()
                        .Where(x => x.Id == createdLogin.Id).Delete();
                    Logger.ShowError("Ошибка при создании сотрудника");
                    return;
                }

                if (dataGridView != null && locallyAddedEmployeesIds != null)
                {
                    await DataTables.AddEmployeeRow(dataGridView, employeeResult.Model);
                    locallyAddedEmployeesIds.Add(employeeResult.Model.Id);
                }

                Logger.Info($"Зарегистрирован сотрудник {employeeResult.Model.FIO}");
                Logger.ShowInfo("Регистрация прошла успешно");
            }
            catch (Exception ex)
            {
                Logger.Critical("Ошибка при регистрации", ex);
                Logger.ShowCritical("Ошибка при регистрации учётной записи");
            }
        }

        // ===================================================================
        // ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ
        // ===================================================================

        /// <summary>Убирает миллисекунды из DateTime (точность до секунды).</summary>
        private static DateTime TruncateToSeconds(DateTime dt) =>
            new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, DateTimeKind.Utc);

        /// <summary>
        /// Получает внешний IP-адрес и местоположение через ip-api.com.
        /// Возвращает (ip, location) — например ("95.31.12.45", "Москва, Россия").
        /// При недоступности сервиса возвращает ("unknown", "").
        /// </summary>
        private static async Task<(string Ip, string Location)> GetExternalIpInfoAsync()
        {
            try
            {
                using (var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) })
                {
                    string json = await http.GetStringAsync(IpApiUrl);
                    var obj = JObject.Parse(json);

                    string ip = obj["query"]?.ToString() ?? "unknown";
                    string city = obj["city"]?.ToString() ?? "";
                    string country = obj["country"]?.ToString() ?? "";

                    string location = (city.Length > 0 && country.Length > 0)
                        ? $"{city}, {country}"
                        : country.Length > 0 ? country : "";

                    return (ip, location);
                }
            }
            catch
            {
                return ("unknown", "");
            }
        }

        // ===================================================================
        // ТЕКУЩИЙ ПОЛЬЗОВАТЕЛЬ
        // ===================================================================

        public static class CurrentUser
        {
            public static DataBase.Employees Employee { get; set; }
            public static DataBase.Login Login { get; set; }
            public static List<Guid> RegionIds { get; set; } = new List<Guid>();

            public static void Clear()
            {
                Employee = null;
                Login = null;
                RegionIds.Clear();
            }
        }
    }
}