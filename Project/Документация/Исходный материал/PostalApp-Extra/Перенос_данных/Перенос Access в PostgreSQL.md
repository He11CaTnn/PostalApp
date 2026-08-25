ПЕРЕНОС ACCESS → POSTGRESQL
============================

Методы: TransferRegionsToServer(), TransferMarkersToServer()
Класс: CreateBalancedRegions

Строка подключения PostgreSQL: Program.BuildPgConnectionString()
Строка подключения Access: Provider=Microsoft.ACE.OLEDB.12.0;Data Source={путь к BD.accdb}

Перенос участков и узлов (TransferRegionsToServer)
---------------------------------------------------
1. Снятие внешних ключей на PostgreSQL:
   ALTER TABLE "Участки" DROP CONSTRAINT IF EXISTS "Участки_Id сотрудника_fkey";
   ALTER TABLE "Метки"   DROP CONSTRAINT IF EXISTS "Метки_Id участка_fkey";
   ALTER TABLE "Узлы"    DROP CONSTRAINT IF EXISTS "Узлы_Id участка_fkey";

2. Очистка PostgreSQL:
   DELETE FROM "Узлы" (зависит от Участков → сначала)
   DELETE FROM "Участки"

3. SELECT id, Название, Цвет FROM Участки (Access)
   Для каждой записи:
     Guid из Access читается как строка и парсится через Trim('{', '}').
     INSERT INTO "Участки" (id, Название, Цвет, "Id сотрудника") ... ON CONFLICT (id) DO UPDATE SET ...
     "Id сотрудника" = Guid.Empty (участки без привязки к сотруднику).

   Для каждого участка: SELECT id, Долгота, Широта, Номер FROM Узлы WHERE Id участка = ?
     INSERT INTO "Узлы" (id, Широта, Долгота, "Id участка", Номер) ... ON CONFLICT (id) DO UPDATE SET ...
     Поля Широта и Долгота не меняются местами — они переносятся как есть из Access
     (с учётом того, что в Access они уже переставлены).

4. Восстановление внешних ключей:
   ALTER TABLE "Метки" ADD CONSTRAINT "Метки_Id участка_fkey"
     FOREIGN KEY ("Id участка") REFERENCES "Участки"(id) ON DELETE CASCADE;
   ALTER TABLE "Узлы" ADD CONSTRAINT "Узлы_Id участка_fkey"
     FOREIGN KEY ("Id участка") REFERENCES "Участки"(id) ON DELETE CASCADE;

Перенос меток (TransferMarkersToServer)
-----------------------------------------
1. Снятие FK:
   ALTER TABLE "Метки" DROP CONSTRAINT IF EXISTS "Метки_Id участка_fkey";

2. DELETE FROM "Метки"

3. SELECT id, Долгота, Широта, Тип здания, Улица, Дом, Корпус, Квартира, Id участка FROM Метки (Access)
   Для каждой записи:
     Id метки: Trim('{', '}') + Guid.Parse.
     Id участка: если не пустой и парсится как Guid → передаётся Guid;
                 иначе → Guid.Empty.
     INSERT INTO "Метки" ... ON CONFLICT (id) DO UPDATE SET ...
     "Id читателей" всегда передаётся как пустая строка (не переносится из Access).

4. Восстановление FK "Метки_Id участка_fkey".

Порядок вызова
--------------
TransferRegionsToServer() вызывается первым — создаёт участки и узлы в PostgreSQL.
TransferMarkersToServer() вызывается вторым — метки ссылаются на уже существующие участки.
