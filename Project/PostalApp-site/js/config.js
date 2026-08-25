// ============================================================
//  КОНФИГУРАЦИЯ САЙТА — редактируй только этот файл
// ============================================================

const SITE_CONFIG = {

  // --- НАЗВАНИЯ ПРИЛОЖЕНИЙ ---
  appNameSystem:      "PostalApp",
  appNameRu:          "Почтовое приложение",
  appShort:           "PostalApp",
  extraAppNameRu:     "Инструментальное приложение",
  extraAppNameSystem: "PostalApp Extra",

  // --- ГЛАВНЫЙ АДМИНИСТРАТОР (фиксирован, нельзя разжаловать) ---
  superAdminEmail: "rozkovvitalij04@gmail.com",

  // --- ИКОНКИ ---
  icons: {
    mainApp:  "assets/icons/app-main.png",
    extraApp: "assets/icons/app-extra.png",
  },

  // --- URL-Ы ДЛЯ ЗАГРУЗКИ ВЕРСИЙ ---
  // mainManifest    — глобальный манифест основного приложения
  // mainVersionBase — базовый URL папки с версиями (до папки vX.X.X/)
  // extraManifest   — манифест доп. приложения
  // extraVersionBase — базовый URL папки с файлами доп. приложения (до папки vX.X.X/)
  urls: {
    mainManifest:     '/updates/manifest.json',
    mainVersionBase:  '/updates/versions/',
    extraManifest:    '/extra-updates/manifest.json',
    extraVersionBase: '/extra-updates/versions/',
  },

  // --- FIREBASE ---
  firebase: {
    apiKey:            "AIzaSyD-Hb2uZmqhPWz3040OEkSJteKveQHSykY",
    authDomain:        "postalapp-site.firebaseapp.com",
    projectId:         "postalapp-site",
    storageBucket:     "postalapp-site.firebasestorage.app",
    messagingSenderId: "898238079739",
    appId:             "1:898238079739:web:0780d5b02b8427efc6865e",
  },

  // --- КОНТАКТЫ ---
  contact: {
    email:    "rozkovvitalij04@gmail.com",
    // Ссылка на Telegram канал или профиль. Если оставить пустой строкой — кнопка не показывается.
    telegram: "https://t.me/He11_caTnn",
  },

  heroSubtitle: "Профессиональное решение для почтальонов, операторов и руководителей. Маршруты, подписки, карта — всё в одном приложении.",

  // --- ВОЛНОВАЯ ФОНОВАЯ СЕТКА ---
  waveGrid: {
    enabled:    true,      // true/false — включить или выключить анимацию

    // Внешний вид сетки
    cellSize:   70,        // Размер ячейки в пикселях. Рекомендуется: 40–150
    lineColor:  'rgba(255,255,255,1)', // Цвет линий сетки (CSS-цвет или rgba)
    lineWidth:  0.4,       // Толщина линий. Рекомендуется: 0.2–2.0
    opacity:    0.06,     // Прозрачность всей сетки. Рекомендуется: 0.01–0.15

    // Горизонтальные линии — волна по вертикали (линии «качаются» вверх-вниз)
    hAmplitude: 5,         // Высота волны горизонтальных линий в пикселях. Рекомендуется: 0–30
    hFrequency: 1.2,       // Частота волны: сколько полных волн на ширину экрана. Рекомендуется: 0.3–5
    hSpeed:     0.5,       // Скорость анимации горизонтальных волн. Рекомендуется: 0.1–3. 0 = стоп

    // Вертикальные линии — волна по горизонтали (линии «качаются» влево-вправо)
    vAmplitude: 4,         // Высота волны вертикальных линий в пикселях. Рекомендуется: 0–30
    vFrequency: 1.0,       // Частота волны вертикальных линий. Рекомендуется: 0.3–5
    vSpeed:     0.4,       // Скорость анимации вертикальных волн. Рекомендуется: 0.1–3. 0 = стоп
  },

  // --- БЕГУЩАЯ СТРОКА ---
  // Скорость в секундах для одного цикла. Больше = медленнее. По умолчанию: 44 и 48.
  tickerSpeed: { row1: 44, row2: 48 },

  // --- ЛАЙТБОКС ---
  // Максимальное смещение картинки при перетаскивании в пикселях (при zoom=1).
  // При большем зуме граница пропорционально увеличивается.
  lightboxPanLimit: 600,

  screenshots: [
    { src: "assets/screenshot1.png", caption: "Карта с маршрутами" },
    { src: "assets/screenshot2.png", caption: "Управление подписками" },
    { src: "assets/screenshot3.png", caption: "Назначение задач" },
  ],
};