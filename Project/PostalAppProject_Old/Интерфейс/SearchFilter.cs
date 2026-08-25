using System;
using System.Reflection;

namespace Интерфейс
{
    /// <summary>
    /// Универсальный фильтр поиска для любой таблицы PostgreSQL.
    /// Поддерживает поиск по русским символам через ILIKE (регистронезависимо),
    /// а также точный поиск по числовым полям.
    /// </summary>
    public class SearchFilter<T> where T : new()
    {
        public string FilterColumn { get; private set; } = "";
        public string FilterValue  { get; private set; } = "";

        public bool IsActive => !string.IsNullOrEmpty(FilterColumn) && !string.IsNullOrEmpty(FilterValue);

        /// <summary>
        /// Устанавливает фильтр.
        /// </summary>
        /// <param name="columnPropName">Имя C#-свойства модели</param>
        /// <param name="value">Значение для поиска</param>
        public void SetFilter(string columnPropName, string value)
        {
            FilterColumn = columnPropName;
            FilterValue  = value?.Trim() ?? "";
        }

        public void Clear()
        {
            FilterColumn = "";
            FilterValue  = "";
        }

        /// <summary>
        /// Применяет фильтр к запросу PostgreSQL.
        /// Для текстовых полей использует ILIKE — регистронезависимый поиск,
        /// включая русские символы (работает при Encoding=UTF8 в строке подключения).
        /// </summary>
        public PgQuery<T> ApplyToQuery(PgQuery<T> query)
        {
            if (!IsActive) return query;

            string dbColumnName = GetDbColumnName(FilterColumn);
            PropertyInfo propInfo = typeof(T).GetProperty(FilterColumn);

            // Специальная обработка для столбца "Login" в таблице Employees
            // Логин хранится в связанной таблице "Логин", поэтому фильтруем по IdLogin
            if (propInfo == null && FilterColumn == "Login" && typeof(T).Name == "Employees")
            {
                // Для фильтрации по логину нужно найти ID логинов, которые содержат искомый текст
                // Это делается через подзапрос
                // К сожалению, текущая архитектура не поддерживает JOIN, поэтому просто пропускаем
                // Альтернатива: загрузить все логины и фильтровать на клиенте (но это неэффективно)
                Logger.ShowWarning("Фильтрация по логину временно недоступна.\nИспользуйте фильтр по ФИО или роли.");
                return query;
            }

            // Если свойство не найдено, используем текстовый поиск
            if (propInfo == null)
            {
                // Для несуществующих свойств просто используем ILIKE по имени столбца в БД
                return query.Filter(dbColumnName, "ILIKE", $"%{FilterValue}%");
            }

            // Проверка на DateTime
            bool isDateTime = propInfo.PropertyType == typeof(DateTime) ||
                              propInfo.PropertyType == typeof(DateTime?);

            // Проверка на числовые типы
            bool isNumeric = propInfo.PropertyType == typeof(float)  ||
                             propInfo.PropertyType == typeof(int)    ||
                             propInfo.PropertyType == typeof(double) ||
                             propInfo.PropertyType == typeof(decimal) ||
                             propInfo.PropertyType == typeof(long)   ||
                             propInfo.PropertyType == typeof(short);

            if (isDateTime)
            {
                // Для DateTime используем частичный поиск по дате с приведением к тексту
                // Поддерживаем форматы: "30" (день), "30.12" (день.месяц), "30.12.2025" (полная дата)
                string searchPattern = FilterValue.Trim();
                
                // Преобразуем точки в дефисы для PostgreSQL формата
                // PostgreSQL хранит даты в формате YYYY-MM-DD
                string[] parts = searchPattern.Split('.');
                
                if (parts.Length == 1)
                {
                    // Только день: ищем по дню (например, "30" найдёт все 30-е числа)
                    return query.FilterWithCast(dbColumnName, "TEXT", "ILIKE", $"%-{parts[0].PadLeft(2, '0')}%");
                }
                else if (parts.Length == 2)
                {
                    // День и месяц: ищем по дню и месяцу (например, "30.12" найдёт все 30 декабря)
                    string day = parts[0].PadLeft(2, '0');
                    string month = parts[1].PadLeft(2, '0');
                    return query.FilterWithCast(dbColumnName, "TEXT", "ILIKE", $"%-{month}-{day}%");
                }
                else if (parts.Length == 3)
                {
                    // Полная дата: ищем точное совпадение дня
                    string day = parts[0].PadLeft(2, '0');
                    string month = parts[1].PadLeft(2, '0');
                    string year = parts[2];
                    return query.FilterWithCast(dbColumnName, "TEXT", "ILIKE", $"{year}-{month}-{day}%");
                }
                else
                {
                    // Неправильный формат - ищем как есть
                    return query.FilterWithCast(dbColumnName, "TEXT", "ILIKE", $"%{FilterValue}%");
                }
            }
            else if (isNumeric)
            {
                // Числовое поле: точное совпадение
                if (float.TryParse(FilterValue.Replace(".", ","), out float numericValue) ||
                    float.TryParse(FilterValue.Replace(",", "."), out numericValue))
                {
                    return query.Filter(dbColumnName, "=", numericValue);
                }
                else
                {
                    // Нечисловой ввод в числовое поле — возвращаем пустой результат
                    return query.Filter(dbColumnName, "=", -9999999);
                }
            }
            else
            {
                // Текстовое поле: ILIKE с % для поиска подстроки
                // Поддерживает русские символы при UTF-8 кодировке БД
                return query.Filter(dbColumnName, "ILIKE", $"%{FilterValue}%");
            }
        }

        /// <summary>
        /// Возвращает имя столбца в БД по имени C#-свойства через DbColumnAttribute.
        /// </summary>
        private string GetDbColumnName(string propertyName)
        {
            var property  = typeof(T).GetProperty(propertyName);
            var attribute = property?.GetCustomAttribute<DbColumnAttribute>();
            return attribute?.ColumnName ?? propertyName;
        }
    }
}
