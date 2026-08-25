; ═══════════════════════════════════════════════════════════════════════════════
; Inno Setup — PostalApp
; Шаблон: значения ##PLACEHOLDER## подставляются автоматически через manifests.py
; ═══════════════════════════════════════════════════════════════════════════════

[Setup]
AppName=PostalApp
AppVersion=##VERSION##
AppPublisher=User_Company

AppId={{D9FBD3F3-9362-4900-B109-E998A4F0EA6C}

DefaultDirName={autopf32}\PostalApp
DefaultGroupName=PostalApp

DisableProgramGroupPage=yes
DisableDirPage=no

Compression=lzma2
SolidCompression=yes
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64

; SetupIconFile=...\icon.ico

OutputBaseFilename=##OUTPUT_FILENAME##
OutputDir=##OUTPUT_DIR##

; ═══════════════════════════════════════════════════════════════════════════════
; ФАЙЛЫ
; ═══════════════════════════════════════════════════════════════════════════════

[Files]
Source: "##RELEASE_DIR##\PostalApp.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "##RELEASE_DIR##\*"; DestDir: "{app}"; Excludes: "logs\*,*.pdb,*.xml,*.log,app.publish\*"; Flags: ignoreversion recursesubdirs

; Сертификат User — строка подставляется из manifests.py
; Если .cer не найден рядом со скриптом, эта строка заменяется комментарием.
##CER_FILE_ENTRY##

; ═══════════════════════════════════════════════════════════════════════════════
; ЗАДАЧИ
; ═══════════════════════════════════════════════════════════════════════════════

[Tasks]
Name: "desktopicon";  Description: "Создать ярлык на рабочем столе"; GroupDescription: "Дополнительные значки:"
Name: "startmenuicon"; Description: "Создать ярлык в меню «Пуск»";   GroupDescription: "Дополнительные значки:"; Flags: unchecked

; ═══════════════════════════════════════════════════════════════════════════════
; ЯРЛЫКИ
; ═══════════════════════════════════════════════════════════════════════════════

[Icons]
Name: "{autodesktop}\PostalApp";             Filename: "{app}\PostalApp.exe"; Tasks: desktopicon;  Comment: "Запуск почтового приложения"
Name: "{userprograms}\PostalApp\PostalApp";  Filename: "{app}\PostalApp.exe"; Tasks: startmenuicon; Comment: "Запуск почтового приложения"

; ═══════════════════════════════════════════════════════════════════════════════
; ЗАПУСК ПОСЛЕ УСТАНОВКИ
; ═══════════════════════════════════════════════════════════════════════════════

[Run]
Filename: "{app}\PostalApp.exe"; Description: "Запустить PostalApp"; Flags: nowait postinstall skipifsilent

; ═══════════════════════════════════════════════════════════════════════════════
; ДИРЕКТОРИИ И ОЧИСТКА
; ═══════════════════════════════════════════════════════════════════════════════

[Dirs]
Name: "{app}\logs"; Permissions: users-modify

[UninstallDelete]
Type: filesandordirs; Name: "{app}"

; ═══════════════════════════════════════════════════════════════════════════════
; ЯЗЫК
; ═══════════════════════════════════════════════════════════════════════════════

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

; ═══════════════════════════════════════════════════════════════════════════════
; КОД
; ═══════════════════════════════════════════════════════════════════════════════

[Code]

// ═══════════════════════════════════════════════════════════════════════════════
// СЕРТИФИКАТ User
// ═══════════════════════════════════════════════════════════════════════════════

// Проверяет наличие сертификата User в хранилище LocalMachine\Root.
// Использует certutil + findstr — надёжно работает в контексте Inno Setup,
// без зависимости от политики выполнения PowerShell.
function IsCertInstalled(): Boolean;
var
  ResultCode: Integer;
begin
  // certutil -store Root выводит все сертификаты Root.
  // findstr /i ищет строку User без учёта регистра.
  // Возвращает 0 если найдено, 1 если нет.
  Exec(ExpandConstant('{cmd}'),
    '/c certutil -store Root | findstr /i "User" > nul 2>&1',
    '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Result := (ResultCode = 0);
end;

// Устанавливает сертификат в Root и TrustedPublisher через certutil.
//
// ВАЖНО: InitializeSetup запускается ДО фазы копирования файлов, поэтому
// нельзя просто проверить FileExists('{tmp}\User.cer') — файла там ещё нет.
// ExtractTemporaryFile явно достаёт файл из архива установщика в {tmp}
// и работает на любой фазе, включая InitializeSetup.
// Флаг [Files] dontcopy означает: файл встроен в архив, но автоматически
// НЕ копируется — только через ExtractTemporaryFile.
procedure InstallCert();
var
  CertFile: String;
  ResultCode: Integer;
begin
  CertFile := ExpandConstant('{tmp}\User.cer');

  // Извлекаем .cer из архива установщика в {tmp}.
  // Если файл не был встроен при сборке — ExtractTemporaryFile бросит исключение,
  // которое мы перехватываем и тихо выходим.
  try
    ExtractTemporaryFile('User.cer');
  except
    Exit;
  end;

  if not FileExists(CertFile) then
    Exit;

  // Добавляем в Trusted Root CA — Windows будет доверять подписанным файлам
  Exec('certutil.exe',
    '-addstore -f "Root" "' + CertFile + '"',
    '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  // Добавляем в Trusted Publishers — убирает предупреждения UAC при запуске
  Exec('certutil.exe',
    '-addstore -f "TrustedPublisher" "' + CertFile + '"',
    '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
end;

// ═══════════════════════════════════════════════════════════════════════════════
// .NET FRAMEWORK 4.8
// ═══════════════════════════════════════════════════════════════════════════════

// Release >= 528040 → .NET 4.8 или новее
function IsDotNet48Installed(): Boolean;
var
  Release: Cardinal;
begin
  Result := RegQueryDWordValue(
    HKLM,
    'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full',
    'Release',
    Release
  ) and (Release >= 528040);
end;

function DownloadAndInstallDotNet48(): Boolean;
var
  TempFile: String;
  ResultCode: Integer;
  PSCmd: String;
begin
  Result   := False;
  TempFile := ExpandConstant('{tmp}\ndp48-web.exe');

  MsgBox(
    'Сейчас начнётся загрузка .NET Framework 4.8 с серверов Microsoft.' + #13#10 +
    'Это может занять несколько минут.' + #13#10 + #13#10 +
    'Нажмите «ОК» — установщик запустится автоматически после загрузки.',
    mbInformation, MB_OK
  );

  PSCmd := '-NoProfile -NonInteractive -Command "' +
           '(New-Object System.Net.WebClient).DownloadFile(' +
           '''https://go.microsoft.com/fwlink/?LinkId=2085155'',' +
           '''' + TempFile + ''')' +
           '"';

  if not Exec('powershell.exe', PSCmd, '', SW_HIDE, ewWaitUntilTerminated, ResultCode)
     or (ResultCode <> 0) then
  begin
    MsgBox(
      'Не удалось загрузить .NET Framework 4.8.' + #13#10 +
      'Проверьте подключение к интернету и запустите установщик снова.' + #13#10 + #13#10 +
      'Или загрузите вручную: https://dotnet.microsoft.com/download/dotnet-framework/net48',
      mbError, MB_OK
    );
    Exit;
  end;

  if not Exec(TempFile, '/q /norestart', '', SW_SHOW, ewWaitUntilTerminated, ResultCode) then
  begin
    MsgBox(
      'Не удалось запустить установщик .NET Framework 4.8.' + #13#10 +
      'Попробуйте запустить установщик PostalApp от имени администратора.',
      mbError, MB_OK
    );
    Exit;
  end;

  Result := IsDotNet48Installed();
  if not Result then
    MsgBox(
      'Установка .NET Framework 4.8 не завершена или прошла с ошибкой.' + #13#10 +
      'Установите его вручную и запустите установщик снова.' + #13#10 + #13#10 +
      'https://dotnet.microsoft.com/download/dotnet-framework/net48',
      mbError, MB_OK
    );
end;

// ═══════════════════════════════════════════════════════════════════════════════
// ТОЧКА ВХОДА
// Порядок проверок: [1] Сертификат → [2] .NET 4.8
// ═══════════════════════════════════════════════════════════════════════════════

function InitializeSetup(): Boolean;
var
  ErrorCode: Integer;
begin
  Result := True;

  // ── [1] Сертификат User ─────────────────────────────────────────────
  //
  // Если сертификат уже установлен — пропускаем молча.
  // Если не установлен — устанавливаем автоматически (без выбора пользователя).
  // Если User.cer не встроен в этот установщик — пропускаем тихо.
  //
  if not IsCertInstalled() then
  begin
    MsgBox(
      'Для корректной работы PostalApp необходим доверенный сертификат User.' + #13#10 +
      'Сертификат будет установлен автоматически. Это займёт несколько секунд.',
      mbInformation, MB_OK
    );
    InstallCert();
    if not IsCertInstalled() then
      MsgBox(
        'Предупреждение: сертификат User не удалось установить автоматически.' + #13#10 +
        'Приложение будет установлено, однако возможны предупреждения безопасности.' + #13#10 +
        'При необходимости установите сертификат User.cer вручную.',
        mbError, MB_OK
      );
  end;
  // Если уже был — ничего не делаем

  // ── [2] .NET Framework 4.8 ───────────────────────────────────────────────
  if IsDotNet48Installed() then
    Exit;

  if MsgBox(
    'Для работы PostalApp требуется .NET Framework 4.8.' + #13#10 +
    'На вашем компьютере он не установлен.' + #13#10 + #13#10 +
    'Нажмите «Да», чтобы загрузить и установить автоматически,' + #13#10 +
    'или «Нет» для отмены.',
    mbConfirmation, MB_YESNO
  ) = IDYES then
  begin
    if DownloadAndInstallDotNet48() then
    begin
      if MsgBox(
        '.NET Framework 4.8 успешно установлен!' + #13#10 + #13#10 +
        'Рекомендуется перезагрузить компьютер перед продолжением.' + #13#10 +
        '«Да» — перезагрузить сейчас, «Нет» — продолжить без перезагрузки.',
        mbConfirmation, MB_YESNO
      ) = IDYES then
      begin
        ShellExec('', 'shutdown.exe',
          '/r /t 10 /c "Перезагрузка для завершения установки .NET Framework 4.8"',
          '', SW_HIDE, ewNoWait, ErrorCode);
        MsgBox(
          'Компьютер перезагрузится через 10 секунд.' + #13#10 +
          'После перезагрузки запустите установщик PostalApp снова.',
          mbInformation, MB_OK
        );
        Result := False;
        Exit;
      end;
    end
    else
    begin
      Result := False;
    end;
  end
  else
  begin
    MsgBox(
      'Установка PostalApp отменена.' + #13#10 +
      'Для работы приложения необходим .NET Framework 4.8.' + #13#10 + #13#10 +
      'https://dotnet.microsoft.com/download/dotnet-framework/net48',
      mbError, MB_OK
    );
    Result := False;
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    // Пост-установка: здесь можно создать default config и т.д.
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usUninstall then
  begin
    // При удалении: здесь можно спросить, удалять ли данные.
  end;
end;
