#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
manifests.py — Менеджер версий PostalApp / PostalApp-Extra

Структура папки рядом со скриптом:
  manifests.py
  config.cfg
  He11Catnn.pfx      (электронная подпись)
  He11Catnn.cer      (сертификат для встраивания в установщик)
  InnoSetup/
      PostalApp_Setup.iss
      PostalApp-Extra_Setup.iss
  PostalApp/
      manifest.json
      versions/
          v0.31/
              version_manifest.json
              update.zip
              PostalApp_Setup_v0.31.exe
  PostalApp-Extra/
      ...
"""

import os
import sys
import json
import hashlib
import zipfile
import subprocess
import configparser
import tempfile
from datetime import date
from pathlib import Path

# ─── Корень и константы ───────────────────────────────────────────────────────
SCRIPT_DIR    = Path(__file__).parent
CONFIG_FILE   = SCRIPT_DIR / "config.cfg"
INNO_DIR      = SCRIPT_DIR / "InnoSetup"
EXCLUDED_EXTS = {".pdb", ".xml", ".log"}

APPS = {
    "1": {
        "name":          "PostalApp",
        "label":         "PostalApp (почтовое)",
        "iss_file":      "PostalApp_Setup.iss",
        "out_dir":       SCRIPT_DIR / "PostalApp",
        "exe_name":      "PostalApp.exe",
        "setup_tpl":     "PostalApp_Setup_v{ver}",
        "excluded_dirs": {"logs"},
        "cfg_section":   "PostalApp",
        "server_path_key": "postalapp_server_path",
    },
    "2": {
        "name":          "PostalApp-Extra",
        "label":         "PostalApp-Extra (инструментальное)",
        "iss_file":      "PostalApp-Extra_Setup.iss",
        "out_dir":       SCRIPT_DIR / "PostalApp-Extra",
        "exe_name":      "PostalApp-Extra.exe",
        "setup_tpl":     "PostalApp-Extra_Setup_v{ver}",
        "excluded_dirs": {"data"},
        "cfg_section":   "PostalApp-Extra",
        "server_path_key": "postalapp_extra_server_path",
    },
}


# ═══════════════════════════════════════════════════════════════════════════════
# CONFIG
# ═══════════════════════════════════════════════════════════════════════════════

DEFAULT_CONFIG = r"""[PostalApp]
release_dir        = 
base_url           = http://81.90.25.60/updates/versions/v{version}/update.zip
server_upload_path = /var/www/postalapp_updates/versions

[PostalApp-Extra]
release_dir        = 
base_url           = http://81.90.25.60/extra-updates/versions/v{version}/update.zip
server_upload_path = /var/www/postalapp-extra_updates/versions

[Signing]
enabled       = true
signtool_path = D:\Windows Kits\10\bin\10.0.26100.0\x64\signtool.exe
pfx_path      = He11Catnn.pfx
pfx_password  = 
timestamp_url = http://timestamp.digicert.com

[InnoSetup]
iscc_path = C:\Program Files (x86)\Inno Setup 6\ISCC.exe

