/* ============================================================
   APP.JS
   ============================================================ */

let currentUser = null;
let userData    = null;
let mainDlUrl   = null;
let extraDlUrl  = null;
let db = null, auth = null;

// Списки всех версий (заполняются при загрузке)
let mainVersionsList  = []; // [{version, url}]
let extraVersionsList = []; // [{version, url}]

document.addEventListener('DOMContentLoaded', async () => {
  applyConfig();
  initWaveGrid();
  initRevealAnimations();
  initNavScroll();
  initMobileMenu();
  initAvatarDropdown();
  initTicker();
  renderScreenshots();
  initLightboxEvents();
  await initFirebase();
  await loadAllVersions();
});

document.addEventListener('keydown', e => {
  if (e.key === 'Escape') closeLightboxBtn();
  if (e.key === 'ArrowLeft')  lightboxNav(-1);
  if (e.key === 'ArrowRight') lightboxNav(1);
});

function applyConfig() {
  const C = SITE_CONFIG;
  const s = (id,v) => { const e=document.getElementById(id); if(e) e.textContent=v; };
  document.title = C.appNameRu + ' — ' + C.appNameSystem;
  s('nav-app-name',     C.appShort);
  s('hero-app-name-ru', C.appNameRu);
  s('hero-app-name-en', C.appNameSystem);
  s('hero-subtitle',    C.heroSubtitle);
  s('dl-main-title',    C.appNameRu);
  s('dl-extra-title',   C.extraAppNameRu);
  s('footer-app-name',  C.appShort);
  s('footer-copy',      '© ' + new Date().getFullYear() + ' ' + C.appNameSystem);
  s('mockup-title',     C.appNameSystem + ' — Карта маршрутов');
  const fav = document.getElementById('site-favicon');
  if (fav) fav.href = C.icons.mainApp;
  // Telegram кнопка в футере
  const tgLink = document.getElementById('footer-telegram-link');
  if (tgLink) {
    const tg = C.contact?.telegram || '';
    if (tg) { tgLink.href = tg; tgLink.style.display = ''; }
    else     { tgLink.style.display = 'none'; }
  }
}

/* ============================================================
   FIREBASE
   ============================================================ */
async function initFirebase() {
  const cfg = SITE_CONFIG.firebase;
  if (!cfg.apiKey || cfg.apiKey.startsWith('ВАШ')) {
    showToast('Firebase не настроен. Заполни js/config.js', 'error', 6000); return;
  }
  try {
    firebase.initializeApp(cfg);
    db   = firebase.firestore();
    auth = firebase.auth();
    auth.onAuthStateChanged(onAuthStateChanged);
  } catch(err) { console.error('Firebase init error:', err); }
}

async function onAuthStateChanged(user) {
  currentUser = user;
  if (user) {
    await ensureUserInFirestore(user);
    await loadUserData(user.uid);
  } else { userData = null; }
  updateNavAuth(user);
  updateAuthSection(user);
  updateExtraDownloadCard();
}

async function ensureUserInFirestore(user) {
  if (!db) return;
  try {
    const ref = db.collection('users').doc(user.uid);
    const doc = await ref.get();
    if (!doc.exists) {
      await ref.set({
        email: user.email, name: user.displayName||'', photoURL: user.photoURL||'',
        isModerator: false, isAdmin: false, requestStatus: null,
        registeredAt: firebase.firestore.FieldValue.serverTimestamp(),
      });
    } else {
      await ref.set({
        email: user.email,
        name: user.displayName || doc.data().name || '',
        photoURL: user.photoURL || doc.data().photoURL || ''
      }, { merge: true });
    }
  } catch(err) { console.warn('Firestore write:', err); }
}

async function loadUserData(uid) {
  if (!db) return;
  try {
    const d = await db.collection('users').doc(uid).get();
    if(d.exists) userData = d.data();
  } catch(e) { console.warn('loadUserData:', e); }
}

/* ============================================================
   РОЛИ
   ============================================================ */
function isSuperAdmin(user) { return user && user.email === SITE_CONFIG.superAdminEmail; }
function isAdminRole(user, data) { return isSuperAdmin(user) || data?.isAdmin === true; }
function isModeratorRole(data) { return data?.isModerator === true; }

/* ============================================================
   АВТОРИЗАЦИЯ
   ============================================================ */
