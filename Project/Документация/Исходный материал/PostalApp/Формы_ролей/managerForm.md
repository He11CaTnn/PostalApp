ФОРМА РУКОВОДИТЕЛЯ ПОДПИСОК (managerForm)
==========================================

Файл: managerForm.cs
Роль пользователя: Руководитель подписок
Библиотека: EPPlus (OfficeOpenXml) — работа с Excel-файлами

Общая структура
---------------
Две панели, переключаемые боковым меню:
  subscriptionsEditionsPanel — просмотр подписок и изданий, загрузка из Excel
  tasksPanel                 — задания

Форма не имеет карты.

Панель: Задания
---------------
Полностью идентична postmanForm — загружаются только задания текущего пользователя,
ленивая загрузка, поиск, смена статуса, таймер проверки новых заданий.

Панель: Подписки и Издания — две вкладки
------------------------------------------
Переключение через кнопки subscriptionsButton1_1/2:
  Вкладка 1 — Подписки (subscriptionsEditionsDataGridView1_1)
  Вкладка 2 — Издания  (subscriptionsEditionsDataGridView1_2)

При переключении: активная таблица растягивается на Dock.Fill, неактивная скрывается.
ComboBox фильтрации перестраивается под столбцы активной таблицы.

LazyLoader:
  _loaderSubs     — для таблицы Подписки (SearchFilter<Subscriptions>)
  _loaderEditions — для таблицы Издания  (SearchFilter<Editions>)
  _loaderTasks    — для таблицы Задания  (SearchFilter<Tasks>)

Поиск:
  SearchSubscriptionsEditionsTextBox1 + SearchSubscriptionsEditionsComboBox1 —
  единый поиск для обеих вкладок. Фильтр применяется к активному загрузчику.

Удаление строки (кнопка gMapButton4_4):
  Вкладка «Издания»: Delete из Editions по _selectedEdition.Id, удаление строки.
  Вкладка «Подписки»: Delete из Subscriptions по _selectedSubscription.Id, удаление строки.

Очистка всей таблицы изданий (cuiButton2):
  Работает только на вкладке «Издания».
  Диалог подтверждения → Delete с условием WHERE id != null (удаляет все записи).
  На вкладке «Подписки» показывает предупреждение.

Загрузка изданий из Excel
--------------------------
Зона перетаскивания файла (cuiFileDropper1):
  Принимает перетаскивание .xlsx или .xls файлов.
  Проверяет расширение: другие форматы отклоняются.
  Отображает имя файла и размер в удобочитаемом виде (GetFileSizeString).
  currentExcelFilePath сохраняет путь к файлу.
  Кнопка очистки (subscriptionsEditionsPictureBox3_2) сбрасывает путь и лейблы.

Запуск загрузки (subscriptionsEditionsButton3_1):
  InputBox запрашивает номер строки начала данных (по умолчанию 2, минимум 2 — 1-я
  строка считается заголовком и пропускается).
  Создаётся CancellationTokenSource для прерывания.
  Открывается ValidationForm в режиме фазы 1.
  Вызывается ValidateAndParseExcel(startRow, validationForm).
  По завершении (или отмене) вызывается SaveToDatabase или RollbackValidatedData.
  В finally: ValidationForm закрывается, таблица перезагружается из БД.

Валидация и разбор Excel (ValidateAndParseExcel)
-------------------------------------------------
ExcelPackage(fileInfo) — открывает файл через EPPlus.
worksheet[0] — первый лист книги.

Для каждой строки начиная с startRow:
  Считываются ячейки столбцов 1–9:
    1 — Index (строка)
    2 — Name (строка)
    3 — TypeEdition (строка)
    4–9 — числовые значения (float)

  Числовые значения разбирает ParseFloatValue():
    Пустое значение → ValidationException("Пустое значение").
    Замена ',' на '.' для поддержки российского формата числа.
    float.TryParse с InvariantCulture → при ошибке: ValidationException("Неверный формат числа").

  При ValidationException — вызывается validationForm.ShowError(), которая открывает
    диалог с исходным значением и полем ввода исправления.
    Возможные результаты диалога:
      DialogResult.OK      — взять исправленное значение из validationForm.GetCorrectedValue(),
                             добавить строку в validatedData, повторить ту же строку (row--).
      DialogResult.Ignore  — строка пропускается (не добавляется в validatedData).
      DialogResult.Cancel  — validationCancellation.Cancel(); выход из цикла.

  После успешного разбора словарь с данными добавляется в validatedData.
  Прогресс (текущая строка / всего строк) и статус передаются в ValidationForm.

Запись в БД (SaveToDatabase)
-----------------------------
Переключает ValidationForm в режим фазы 2.
Перебирает validatedData, создаёт объекты DataBase.Editions и выполняет Upsert.
Каждые 10 записей — Task.Delay(1) для освобождения UI-потока и обновления прогресс-бара.
Прогресс записывается через validationForm.SetPhase2Progress().

Откат (RollbackValidatedData):
  validatedData.Clear() — данные уже в памяти не записываются в БД.
  В БД записи добавляются только внутри SaveToDatabase, который вызывается только
  после успешного завершения ValidateAndParseExcel без отмены. Поэтому откат
  является просто очисткой памяти — частичной записи не происходит.

Вложенный класс ValidationException
--------------------------------------
Наследует Exception. Дополнительные свойства: Column (имя столбца), Row (номер строки),
Value (проблемное значение). Бросается внутри ParseFloatValue при ошибке парсинга.
Перехватывается в ValidateAndParseExcel и передаётся в ValidationForm.

Кнопка проверки целостности:
  cuiPanel2_Click → открывает IntegrityCheckForm.