[Server]
host                        = 81.90.25.60
user                        = root
ssh_key_path                = 
postalapp_server_path       = /var/www/postalapp_updates/versions
postalapp_extra_server_path = /var/www/postalapp-extra_updates/versions
"""


def load_config() -> configparser.ConfigParser:
    cfg = configparser.ConfigParser()
    if not CONFIG_FILE.exists():
        CONFIG_FILE.write_text(DEFAULT_CONFIG, encoding="utf-8")
        print(f"  [CONFIG] Создан config.cfg: {CONFIG_FILE}")
    cfg.read(CONFIG_FILE, encoding="utf-8")
    return cfg


def save_config(cfg: configparser.ConfigParser) -> None:
    with open(CONFIG_FILE, "w", encoding="utf-8") as f:
        cfg.write(f)


def resolve_path(raw: str) -> Path:
    """Относительный путь разворачивает от SCRIPT_DIR."""
    p = Path(raw.strip().strip('"'))
    return p if p.is_absolute() else SCRIPT_DIR / p


def get_pfx(cfg: configparser.ConfigParser) -> Path:
    return resolve_path(cfg.get("Signing", "pfx_path", fallback="He11Catnn.pfx"))


# ═══════════════════════════════════════════════════════════════════════════════
# UI-УТИЛИТЫ
# ═══════════════════════════════════════════════════════════════════════════════

def cls():
    os.system("cls" if os.name == "nt" else "clear")


def hr(char="═", w=64):
    print(char * w)


def header(title: str):
    print()
    hr()
    print(f"  {title}")
    hr()
    print()


def press_enter():
    input("\n  Нажмите Enter для возврата в меню...")


def yn(prompt: str) -> bool:
    ans = input(f"  {prompt} [д/н]: ").strip().lower()
    return ans in ("д", "да", "y", "yes")


def choose_app(prompt: str = "Выберите приложение") -> str | None:
    print(f"  {prompt}:")
    for k, v in APPS.items():
        print(f"    [{k}] {v['label']}")
    print("    [0] Назад")
    while True:
        c = input("\n  Выбор: ").strip()
        if c == "0":
            return None
        if c in APPS:
            return c
        print("  [!] Введите 1, 2 или 0.")


def sanitize_ver(version: str) -> str:
    """'0.44 beta' → '0.44_beta'  (безопасно для имён файлов)"""
    return version.strip().replace(" ", "_")


# ═══════════════════════════════════════════════════════════════════════════════
# ПОДПИСАНИЕ
# ═══════════════════════════════════════════════════════════════════════════════

def sign_file(path: Path, cfg: configparser.ConfigParser, label: str = "") -> bool:
    if not cfg.getboolean("Signing", "enabled", fallback=True):
        return True

    signtool = resolve_path(cfg.get("Signing", "signtool_path", fallback=""))
    pfx      = get_pfx(cfg)
    password = cfg.get("Signing", "pfx_password", fallback="")
    ts_url   = cfg.get("Signing", "timestamp_url", fallback="http://timestamp.digicert.com")
    tag      = f"[{label}] " if label else ""

    for check, desc in ((signtool, "signtool.exe"), (pfx, "PFX"), (path, "целевой файл")):
        if not check.is_file():
            print(f"    {tag}[ОШИБКА] {desc} не найден: {check}")
            return False

    print(f"    {tag}Подписание: {path.name}")
    r = subprocess.run(
        [str(signtool), "sign",
         "/f", str(pfx), "/p", password,
         "/fd", "SHA256", "/tr", ts_url, "/td", "SHA256", "/v",
         str(path)],
        capture_output=True, text=True,
    )
    if r.returncode == 0:
        print(f"    {tag}✓ Подписан: {path.name}")
        return True
    print(f"    {tag}[ОШИБКА] signtool код {r.returncode}")
    for line in (r.stdout, r.stderr):
        if line.strip():
            print(f"      {line.strip()}")
    return False


def sign_exes_in_dir(release_dir: Path, excluded_dirs: set,
                     cfg: configparser.ConfigParser) -> None:
    if not cfg.getboolean("Signing", "enabled", fallback=True):
        return
    exe_files = [
        f for f in sorted(release_dir.rglob("*.exe"))
        if f.is_file()
        and not any(p.lower() in excluded_dirs
                    for p in f.relative_to(release_dir).parts)
    ]
    if not exe_files:
        print("    [!] .exe в Release не найдены — пропуск.")
        return
    print(f"    Найдено .exe: {len(exe_files)}")
    for exe in exe_files:
        sign_file(exe, cfg)


# ═══════════════════════════════════════════════════════════════════════════════
# ФАЙЛЫ / MD5 / ZIP
# ═══════════════════════════════════════════════════════════════════════════════

def md5_file(path: Path) -> str:
    h = hashlib.md5()
    with open(path, "rb") as f:
        for chunk in iter(lambda: f.read(65536), b""):
            h.update(chunk)
    return h.hexdigest()


def collect_files(release_dir: Path, excluded_dirs: set) -> list:
    result = []
    for file in sorted(release_dir.rglob("*")):
        if not file.is_file():
            continue
        parts = file.relative_to(release_dir).parts
        if any(p.lower() in excluded_dirs for p in parts):
            continue
        if file.suffix.lower() in EXCLUDED_EXTS:
            continue
        result.append({
            "path": str(file.relative_to(release_dir)).replace("\\", "/"),
            "md5":  md5_file(file),
            "size": file.stat().st_size,
        })
    return result


def create_update_zip(release_dir: Path, out_path: Path,
                      excluded_dirs: set) -> tuple[str, int]:
    with zipfile.ZipFile(out_path, "w", zipfile.ZIP_DEFLATED) as zf:
        for file in sorted(release_dir.rglob("*")):
            if not file.is_file():
                continue
            parts = file.relative_to(release_dir).parts
            if any(p.lower() in excluded_dirs for p in parts):
                continue
            if file.suffix.lower() in EXCLUDED_EXTS:
                continue
            zf.write(file, str(file.relative_to(release_dir)).replace("\\", "/"))
    checksum = md5_file(out_path)
    size     = out_path.stat().st_size
    return checksum, size


# ═══════════════════════════════════════════════════════════════════════════════
# INNO SETUP
# ═══════════════════════════════════════════════════════════════════════════════

def run_iscc(iss_path: Path, replacements: dict,
             cfg: configparser.ConfigParser) -> bool:
    """
    Читает .iss шаблон, заменяет ##PLACEHOLDER##,
    сохраняет во временный файл, запускает ISCC.exe.
    Возвращает True при успехе.
    """
    iscc_raw = cfg.get("InnoSetup", "iscc_path",
                       fallback=r"C:\Program Files (x86)\Inno Setup 6\ISCC.exe")
    iscc = resolve_path(iscc_raw)
    if not iscc.is_file():
        print(f"    [ОШИБКА] ISCC.exe не найден: {iscc}")
        print(f"    Укажите правильный путь в config.cfg → [InnoSetup] iscc_path")
        return False

    template = iss_path.read_text(encoding="utf-8")
    for key, value in replacements.items():
        template = template.replace(f"##{key}##", str(value))

    tmp_iss = Path(tempfile.mktemp(suffix="_postal.iss"))
    try:
        tmp_iss.write_text(template, encoding="utf-8")
        print(f"    Запуск: {iscc.name}")
        r = subprocess.run(
            [str(iscc), str(tmp_iss)],
            capture_output=True, text=True,
            encoding="cp1251", errors="replace",
        )
        if r.returncode == 0:
            print("    ✓ Установщик собран.")
            return True
        print(f"    [ОШИБКА] ISCC код {r.returncode}")
        # Показываем конец вывода ISCC (часто там полезная строка)
        for block in (r.stdout, r.stderr):
            tail = block.strip()[-1500:] if block.strip() else ""
            if tail:
                print(tail)
        return False
    finally:
        if tmp_iss.exists():
            tmp_iss.unlink()


# ═══════════════════════════════════════════════════════════════════════════════
# ГЛОБАЛЬНЫЙ МАНИФЕСТ
# ═══════════════════════════════════════════════════════════════════════════════

def _manifest_path(app: dict) -> Path:
    return app["out_dir"] / "manifest.json"


def load_global_manifest(app: dict) -> dict:
    p = _manifest_path(app)
    if p.exists():
        with open(p, encoding="utf-8") as f:
            return json.load(f)
    return {"versions": [], "downloadUrl": "", "fileSize": 0, "checksum": ""}


def save_global_manifest(app: dict, manifest: dict) -> None:
    app["out_dir"].mkdir(parents=True, exist_ok=True)
    with open(_manifest_path(app), "w", encoding="utf-8") as f:
        json.dump(manifest, f, ensure_ascii=False, indent=2)


def update_global_manifest_auto(version: str, checksum: str, size: int,
                                 app: dict, cfg: configparser.ConfigParser) -> None:
    section  = app["cfg_section"]
    base_url = cfg.get(section, "base_url",
                       fallback="http://server/versions/v{version}/update.zip")
    url = base_url.replace("{version}", version.replace(" ", "%20"))

    m = load_global_manifest(app)
    versions: list = m.get("versions", [])
    if version in versions:
        versions.remove(version)
    versions.insert(0, version)
    m["versions"]    = versions
    m["downloadUrl"] = url
    m["fileSize"]    = size
    m["checksum"]    = checksum
    save_global_manifest(app, m)
    print(f"    ✓ manifest.json обновлён. Версии: {versions}")


# ═══════════════════════════════════════════════════════════════════════════════
# СОЗДАНИЕ ВЕРСИИ
# ═══════════════════════════════════════════════════════════════════════════════

def menu_create_version(cfg: configparser.ConfigParser) -> None:
    header("Создание версии приложения")

    # 1. Выбор приложения
    app_key = choose_app()
    if app_key is None:
        return
    app     = APPS[app_key]
    section = app["cfg_section"]

    # 2. Путь до Release
    release_raw = cfg.get(section, "release_dir", fallback="").strip()
    if release_raw:
        print(f"  Release из config.cfg:\n    {release_raw}")
        inp = input("  Использовать? [Enter — да / введите другой]: ").strip().strip('"')
        release_dir = Path(inp) if inp else Path(release_raw)
    else:
        raw = input("  Путь до папки Release: ").strip().strip('"')
        if not raw:
            print("  [ОШИБКА] Путь не указан.")
            press_enter(); return
        release_dir = Path(raw)

    if not release_dir.is_dir():
        print(f"  [ОШИБКА] Папка не найдена: {release_dir}")
        press_enter(); return

    # 3. Версия
    version = input("\n  Версия (например «0.44 beta»): ").strip()
    if not version:
        print("  [ОШИБКА] Версия не указана.")
        press_enter(); return

    ver_s       = sanitize_ver(version)
    versions_dir = app["out_dir"] / "versions"
    version_dir  = versions_dir / f"v{version.strip()}"

    # 4. Проверка: версия уже существует?
    if version_dir.exists():
        print(f"\n  [!] Папка уже существует: {version_dir}")
        if not yn("Перезаписать?"):
            print("  Отменено.")
            press_enter(); return

    # 5. releaseNotes
    print()
    print("  ─── releaseNotes ─────────────────────────────────────────────")
    print("  Заметки к релизу (можно оставить пустым, но рекомендуется заполнить).")
    release_notes = input("  releaseNotes: ").strip()
    if not release_notes:
        print("  [i] releaseNotes оставлены пустыми.")

    # ── Pipeline ─────────────────────────────────────────────────────────────
    print(f"\n  ─── {app['label']}  v{version} ───")

    versions_dir.mkdir(parents=True, exist_ok=True)
    version_dir.mkdir(parents=True, exist_ok=True)

    # ── 1/5: Подписание .exe в Release ───────────────────────────────────────
    print("\n  [1/5] Подписание .exe в Release...")
    sign_exes_in_dir(release_dir, app["excluded_dirs"], cfg)

    # ── 2/5: version_manifest.json ────────────────────────────────────────────
    print("\n  [2/5] Сканирование файлов и создание version_manifest.json...")
    files   = collect_files(release_dir, app["excluded_dirs"])
    vm_path = version_dir / "version_manifest.json"
    vm = {
        "version":      version,
        "releaseDate":  str(date.today()),
        "releaseNotes": release_notes,
        "files":        files,
    }
    with open(vm_path, "w", encoding="utf-8") as f:
        json.dump(vm, f, ensure_ascii=False, indent=2)
    print(f"    Файлов в Release: {len(files)}")
    print(f"    ✓ version_manifest.json → {vm_path}")

    # ── 3/5: update.zip ───────────────────────────────────────────────────────
    print("\n  [3/5] Создание update.zip...")
    zip_path = version_dir / "update.zip"
    checksum, zip_size = create_update_zip(release_dir, zip_path, app["excluded_dirs"])
    print(f"    ✓ update.zip  {zip_size:,} байт  MD5: {checksum}")

    # ── 4/5: Inno Setup → установщик ─────────────────────────────────────────
    print("\n  [4/5] Сборка установщика (Inno Setup)...")
    iss_path   = INNO_DIR / app["iss_file"]
    setup_name = app["setup_tpl"].format(ver=ver_s)
    cer_path   = SCRIPT_DIR / "He11Catnn.cer"

    if cer_path.exists():
        cer_entry = (
            'Source: "' + str(cer_path) + '"; '
            'DestDir: "{tmp}"; Flags: dontcopy'
        )
    else:
        cer_entry = "; [!] He11Catnn.cer не найден рядом со скриптом"
        print("    [!] He11Catnn.cer не найден — сертификат НЕ будет встроен в установщик.")

    replacements = {
        "VERSION":         version,
        "OUTPUT_FILENAME": setup_name,
        "OUTPUT_DIR":      str(version_dir),
        "RELEASE_DIR":     str(release_dir),
        "CER_FILE_ENTRY":  cer_entry,
    }

    installer_ok = False
    if not iss_path.exists():
        print(f"    [ОШИБКА] ISS шаблон не найден: {iss_path}")
    else:
        if run_iscc(iss_path, replacements, cfg):
            installer = version_dir / f"{setup_name}.exe"
            if installer.is_file():
                print(f"    Подписание установщика...")
                sign_file(installer, cfg, "Setup")
                installer_ok = True
            else:
                print(f"    [!] Установщик не найден: {installer}")

    # ── 5/5: Глобальный манифест ──────────────────────────────────────────────
    print("\n  [5/5] Обновление глобального manifest.json...")
    update_global_manifest_auto(version, checksum, zip_size, app, cfg)

    # ── Итог ──────────────────────────────────────────────────────────────────
    print()
    hr("─", 64)
    print(f"  ✓ Версия v{version} ({app['name']}) создана!")
    print(f"  Папка: {version_dir}")
    print()
    print(f"    version_manifest.json  ✓")
    print(f"    update.zip             ✓")
    print(f"    {setup_name}.exe" + ("  ✓" if installer_ok else "  ✗ (не создан)"))
    hr("─", 64)
    press_enter()


# ═══════════════════════════════════════════════════════════════════════════════
# РЕДАКТИРОВАНИЕ CONFIG.CFG
# ═══════════════════════════════════════════════════════════════════════════════

def menu_edit_config(cfg: configparser.ConfigParser) -> None:
    while True:
        cls()
        header(f"Редактирование config.cfg")
        print(f"  Файл: {CONFIG_FILE}\n")

        items: list[tuple[str, str, str]] = []
        for section in cfg.sections():
            for key, val in cfg.items(section):
                items.append((section, key, val))

        # Группировка по секциям для читаемости
        cur_section = None
        for i, (section, key, val) in enumerate(items, 1):
            if section != cur_section:
                if cur_section is not None:
                    print()
                print(f"  [{section}]")
                cur_section = section
            display_val = val if val else "(пусто)"
            # Скрываем пароль
            if "password" in key.lower():
                display_val = "●●●●●●" if val else "(пусто)"
            print(f"    [{i:2d}] {key:30s} = {display_val}")

        print()
        print("  [0] Назад")
        choice = input("\n  Номер для редактирования: ").strip()

        if choice == "0":
            return

        try:
            idx = int(choice) - 1
            if not (0 <= idx < len(items)):
                raise ValueError
        except ValueError:
            input("  [!] Неверный номер. Нажмите Enter..."); continue

        section, key, old_val = items[idx]
        print(f"\n  [{section}] {key}")
        cur_display = old_val if ("password" not in key.lower()) else "●●●●●●"
        print(f"  Текущее: {cur_display or '(пусто)'}")
        new_val = input("  Новое значение [Enter = оставить]: ").strip()
        if new_val:
            cfg.set(section, key, new_val)
            save_config(cfg)
            print("  ✓ Сохранено.")
            input("  Нажмите Enter...")


# ═══════════════════════════════════════════════════════════════════════════════
# РЕДАКТИРОВАНИЕ ГЛОБАЛЬНОГО МАНИФЕСТА
# ═══════════════════════════════════════════════════════════════════════════════

def menu_edit_global_manifest(cfg: configparser.ConfigParser) -> None:
    header("Редактирование глобального манифеста")
    app_key = choose_app()
    if app_key is None:
        return
    app = APPS[app_key]

    if not _manifest_path(app).exists():
        print(f"\n  [!] manifest.json не найден: {_manifest_path(app)}")
        press_enter(); return

    while True:
        cls()
        header(f"Глобальный манифест — {app['name']}")
        m        = load_global_manifest(app)
        versions: list = m.get("versions", [])

        print(f"  Файл: {_manifest_path(app)}\n")
        print(f"  downloadUrl : {m.get('downloadUrl', '')}")
        print(f"  fileSize    : {m.get('fileSize', 0):,} байт")
        print(f"  checksum    : {m.get('checksum', '')}")
        print(f"\n  Поддерживаемые версии  (позиция [1] = latest):")
        if versions:
            for i, v in enumerate(versions, 1):
                latest = "  ← latest" if i == 1 else ""
                print(f"    [{i}] {v}{latest}")
        else:
            print("    (пусто)")

        print()
        hr("─", 64)
        print("  [a] Добавить версию    [d] Удалить версию")
        print("  [u] Поднять выше       [w] Опустить ниже")
        print("  [0] Назад")
        hr("─", 64)
        cmd = input("\n  Команда: ").strip().lower()

        if cmd == "0":
            return

        elif cmd == "a":
            v = input("  Добавить версию: ").strip()
            if not v:
                continue
            if v in versions:
                print(f"  [!] Версия {v} уже есть в списке.")
                input("  Нажмите Enter..."); continue
            versions.insert(0, v)
            m["versions"] = versions
            save_global_manifest(app, m)
            print(f"  ✓ Добавлена: {v}")
            input("  Нажмите Enter...")

        elif cmd == "d":
            if not versions:
                input("  Список пуст. Нажмите Enter..."); continue
            num = input("  Номер для удаления: ").strip()
            try:
                removed = versions.pop(int(num) - 1)
                m["versions"] = versions
                save_global_manifest(app, m)
                print(f"  ✓ Удалена: {removed}")
            except (ValueError, IndexError):
                print("  [!] Неверный номер.")
            input("  Нажмите Enter...")

        elif cmd == "u":
            if not versions:
                input("  Список пуст. Нажмите Enter..."); continue
            num = input("  Номер для поднятия: ").strip()
            try:
                idx = int(num) - 1
                if idx > 0:
                    versions[idx], versions[idx-1] = versions[idx-1], versions[idx]
                    m["versions"] = versions
                    save_global_manifest(app, m)
                    print(f"  ✓ Перемещена: [{idx}] ← [{idx+1}]")
                else:
                    print("  Уже на первом месте.")
            except (ValueError, IndexError):
                print("  [!] Неверный номер.")
            input("  Нажмите Enter...")

        elif cmd == "w":
            if not versions:
                input("  Список пуст. Нажмите Enter..."); continue
            num = input("  Номер для опускания: ").strip()
            try:
                idx = int(num) - 1
                if idx < len(versions) - 1:
                    versions[idx], versions[idx+1] = versions[idx+1], versions[idx]
                    m["versions"] = versions
                    save_global_manifest(app, m)
                    print(f"  ✓ Перемещена: [{idx+1}] ← [{idx+2}]")
                else:
                    print("  Уже на последнем месте.")
            except (ValueError, IndexError):
                print("  [!] Неверный номер.")
            input("  Нажмите Enter...")


# ═══════════════════════════════════════════════════════════════════════════════
# РЕДАКТИРОВАНИЕ ВЕРСИОННОГО МАНИФЕСТА
# ═══════════════════════════════════════════════════════════════════════════════

def list_version_dirs(app: dict) -> list[Path]:
    versions_dir = app["out_dir"] / "versions"
    if not versions_dir.exists():
        return []
    return sorted(
        [d for d in versions_dir.iterdir()
         if d.is_dir() and d.name.startswith("v")],
        key=lambda d: d.name,
        reverse=True,
    )


def menu_edit_version_manifest(cfg: configparser.ConfigParser) -> None:
    header("Редактирование версионного манифеста")
    app_key = choose_app()
    if app_key is None:
        return
    app = APPS[app_key]

    ver_dirs = list_version_dirs(app)
    if not ver_dirs:
        print("\n  [!] Версии не найдены.")
        press_enter(); return

    print(f"\n  {app['name']} — доступные версии:")
    for i, d in enumerate(ver_dirs, 1):
        has_vm  = (d / "version_manifest.json").exists()
        has_zip = (d / "update.zip").exists()
        mark = ("✓vm" if has_vm else "✗vm") + " " + ("✓zip" if has_zip else "✗zip")
        print(f"    [{i}] {d.name}  ({mark})")
    print("    [0] Назад")

    choice = input("\n  Выбор: ").strip()
    if choice == "0":
        return
    try:
        version_dir = ver_dirs[int(choice) - 1]
    except (ValueError, IndexError):
        input("  [!] Неверный номер. Нажмите Enter..."); return

    vm_path = version_dir / "version_manifest.json"
    if not vm_path.exists():
        print(f"\n  [!] version_manifest.json не найден: {vm_path}")
        press_enter(); return

    with open(vm_path, encoding="utf-8") as f:
        vm = json.load(f)

    while True:
        cls()
        header(f"Версионный манифест — {app['name']} / {version_dir.name}")
        print(f"  version     : {vm.get('version', '')}")
        print(f"  releaseDate : {vm.get('releaseDate', '')}")
        notes = vm.get("releaseNotes", "") or "(пусто)"
        print(f"  releaseNotes: {notes}")
        print(f"  files       : {len(vm.get('files', []))} шт.")
        print()
        print("  [1] Изменить releaseNotes")
        print("  [2] Изменить releaseDate")
        print("  [0] Назад")

        cmd = input("\n  Команда: ").strip()
        if cmd == "0":
            return

        elif cmd == "1":
            print(f"\n  Текущий: {vm.get('releaseNotes', '') or '(пусто)'}")
            new_val = input("  Новый releaseNotes [Enter = очистить]: ")
            vm["releaseNotes"] = new_val.strip()
            with open(vm_path, "w", encoding="utf-8") as f:
                json.dump(vm, f, ensure_ascii=False, indent=2)
            print("  ✓ Сохранено.")
            input("  Нажмите Enter...")

        elif cmd == "2":
            print(f"\n  Текущий: {vm.get('releaseDate', '')}")
            new_val = input("  Новая дата ГГГГ-ММ-ДД [Enter = сегодня]: ").strip()
            vm["releaseDate"] = new_val or str(date.today())
            with open(vm_path, "w", encoding="utf-8") as f:
                json.dump(vm, f, ensure_ascii=False, indent=2)
            print("  ✓ Сохранено.")
            input("  Нажмите Enter...")


# ═══════════════════════════════════════════════════════════════════════════════
# SSH — ВСПОМОГАТЕЛЬНЫЕ ФУНКЦИИ
# ═══════════════════════════════════════════════════════════════════════════════

def _ssh_connect_params(cfg: configparser.ConfigParser) -> dict | None:
    """
    Читает параметры подключения из config.cfg.
    Возвращает dict с ключами host / user / remote / ssh_flags / scp_flags,
    или None если host не задан.
    """
    host     = cfg.get("Server", "host",         fallback="").strip()
    user     = cfg.get("Server", "user",         fallback="root").strip()
    key_path = cfg.get("Server", "ssh_key_path", fallback="").strip()

    if not host:
        print("  [!] Не задан host в config.cfg → [Server].")
        return None

    flags = ["-o", "StrictHostKeyChecking=no"]
    if key_path:
        flags += ["-i", key_path]

    return {
        "host":       host,
        "user":       user,
        "remote":     f"{user}@{host}",
        "ssh_flags":  flags,
        "scp_flags":  list(flags),
    }


def _global_remote_dir(server_path: str) -> str:
    """Папка глобального манифеста: на уровень выше /versions."""
    return (
        server_path.rsplit("/versions", 1)[0]
        if "/versions" in server_path
        else server_path
    )


def _scp_file(local: Path, remote_dest: str,
              conn: dict, label: str = "") -> bool:
    """Загружает один файл через scp. Возвращает True при успехе."""
    tag = f"  [{label}] " if label else "  "
    print(f"{tag}scp {local.name} → {remote_dest}")
    r = subprocess.run(
        ["scp"] + conn["scp_flags"] + [str(local), remote_dest]
    )
    if r.returncode == 0:
        print(f"{tag}✓ Загружен: {local.name}")
        return True
    print(f"{tag}[ОШИБКА] SCP завершился с ошибкой (код {r.returncode}).")
    return False


# ═══════════════════════════════════════════════════════════════════════════════
# SSH — ЗАГРУЗКА НА СЕРВЕР (ПОДМЕНЮ)
# ═══════════════════════════════════════════════════════════════════════════════

def _upload_full_version(app: dict, server_path: str, conn: dict) -> None:
    """Загружает всю папку версии + глобальный manifest.json."""
    ver_dirs = list_version_dirs(app)
    if not ver_dirs:
        print("\n  [!] Версии не найдены.")
        press_enter(); return

    print(f"\n  {app['name']} — выберите версию для загрузки:")
    for i, d in enumerate(ver_dirs, 1):
        print(f"    [{i}] {d.name}")
    print("    [0] Назад")

    choice = input("\n  Выбор: ").strip()
    if choice == "0":
        return
    try:
        version_dir = ver_dirs[int(choice) - 1]
    except (ValueError, IndexError):
        input("  [!] Неверный номер. Нажмите Enter..."); return

    remote_ver_dir  = f"{server_path}/{version_dir.name}"
    global_manifest = app["out_dir"] / "manifest.json"
    local_files     = [f for f in version_dir.iterdir() if f.is_file()]

    print(f"\n  Загрузка папки: {version_dir.name}")
    print(f"  Сервер:         {conn['remote']}:{remote_ver_dir}")
    print(f"  Файлы ({len(local_files)}):")
    for lf in local_files:
        print(f"    {lf.name}  ({lf.stat().st_size:,} байт)")
    if global_manifest.exists():
        grd = _global_remote_dir(server_path)
        print(f"  + manifest.json (глобальный) → {grd}/")
    print()
    if not yn("Начать загрузку?"):
        print("  Отменено."); press_enter(); return

    # Создать папку версии на сервере
    print(f"\n  mkdir {remote_ver_dir}...")
    r = subprocess.run(
        ["ssh"] + conn["ssh_flags"] +
        [conn["remote"], f"mkdir -p '{remote_ver_dir}'"]
    )
    if r.returncode != 0:
        print("  [ОШИБКА] Не удалось создать папку на сервере.")
        press_enter(); return

    # Загрузить все файлы версии одной командой scp
    print("  scp файлы версии...")
    r = subprocess.run(
        ["scp"] + conn["scp_flags"] +
        [str(f) for f in local_files] +
        [f"{conn['remote']}:{remote_ver_dir}/"]
    )
    if r.returncode != 0:
        print("  [ОШИБКА] SCP завершился с ошибкой.")
        press_enter(); return
    print("  ✓ Файлы версии загружены.")

    # Загрузить глобальный manifest.json
    if global_manifest.exists():
        grd = _global_remote_dir(server_path)
        _scp_file(global_manifest, f"{conn['remote']}:{grd}/manifest.json", conn)

    print()
    hr("─", 64)
    print("  ✓ Загрузка завершена!")
    hr("─", 64)
    press_enter()


def _upload_global_manifest(app: dict, server_path: str, conn: dict) -> None:
    """Загружает только глобальный manifest.json."""
    manifest = app["out_dir"] / "manifest.json"
    if not manifest.exists():
        print(f"\n  [!] Файл не найден: {manifest}")
        press_enter(); return

    grd  = _global_remote_dir(server_path)
    dest = f"{conn['remote']}:{grd}/manifest.json"

    print(f"\n  Файл:   {manifest}")
    print(f"  Сервер: {dest}")
    print()
    if not yn("Загрузить?"):
        print("  Отменено."); press_enter(); return

    print()
    ok = _scp_file(manifest, dest, conn)
    print()
    hr("─", 64)
    print("  ✓ manifest.json загружен!" if ok else "  ✗ Загрузка не удалась.")
    hr("─", 64)
    press_enter()


def _upload_version_manifest(app: dict, server_path: str, conn: dict) -> None:
    """Загружает только version_manifest.json выбранной версии."""
    ver_dirs = list_version_dirs(app)
    if not ver_dirs:
        print("\n  [!] Версии не найдены.")
        press_enter(); return

    print(f"\n  {app['name']} — выберите версию:")
    for i, d in enumerate(ver_dirs, 1):
        has_vm = (d / "version_manifest.json").exists()
        print(f"    [{i}] {d.name}  {'✓vm' if has_vm else '✗vm'}")
    print("    [0] Назад")

    choice = input("\n  Выбор: ").strip()
    if choice == "0":
        return
    try:
        version_dir = ver_dirs[int(choice) - 1]
    except (ValueError, IndexError):
        input("  [!] Неверный номер. Нажмите Enter..."); return

    vm_path = version_dir / "version_manifest.json"
    if not vm_path.exists():
        print(f"\n  [!] Файл не найден: {vm_path}")
        press_enter(); return

    remote_ver_dir = f"{server_path}/{version_dir.name}"
    dest           = f"{conn['remote']}:{remote_ver_dir}/version_manifest.json"

    print(f"\n  Файл:   {vm_path}")
    print(f"  Сервер: {dest}")
    print()
    if not yn("Загрузить?"):
        print("  Отменено."); press_enter(); return

    # Убеждаемся что папка версии на сервере существует
    print(f"\n  mkdir {remote_ver_dir}...")
    subprocess.run(
        ["ssh"] + conn["ssh_flags"] +
        [conn["remote"], f"mkdir -p '{remote_ver_dir}'"],
        capture_output=True,
    )

    print()
    ok = _scp_file(vm_path, dest, conn)
    print()
    hr("─", 64)
    print("  ✓ version_manifest.json загружен!" if ok else "  ✗ Загрузка не удалась.")
    hr("─", 64)
    press_enter()


def menu_upload_server(cfg: configparser.ConfigParser) -> None:
    conn = _ssh_connect_params(cfg)
    if conn is None:
        press_enter(); return

    while True:
        cls()
        header("Загрузка на сервер (SSH / SCP)")
        print(f"  Сервер: {conn['remote']}\n")
        print("  Что загрузить?")
        print("  [1]  Всю папку версии (файлы + оба манифеста)")
        print("  [2]  Только глобальный  manifest.json")
        print("  [3]  Только версионный  version_manifest.json")
        print()
        print("  [0]  Назад")
        hr("─", 64)

        mode = input("\n  Выбор: ").strip()
        if mode == "0":
            return

        if mode not in ("1", "2", "3"):
            input("  [!] Введите 1, 2, 3 или 0. Нажмите Enter..."); continue

        # Выбор приложения (нужен для всех режимов)
        app_key = choose_app()
        if app_key is None:
            continue
        app = APPS[app_key]

        server_path_key = app["server_path_key"]
        server_path = cfg.get("Server", server_path_key, fallback="").strip()
        if not server_path:
            print(f"\n  [!] Не задан {server_path_key} в config.cfg → [Server].")
            press_enter(); continue

        if mode == "1":
            _upload_full_version(app, server_path, conn)
        elif mode == "2":
            _upload_global_manifest(app, server_path, conn)
        elif mode == "3":
            _upload_version_manifest(app, server_path, conn)


# ═══════════════════════════════════════════════════════════════════════════════
# ГЛАВНОЕ МЕНЮ
# ═══════════════════════════════════════════════════════════════════════════════

def main() -> None:
    cfg = load_config()

    while True:
        cls()
        hr()
        print("  PostalApp — Менеджер версий")
        hr()
        print()
        print("  [1]  Создать версию приложения")
        print("  [2]  Редактировать config.cfg")
        print("  [3]  Редактировать глобальный манифест")
        print("  [4]  Редактировать версионный манифест")
        print("  [5]  Загрузить версию на сервер (SSH)")
        print()
        print("  [0]  Выход")
        print()
        hr("─", 64)

        choice = input("  Выбор: ").strip()

        if choice == "0":
            print("\n  До свидания!\n")
            break
        elif choice == "1":
            menu_create_version(cfg)
            cfg = load_config()
        elif choice == "2":
            menu_edit_config(cfg)
            cfg = load_config()
        elif choice == "3":
            menu_edit_global_manifest(cfg)
        elif choice == "4":
            menu_edit_version_manifest(cfg)
        elif choice == "5":
            menu_upload_server(cfg)
        else:
            input("  [!] Неверный выбор. Нажмите Enter...")


if __name__ == "__main__":
    main()