async function handleGoogleSignIn() {
  if (!auth) { showToast('Firebase не инициализирован', 'error'); return; }
  try {
    await auth.signInWithPopup(new firebase.auth.GoogleAuthProvider());
    showToast('Вход выполнен!', 'success');
  } catch(err) {
    if(err.code !== 'auth/popup-closed-by-user')
      showToast('Ошибка: '+(err.message||err.code), 'error');
  }
}
async function handleLogout() {
  if (auth) { await auth.signOut(); showToast('Выход выполнен','info'); }
}
function handleAuthClick() { document.getElementById('auth')?.scrollIntoView({behavior:'smooth'}); }

/* ============================================================
   МОБИЛЬНОЕ МЕНЮ
   ============================================================ */
function initMobileMenu() {
  const burger  = document.getElementById('nav-burger');
  const drawer  = document.getElementById('mobile-menu-drawer');
  const overlay = document.getElementById('mobile-menu-overlay');
  if (!burger || !drawer) return;
  burger.addEventListener('click', () => {
    const open = drawer.classList.toggle('open');
    burger.classList.toggle('active', open);
    if (overlay) overlay.classList.toggle('visible', open);
  });
  if (overlay) overlay.addEventListener('click', closeMobileMenu);
  drawer.querySelectorAll('a').forEach(a => a.addEventListener('click', closeMobileMenu));
}
function closeMobileMenu() {
  document.getElementById('nav-burger')?.classList.remove('active');
  document.getElementById('mobile-menu-drawer')?.classList.remove('open');
  document.getElementById('mobile-menu-overlay')?.classList.remove('visible');
}

/* ============================================================
   AVATAR DROPDOWN
   ============================================================ */
function initAvatarDropdown() {
  const trigger  = document.getElementById('nav-avatar-trigger');
  const dropdown = document.getElementById('nav-avatar-dropdown');
  if (!trigger || !dropdown) return;
  trigger.addEventListener('click', e => { e.stopPropagation(); dropdown.classList.toggle('open'); });
  document.addEventListener('click', () => dropdown.classList.remove('open'));
}

/* ============================================================
   НАВБАР
   ============================================================ */
function updateNavAuth(user) {
  const loginBtn       = document.getElementById('nav-login-btn');
  const widget         = document.getElementById('nav-user-widget');
  const avatarImg      = document.getElementById('nav-avatar-img');
  const avatarPh       = document.getElementById('nav-avatar-placeholder');
  const nameEl         = document.getElementById('nav-user-name');
  const adminLink      = document.getElementById('nav-admin-link');
  const ddName         = document.getElementById('nav-dd-name');
  const ddEmail        = document.getElementById('nav-dd-email');
  const mobileAdmin    = document.getElementById('mobile-admin-link');
  const mobileLogin    = document.getElementById('mobile-login-btn');
  const mobileLogout   = document.getElementById('mobile-logout-btn');

  if (!user) {
    if(loginBtn)    loginBtn.style.display = 'block';
    if(widget)      widget.style.display   = 'none';
    if(adminLink)   adminLink.style.display= 'none';
    if(mobileAdmin) mobileAdmin.style.display='none';
    if(mobileLogin) mobileLogin.style.display='block';
    if(mobileLogout)mobileLogout.style.display='none';
    return;
  }

  if(loginBtn)  loginBtn.style.display  = 'none';
  if(widget)    widget.style.display    = 'flex';

  if (user.photoURL) {
    if(avatarImg){ avatarImg.src=user.photoURL; avatarImg.style.display='block'; }
    if(avatarPh)   avatarPh.style.display='none';
  } else {
    if(avatarImg)  avatarImg.style.display='none';
    if(avatarPh)   avatarPh.style.display='grid';
  }

  const firstName = user.displayName?.split(' ')[0] || user.email;
  if(nameEl)  nameEl.textContent  = firstName;
  if(ddName)  ddName.textContent  = user.displayName || firstName;
  if(ddEmail) ddEmail.textContent = user.email;

  const showAdmin = isAdminRole(user, userData);
  if(adminLink)   adminLink.style.display  = showAdmin ? 'block' : 'none';
  if(mobileAdmin) mobileAdmin.style.display= showAdmin ? 'block' : 'none';
  if(mobileLogin) mobileLogin.style.display='none';
  if(mobileLogout)mobileLogout.style.display='block';
}

/* ============================================================
   БЛОК АВТОРИЗАЦИИ (Личный кабинет)
   ============================================================ */
