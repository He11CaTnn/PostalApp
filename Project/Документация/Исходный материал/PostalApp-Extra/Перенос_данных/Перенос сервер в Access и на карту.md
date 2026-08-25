ПЕРЕНОС СЕРВЕР → ACCESS И СЕРВЕР → КАРТА
==========================================

Методы: LoadFromServerToAcccess(), TransferFromServerToAccess(), LoadFromServerAndDisplayOnMap()
Класс: CreateBalancedRegions

Перенос Сервер → Access (LoadFromServerToAcccess)
--------------------------------------------------
Загружает данные из PostgreSQL напрямую в BD.accdb.

1. OleDbConnection(connectionString) — текущая рабочая БД.
   Очистка таблиц в правильном порядке: Метки → Узлы → Участки.

2. NpgsqlConnection(pgConn).Open()

3. Участки:
   SELECT "id", "Название", "Цвет" FROM "Участки"
   INSERT INTO Участки ([id],[Название],[Цвет]) VALUES (?,?,?)

4. Узлы:
   SELECT "id","Широта","Долгота","Id участка","Номер" FROM "Узлы" ORDER BY "Id участка","Номер"
   INSERT INTO Узлы ([id],[Долгота],[Широта],[Id участка],[Номер]) VALUES (?,?,?,?,?)
   Намеренный своп: в Access [Долгота] ← pgReader["Широта"], [Широта] ← pgReader["Долгота"].
   Это воспроизводит ту же инверсию, что исторически сложилась в Access-схеме.

5. Метки:
   SELECT "id","Широта","Долгота","Тип здания","Улица","Дом","Корпус","Квартира","Id участка" FROM "Метки"
   INSERT INTO Метки ([id],[Долгота],[Широта],[Тип здания],[Улица],[Дом],[Корпус],[Квартира],[Id участка])
   Для меток своп не применяется.
   Id участка: если DBNull → DBNull; иначе строка.

TransferFromServerToAccess(targetConnectionString)
---------------------------------------------------
Используется в button29_Click (создание новой БД из данных сервера).
Отличается от LoadFromServerToAcccess:
  - Принимает произвольный targetConnectionString (путь выбирается через SaveFileDialog).
  - Не очищает таблицы перед вставкой (новая БД уже пустая).
  - Логика вставки идентична, своп Узлов воспроизводится.

button29_Click — создание новой БД из данных сервера
-----------------------------------------------------
1. SaveFileDialog (*.accdb), начальная папка: ..\..\Data относительно exe.
2. Копирование BD.accdb → выбранный путь (File.Copy, overwrite: true).
   Копируется структура таблиц вместе с файлом.
3. Очистка скопированной БД (DELETE FROM Метки, Узлы, Участки).
4. TransferFromServerToAccess(targetConnectionString) — заполнение данными.

Загрузка Сервер → Карта (LoadFromServerAndDisplayOnMap)
--------------------------------------------------------
Загружает участки, узлы и метки из PostgreSQL и немедленно отображает на карте.

1. SELECT "id","Название","Цвет" FROM "Участки" — создаёт объекты Region.
   Цвет: если начинается с "#" → HexToColor, иначе → regionColors[idx % length].

2. SELECT "Id участка","Долгота" AS lat,"Широта" AS lon,"Номер" FROM "Узлы"
   Намеренный своп при чтении: PostgreSQL-поле "Долгота" читается как lat,
   "Широта" — как lon. Это зеркально отражает своп при записи (TransferRegionsToServer).

3. SELECT метки: "Широта" AS lat, "Долгота" AS lon — своп не применяется.
   Каждая метка привязывается к региону по "Id участка".
   Метки без участка помещаются в временный регион "__orphan__" (серый).

4. Отрисовка:
   Полигоны (polygons): GMapPolygon с Stroke = цвет участка, Fill = полупрозрачный цвет.
   Маркеры (markers): цветные кастомные круги CreateSmallCircleBitmap(region.Color).
   Центральные маркеры: blue_pushpin на среднем значении координат узлов.
   Маркер почты: red_pushpin на postOfficeLocation.

5. balancedRegions = регионы без "__orphan__" (доступны для редактирования узлов).

6. MessageBox: количество участков и меток.
