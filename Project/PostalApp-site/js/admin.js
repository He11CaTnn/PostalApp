/* ============================================================
   ADMIN.JS
   ============================================================ */

let db = null, auth = null, adminUser = null;
let isSuperAdminSession = false;
let allUsers = [];    // кеш для поиска
let allRequests = []; // кеш для поиска

document.addEventListener('DOMContentLoaded', async () => {
  const cfg = SITE_CONFIG.firebase;
  if (!cfg.apiKey || cfg.apiKey.startsWith('ВАШ')) {
    showGateError('Firebase не настроен. Заполни js/config.js'); return;
  }
  try {
    firebase.initializeApp(cfg);
    db = firebase.firestore();
    auth = firebase.auth();
    auth.onAuthStateChanged(onAdminAuthChanged);
  } catch(err) { showGateError('Ошибка Firebase: ' + err.message); }
});

async function onAdminAuthChanged(user) {
  if (!user) { showGate(); return; }

  const superAdmin = user.email === SITE_CONFIG.superAdminEmail;
  let regularAdmin = false;

  if (!superAdmin) {
    try {
      const doc = await db.collection('users').doc(user.uid).get();
      regularAdmin = doc.exists && doc.data().isAdmin === true;
    } catch(e) { console.warn('Cannot check admin:', e); }
  }

  if (!superAdmin && !regularAdmin) {
    showGateError('Доступ запрещён. У вашего аккаунта нет прав администратора.');
    await auth.signOut(); return;
  }

  adminUser = user;
  isSuperAdminSession = superAdmin;
  showAdminPanel(user, superAdmin);
  loadAllData();
}

function showGate() {
  document.getElementById('access-gate').style.display = 'flex';
  document.getElementById('admin-app').style.display   = 'none';
}
function showAdminPanel(user, isSA) {
  document.getElementById('access-gate').style.display = 'none';
  document.getElementById('admin-app').style.display   = 'flex';
  document.getElementById('admin-email-display').textContent = user.email;
  const badge = document.getElementById('superadmin-badge');
  if (badge) badge.style.display = isSA ? 'inline-flex' : 'none';
}
function showGateError(msg) {
  const el = document.getElementById('gate-error');
  if (el) { el.style.display='block'; el.textContent=msg; }
}

async function handleAdminSignIn() {
  if (!auth) return;
  try { await auth.signInWithPopup(new firebase.auth.GoogleAuthProvider()); }
  catch(err) { if(err.code!=='auth/popup-closed-by-user') showGateError('Ошибка: '+(err.message||err.code)); }
}
async function handleAdminLogout() {
  if (auth) { await auth.signOut(); showGate(); }
}

/* ============================================================
   ЗАГРУЗКА ДАННЫХ
   ============================================================ */
async function loadAllData() {
  await Promise.all([loadRequests(), loadUsers()]);
}

/* ---- ЗАЯВКИ ----
   Убрали orderBy → нет нужды в составном индексе Firestore.
   Сортируем вручную в памяти.                                */
async function loadRequests() {
  const tbody = document.getElementById('requests-tbody');
  if (!db || !tbody) return;
  try {
    const snap = await db.collection('users').where('requestStatus','==','pending').get();
    allRequests = [];
    snap.forEach(doc => allRequests.push({ id: doc.id, ...doc.data() }));
    allRequests.sort((a,b) => (b.requestedAt?.seconds||0) - (a.requestedAt?.seconds||0));

    const badge = document.getElementById('pending-badge');
    const statEl = document.getElementById('stat-pending');
    if (badge) { badge.textContent=allRequests.length; badge.style.display=allRequests.length?'inline-flex':'none'; }
    if (statEl) statEl.textContent = allRequests.length;

    renderRequests(allRequests);
  } catch(err) {
    tbody.innerHTML=`<tr><td colspan="4" style="color:#f87171;padding:20px">Ошибка: ${escHtml(err.message)}</td></tr>`;
    console.error('loadRequests:', err);
  }
}

function filterRequests(query) {
  const q = query.trim().toLowerCase();
  const filtered = q
    ? allRequests.filter(u => (u.name||'').toLowerCase().includes(q) || (u.email||'').toLowerCase().includes(q))
    : allRequests;
  renderRequests(filtered);
}