function updateAuthSection(user) {
  const loginCard = document.getElementById('login-card');
  const userCard  = document.getElementById('user-card');
  if (!user) { loginCard.style.display='block'; userCard.style.display='none'; return; }
  loginCard.style.display='none'; userCard.style.display='block';

  const av  = document.getElementById('user-avatar');
  const avp = document.getElementById('user-avatar-placeholder');
  if (user.photoURL) { av.src=user.photoURL; av.style.display='block'; avp.style.display='none'; }
  else               { av.style.display='none'; avp.style.display='grid'; }

  document.getElementById('user-display-name').textContent  = user.displayName || 'Пользователь';
  document.getElementById('user-display-email').textContent = user.email;

  const badge   = document.getElementById('user-role-badge');
  const sb      = document.getElementById('status-block');
  const baseStyle = 'padding:14px 16px;border-radius:var(--radius-md);text-align:left;font-size:0.85rem;margin-bottom:20px;';

  if (isSuperAdmin(user)) {
    badge.textContent='Главный администратор';
    badge.style.cssText='background:rgba(212,168,83,0.15);color:#d4a853;border:1px solid rgba(212,168,83,0.35)';
    sb.innerHTML='👑 Вы главный администратор. Полный доступ.';
    sb.style.cssText=baseStyle+'background:rgba(212,168,83,0.08);border:1px solid rgba(212,168,83,0.25);color:var(--gold)';
  } else if (isAdminRole(user, userData)) {
    badge.textContent='Администратор';
    badge.style.cssText='background:rgba(212,168,83,0.15);color:#d4a853;border:1px solid rgba(212,168,83,0.35)';
    sb.innerHTML='✅ Вы администратор. Доступна панель управления.';
    sb.style.cssText=baseStyle+'background:rgba(212,168,83,0.08);border:1px solid rgba(212,168,83,0.25);color:var(--gold)';
  } else if (isModeratorRole(userData)) {
    badge.textContent='Модератор';
    badge.style.cssText='background:rgba(79,156,249,0.12);color:#4f9cf9;border:1px solid rgba(79,156,249,0.3)';
    sb.innerHTML='✅ Вы модератор. Доступно дополнительное приложение.';
    sb.style.cssText=baseStyle+'background:rgba(79,156,249,0.08);border:1px solid rgba(79,156,249,0.25);color:var(--blue)';
  } else {
    badge.textContent='Пользователь';
    badge.style.cssText='background:rgba(255,255,255,0.05);color:var(--text-muted);border:1px solid var(--border)';
    const rs = userData?.requestStatus;
    if      (rs==='pending')  sb.innerHTML='⏳ Заявка подана. Ожидайте решения администратора.';
    else if (rs==='rejected') sb.innerHTML='❌ Заявка отклонена администратором.';
    else sb.innerHTML='Вы вошли в аккаунт. Подайте заявку для получения доступа к дополнительному приложению.';
    sb.style.cssText=baseStyle+'background:rgba(255,255,255,0.04);border:1px solid var(--border);color:var(--text-secondary)';
  }
}

/* ============================================================
   ДОП. ПРИЛОЖЕНИЕ — блок скачивания
   ============================================================ */
async function updateExtraDownloadCard() {
  const lockInfo    = document.getElementById('extra-lock-info');
  const lockText    = document.getElementById('extra-lock-text');
  const requestWrap = document.getElementById('request-btn-wrap');
  const statusWrap  = document.getElementById('request-status-wrap');
  const btnWrap     = document.getElementById('extra-btn-wrap');
  const extraCard   = document.getElementById('extra-card');
  const accessLabel = document.getElementById('extra-access-label');

  lockInfo.style.display='none'; requestWrap.style.display='none';
  statusWrap.style.display='none'; btnWrap.style.display='none';
  extraCard.classList.remove('unlocked');

  if (!currentUser) {
    lockInfo.style.display='flex';
    lockText.textContent='Войдите через Google, чтобы подать заявку.';
    accessLabel.textContent='Ограниченный'; accessLabel.style.color='var(--text-muted)'; return;
  }

  if (isAdminRole(currentUser, userData) || isModeratorRole(userData)) {
    extraCard.classList.add('unlocked');
    accessLabel.textContent='Открыт'; accessLabel.style.color='var(--blue)';

    if (extraVersionsList.length > 0 || extraDlUrl) {
      btnWrap.style.display='block';
      // Устанавливаем текущий выбор
      const sel = document.getElementById('extra-version-select');
      if (sel && sel.options.length > 0) {
        extraDlUrl = sel.options[sel.selectedIndex].value;
      }
    } else {
      lockInfo.style.display='flex';
      lockText.textContent='✅ Доступ открыт. Администратор ещё не загрузил файл.';
    }
    return;
  }

  accessLabel.textContent='Ограниченный'; accessLabel.style.color='var(--text-muted)';
  const rs = userData?.requestStatus;
  if (!rs) {
    lockInfo.style.display='flex';
    lockText.textContent='Нажмите кнопку ниже, чтобы подать заявку администратору.';
    requestWrap.style.display='block';
  } else if (rs==='pending') {
    statusWrap.style.display='block';
    statusWrap.style.cssText='display:block;margin-top:16px;padding:12px 16px;border-radius:var(--radius-md);border:1px solid rgba(212,168,83,0.25);background:rgba(212,168,83,0.08);font-size:0.83rem;text-align:center;color:var(--gold)';
    statusWrap.textContent='⏳ Заявка отправлена. Ожидайте решения администратора.';
  } else if (rs==='rejected') {
    statusWrap.style.display='block';
    statusWrap.style.cssText='display:block;margin-top:16px;padding:12px 16px;border-radius:var(--radius-md);border:1px solid rgba(239,68,68,0.25);background:rgba(239,68,68,0.08);font-size:0.83rem;text-align:center;color:#f87171';
    statusWrap.textContent='❌ Заявка отклонена. Обратитесь к администратору напрямую.';
  }
}

