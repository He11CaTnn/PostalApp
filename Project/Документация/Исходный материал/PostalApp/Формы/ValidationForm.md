ФОРМА ВАЛИДАЦИИ EXCEL-ДАННЫХ (ValidationForm)
===============================================

Файл: ValidationForm.cs

Назначение
----------
Двухфазный диалог для импорта данных из Excel в PostgreSQL.
Фаза 1 — интерактивная проверка строк с возможностью исправления ошибок на месте.
Фаза 2 — отображение прогресса записи в базу данных.

Форма не содержит бизнес-логики — только UI. Вызывающий код управляет ею
через публичный API, вызывая методы из фонового потока или async-метода.

Публичный API — Фаза 1
-----------------------
UpdateStatus(status):
  Обновляет текст статуса (_statusLabel). Thread-safe через InvokeRequired.

SetProgress(current, total):
  Обновляет ширину _progressBar пропорционально current/total.
  Обновляет _counterLabel текстом "{current} из {total} строк".

ShowError(column, row, value, message) → DialogResult:
  Отображает панель ошибки (_errorPanel) с информацией о проблемной ячейке:
    _errorTitle   → "⚠  Строка N  |  Столбец: «название»"
    _errorMessage → описание ошибки
    _originalValueBox → исходное значение
    _correctedValueBox → редактируемое поле для исправления
  Возвращает результат ShowDialog():
    DialogResult.OK     — пользователь нажал «Исправить» (взять значение из _correctedValueBox)
    DialogResult.Ignore — нажал «Пропустить строку»
    DialogResult.Cancel — нажал «Отменить импорт»

GetCorrectedValue() → string:
  Возвращает содержимое _correctedValueBox.Content для использования вместо
  исходного некорректного значения.

Публичный API — Фаза 2
-----------------------
StartPhase2(totalRecords):
  Переводит форму в режим импорта:
    Фаза 1 Pill → зелёный фон, галочка.
    Фаза 2 Pill → тёмно-зелёный фон, белый текст.
    _errorPanel скрыт.
    _progressBar сброшен в 0.
    _counterLabel → "0 из N записей".

SetPhase2Progress(current, total):
  Обновляет прогресс-бар, счётчик и статус с процентом.
  Thread-safe.

Кнопки
------
«Исправить»  — закрывает форму с DialogResult.OK (FixButton_Click → Close).
«Пропустить» — закрывает форму с DialogResult.Ignore (SkipButton_Click → Close).
«Отменить»   — закрывает с DialogResult.Cancel (CancelButton_Click → Close).
Крестик шапки — SafeCancel() → DialogResult.Cancel + Close.
Escape — SafeCancel().

Перемещение окна
----------------
Header_MouseDown — P/Invoke для перетаскивания за шапку.
