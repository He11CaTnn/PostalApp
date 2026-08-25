PgClient И PgQuery — СОБСТВЕННАЯ ORM
=====================================

Назначение
----------
PgClient и PgQuery<T> — самостоятельная реализация ORM поверх Npgsql.
Предоставляет Fluent API для построения и выполнения SQL-запросов к PostgreSQL
с автоматическим маппингом строк на C#-объекты через атрибуты.

Атрибуты маппинга
-----------------
[DbTable("имя таблицы")]      — применяется к классу, задаёт имя таблицы в БД.
[DbColumn("имя столбца")]     — применяется к свойству, задаёт имя столбца.
  IsPrimaryKey = true         — помечает первичный ключ (используется в UPDATE/UPSERT).

Если атрибут отсутствует, используется имя класса/свойства напрямую.

PgClient
--------
Хранит строку подключения. Открывает NpgsqlConnection синхронно (OpenConnection)
или асинхронно (OpenConnectionAsync). Метод From<T>() создаёт PgQuery<T> для данной таблицы.

PgQuery<T> — построитель запросов
-----------------------------------

Инициализация:
  При создании через рефлексию строится двусторонняя карта:
    _propToCol: имя C#-свойства → имя столбца в БД
    _colToProp: имя столбца в БД → PropertyInfo
  Определяется _pkColumn — имя столбца первичного ключа.

Fluent-методы:
  .Where(Expression<Func<T,bool>> predicate)
    Разбирает лямбда-выражение через Expression API.
    Поддерживаются: AndAlso (&&), Equal (==), NotEqual (!=, только для не-null).
    Добавляет условия в список _conditions.

  .Filter(propOrColName, op, value)
    Прямое добавление условия по имени свойства или столбца.
    Поддерживаемые операторы: "=", "<>", "ILIKE", "IN", "IS NULL", "IS NOT NULL".

  .FilterWithCast(propOrColName, castType, op, value)
    Генерирует CAST(столбец AS тип) в WHERE. Используется для поиска по дате через CAST(... AS TEXT).

  .Set(selector, value)
    Добавляет поле в список _sets для частичного UPDATE.

  .Range(from, to)
    Добавляет LIMIT и OFFSET (включительные индексы).

  .Order(propOrColName, ascending)
    Добавляет ORDER BY.

Выполнение запросов:
  .Get()   → SELECT * с WHERE, ORDER BY, LIMIT/OFFSET. Возвращает PgResponse<T>.
  .Single() → SELECT * LIMIT 1. Возвращает T или default.
  .Insert(record) → INSERT INTO ... VALUES (...) RETURNING *. Возвращает PgResponse<T>.
  .Upsert(record) → INSERT ... ON CONFLICT (pk) DO UPDATE SET ... RETURNING *.
  .Update(record) → UPDATE SET ... WHERE ... RETURNING *.
    Если вызывался .Set() — частичное обновление только указанных полей.
    Если передан record — полное обновление всех полей кроме PK.
    Если WHERE не задан — фильтрация по значению PK из record.
  .Delete() → DELETE FROM ... WHERE ...

Построение SQL
--------------
Все имена таблиц и столбцов оборачиваются в двойные кавычки функцией Q()
для поддержки русских имён (таблицы «Сотрудники», «Участки» и т.д.).
Параметры запроса именуются @w0, @w1... (WHERE), @s0, @s1... (SET), @i0... (INSERT).

Типизация параметров
--------------------
BuildScalarParam() явно устанавливает NpgsqlDbType для Guid, string, int, float, double, DateTime.
BuildArrayParam() поддерживает Guid[] и string[] для оператора IN (преобразуется в ANY(@param)).
BuildTypedParam() используется в INSERT/UPDATE для маппинга типов через PropertyInfo.

Маппинг строк (MapRow)
-----------------------
Для каждого поля DataReader берётся имя столбца, ищется PropertyInfo в _colToProp.
SetProperty() выполняет явное приведение типов с учётом: Guid, string, string[],
DateTime (поддерживает DateTimeOffset), float, double, int, а также Nullable<T>.
DBNull-значения пропускаются.

PgResponse<T>
-------------
Обёртка над List<T>:
  Models — полный список результатов.
  Model  — первый элемент или default (удобный accessor для Single-результатов).
