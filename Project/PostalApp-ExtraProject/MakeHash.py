import hashlib
import base64
import textwrap
import os

print("=" * 50)
print("  Генератор Base64 + SHA-256 для C#")
print("=" * 50)

while True:
    path = input("\nПуть до файла: ").strip().strip('"')
    if os.path.exists(path):
        break
    print(f"  [ОШИБКА] Файл не найден: {path}")

with open(path, "rb") as f:
    raw = f.read()

# Нормализуем: BOM + CRLF (как требует C# приложение)
bom = b"\xef\xbb\xbf"
body = raw[3:] if raw[:3] == bom else raw
data = bom + body.replace(b"\r\n", b"\n").replace(b"\n", b"\r\n")

sha256 = hashlib.sha256(data).hexdigest()
b64    = base64.b64encode(data).decode("ascii")
chunks = textwrap.wrap(b64, 76)

lines = []
for i, chunk in enumerate(chunks):
    if i < len(chunks) - 1:
        lines.append(f'    "{chunk}" +')
    else:
        lines.append(f'    "{chunk}";')

b64_block  = "\n".join(lines)
filename   = os.path.basename(path)

result = f"""// ─── Встроенный Python-скрипт {filename} ───────────────────────────
// Скрипт закодирован в Base64, чтобы исключить проблемы с экранированием.
// Ожидаемый SHA-256 (от исходных байт файла, включая BOM):
//   {sha256}
private static readonly string EmbeddedScriptBase64 =
{b64_block}

private static readonly string EmbeddedScriptHash =
    "{sha256}";"""

print("\n" + "=" * 50)
print(result)
print("=" * 50)

input("\nНажмите Enter для выхода...")
