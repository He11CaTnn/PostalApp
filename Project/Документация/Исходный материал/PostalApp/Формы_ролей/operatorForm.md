ФОРМА ОПЕРАТОРА (operatorForm)
================================

Файл: operatorForm.cs
Роль пользователя: Оператор

Общая структура
---------------
Две панели, переключаемые боковым меню:
  subscriptionsPanel — работа с подписками, изданиями и читателями
  tasksPanel         — задания

Форма не имеет карты.

Панель: Задания
---------------
Полностью идентична postmanForm — загружаются только задания текущего оператора,
ленивая загрузка, поиск, смена статуса, таймер проверки новых заданий.

Панель: Подписки — три вкладки
---------------------------------
Переключение через кнопки subscriptionsButton1_1/2/3:
  Вкладка 1 — Подписки    (subscriptionsDataGridView1_1)
  Вкладка 2 — Издания     (subscriptionsDataGridView1_2)
  Вкладка 3 — Читатели    (subscriptionsDataGridView1_3)

При переключении вкладки:
  Предыдущая таблица скрывается, новая растягивается через Dock.Fill.
  ComboBox фильтрации перестраивается под видимые столбцы новой таблицы.
  В зависимости от вкладки показываются/скрываются боковые панели:
    Вкладки «Подписки» и «Издания» → AcceptSubscriptionsPanel + EditSubscriptionsPanel.
    Вкладка «Читатели»             → RegistrationReaderPanel + EditReaderPanel.

Поиск по таблицам:
  SearchTasksTextBox1 + SearchTasksComboBox1 — единый поиск для всех трёх вкладок.
  Фильтр применяется к активному LazyLoader (_loaderSubs / _loaderEditions / _loaderReds).
  Сброс LazyLoader.Reset() сбрасывает смещение пагинации на 0 перед перезагрузкой.
  Каждая таблица имеет свой независимый SearchFilter и LazyLoader.

Ленивая загрузка:
  Восемь независимых пар SearchFilter + LazyLoader:
    subscriptionsDataGridView1_1 — Subscriptions (основной список подписок)
    subscriptionsDataGridView1_2 — Editions      (основной список изданий)
    subscriptionsDataGridView1_3 — Readers        (основной список читателей)
    AcceptSubscriptionsDataGridView2_1 — Readers  (для выбора читателя при оформлении)
    EditSubscriptionsDataGridView2_1   — Readers  (для выбора читателя при редактировании)
    RegistrationReaderDataGridView2_1  — Markers  (адреса для новых читателей)
    EditReaderDataGridView2_1          — Markers  (адреса при редактировании)
    tasksDataGridView                  — Tasks

Оформление новой подписки (AcceptSubscriptionsPanel)
-----------------------------------------------------
Кнопка-заголовок AcceptSubscriptionsPanel1 разворачивает/сворачивает панель анимационно.

Выбор читателя:
  AcceptSubscriptionsDataGridView2_1 — список всех читателей.
  AcceptSubscriptionsTextBox2 + AcceptSubscriptionsButton1 — поиск по ФИО.
  Клик по строке: запоминает _selectedReader, заносит ФИО в текстовое поле.

Выбор издания:
  Происходит через клик в подписках на вкладке «Издания» (subscriptionsDataGridView1_2):
    Запоминает _selectedEdition, подставляет название в cuiTextBox2/cuiTextBox3,
    вычисляет цену за месяц (MinTermHousePrice / MinTermSubscription).

Выбор срока подписки:
  12 кнопок-тоглов AcceptSubscriptionsButton2..13 — каждая соответствует одному месяцу.
  TermCalculate(button) возвращает "1" если нажата, "0" иначе.
  Итоговая строка TermSubscription — 12-символьная маска, например "110000000100".
  Количество выбранных месяцев (count единиц) используется для:
    - Расчёта итоговой цены: (MinTermHousePrice / MinTermSubscription) × count.
    - Валидации: count < MinTermSubscription → предупреждение; count > MaxTermSubscription → предупреждение.

Живой расчёт цены:
  AcceptSubscriptionsButton2_Click (вызывается при любом изменении тоглов):
    Пересчитывает и показывает цену за месяц и итоговую сумму с задержкой Task.Delay(100)
    чтобы дождаться применения состояния кнопки.

Кнопка «Оформить подписку»:
  Создаёт Subscriptions: новый Guid, TermSubscription, PriceSubscription, Kit (из textbox),
    DateRegistred = DateTime.Now, IndexEdition = _selectedEdition.Index.
  INSERT в Subscriptions.
  Обновляет _selectedReader.IdActiveSubscriptions: если пустой — просто новый Guid,
    иначе добавляет через запятую к существующим.
  Upsert Readers с обновлённым IdActiveSubscriptions.

Редактирование подписки (EditSubscriptionsPanel)
-------------------------------------------------
Аналогичная логика но для уже существующей подписки (_selectedSubscription).
Данные заполняются при клике на строку в subscriptionsDataGridView1_1:
  TermResult() проверяет знаки строки TermSubscription и восстанавливает состояние тоглов.
Кнопка «Сохранить»: Upsert подписки, Upsert читателя.
  При Upsert читателя IdActiveSubscriptions обновляется: добавляется Id подписки.
Кнопка «Удалить»: диалог → Delete подписки, удаление строки из таблицы.

Регистрация читателя (RegistrationReaderPanel)
-----------------------------------------------
Поля: Имя, Фамилия, Отчество (без пробелов — KeyPress-блокировка).
Поле адреса (RegistrationReaderTextBox4) — доступно только для чтения через клик.

Выбор адреса:
  RegistrationReaderDataGridView2_1 — таблица адресов (Markers) для участков оператора.
  RegistrationReaderButton1 — поиск адреса по введённому тексту (фильтр по полю Street).
  Клик по строке адреса: сохраняет _selectedStreet, формирует строку адреса:
    Квартира → Корпус → Дом → TypeBuilding (приоритет по непустоте).

Кнопка «Зарегистрировать»:
  Создаёт Readers: новый Guid, ФИО из трёх полей, IdActiveSubscriptions = "".
  Upsert Readers.
  Обновляет Markers._selectedStreet.IdReaders: добавляет новый Guid читателя через запятую.
  Добавляет строку во все три таблицы читателей (основная, для оформления, для редактирования).
  Регистрирует Id в _locallyAddedReaderIds.

Редактирование читателя (EditReaderPanel)
------------------------------------------
При клике на адресную строку EditReaderDataGridView2_1:
  Парсит IdReaders метки, загружает первого читателя по Guid,
  разбирает ФИО посимвольно (пробел = смена части) и заполняет поля Имя/Фамилия/Отчество.
Кнопка «Сохранить»: Upsert Readers, Upsert Markers (обновление IdReaders),
  обновление строк во всех трёх таблицах читателей.
Кнопка «Удалить»: диалог → Delete Readers, удаление строк из всех трёх таблиц.

Поля-заглушки (BlockTextBox_KeyPress):
  cuiTextBox2/cuiTextBox3 (название издания), AcceptSubscriptionsTextBox2/EditSubscriptionsTextBox2
  (ФИО читателя), поля адреса — полностью заблокированы от ввода.
  Заполняются только программно при выборе из таблицы.

Кнопка проверки целостности:
  cuiPanel2_Click → открывает IntegrityCheckForm.
