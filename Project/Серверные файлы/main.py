import json
import hashlib
import base64
import asyncpg
import time
from collections import defaultdict
from pathlib import Path
from fastapi import FastAPI, HTTPException, Request
from pydantic import BaseModel

app = FastAPI()

with open("/var/www/postalapp_api/config.json", "r") as f:
    CFG = json.load(f)

# ─── Папки с файлами обновлений ───────────────────────────────────────────────

UPDATES_DIR       = Path("/var/www/postalapp_updates")
EXTRA_UPDATES_DIR = Path("/var/www/postalapp-extra_updates")

# ─── Разрешённые роли для PostalApp-Extra ─────────────────────────────────────
# Редактируй этот список чтобы добавить/убрать роли.

EXTRA_ALLOWED_ROLES = [
    "Директор",
]

# ─── Токен разработчика ───────────────────────────────────────────────────────
# Вставь сюда свой сгенерированный токен (см. инструкцию ниже).
# Этот токен даёт доступ к /api/getconfig_extra_dev, минуя проверки
# версии, целостности и роли. Используется ТОЛЬКО для разработки/тестирования.
# НЕ коммить в git и не передавай третьим лицам.

DEV_TOKEN = "token"

# ─── Rate Limiting ────────────────────────────────────────────────────────────

_login_attempts:  dict = defaultdict(list)   # ручной вход:  5  попыток / 60 сек
_device_attempts: dict = defaultdict(list)   # автовход:    60 попыток / 60 сек

LOGIN_LIMIT  = 5
DEVICE_LIMIT = 60
WINDOW       = 60  # секунд


def get_retry_after(attempts: list, limit: int) -> int:
    now    = time.time()
    recent = [t for t in attempts if now - t < WINDOW]
    if len(recent) < limit:
        return 0
    oldest = min(recent)
    return max(1, int(WINDOW - (now - oldest)))


def check_login_rate(ip: str) -> None:
    now = time.time()
    _login_attempts[ip] = [t for t in _login_attempts[ip] if now - t < WINDOW]
    retry_after = get_retry_after(_login_attempts[ip], LOGIN_LIMIT)
    if retry_after > 0:
        raise HTTPException(
            status_code=429,
            detail={"reason": "rate_limit", "retry_after": retry_after}
        )
    _login_attempts[ip].append(now)


def check_device_rate(ip: str) -> None:
    now = time.time()
    _device_attempts[ip] = [t for t in _device_attempts[ip] if now - t < WINDOW]
    retry_after = get_retry_after(_device_attempts[ip], DEVICE_LIMIT)
    if retry_after > 0:
        raise HTTPException(
            status_code=429,
            detail={"reason": "rate_limit", "retry_after": retry_after}
        )
    _device_attempts[ip].append(now)


def get_login_block_seconds(ip: str) -> int:
    now    = time.time()
    recent = [t for t in _login_attempts[ip] if now - t < WINDOW]
    return get_retry_after(recent, LOGIN_LIMIT)


# ─── Вспомогательные функции ──────────────────────────────────────────────────

def get_conn_string(db_entry) -> str:
    if isinstance(db_entry, dict):
        return db_entry["conn"]
    return db_entry


def get_coords(db_entry) -> tuple:
    if isinstance(db_entry, dict):
        return db_entry.get("lat"), db_entry.get("lng")
    return None, None


def build_dsn(conn_string: str) -> str:
    parts = conn_string.split("|")
    return f"postgresql://{parts[3]}:{parts[4]}@{parts[0]}:{parts[1]}/{parts[2]}"


def verify_pbkdf2(password: str, stored_hash: str) -> bool:
    try:
        combined = base64.b64decode(stored_hash)
        salt     = combined[:16]
        stored   = combined[16:]
        computed = hashlib.pbkdf2_hmac('sha256', password.encode('utf-8'), salt, 100000, dklen=32)
        return computed == stored
    except Exception:
        return False


# ─── PostalApp: манифест и проверка ──────────────────────────────────────────

def get_global_manifest() -> dict:
    manifest_path = UPDATES_DIR / "manifest.json"
    if not manifest_path.exists():
        return {}
    with open(manifest_path, encoding="utf-8") as f:
        return json.load(f)


def get_exe_md5_from_version_manifest(version: str) -> str | None:
    manifest_path = UPDATES_DIR / "versions" / f"v{version}" / "version_manifest.json"
    if not manifest_path.exists():
        return None
    try:
        with open(manifest_path, encoding="utf-8") as f:
            data = json.load(f)
        for entry in data.get("files", []):
            if entry.get("path", "").lower() == "postalapp.exe":
                return entry.get("md5", "").lower()
    except Exception:
        pass
    return None