async function handleSendRequest() {
  if (!currentUser || !db) return;
  const btn = document.getElementById('request-btn');
  if (btn) { btn.disabled=true; btn.textContent='Отправка...'; }
  try {
    await db.collection('users').doc(currentUser.uid).set({
  requestStatus: 'pending',
  requestedAt:   firebase.firestore.FieldValue.serverTimestamp(),
}, { merge: true });
    if (userData) userData.requestStatus = 'pending';
    showToast('Заявка отправлена!', 'success');
    await updateExtraDownloadCard();
    updateAuthSection(currentUser);
  } catch(err) {
    showToast('Ошибка: '+err.message, 'error');
    if (btn) { btn.disabled=false; btn.textContent='📨 Подать заявку на доступ'; }
  }
}

/* ============================================================
   ВЕРСИИ — загрузка с сервера через manifest.json
   ============================================================ */
async function loadAllVersions() {
  await Promise.all([
    loadVersionsForApp('main'),
    loadVersionsForApp('extra'),
  ]);
}

async function loadVersionsForApp(type) {
  const U           = SITE_CONFIG.urls;
  const manifestUrl = type === 'main' ? U.mainManifest : U.extraManifest;
  const versionBase = type === 'main' ? U.mainVersionBase : U.extraVersionBase;
  const selectId    = type === 'main' ? 'main-version-select' : 'extra-version-select';

  try {
    const res = await fetch(manifestUrl + '?_=' + Date.now()); // без кеша
    if (!res.ok) throw new Error('Manifest not found: ' + res.status);
    const manifest = await res.json();

    // Манифест содержит массив versions[] — первый элемент самый новый
    const versions = manifest.versions;
    if (!Array.isArray(versions) || versions.length === 0)
      throw new Error('versions[] не найден или пуст в манифесте');

    const list = versions.map(v => {
      // Для папки: пробел → %20 ("0.1.24 alpha" → "v0.1.24%20alpha/")
      // Для имени файла: пробел → _ ("0.1.24 alpha" → "PostalApp_Setup_v0.1.24_alpha.exe")
      const urlVersion  = v.replace(/ /g, '%20');
      const fileVersion = v.replace(/ /g, '_');
      const url = type === 'main'
        ? `${versionBase}v${urlVersion}/PostalApp_Setup_v${fileVersion}.exe`
        : `${versionBase}v${urlVersion}/PostalApp-Extra_Setup_v${fileVersion}.exe`;
      return { url, version: v };
    });

    if (type === 'main') {
      mainVersionsList = list;
      mainDlUrl        = list[0].url;
      populateVersionSelect(selectId, list, 'main');
      const badge = document.getElementById('hero-version-badge');
      if (badge) badge.textContent = 'v' + list[0].version;
    } else {
      extraVersionsList = list;
      extraDlUrl        = list[0].url;
      populateVersionSelect(selectId, list, 'extra');
    }

  } catch(err) {
    console.warn('Version load error (' + type + '):', err);
    if (type === 'main') {
      setVersionSelectError(selectId);
      const b = document.getElementById('main-dl-btn');
      if (b) b.disabled = true;
    }
    // Для extra — ошибка некритична, файла может ещё не быть
  }
}

