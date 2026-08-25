using System;
using System.Threading.Tasks;

namespace Интерфейс
{
    /// <summary>
    /// Точка доступа к базе данных PostgreSQL.
    /// Содержит клиент и все модели таблиц.
    /// </summary>
    public static class DataBase
    {
        public static PgClient _client;

        // ===================================================================
        // async-метод — используется в StartupForm.
        // Бросает исключение при ошибке (форма сама показывает кнопку Retry).
        // ===================================================================

        public static async Task TryConnectAsync()
        {
            // Создаём клиент
            _client = CreateClient();

            // Пробуем реальный запрос, чтобы убедиться, что соединение живое
            await _client.From<Test>().Get();

            Logger.Info("Подключение к базе данных установлено");
        }

        // ===================================================================
        // Вспомогательный метод: собирает строку подключения и создаёт клиент
        // ===================================================================

        private static PgClient CreateClient()
        {
            string connStr =
                $"Host={Program.ServerIP};"      +
                $"Port={Program.ServerPort};"    +
                $"Database={Program.ServerDatabase};" +
                $"Username={Program.ServerUser};"    +
                $"Password={Program.ServerPassword};" +
                "Encoding=UTF8;"                 +
                "Client Encoding=UTF8;";

            return new PgClient(connStr);
        }

        // ===================================================================
        // Таблица: Сотрудники
        // ===================================================================

        [DbTable("Сотрудники")]
        public class Employees
        {
            [DbColumn("id", IsPrimaryKey = true)]   // id
            public Guid Id { get; set; }

            [DbColumn("ФИО")]                        // ФИО
            public string FIO { get; set; }

            [DbColumn("Роль")]                       // Роль
            public string Role { get; set; }

            [DbColumn("Id логина")]                  // Id логина
            public Guid IdLogin { get; set; }

            [DbColumn("Дата создания")]              // Дата создания
            public DateTime CreatedAt { get; set; }

            [DbColumn("Устройства")]                 // Устройства
            public string[] Devices { get; set; }
        }

        // ===================================================================
        // Таблица: Логин
        // ===================================================================

        [DbTable("Логин")]
        public class Login
        {
            [DbColumn("id", IsPrimaryKey = true)]   // id
            public Guid Id { get; set; }

            [DbColumn("Почта")]                      // Почта
            public string Email { get; set; }

            [DbColumn("Пароль")]                     // Пароль
            public string Password { get; set; }
        }

        // ===================================================================
        // Таблица: Метки
        // ===================================================================

        [DbTable("Метки")]
        public class Markers
        {
            [DbColumn("id", IsPrimaryKey = true)]   // id
            public Guid Id { get; set; }

            [DbColumn("Широта")]                     // Широта
            public double Latitude { get; set; }

            [DbColumn("Долгота")]                    // Долгота
            public double Longitude { get; set; }

            [DbColumn("Тип здания")]                 // Тип здания
            public string TypeBuilding { get; set; }

            [DbColumn("Улица")]                      // Улица
            public string Street { get; set; }

            [DbColumn("Дом")]                        // Дом
            public string House { get; set; }

            [DbColumn("Корпус")]                     // Корпус
            public string Building { get; set; }

            [DbColumn("Квартира")]                   // Квартира
            public string Apartment { get; set; }

            [DbColumn("Id участка")]                 // Id участка
            public Guid IdRegion { get; set; }

            [DbColumn("Id читателей")]               // Id читателей
            public string IdReaders { get; set; }
        }

        // ===================================================================
        // Таблица: Участки
        // ===================================================================

        [DbTable("Участки")]
        public class Regions
        {
            [DbColumn("id", IsPrimaryKey = true)]   // id
            public Guid Id { get; set; }

            [DbColumn("Название")]                   // Название
            public string Name { get; set; }

            [DbColumn("Цвет")]                       // Цвет
            public string Color { get; set; }

            [DbColumn("Id сотрудника")]              // Id сотрудника
            public Guid IdEmployee { get; set; }
        }

        // ===================================================================
        // Таблица: Узлы
        // ===================================================================

        [DbTable("Узлы")]
        public class Nodes
        {
            [DbColumn("id", IsPrimaryKey = true)]   // id
            public Guid Id { get; set; }

            [DbColumn("Широта")]                    // Широта
            public double Latitude { get; set; }

            [DbColumn("Долгота")]                    // Долгота
            public double Longitude { get; set; }

            [DbColumn("Id участка")]                 // Id участка
            public Guid IdRegion { get; set; }

            [DbColumn("Номер")]                      // Номер
            public int Number { get; set; }
        }

        // ===================================================================
        // Таблица: Задания
        // ===================================================================