function renderRequests(reqs) {
  const tbody = document.getElementById('requests-tbody');
  if (!tbody) return;
  if (!reqs.length) {
    tbody.innerHTML=`<tr><td colspan="4" style="text-align:center;color:var(--text-muted);padding:60px">Нет новых заявок ✓</td></tr>`; return;
  }
  const adminBtn = isSuperAdminSession
    ? (uid) => `<button class="btn btn-sm btn-approve" style="background:rgba(212,168,83,0.15);color:var(--gold);border-color:rgba(212,168,83,0.3)" onclick="approveAsAdmin('${uid}')">👑 Администратором</button>`
    : () => '';
  tbody.innerHTML = reqs.map(u => {
    const date = u.requestedAt?.toDate ? u.requestedAt.toDate().toLocaleString('ru-RU') : '—';
    return `<tr id="req-row-${u.id}">
      <td>
        <div style="display:flex;align-items:center;gap:10px">
          ${u.photoURL
            ? `<img src="${u.photoURL}" style="width:32px;height:32px;border-radius:50%;object-fit:cover">`
            : `<div style="width:32px;height:32px;border-radius:50%;background:var(--gold-dim);display:grid;place-items:center">👤</div>`}
          <span style="font-weight:600;font-size:0.88rem">${escHtml(u.name||'Без имени')}</span>
        </div>
      </td>
      <td style="font-size:0.85rem;color:var(--text-secondary)">${escHtml(u.email||'—')}</td>
      <td style="font-size:0.82rem;color:var(--text-muted)">${date}</td>
      <td style="display:flex;gap:8px;flex-wrap:wrap;padding-top:12px">
        <button class="btn btn-sm btn-approve" onclick="approveRequest('${u.id}')">✅ Модератором</button>
        ${adminBtn(u.id)}
        <button class="btn btn-sm btn-reject" onclick="rejectRequest('${u.id}')">❌ Отклонить</button>
      </td>
    </tr>`;
  }).join('');
}

async function approveRequest(uid) {
  try {
    await db.collection('users').doc(uid).update({ requestStatus:'approved', isModerator:true });
    showToast('Пользователь стал модератором', 'success');
    removeReqRow(uid); loadStats();
  } catch(err) { showToast('Ошибка: '+err.message,'error'); }
}

async function approveAsAdmin(uid) {
  if (!isSuperAdminSession) { showToast('Только главный администратор может назначать администраторов','error'); return; }
  const doc = await db.collection('users').doc(uid).get();
  if (doc.exists && doc.data().email === SITE_CONFIG.superAdminEmail) {
    showToast('Этот пользователь уже главный администратор','info'); return;
  }
  try {
    await db.collection('users').doc(uid).update({ requestStatus:'approved', isAdmin:true, isModerator:true });
    showToast('Пользователь назначен администратором', 'success');
    removeReqRow(uid); loadStats();
  } catch(err) { showToast('Ошибка: '+err.message,'error'); }
}

async function rejectRequest(uid) {
  try {
    await db.collection('users').doc(uid).update({ requestStatus:'rejected' });
    showToast('Заявка отклонена', 'info');
    removeReqRow(uid); loadStats();
  } catch(err) { showToast('Ошибка: '+err.message,'error'); }
}

function removeReqRow(uid) {
  // Убираем из кеша
  allRequests = allRequests.filter(r => r.id !== uid);
  // Перерисовываем с учётом текущего поиска
  const q = (document.getElementById('requests-search')?.value || '').trim().toLowerCase();
  filterRequests(q);
  // Обновляем бейдж
  const badge = document.getElementById('pending-badge');
  if (badge) { badge.textContent=allRequests.length; badge.style.display=allRequests.length?'inline-flex':'none'; }
}

/* ---- ПОЛЬЗОВАТЕЛИ ---- */
async function loadUsers() {
  const tbody = document.getElementById('users-tbody');
  if (!db || !tbody) return;
  try {
    const snap = await db.collection('users').get();
    allUsers = []; snap.forEach(doc=>allUsers.push({id:doc.id,...doc.data()}));
    allUsers.sort((a,b)=>(b.registeredAt?.seconds||0)-(a.registeredAt?.seconds||0));
    loadStats(allUsers);
    renderUsers(allUsers);
  } catch(err) {
    tbody.innerHTML=`<tr><td colspan="4" style="color:#f87171;padding:20px">Ошибка: ${escHtml(err.message)}</td></tr>`;
    console.error('loadUsers:', err);
  }
}

function filterUsers(query) {
  const q = query.trim().toLowerCase();
  const filtered = q
    ? allUsers.filter(u => (u.name||'').toLowerCase().includes(q) || (u.email||'').toLowerCase().includes(q))
    : allUsers;
  renderUsers(filtered);
}