function populateVersionSelect(selectId, list, type) {
  const sel = document.getElementById(selectId);
  if (!sel) return;
  sel.innerHTML = list.map((item, i) =>
    `<option value="${escHtml(item.url)}"${i===0?'selected':''}>v${escHtml(item.version)}${i===0?' (последняя)':''}</option>`
  ).join('');
  sel.style.display = 'block';

  // Скрываем заглушку-текст
  const placeholder = document.getElementById(selectId + '-placeholder');
  if (placeholder) placeholder.style.display = 'none';

  // Вешаем обработчик смены версии
  sel.onchange = () => {
    if (type === 'main') mainDlUrl = sel.value;
    else                 extraDlUrl = sel.value;
  };
}

function setVersionSelectError(selectId) {
  const sel = document.getElementById(selectId);
  if (sel) {
    sel.innerHTML = '<option>Ошибка загрузки</option>';
    sel.style.display = 'block';
    sel.disabled = true;
  }
}

function compareVersions(a, b) {
  // Разбивает "X.X.X суффикс" на числа и суффикс
  const split = v => {
    const sp  = v.indexOf(' ');
    const num = sp >= 0 ? v.slice(0, sp) : v;
    const lbl = sp >= 0 ? v.slice(sp + 1).trim().toLowerCase() : '';
    return { nums: num.split('.').map(Number), lbl };
  };
  // Порядок суффиксов: alpha < beta < rc < "" (release)
  const rank = s => ({ '': 4, 'rc': 3, 'beta': 2, 'alpha': 1 }[s] ?? 0);

  const pa = split(a), pb = split(b);
  const len = Math.max(pa.nums.length, pb.nums.length);
  for (let i = 0; i < len; i++) {
    if ((pa.nums[i] || 0) > (pb.nums[i] || 0)) return 1;
    if ((pa.nums[i] || 0) < (pb.nums[i] || 0)) return -1;
  }
  const sr = rank(pa.lbl) - rank(pb.lbl);
  return sr > 0 ? 1 : sr < 0 ? -1 : 0;
}

/* ============================================================
   СКАЧИВАНИЕ
   ============================================================ */
function handleMainDownload() {
  // Берём актуальное значение из select на момент клика
  const sel = document.getElementById('main-version-select');
  if (sel && sel.value) mainDlUrl = sel.value;

  if (!mainDlUrl) {
    showToast('Файл не найден. Положи установщик в assets/postalapp/', 'error'); return;
  }
  triggerDownload(mainDlUrl);
  showToast('Скачивание началось...', 'success');
}

function handleExtraDownload() {
  const sel = document.getElementById('extra-version-select');
  if (sel && sel.value) extraDlUrl = sel.value;

  if (!extraDlUrl) {
    showToast('Ссылка не настроена администратором.', 'error'); return;
  }
  triggerDownload(extraDlUrl);
  showToast('Скачивание началось...', 'success');
}

function triggerDownload(url) {
  const a = document.createElement('a');
  a.href = url; a.download = '';
  document.body.appendChild(a); a.click(); document.body.removeChild(a);
}

/* ============================================================
   ВОЛНОВАЯ СЕТКА (canvas, только в секции hero)
   ============================================================ */
