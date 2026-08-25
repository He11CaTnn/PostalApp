FIRESTORE — СТРУКТУРА ДАННЫХ
==============================

Файл: js/app.js, js/admin.js, firestore.rules
Проект: postalapp-site

Коллекция: users
----------------
Путь документа: /users/{uid}
uid — Firebase Auth UID пользователя.

Поля документа:
  email         (string)    — email из Google-аккаунта
  name          (string)    — displayName из Google-аккаунта
  photoURL      (string)    — URL фото профиля
  isModerator   (boolean)   — true = модератор, доступ к Extra
  isAdmin       (boolean)   — true = администратор, доступ к панели
  requestStatus (string|null) — null / "pending" / "approved" / "rejected"
  requestedAt   (Timestamp) — время подачи заявки (serverTimestamp)
  registeredAt  (Timestamp) — время первой регистрации (serverTimestamp)

Переходы requestStatus:
  null → "pending"   — пользователь нажал «Подать заявку» (handleSendRequest)
  "pending" → "approved" + isModerator:true — администратор одобрил модератором (approveRequest)
  "pending" → "approved" + isAdmin:true + isModerator:true — суперадмин одобрил администратором (approveAsAdmin)
  "pending" → "rejected" — администратор отклонил (rejectRequest)

Коллекция: downloads
---------------------
Путь: /downloads/{docId}
Зарезервирована для будущего хранения метаданных скачиваний.
В текущей версии сайта не используется активно.

Правила (firestore.rules)
--------------------------
Коллекция /users/{uid}:
  get    — сам пользователь (uid совпадает) или администратор
  list   — только администратор (чтение всей коллекции)
  create — только сам пользователь для своего документа
  update — администратор (любые поля) ИЛИ сам пользователь с ограничением:
           нельзя изменять isModerator и isAdmin (affectedKeys не содержат эти поля)
           Это не позволяет пользователю самостоятельно выдать себе роль.
  delete — только администратор

Коллекция /downloads/{docId}:
  read  — администратор или модератор
  write — только администратор

Функции Firestore Rules:
  isSuperAdmin() — request.auth.token.email == SITE_CONFIG.superAdminEmail (жёстко прошит)
  isAdmin()      — isSuperAdmin() ИЛИ документ пользователя имеет isAdmin == true
  isModerator()  — документ пользователя имеет isModerator == true
