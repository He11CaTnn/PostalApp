using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PostalApp
{
    // ===================================================================
    // Атрибуты для маппинга C#-классов на таблицы и столбцы PostgreSQL
    // ===================================================================

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class DbTableAttribute : Attribute
    {
        public string TableName { get; }
        public DbTableAttribute(string tableName) { TableName = tableName; }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class DbColumnAttribute : Attribute
    {
        public string ColumnName { get; }
        public bool   IsPrimaryKey { get; set; }
        public DbColumnAttribute(string columnName) { ColumnName = columnName; }
    }

    // ===================================================================
    // Ответ от базы данных (аналог ModeledResponse<T> из Supabase)
    // ===================================================================

    public class PgResponse<T>
    {
        public List<T> Models { get; }
        public T Model => Models != null && Models.Count > 0 ? Models[0] : default;

        public PgResponse(List<T> models) { Models = models ?? new List<T>(); }
    }

    // ===================================================================
    // Главный клиент PostgreSQL (замена Supabase.Client)
    // ===================================================================

    public class PgClient
    {
        private readonly string _connectionString;

        public PgClient(string connectionString) { _connectionString = connectionString; }

        public NpgsqlConnection OpenConnection()
        {
            var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            return conn;
        }

        public async Task<NpgsqlConnection> OpenConnectionAsync()
        {
            var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            return conn;
        }

        /// <summary>Начинает построение запроса к таблице типа T</summary>
        public PgQuery<T> From<T>() where T : new()
            => new PgQuery<T>(this);
    }

    // ===================================================================
    // Построитель запросов (Fluent API, аналог IPostgrestTable<T>)
    // ===================================================================

    public class PgQuery<T> where T : new()
    {
        // ------------------------------------------------------------------
        // Внутреннее состояние
        // ------------------------------------------------------------------
        private readonly PgClient _client;
        private readonly string   _tableName;
        private readonly string   _pkColumn;   // имя столбца PK в БД

        // Карта: имя C#-свойства → имя столбца в БД
        private readonly Dictionary<string, string> _propToCol;
        // Карта: имя столбца в БД → PropertyInfo
        private readonly Dictionary<string, PropertyInfo> _colToProp;

        private readonly List<(string col, string op, object val)> _conditions
            = new List<(string, string, object)>();
        private readonly List<(string col, object val)> _sets
            = new List<(string, object)>();

        private int?   _rangeFrom;
        private int?   _rangeTo;
        private string _orderCol;
        private bool   _orderAsc = true;

        // ------------------------------------------------------------------
        // Конструктор
        // ------------------------------------------------------------------
        public PgQuery(PgClient client)
        {
            _client = client;

            var type      = typeof(T);
            var tableAttr = type.GetCustomAttribute<DbTableAttribute>();
            _tableName    = tableAttr != null ? tableAttr.TableName : type.Name;

            _propToCol  = new Dictionary<string, string>(StringComparer.Ordinal);
            _colToProp  = new Dictionary<string, PropertyInfo>(StringComparer.Ordinal);

            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var colAttr = prop.GetCustomAttribute<DbColumnAttribute>();
                string colName = colAttr != null ? colAttr.ColumnName : prop.Name;
                _propToCol[prop.Name] = colName;
                _colToProp[colName]   = prop;
                if (colAttr != null && colAttr.IsPrimaryKey)
                    _pkColumn = colName;
            }
        }

        // ------------------------------------------------------------------
        // Вспомогательные методы
        // ------------------------------------------------------------------

        /// <summary>Возвращает имя столбца по имени свойства или по имени столбца</summary>
        private string ResolveColumn(string propOrColName)
        {
            if (_propToCol.TryGetValue(propOrColName, out string colName))
                return colName;
            return propOrColName; // уже имя столбца
        }

        /// <summary>Обрамляет имя столбца/таблицы двойными кавычками для PostgreSQL</summary>
        private static string Q(string name) => $"\"{name}\"";

        // ------------------------------------------------------------------
        // Парсинг выражений WHERE
        // ------------------------------------------------------------------

        private void ParseExpression(Expression expr, ParameterExpression param)
        {
            if (expr is BinaryExpression bin)
            {
                switch (bin.NodeType)
                {
                    case ExpressionType.AndAlso:
                        ParseExpression(bin.Left, param);
                        ParseExpression(bin.Right, param);
                        break;

                    case ExpressionType.Equal:
                        TryAddCondition(bin.Left, bin.Right, param, "=");
                        break;

                    case ExpressionType.NotEqual:
                        // x.Field != null — игнорируем (не добавляем условие)
                        if (!IsNullConstant(bin.Left) && !IsNullConstant(bin.Right))
                            TryAddCondition(bin.Left, bin.Right, param, "<>");
                        break;
                }
            }
        }

        private void TryAddCondition(Expression left, Expression right,
                                     ParameterExpression param, string op)
        {
            MemberExpression memberExpr = null;
            Expression       valueExpr  = null;

            if (IsMemberOfParam(left, param))  { memberExpr = (MemberExpression)left;  valueExpr = right; }
            else if (IsMemberOfParam(right, param)) { memberExpr = (MemberExpression)right; valueExpr = left; }

            if (memberExpr == null) return;

            string propName = memberExpr.Member.Name;
            string colName  = ResolveColumn(propName);
            object value    = EvaluateExpression(valueExpr);

            _conditions.Add((colName, op, value));
        }

        private static bool IsMemberOfParam(Expression expr, ParameterExpression param)
            => expr is MemberExpression m && m.Expression == param;

        private static bool IsNullConstant(Expression expr)
            => expr is ConstantExpression c && c.Value == null;

        private static object EvaluateExpression(Expression expr)
        {
            if (expr is ConstantExpression c) return c.Value;
            try { return Expression.Lambda(expr).Compile().DynamicInvoke(); }
            catch { return null; }
        }

        // ------------------------------------------------------------------
        // Fluent WHERE / FILTER / SET / RANGE / ORDER
        // ------------------------------------------------------------------

        public PgQuery<T> Where(Expression<Func<T, bool>> predicate)
        {
            ParseExpression(predicate.Body, predicate.Parameters[0]);
            return this;
        }

        /// <summary>Добавляет условие фильтра по имени свойства или столбца</summary>
        public PgQuery<T> Filter(string propOrColName, string op, object value)
        {
            _conditions.Add((ResolveColumn(propOrColName), op, value));
            return this;
        }

        /// <summary>Добавляет условие фильтра с приведением типа (для поиска по датам как по тексту)</summary>
        public PgQuery<T> FilterWithCast(string propOrColName, string castType, string op, object value)
        {
            string colName = ResolveColumn(propOrColName);
            // Используем специальный маркер для обозначения CAST
            _conditions.Add(($"CAST:{colName}:{castType}", op, value));
            return this;
        }

        /// <summary>Добавляет поле для частичного обновления (SET)</summary>
        public PgQuery<T> Set<TVal>(Expression<Func<T, TVal>> selector, TVal value)
        {
            var member  = (MemberExpression)selector.Body;
            string col  = ResolveColumn(member.Member.Name);
            _sets.Add((col, value));
            return this;
        }

        /// <summary>Пагинация — включительные индексы строк (0-based)</summary>
        public PgQuery<T> Range(int from, int to) { _rangeFrom = from; _rangeTo = to; return this; }

        /// <summary>Сортировка по столбцу</summary>
        public PgQuery<T> Order(string propOrColName, bool ascending = true)
        {
            _orderCol = ResolveColumn(propOrColName);
            _orderAsc = ascending;
            return this;
        }

        // ------------------------------------------------------------------
        // Построение SQL
        // ------------------------------------------------------------------

        private (string sql, List<NpgsqlParameter> pars) BuildWhereClause(int startIdx = 0)
        {
            if (!_conditions.Any()) return ("", new List<NpgsqlParameter>());

            var parts = new List<string>();
            var pars  = new List<NpgsqlParameter>();
            int idx   = startIdx;

            foreach (var (col, op, val) in _conditions)
            {
                string qCol;
                string pName = $"@w{idx++}";

                // Проверяем, нужно ли применить CAST
                if (col.StartsWith("CAST:"))
                {
                    // Формат: CAST:columnName:type
                    string[] castParts = col.Split(':');
                    string columnName = castParts[1];
                    string castType = castParts[2];
                    qCol = $"CAST({Q(columnName)} AS {castType})";
                }
                else
                {
                    qCol = Q(col);
                }

                if (op.Equals("IN", StringComparison.OrdinalIgnoreCase))
                {
                    parts.Add($"{qCol} = ANY({pName})");
                    pars.Add(BuildArrayParam(pName, val));
                }
                else if (op.Equals("IS NULL", StringComparison.OrdinalIgnoreCase))
                {
                    parts.Add($"{qCol} IS NULL");
                }
                else if (op.Equals("IS NOT NULL", StringComparison.OrdinalIgnoreCase))
                {
                    parts.Add($"{qCol} IS NOT NULL");
                }
                else if (val == null)
                {
                    parts.Add($"{qCol} IS NULL");
                }
                else
                {
                    parts.Add($"{qCol} {op} {pName}");
                    pars.Add(BuildScalarParam(pName, val));
                }
            }

            return ($"WHERE {string.Join(" AND ", parts)}", pars);
        }

        private static NpgsqlParameter BuildScalarParam(string name, object val)
        {
            var p = new NpgsqlParameter(name, val ?? DBNull.Value);
            // Явное указание типа для Guid (чтобы избежать проблем с кастом)
            if (val is Guid)    p.NpgsqlDbType = NpgsqlDbType.Uuid;
            if (val is string)  p.NpgsqlDbType = NpgsqlDbType.Text;
            if (val is int)     p.NpgsqlDbType = NpgsqlDbType.Integer;
            if (val is float)   p.NpgsqlDbType = NpgsqlDbType.Real;
            if (val is double)  p.NpgsqlDbType = NpgsqlDbType.Double;
            if (val is DateTime)p.NpgsqlDbType = NpgsqlDbType.TimestampTz;
            return p;
        }

        private static NpgsqlParameter BuildArrayParam(string name, object val)
        {
            if (val is Guid[] guidArr)
            {
                return new NpgsqlParameter(name, guidArr) { NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Uuid };
            }
            if (val is string[] strArr)
            {
                return new NpgsqlParameter(name, strArr) { NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Text };
            }
            // Общий случай — конвертируем IEnumerable в object[]
            if (val is System.Collections.IEnumerable en)
            {
                var list = new List<object>();
                foreach (var item in en) list.Add(item);
                return new NpgsqlParameter(name, list.ToArray());
            }
            return new NpgsqlParameter(name, val ?? DBNull.Value);
        }

        // ------------------------------------------------------------------
        // Маппинг строки DataReader → объект T
        // ------------------------------------------------------------------

        private T MapRow(NpgsqlDataReader reader)
        {
            var obj = new T();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (reader.IsDBNull(i)) continue;

                string colName = reader.GetName(i);
                if (!_colToProp.TryGetValue(colName, out PropertyInfo prop)) continue;

                object raw = reader.GetValue(i);
                try
                {
                    SetProperty(prop, obj, raw);
                }
                catch { /* не падаем при проблеме с типом */ }
            }
            return obj;
        }

        private static void SetProperty(PropertyInfo prop, object obj, object raw)
        {
            if (raw == null || raw == DBNull.Value) return;

            Type pType = prop.PropertyType;
            Type uType = Nullable.GetUnderlyingType(pType);
            Type target = uType ?? pType;

            if (target == typeof(Guid))
            {
                Guid guid = raw is Guid g ? g : Guid.Parse(raw.ToString());
                prop.SetValue(obj, guid);
            }
            else if (target == typeof(string))
            {
                prop.SetValue(obj, raw.ToString());
            }
            else if (target == typeof(string[]))
            {
                if (raw is string[] arr) prop.SetValue(obj, arr);
                else if (raw is Array a)
                {
                    var strs = new string[a.Length];
                    for (int j = 0; j < a.Length; j++)
                        strs[j] = a.GetValue(j)?.ToString();
                    prop.SetValue(obj, strs);
                }
            }
            else if (target == typeof(DateTime))
            {
                if (raw is DateTime dt)    prop.SetValue(obj, dt);
                else if (raw is DateTimeOffset dto) prop.SetValue(obj, dto.DateTime);
                else prop.SetValue(obj, Convert.ToDateTime(raw));
            }
            else if (target == typeof(float))
            {
                prop.SetValue(obj, Convert.ToSingle(raw));
            }
            else if (target == typeof(double))
            {
                prop.SetValue(obj, Convert.ToDouble(raw));
            }
            else if (target == typeof(int))
            {
                prop.SetValue(obj, Convert.ToInt32(raw));
            }
            else
            {
                prop.SetValue(obj, Convert.ChangeType(raw, target));
            }
        }

        // Получаем все свойства с их значениями (для INSERT/UPDATE)
        private List<(string col, object val, PropertyInfo prop)> GetColumnValues(T record,
            bool skipPk = false, bool skipNull = false)
        {
            var result = new List<(string, object, PropertyInfo)>();
            foreach (var prop in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var colAttr = prop.GetCustomAttribute<DbColumnAttribute>();
                if (colAttr == null) continue;
                if (skipPk && colAttr.IsPrimaryKey) continue;

                object val = prop.GetValue(record);
                if (skipNull && val == null) continue;

                result.Add((colAttr.ColumnName, val, prop));
            }
            return result;
        }

        // ------------------------------------------------------------------
        // GET — SELECT
        // ------------------------------------------------------------------

        public async Task<PgResponse<T>> Get()
        {
            var (whereSql, wherePars) = BuildWhereClause();

            var sb = new StringBuilder();
            sb.Append($"SELECT * FROM {Q(_tableName)} ");
            if (!string.IsNullOrEmpty(whereSql)) sb.Append(whereSql + " ");
            if (_orderCol != null) sb.Append($"ORDER BY {Q(_orderCol)} {(_orderAsc ? "ASC" : "DESC")} ");
            if (_rangeFrom.HasValue)
            {
                int limit  = _rangeTo.Value - _rangeFrom.Value + 1;
                int offset = _rangeFrom.Value;
                sb.Append($"LIMIT {limit} OFFSET {offset}");
            }

            var models = new List<T>();
            using (var conn = await _client.OpenConnectionAsync())
            using (var cmd  = new NpgsqlCommand(sb.ToString(), conn))
            {
                foreach (var p in wherePars) cmd.Parameters.Add(p);
                using (var reader = await cmd.ExecuteReaderAsync())
                    while (await reader.ReadAsync())
                        models.Add(MapRow(reader));
            }
            return new PgResponse<T>(models);
        }

        // ------------------------------------------------------------------
        // SINGLE — SELECT LIMIT 1
        // ------------------------------------------------------------------

        public async Task<T> Single()
        {
            var (whereSql, wherePars) = BuildWhereClause();

            var sql = $"SELECT * FROM {Q(_tableName)} {whereSql} LIMIT 1";

            using (var conn = await _client.OpenConnectionAsync())
            using (var cmd  = new NpgsqlCommand(sql, conn))
            {
                foreach (var p in wherePars) cmd.Parameters.Add(p);
                using (var reader = await cmd.ExecuteReaderAsync())
                    if (await reader.ReadAsync())
                        return MapRow(reader);
            }
            return default;
        }

        // ------------------------------------------------------------------
        // INSERT
        // ------------------------------------------------------------------

        public async Task<PgResponse<T>> Insert(T record)
        {
            var cols = GetColumnValues(record);

            var colNames   = cols.Select(c => Q(c.col)).ToList();
            var paramNames = cols.Select((_, i) => $"@i{i}").ToList();

            var sql = $"INSERT INTO {Q(_tableName)} ({string.Join(", ", colNames)}) " +
                      $"VALUES ({string.Join(", ", paramNames)}) RETURNING *";

            var models = new List<T>();
            using (var conn = await _client.OpenConnectionAsync())
            using (var cmd  = new NpgsqlCommand(sql, conn))
            {
                for (int i = 0; i < cols.Count; i++)
                    cmd.Parameters.Add(BuildTypedParam($"@i{i}", cols[i].val, cols[i].prop.PropertyType));

                using (var reader = await cmd.ExecuteReaderAsync())
                    while (await reader.ReadAsync())
                        models.Add(MapRow(reader));
            }
            return new PgResponse<T>(models);
        }

        // ------------------------------------------------------------------
        // UPSERT — INSERT ON CONFLICT DO UPDATE
        // ------------------------------------------------------------------

        public async Task<PgResponse<T>> Upsert(T record)
        {
            if (_pkColumn == null) return await Insert(record);

            var cols     = GetColumnValues(record);
            var allCols  = cols.Select(c => Q(c.col)).ToList();
            var pNames   = cols.Select((_, i) => $"@u{i}").ToList();
            var updateSet= cols
                .Where(c => c.col != _pkColumn)
                .Select(c => $"{Q(c.col)} = EXCLUDED.{Q(c.col)}")
                .ToList();

            var sql = $"INSERT INTO {Q(_tableName)} ({string.Join(", ", allCols)}) " +
                      $"VALUES ({string.Join(", ", pNames)}) " +
                      $"ON CONFLICT ({Q(_pkColumn)}) DO UPDATE SET {string.Join(", ", updateSet)} RETURNING *";

            var models = new List<T>();
            using (var conn = await _client.OpenConnectionAsync())
            using (var cmd  = new NpgsqlCommand(sql, conn))
            {
                for (int i = 0; i < cols.Count; i++)
                    cmd.Parameters.Add(BuildTypedParam($"@u{i}", cols[i].val, cols[i].prop.PropertyType));

                using (var reader = await cmd.ExecuteReaderAsync())
                    while (await reader.ReadAsync())
                        models.Add(MapRow(reader));
            }
            return new PgResponse<T>(models);
        }

        // ------------------------------------------------------------------
        // UPDATE — с записью ИЛИ с SET()-цепочкой
        // ------------------------------------------------------------------

        public async Task<PgResponse<T>> Update(T record = default)
        {
            List<(string col, object val)> setClauses;

            if (_sets.Count > 0)
            {
                // Частичное обновление через Set()
                setClauses = _sets;
            }
            else if (record != null)
            {
                // Полное обновление — все поля кроме PK
                setClauses = GetColumnValues(record, skipPk: true)
                    .Select(c => (c.col, c.val))
                    .ToList();

                // Если WHERE не задан — фильтруем по PK из записи
                if (!_conditions.Any() && _pkColumn != null)
                {
                    var pkProp = _colToProp.TryGetValue(_pkColumn, out PropertyInfo p) ? p : null;
                    if (pkProp != null)
                    {
                        object pkVal = pkProp.GetValue(record);
                        _conditions.Add((_pkColumn, "=", pkVal));
                    }
                }
            }
            else
            {
                return new PgResponse<T>(new List<T>());
            }

            var (whereSql, wherePars) = BuildWhereClause(startIdx: setClauses.Count);

            var setParts = setClauses.Select((c, i) => $"{Q(c.col)} = @s{i}").ToList();
            var sql = $"UPDATE {Q(_tableName)} SET {string.Join(", ", setParts)} {whereSql} RETURNING *";

            var models = new List<T>();
            using (var conn = await _client.OpenConnectionAsync())
            using (var cmd  = new NpgsqlCommand(sql, conn))
            {
                // Параметры SET
                for (int i = 0; i < setClauses.Count; i++)
                {
                    PropertyInfo prop = null;
                    _colToProp.TryGetValue(setClauses[i].col, out prop);
                    cmd.Parameters.Add(BuildTypedParam($"@s{i}", setClauses[i].val,
                        prop?.PropertyType ?? typeof(object)));
                }
                // Параметры WHERE
                foreach (var p in wherePars) cmd.Parameters.Add(p);

                using (var reader = await cmd.ExecuteReaderAsync())
                    while (await reader.ReadAsync())
                        models.Add(MapRow(reader));
            }
            return new PgResponse<T>(models);
        }

        // ------------------------------------------------------------------
        // DELETE
        // ------------------------------------------------------------------

        public async Task Delete()
        {
            var (whereSql, wherePars) = BuildWhereClause();

            // Защита от случайного удаления ВСЕГО: если нет условий,
            // требуем хотя бы один пустой фильтр, что означает «удалить всё»
            string sql;
            if (string.IsNullOrEmpty(whereSql))
                sql = $"DELETE FROM {Q(_tableName)}";
            else
                sql = $"DELETE FROM {Q(_tableName)} {whereSql}";

            using (var conn = await _client.OpenConnectionAsync())
            using (var cmd  = new NpgsqlCommand(sql, conn))
            {
                foreach (var p in wherePars) cmd.Parameters.Add(p);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        // ------------------------------------------------------------------
        // Вспомогательный: создание типизированного параметра
        // ------------------------------------------------------------------

        private static NpgsqlParameter BuildTypedParam(string name, object val, Type propType)
        {
            if (val == null || (val is Guid g && g == Guid.Empty && propType == typeof(Guid)))
            {
                // Guid.Empty → DBNull для необязательных GUID-полей? Нет — Empty это валидный guid
                if (val == null) return new NpgsqlParameter(name, DBNull.Value);
            }

            Type underlying = Nullable.GetUnderlyingType(propType) ?? propType;

            if (underlying == typeof(Guid))
                return new NpgsqlParameter(name, val ?? DBNull.Value) { NpgsqlDbType = NpgsqlDbType.Uuid };
            if (underlying == typeof(string))
                return new NpgsqlParameter(name, val ?? DBNull.Value) { NpgsqlDbType = NpgsqlDbType.Text };
            if (underlying == typeof(string[]))
                return new NpgsqlParameter(name, val ?? DBNull.Value)
                    { NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Text };
            if (underlying == typeof(DateTime))
                return new NpgsqlParameter(name, val ?? DBNull.Value) { NpgsqlDbType = NpgsqlDbType.TimestampTz };
            if (underlying == typeof(float))
                return new NpgsqlParameter(name, val == null ? (object)DBNull.Value : Convert.ToSingle(val))
                    { NpgsqlDbType = NpgsqlDbType.Real };
            if (underlying == typeof(double))
                return new NpgsqlParameter(name, val == null ? (object)DBNull.Value : Convert.ToDouble(val))
                    { NpgsqlDbType = NpgsqlDbType.Double };
            if (underlying == typeof(int))
                return new NpgsqlParameter(name, val == null ? (object)DBNull.Value : Convert.ToInt32(val))
                    { NpgsqlDbType = NpgsqlDbType.Integer };
            if (underlying == typeof(bool))
                return new NpgsqlParameter(name, val ?? DBNull.Value) { NpgsqlDbType = NpgsqlDbType.Boolean };

            return new NpgsqlParameter(name, val ?? DBNull.Value);
        }
    }
}