function initWaveGrid() {
  const cfg = SITE_CONFIG.waveGrid;
  if (!cfg || cfg.enabled === false) return;

  const canvas = document.getElementById('wave-grid-canvas');
  if (!canvas) return;
  const ctx = canvas.getContext('2d');

  let t = 0;

  function resize() {
    const hero = document.getElementById('home');
    if (hero) {
      canvas.width  = hero.offsetWidth;
      canvas.height = hero.offsetHeight;
    } else {
      canvas.width  = window.innerWidth;
      canvas.height = window.innerHeight;
    }
  }
  window.addEventListener('resize', resize, { passive:true });
  resize();

  function draw() {
    const {
      cellSize, lineColor, lineWidth, opacity,
      hAmplitude, hFrequency, hSpeed,
      vAmplitude, vFrequency, vSpeed,
    } = cfg;

    ctx.clearRect(0, 0, canvas.width, canvas.height);
    ctx.strokeStyle = lineColor;
    ctx.lineWidth   = lineWidth;
    ctx.globalAlpha = opacity;

    // Горизонтальные линии — волны по вертикали
    for (let row = 0; row * cellSize <= canvas.height + cellSize; row++) {
      const yBase = row * cellSize;
      ctx.beginPath();
      for (let x = 0; x <= canvas.width; x += 3) {
        const dy = hAmplitude * Math.sin(x * hFrequency * 0.01 + t * hSpeed);
        if (x === 0) ctx.moveTo(x, yBase + dy);
        else         ctx.lineTo(x, yBase + dy);
      }
      ctx.stroke();
    }

    // Вертикальные линии — волны по горизонтали
    for (let col = 0; col * cellSize <= canvas.width + cellSize; col++) {
      const xBase = col * cellSize;
      ctx.beginPath();
      for (let y = 0; y <= canvas.height; y += 3) {
        const dx = vAmplitude * Math.sin(y * vFrequency * 0.01 + t * vSpeed);
        if (y === 0) ctx.moveTo(xBase + dx, y);
        else         ctx.lineTo(xBase + dx, y);
      }
      ctx.stroke();
    }

    t += 0.016;
    requestAnimationFrame(draw);
  }

  draw();
}
function initTicker() {
  const items = [
    'Оптимизация маршрутов алгоритмом 2-opt',
    'Интерактивная карта с OpenStreetMap',
    'Управление подписками и читателями',
    'Импорт данных из Excel с валидацией',
    'Постановка задач сотрудникам',
    'Автообновления с проверкой целостности MD5',
    'SSL Pinning для защиты соединения',
    'Хеширование паролей PBKDF2',
    'Балансировка участков алгоритмом K-means',
    'Построение полигонов по алгоритму Грэма',
    'Расчёт времени и расстояния доставки по маршруту',
    'Ролевое разграничение прав доступа',
    'Клиент-серверная архитектура',
    ];

  function buildTrack(trackEl, list) {
    const doubled = [...list, ...list];
    trackEl.innerHTML = doubled.map(t =>
      `<span class="ticker-item"><span class="ticker-dot"></span>${t}</span>`
    ).join('');
  }

  const speed = SITE_CONFIG.tickerSpeed || { row1: 44, row2: 48 };
  const t1 = document.getElementById('ticker-track-1');
  const t2 = document.getElementById('ticker-track-2');
  if (t1) { buildTrack(t1, items); t1.style.animationDuration = speed.row1 + 's'; }
  if (t2) { buildTrack(t2, [...items].reverse()); t2.style.animationDuration = speed.row2 + 's'; }
}

/* ============================================================
   COVERFLOW КАРУСЕЛЬ СКРИНШОТОВ
   ============================================================ */
let carouselIndex = 0;
let carouselItems = [];
let cfDragStartX  = null;

function renderScreenshots() {
  const shots = SITE_CONFIG.screenshots || [];
  const valid  = shots.filter(s => s.src);
  const container = document.getElementById('carousel-container');
  if (!valid.length) {
    const sec = document.getElementById('screenshots');
    if (sec) sec.style.display = 'none';
    return;
  }
  carouselItems = valid;
  if (container) container.style.display = 'block';

  const track = document.getElementById('carousel-track');
  const dots   = document.getElementById('carousel-dots');
  if (!track || !dots) return;

  track.innerHTML = valid.map((s, i) => `
    <div class="cf-slide" data-index="${i}" onclick="cfHandleClick(${i})">
      <img src="${escHtml(s.src)}" alt="${escHtml(s.caption)}" loading="lazy" draggable="false">
      <div class="cf-caption">${escHtml(s.caption)}</div>
    </div>`).join('');

  // Точки создаём в визуальном порядке ...,5,3,1,2,4,...
  // Вычисляем смещение для каждого слайда при carouselIndex=0
  const total = valid.length;
  const visualOrder = valid.map((_, i) => {
    const raw = i;
    let offset;
    if (raw === 0)          offset = 0;
    else if (raw % 2 === 1) offset = (raw + 1) / 2;
    else                    offset = -(raw / 2);
    return { rawIdx: i, offset };
  }).sort((a, b) => a.offset - b.offset).map(x => x.rawIdx);

  dots.innerHTML = visualOrder.map(rawIdx =>
    `<button class="carousel-dot${rawIdx === 0 ? ' active' : ''}" data-raw="${rawIdx}" onclick="carouselGoTo(${rawIdx})" aria-label="Слайд ${rawIdx + 1}"></button>`
  ).join('');

  // Свайп тач
  track.addEventListener('touchstart', e => { cfDragStartX = e.touches[0].clientX; }, { passive:true });
  track.addEventListener('touchend',   e => {
    if (cfDragStartX === null) return;
    const dx = e.changedTouches[0].clientX - cfDragStartX;
    if (Math.abs(dx) > 40) carouselMove(dx < 0 ? 1 : -1);
    cfDragStartX = null;
  });

  // Свайп мышью
  let mdX = null, mdMoved = false;
  track.addEventListener('mousedown', e => { mdX = e.clientX; mdMoved = false; });
  track.addEventListener('mousemove', e => { if (mdX !== null && Math.abs(e.clientX - mdX) > 5) mdMoved = true; });
  track.addEventListener('mouseup',   e => {
    if (mdX === null) return;
    const dx = e.clientX - mdX;
    if (Math.abs(dx) > 40) carouselMove(dx < 0 ? 1 : -1);
    mdX = null;
  });

  cfUpdate();
}