function renderUsers(users) {
  const tbody = document.getElementById('users-tbody');
  if (!tbody) return;

  if (!users.length) {
    tbody.innerHTML=`<tr><td colspan="4" style="text-align:center;color:var(--text-muted);padding:60px">Нет пользователей</td></tr>`; return;
  }

  tbody.innerHTML = users.map(u => {
    const date = u.registeredAt?.toDate ? u.registeredAt.toDate().toLocaleDateString('ru-RU') : '—';
    const isSA   = u.email === SITE_CONFIG.superAdminEmail;
    const isSelf = adminUser && u.id === adminUser.uid;

    let currentRole = 'user';
    if (isSA)            currentRole = 'superadmin';
    else if (u.isAdmin)  currentRole = 'admin';
    else if (u.isModerator) currentRole = 'moderator';

    let roleCol;
    if (isSA) {
      roleCol = `<span style="font-size:0.75rem;color:var(--gold)">👑 Главный администратор</span>`;
    } else if (isSelf) {
      const roleLabel = currentRole==='admin' ? 'Администратор' : currentRole==='moderator' ? 'Модератор' : 'Пользователь';
      roleCol = `<span style="font-size:0.75rem;color:var(--text-muted)">${roleLabel} <span style="opacity:0.5">(вы)</span></span>`;
    } else if (isSuperAdminSession) {
      roleCol = `<select class="version-select" style="max-width:160px;font-size:0.8rem" onchange="setRole('${u.id}',this.value,this)">
        <option value="user"      ${currentRole==='user'      ?'selected':''}>Пользователь</option>
        <option value="moderator" ${currentRole==='moderator' ?'selected':''}>Модератор</option>
        <option value="admin"     ${currentRole==='admin'     ?'selected':''}>Администратор</option>
      </select>`;
    } else {
      if (currentRole === 'admin') {
        roleCol = `<span style="font-size:0.75rem;color:var(--gold)">Администратор</span>`;
      } else {
        roleCol = `<select class="version-select" style="max-width:160px;font-size:0.8rem" onchange="setRole('${u.id}',this.value,this)">
          <option value="user"      ${currentRole==='user'      ?'selected':''}>Пользователь</option>
          <option value="moderator" ${currentRole==='moderator' ?'selected':''}>Модератор</option>
        </select>`;
      }
    }

    return `<tr id="user-row-${u.id}">
      <td>
        <div style="display:flex;align-items:center;gap:10px">
          ${u.photoURL
            ? `<img src="${u.photoURL}" style="width:32px;height:32px;border-radius:50%;object-fit:cover">`
            : `<div style="width:32px;height:32px;border-radius:50%;background:var(--gold-dim);display:grid;place-items:center;font-size:0.85rem">👤</div>`}
          <div style="font-weight:600;font-size:0.88rem">${escHtml(u.name||'Без имени')}</div>
        </div>
      </td>
      <td style="font-size:0.85rem;color:var(--text-secondary)">${escHtml(u.email||'—')}</td>
      <td style="font-size:0.82rem;color:var(--text-muted)">${date}</td>
      <td>${roleCol}</td>
    </tr>`;
  }).join('');
}

async function setRole(uid, newRole, selectEl) {
  // Защита от смены своей роли
  if (adminUser && uid === adminUser.uid) {
    showToast('Нельзя менять свою собственную роль','error');
    loadUsers(); return;
  }
  // Проверка прав
  if (newRole === 'admin' && !isSuperAdminSession) {
    showToast('Только главный администратор может назначать администраторов','error');
    loadUsers(); return;
  }
  // Проверка что не трогаем суперадмина
  try {
    const doc = await db.collection('users').doc(uid).get();
    if (doc.exists && doc.data().email === SITE_CONFIG.superAdminEmail) {
      showToast('Нельзя изменить роль главного администратора','error');
      loadUsers(); return;
    }
    const update = {
      isAdmin:     newRole === 'admin',
      isModerator: newRole === 'admin' || newRole === 'moderator',
    };
    await db.collection('users').doc(uid).update(update);
    const labels = {admin:'Администратор', moderator:'Модератор', user:'Пользователь'};
    showToast('Роль изменена: ' + (labels[newRole]||newRole), 'success');
    loadStats();
  } catch(err) {
    showToast('Ошибка: '+err.message,'error');
    loadUsers();
  }
}

async function loadStats(users) {
  try {
    if (!users) {
      const snap = await db.collection('users').get();
      users = []; snap.forEach(doc=>users.push(doc.data()));
    }
    const admins  = users.filter(u=>u.isAdmin||u.email===SITE_CONFIG.superAdminEmail).length;
    const mods    = users.filter(u=>u.isModerator&&!u.isAdmin&&u.email!==SITE_CONFIG.superAdminEmail).length;
    const pending = users.filter(u=>u.requestStatus==='pending').length;
    setEl('stat-total',  users.length);
    setEl('stat-admins', admins);
    setEl('stat-mods',   mods);
    setEl('stat-pending',pending);
  } catch(e) { console.warn('loadStats:', e); }
}

/* ---- ТАБЫ ---- */
function switchTab(name, btn) {
  document.querySelectorAll('.admin-panel').forEach(p=>p.classList.remove('active'));
  document.querySelectorAll('.admin-tab').forEach(b=>b.classList.remove('active'));
  const p = document.getElementById('tab-'+name); if(p) p.classList.add('active');
  if(btn) btn.classList.add('active');
}

/* ---- УТИЛИТЫ ---- */
function setEl(id, v) { const e=document.getElementById(id); if(e) e.textContent=v; }
function escHtml(str) { return String(str||'').replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;'); }
function showToast(msg, type='info', dur=3500) {
  const c=document.getElementById('toast-container'); if(!c) return;
  const icons={success:'✅',error:'❌',info:'ℹ️'};
  const t=document.createElement('div'); t.className=`toast toast-${type}`;
  t.innerHTML=`<span>${icons[type]||'•'}</span><span>${msg}</span>`;
  c.appendChild(t);
  setTimeout(()=>{t.style.animation='toastIn 0.3s ease reverse both';setTimeout(()=>t.remove(),300);},dur);
}
