DATATABLES — НАСТРОЙКА ТАБЛИЦ И ДОБАВЛЕНИЕ СТРОК
==================================================

Класс: DataTables (статический)
Файл: DataTables.cs

Назначение
----------
Централизованная настройка DataGridView и добавление строк.
Отделяет конфигурацию столбцов от форм, предотвращая дублирование кода.

Методы инициализации таблиц
-----------------------------
Каждый метод принимает DataGridView, устанавливает AutoGenerateColumns = false,
очищает столбцы и добавляет вручную описанные DataGridViewTextBoxColumn.
Видимые столбцы несут отображаемые данные; скрытые (Visible = false) хранят
идентификаторы и технические поля для операций CRUD.

InitializeEditionsTable:
  Видимые: Индекс, Название, Тип издания, Мин/Макс срок и цены подписки (на дом и почтовый ящик).
  Скрытые: Id.

InitializeSubscriptionsTable:
  Видимые: Срок подписки, Цена подписки, Количество комплектов, Дата оформления, Название издания.
  Скрытые: Id, IndexEdition.
  Формат даты: "dd.MM.yyyy".

InitializeReadersTable:
  Видимые: ФИО.
  Скрытые: Id, IdActiveSubscriptions.

InitializeTasksTable:
  Видимые: ФИО сотрудника, Статус, Дата выдачи, Дата сдачи.
  Скрытые: Id, IdEmployee, Text, AttachedMarkers.
  Формат дат: "dd.MM.yyyy".

InitializeEmployeesTable:
  Видимые: ФИО сотрудника, Роль, Логин (email), Дата регистрации.
  Скрытые: Id, IdLogin.
  Формат даты: "dd.MM.yyyy".

InitializeAddressTable:
  Видимые: Адрес (составной из Street + House/Building/Apartment).
  Скрытые: Id.
  Используется в постмене для отображения маршрутных адресов.

Все методы завершаются:
  dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill

Методы добавления строк
------------------------
Каждый метод Add*Row принимает DataGridView и объект модели, добавляет строку
через dataGridView.Rows.Add() и заполняет ячейки напрямую по именам столбцов.

AddEditionRow(DataGridView, Editions) — добавляет издание.
AddSubscriptionRow(DataGridView, Subscriptions) — добавляет подписку.
AddReaderTableRow(DataGridView, Readers) — добавляет читателя.

AddStreetRow(DataGridView, Markers):
  Формирует строку адреса по приоритету:
    Квартира → Корпус → Дом → TypeBuilding
  Отображает в ячейке «Address» вида «улица значение».

AddTaskRow(DataGridView, Tasks) — async:
  Дополнительно запрашивает из БД запись Employees по IdEmployee
  для получения ФИО. Если сотрудник не найден — строка не добавляется.

AddEmployeeRow(DataGridView, Employees) — async:
  Дополнительно запрашивает из БД запись Login по IdLogin для получения email.
  Если логин не найден — строка не добавляется.