function cfHandleClick(idx) {
  if (idx === carouselIndex) openLightbox(idx);
  else carouselGoTo(idx);
}

function cfUpdate() {
  const slides = document.querySelectorAll('.cf-slide');
  const total  = carouselItems.length;
  slides.forEach((sl, i) => {
    // Чередующийся порядок: ...,5,3,1,2,4,...
    // raw — расстояние по кругу от активного
    const raw = ((i - carouselIndex) + total) % total;
    // Нечётные raw → правая сторона (+), чётные ненулевые → левая (-)
    let offset;
    if (raw === 0)          offset = 0;
    else if (raw % 2 === 1) offset = (raw + 1) / 2;   // 1→+1, 3→+2, 5→+3
    else                    offset = -(raw / 2);        // 2→-1, 4→-2, 6→-3

    const abs    = Math.abs(offset);
    const tx     = offset * 30;
    const sc     = Math.max(0.52, 1 - abs * 0.15);
    const ry     = offset > 0 ? -28 : offset < 0 ? 28 : 0;
    const z      = total - abs;
    const op     = abs > 2 ? 0 : Math.max(0.28, 1 - abs * 0.3);
    const bright = abs > 0 ? Math.max(0.42, 1 - abs * 0.24) : 1;

    sl.style.transform  = `translateX(${tx}%) scale(${sc}) rotateY(${ry}deg)`;
    sl.style.zIndex     = z;
    sl.style.opacity    = op;
    sl.style.filter     = `brightness(${bright})`;
    sl.classList.toggle('cf-active', i === carouselIndex);
    sl.style.cursor     = i === carouselIndex ? 'zoom-in' : 'pointer';
    sl.style.pointerEvents = abs > 2 ? 'none' : 'auto';
  });

  document.querySelectorAll('.carousel-dot').forEach(d =>
    d.classList.toggle('active', parseInt(d.dataset.raw) === carouselIndex)
  );
  updateCarouselButtons();
}

function carouselGoTo(idx) {
  const total = carouselItems.length;
  carouselIndex = ((idx % total) + total) % total; // зацикливание
  cfUpdate();
}

function carouselMove(dir) { carouselGoTo(carouselIndex + dir); }

function updateCarouselButtons() {
  // Кнопки никогда не блокируются — карусель зациклена
  const prev = document.getElementById('carousel-prev');
  const next = document.getElementById('carousel-next');
  if (prev) prev.disabled = false;
  if (next) next.disabled = false;
}

/* ============================================================
   ЛАЙТБОКС с зумом и навигацией
   ============================================================ */
let lbIndex   = 0;
let lbZoom    = 1;
let lbPanX    = 0;
let lbPanY    = 0;
let lbDragging= false;
let lbDragStart = {x:0,y:0};

function openLightbox(idx) {
  lbIndex = idx;
  lbZoom  = 1; lbPanX = 0; lbPanY = 0;
  const shots = carouselItems;
  const s = shots[lbIndex];
  const img = document.getElementById('lightbox-img');
  const cap = document.getElementById('lightbox-caption');
  if (img) { img.src = s.src; img.alt = s.caption; }
  if (cap) cap.textContent = s.caption;
  applyLbTransform();
  updateLbNav();
  const lb = document.getElementById('lightbox');
  if (lb) lb.classList.remove('hidden');
  document.body.style.overflow = 'hidden';
}

function closeLightboxBtn() {
  const lb = document.getElementById('lightbox');
  if (lb) lb.classList.add('hidden');
  document.body.style.overflow = '';
}

function lightboxNav(dir) {
  const shots = carouselItems;
  const total = shots.length;
  lbIndex = ((lbIndex + dir) % total + total) % total; // зацикливание
  lbZoom = 1; lbPanX = 0; lbPanY = 0;
  const s = shots[lbIndex];
  const img = document.getElementById('lightbox-img');
  const cap = document.getElementById('lightbox-caption');
  if (img) { img.src = s.src; img.alt = s.caption; }
  if (cap) cap.textContent = s.caption;
  applyLbTransform();
  updateLbNav();
}