def check_version_and_integrity(version: str, exe_md5: str) -> tuple[bool, bool]:
    manifest           = get_global_manifest()
    supported_versions = manifest.get("versions", [])
    version_supported  = version in supported_versions

    expected_md5 = get_exe_md5_from_version_manifest(version)
    integrity_ok = True if expected_md5 is None else \
                   exe_md5.strip().lower() == expected_md5.strip().lower()

    return version_supported, integrity_ok


# ─── PostalApp-Extra: манифест и проверка ────────────────────────────────────

def get_extra_global_manifest() -> dict:
    manifest_path = EXTRA_UPDATES_DIR / "manifest.json"
    if not manifest_path.exists():
        return {}
    with open(manifest_path, encoding="utf-8") as f:
        return json.load(f)


def get_exe_md5_from_version_manifest_extra(version: str) -> str | None:
    manifest_path = EXTRA_UPDATES_DIR / "versions" / f"v{version}" / "version_manifest.json"
    if not manifest_path.exists():
        return None
    try:
        with open(manifest_path, encoding="utf-8") as f:
            data = json.load(f)
        for entry in data.get("files", []):
            if entry.get("path", "").lower() == "postalapp-extra.exe":
                return entry.get("md5", "").lower()
    except Exception:
        pass
    return None


def check_version_and_integrity_extra(version: str, exe_md5: str) -> tuple[bool, bool]:
    manifest           = get_extra_global_manifest()
    supported_versions = manifest.get("versions", [])
    version_supported  = version in supported_versions

    expected_md5 = get_exe_md5_from_version_manifest_extra(version)
    integrity_ok = True if expected_md5 is None else \
                   exe_md5.strip().lower() == expected_md5.strip().lower()

    return version_supported, integrity_ok


# ─── Модели запросов ──────────────────────────────────────────────────────────

class LoginRequest(BaseModel):
    login:    str
    password: str
    version:  str
    exe_md5:  str


class LoginExtraRequest(BaseModel):
    login:    str
    password: str
    version:  str
    exe_md5:  str


class DevTokenRequest(BaseModel):
    dev_token: str
    login:     str
    password:  str


class DeviceRequest(BaseModel):
    motherboard_id: str
    version:        str
    exe_md5:        str


# ─── Эндпоинты ────────────────────────────────────────────────────────────────

@app.post("/api/getconfig")
async def get_config(req: LoginRequest, request: Request):
    """PostalApp — ручной вход с проверкой версии и целостности."""
    ip = request.client.host

    check_login_rate(ip)

    version_supported, integrity_ok = check_version_and_integrity(req.version, req.exe_md5)
    if not version_supported:
        raise HTTPException(status_code=426, detail="Update required")
    if not integrity_ok:
        raise HTTPException(status_code=403, detail="Integrity check failed")

    for city, db_entry in CFG["databases"].items():
        conn_string = get_conn_string(db_entry)
        lat, lng    = get_coords(db_entry)
        try:
            conn = await asyncpg.connect(build_dsn(conn_string))
            try:
                row = await conn.fetchrow(
                    'SELECT "Пароль" FROM "Логин" WHERE "Почта" = $1',
                    req.login
                )
            finally:
                await conn.close()
            if row is None:
                continue
            if verify_pbkdf2(req.password, row["Пароль"]):
                result = {"config": conn_string}
                if lat is not None and lng is not None:
                    result["lat"] = lat
                    result["lng"] = lng
                return result
        except Exception:
            continue

    raise HTTPException(status_code=401, detail="Invalid credentials")