        [DbTable("Задания")]
        public class Tasks
        {
            [DbColumn("id", IsPrimaryKey = true)]   // id
            public Guid Id { get; set; }

            [DbColumn("Id сотрудника")]              // Id сотрудника
            public Guid IdEmployee { get; set; }

            [DbColumn("Текст")]                      // Текст
            public string Text { get; set; }

            [DbColumn("Статус")]                     // Статус
            public string Status { get; set; }

            [DbColumn("Дата выдачи")]                // Дата выдачи
            public DateTime DateIssue { get; set; }

            [DbColumn("Дата завершения")]            // Дата завершения
            public DateTime DateDelivery { get; set; }

            [DbColumn("Прикрепленные метки")]        // Прикрепленные метки
            public string AttachedMarkers { get; set; }
        }

        // ===================================================================
        // Таблица: Издания
        // ===================================================================

        [DbTable("Издания")]
        public class Editions
        {
            [DbColumn("id", IsPrimaryKey = true)]                    // id
            public Guid Id { get; set; }

            [DbColumn("Индекс")]                                     // Индекс
            public string Index { get; set; }

            [DbColumn("Название")]                                   // Название
            public string Name { get; set; }

            [DbColumn("Тип издания")]                                // Тип издания
            public string TypeEdition { get; set; }

            [DbColumn("Минимальный срок подписки")]                  // Минимальный срок подписки
            public float MinTermSubscription { get; set; }

            [DbColumn("Минимальная цена подписки на дом")]           // Минимальная цена подписки на дом
            public float MinTermHousePrice { get; set; }

            [DbColumn("Минимальная цена подписки на почтовый ящик")] // Минимальная цена подписки на почтовый ящик
            public float MinTermPricePerMailbox { get; set; }

            [DbColumn("Максимальный срок подписки")]                 // Максимальный срок подписки
            public float MaxTermSubscription { get; set; }

            [DbColumn("Максимальная цена подписки на дом")]          // Максимальная цена подписки на дом
            public float MaxTermHousePrice { get; set; }

            [DbColumn("Максимальная цена подписки на почтовый ящик")] // Максимальная цена подписки на почтовый ящик
            public float MaxTermPricePerMailbox { get; set; }
        }

        // ===================================================================
        // Таблица: Подписки
        // ===================================================================

        [DbTable("Подписки")]
        public class Subscriptions
        {
            [DbColumn("id", IsPrimaryKey = true)]    // id
            public Guid Id { get; set; }

            [DbColumn("Срок подписки")]              // Срок подписки
            public string TermSubscription { get; set; }

            [DbColumn("Цена подписки")]              // Цена подписки
            public string PriceSubscription { get; set; }

            [DbColumn("Комплекты")]                  // Комплекты
            public int Kit { get; set; }

            [DbColumn("Дата оформления")]            // Дата оформления
            public DateTime DateRegistred { get; set; }

            [DbColumn("Id издания")]                 // Id издания
            public string IndexEdition { get; set; }
        }

        // ===================================================================
        // Таблица: Читатели
        // ===================================================================

        [DbTable("Читатели")]
        public class Readers
        {
            [DbColumn("id", IsPrimaryKey = true)]           // id
            public Guid Id { get; set; }

            [DbColumn("ФИО")]                               // ФИО
            public string FIO { get; set; }

            [DbColumn("Id активных подписок")]              // Id активных подписок
            public string IdActiveSubscriptions { get; set; }
        }

        // ===================================================================
        // Таблица: Устройства
        // ===================================================================

        [DbTable("Устройства")]
        public class Devaices
        {
            [DbColumn("id", IsPrimaryKey = true)]           // id
            public Guid Id { get; set; }

            [DbColumn("Id сотрудника")]                     // Id сотрудника
            public string IdEmployee { get; set; }

            [DbColumn("ip")]                                // ip
            public string Ip { get; set; }

            [DbColumn("Id материнской платы")]              // Id материнской платы
            public string IdMotherboard { get; set; }

            [DbColumn("Название устройства")]               // Название устройства
            public string NameDevaice { get; set; }

            [DbColumn("Местоположение")]                    // Местоположение
            public string Location { get; set; }

            [DbColumn("Постоянный доступ")]                 // Постоянный доступ
            public bool PermanentAccess { get; set; }

            [DbColumn("Последний вход")]                    // Последний вход
            public DateTime LastEntry { get; set; }
        }

        // ===================================================================
        // Таблица: тест
        // ===================================================================

        [DbTable("Тест")]
        public class Test
        {
            [DbColumn("Число", IsPrimaryKey = true)]           // Число
            public string Number { get; set; }
        }
    }
}