function updateLbNav() {
  // Кнопки никогда не блокируются — лайтбокс зациклен
  const prev = document.getElementById('lb-prev');
  const next = document.getElementById('lb-next');
  if (prev) prev.disabled = false;
  if (next) next.disabled = false;
}

function applyLbTransform() {
  const wrap = document.getElementById('lightbox-img-wrap');
  if (!wrap) return;
  wrap.style.transform = `translate(${lbPanX}px,${lbPanY}px) scale(${lbZoom})`;
  wrap.style.transformOrigin = 'center center';
  const slider = document.getElementById('lightbox-zoom-slider');
  if (slider) slider.value = lbZoom;
}

function setZoomFromSlider(val) {
  lbZoom = val; lbPanX = 0; lbPanY = 0; applyLbTransform();
}

function initLightboxEvents() {
  const vp = document.getElementById('lightbox-viewport');
  if (!vp) return;

  // Колёсико — зум относительно позиции курсора
  vp.addEventListener('wheel', e => {
    e.preventDefault();
    const rect = vp.getBoundingClientRect();
    const cx = e.clientX - rect.left - rect.width/2;
    const cy = e.clientY - rect.top  - rect.height/2;
    const factor = e.deltaY < 0 ? 1.12 : 0.88;
    const newZoom = Math.max(0.5, Math.min(5, lbZoom * factor));
    lbPanX = cx - (cx - lbPanX) * (newZoom / lbZoom);
    lbPanY = cy - (cy - lbPanY) * (newZoom / lbZoom);
    lbZoom = newZoom;
    applyLbTransform();
  }, { passive:false });

  // Drag — работает при любом зуме, с границами из config
  vp.addEventListener('mousedown', e => {
    if (e.button !== 0) return;
    lbDragging = true;
    lbDragStart = {x: e.clientX - lbPanX, y: e.clientY - lbPanY};
    vp.classList.add('grabbing');
    e.preventDefault();
  });

  window.addEventListener('mousemove', e => {
    if (!lbDragging) return;
    const limit = (SITE_CONFIG.lightboxPanLimit || 600) * lbZoom;
    lbPanX = Math.max(-limit, Math.min(limit, e.clientX - lbDragStart.x));
    lbPanY = Math.max(-limit, Math.min(limit, e.clientY - lbDragStart.y));
    const wrap = document.getElementById('lightbox-img-wrap');
    if (wrap) wrap.style.transform = `translate(${lbPanX}px,${lbPanY}px) scale(${lbZoom})`;
  });

  window.addEventListener('mouseup', e => {
    if (!lbDragging) return;
    lbDragging = false;
    const vp2 = document.getElementById('lightbox-viewport');
    if (vp2) vp2.classList.remove('grabbing');
    const slider = document.getElementById('lightbox-zoom-slider');
    if (slider) slider.value = lbZoom;
  });

  // Закрытие по клику на фон (только если не было drag)
  const lb = document.getElementById('lightbox');
  if (lb) lb.addEventListener('click', e => {
    if (!lbDragging && (e.target === lb || e.target.id === 'lightbox-content')) closeLightboxBtn();
  });
}

/* ============================================================
   TOAST
   ============================================================ */
function showToast(msg, type='info', dur=3500) {
  const c = document.getElementById('toast-container'); if(!c) return;
  const icons = {success:'✅', error:'❌', info:'ℹ️'};
  const t = document.createElement('div'); t.className=`toast toast-${type}`;
  t.innerHTML=`<span>${icons[type]||'•'}</span><span>${msg}</span>`;
  c.appendChild(t);
  setTimeout(()=>{ t.style.animation='toastIn 0.3s ease reverse both'; setTimeout(()=>t.remove(),300); }, dur);
}

/* ============================================================
   SCROLL / REVEAL / UTILS
   ============================================================ */
function initRevealAnimations() {
  const obs = new IntersectionObserver(entries => {
    entries.forEach(e => { if(e.isIntersecting) e.target.classList.add('visible'); });
  }, {threshold:0.12});
  document.querySelectorAll('.reveal').forEach((el,i) => {
    el.style.transitionDelay = (i%4)*0.08+'s'; obs.observe(el);
  });
}
function initNavScroll() {
  const nav = document.getElementById('navbar'); if(!nav) return;
  window.addEventListener('scroll',()=>nav.classList.toggle('scrolled',window.scrollY>20),{passive:true});
}
function escHtml(str) {
  return String(str||'').replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;');
}