@app.post("/api/getconfig_extra")
async def get_config_extra(req: LoginExtraRequest, request: Request):
    """PostalApp-Extra — ручной вход с проверкой версии, целостности и роли."""
    ip = request.client.host

    check_login_rate(ip)

    # ── Проверка версии и целостности ─────────────────────────────
    version_supported, integrity_ok = check_version_and_integrity_extra(req.version, req.exe_md5)
    if not version_supported:
        raise HTTPException(status_code=426, detail="Update required")
    if not integrity_ok:
        raise HTTPException(status_code=403, detail="Integrity check failed")

    # ── Поиск пользователя и проверка роли ───────────────────────
    for city, db_entry in CFG["databases"].items():
        conn_string = get_conn_string(db_entry)
        lat, lng    = get_coords(db_entry)
        try:
            conn = await asyncpg.connect(build_dsn(conn_string))
            try:
                # Получаем пароль из "Логин" и роль из "Сотрудники" одним запросом.
                # "Сотрудники"."Id логина" — FK на "Логин".id
                row = await conn.fetchrow(
                    'SELECT л."Пароль", с."Роль" '
                    'FROM "Логин" л '
                    'JOIN "Сотрудники" с ON с."Id логина" = л.id '
                    'WHERE л."Почта" = $1',
                    req.login
                )
            finally:
                await conn.close()

            if row is None:
                continue

            if not verify_pbkdf2(req.password, row["Пароль"]):
                # Неверный пароль — не раскрываем причину клиенту
                raise HTTPException(status_code=401, detail="Invalid credentials")

            # ── Проверка роли ──────────────────────────────────────
            role = (row["Роль"] or "").strip()
            if role not in EXTRA_ALLOWED_ROLES:
                raise HTTPException(status_code=403, detail="Role not allowed")

            result = {"config": conn_string}
            if lat is not None and lng is not None:
                result["lat"] = lat
                result["lng"] = lng
            return result

        except HTTPException:
            raise  # пробрасываем ошибки роли/пароля без подавления
        except Exception:
            continue

    raise HTTPException(status_code=401, detail="Invalid credentials")


@app.post("/api/getconfig_extra_dev")
async def get_config_extra_dev(req: DevTokenRequest, request: Request):
    """
    PostalApp-Extra — вход для разработчика.
    Минует проверки версии, целостности и роли.
    Требует dev_token + логин/пароль пользователя (для получения нужной БД).
    Использовать ТОЛЬКО во время разработки и тестирования.
    """
    ip = request.client.host

    # ── Проверка токена разработчика ──────────────────────────────
    # Сравниваем через hmac.compare_digest чтобы избежать timing attack
    import hmac
    if not hmac.compare_digest(req.dev_token, DEV_TOKEN):
        raise HTTPException(status_code=401, detail="Invalid dev token")

    check_login_rate(ip)

    # ── Поиск пользователя (только пароль, без проверки роли) ─────
    for city, db_entry in CFG["databases"].items():
        conn_string = get_conn_string(db_entry)
        lat, lng    = get_coords(db_entry)
        try:
            conn = await asyncpg.connect(build_dsn(conn_string))
            try:
                row = await conn.fetchrow(
                    'SELECT "Пароль" FROM "Логин" WHERE "Почта" = $1',
                    req.login
                )
            finally:
                await conn.close()

            if row is None:
                continue

            if not verify_pbkdf2(req.password, row["Пароль"]):
                raise HTTPException(status_code=401, detail="Invalid credentials")

            result = {"config": conn_string}
            if lat is not None and lng is not None:
                result["lat"] = lat
                result["lng"] = lng
            return result

        except HTTPException:
            raise
        except Exception:
            continue

    raise HTTPException(status_code=401, detail="Invalid credentials")


@app.post("/api/checkdevice")
async def check_device(req: DeviceRequest, request: Request):
    """PostalApp — автовход по ID материнской платы."""
    ip = request.client.host

    login_blocked = get_login_block_seconds(ip)
    if login_blocked > 0:
        raise HTTPException(
            status_code=429,
            detail={"reason": "rate_limit", "retry_after": login_blocked}
        )

    check_device_rate(ip)

    version_supported, integrity_ok = check_version_and_integrity(req.version, req.exe_md5)
    if not version_supported:
        raise HTTPException(status_code=426, detail="Update required")
    if not integrity_ok:
        raise HTTPException(status_code=403, detail="Integrity check failed")

    for city, db_entry in CFG["databases"].items():
        conn_string = get_conn_string(db_entry)
        lat, lng    = get_coords(db_entry)
        try:
            conn = await asyncpg.connect(build_dsn(conn_string))
            try:
                row = await conn.fetchrow(
                    'SELECT id FROM "Устройства" WHERE "Id материнской платы" = $1 '
                    'AND "Постоянный доступ" = true',
                    req.motherboard_id
                )
            finally:
                await conn.close()
            if row is not None:
                result = {"config": conn_string}
                if lat is not None and lng is not None:
                    result["lat"] = lat
                    result["lng"] = lng
                return result
        except Exception:
            continue

    raise HTTPException(status_code=401, detail="Device not found")
