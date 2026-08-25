; ═══════════════════════════════════════════════════════════════════════════════
; Inno Setup — PostalApp-Extra
; Шаблон: значения ##PLACEHOLDER## подставляются автоматически через manifests.py
; ═══════════════════════════════════════════════════════════════════════════════

[Setup]
AppName=PostalApp-Extra
AppVersion=##VERSION##
AppPublisher=User_Company

; Уникальный ID — не меняй после первой публикации!
AppId={{9241ADCF-852F-428D-B3B5-78109081406A}

DefaultDirName={autopf32}\PostalApp-Extra
DefaultGroupName=PostalApp-Extra

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
Source: "##RELEASE_DIR##\PostalApp-Extra.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "##RELEASE_DIR##\*"; DestDir: "{app}"; Excludes: "*.pdb,*.xml,*.log,data\*.txt,data\*.accdb"; Flags: ignoreversion recursesubdirs

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
Name: "{autodesktop}\PostalApp-Extra";                  Filename: "{app}\PostalApp-Extra.exe"; Tasks: desktopicon;  Comment: "Запуск дополнительного почтового приложения"
Name: "{userprograms}\PostalApp-Extra\PostalApp-Extra"; Filename: "{app}\PostalApp-Extra.exe"; Tasks: startmenuicon; Comment: "Запуск дополнительного почтового приложения"

; ═══════════════════════════════════════════════════════════════════════════════
; ЗАПУСК ПОСЛЕ УСТАНОВКИ
; ═══════════════════════════════════════════════════════════════════════════════

[Run]
Filename: "{app}\PostalApp-Extra.exe"; Description: "Запустить PostalApp-Extra"; Flags: nowait postinstall skipifsilent

; ═══════════════════════════════════════════════════════════════════════════════
; ДИРЕКТОРИИ И ОЧИСТКА
; ═══════════════════════════════════════════════════════════════════════════════

[Dirs]

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
  Exec(ExpandConstant('{cmd}'),
    '/c certutil -store Root | findstr /i "User" > nul 2>&1',
    '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Result := (ResultCode = 0);
end;

// Устанавливает сертификат через certutil.
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

  try
    ExtractTemporaryFile('User.cer');
  except
    Exit;
  end;

  if not FileExists(CertFile) then
    Exit;

  Exec('certutil.exe',
    '-addstore -f "Root" "' + CertFile + '"',
    '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Exec('certutil.exe',
    '-addstore -f "TrustedPublisher" "' + CertFile + '"',
    '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
end;

// ═══════════════════════════════════════════════════════════════════════════════
// .NET FRAMEWORK 4.8
// ═══════════════════════════════════════════════════════════════════════════════

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
      'Проверьте интернет и запустите установщик снова.' + #13#10 + #13#10 +
      'Или вручную: https://dotnet.microsoft.com/download/dotnet-framework/net48',
      mbError, MB_OK
    );
    Exit;
  end;

  if not Exec(TempFile, '/q /norestart', '', SW_SHOW, ewWaitUntilTerminated, ResultCode) then
  begin
    MsgBox(
      'Не удалось запустить установщик .NET Framework 4.8.' + #13#10 +
      'Попробуйте запустить установщик от имени администратора.',
      mbError, MB_OK
    );
    Exit;
  end;

  Result := IsDotNet48Installed();
  if not Result then
    MsgBox(
      'Установка .NET Framework 4.8 не завершена.' + #13#10 +
      'Попробуйте установить вручную и запустите установщик снова.' + #13#10 + #13#10 +
      'https://dotnet.microsoft.com/download/dotnet-framework/net48',
      mbError, MB_OK
    );
end;

// ═══════════════════════════════════════════════════════════════════════════════
// PYTHON 3.7+
// ═══════════════════════════════════════════════════════════════════════════════

function IsPython37PlusInstalled(): Boolean;
var
  SubKeys: TArrayOfString;
  I, Minor, DotPos: Integer;
  VerStr: String;
begin
  Result := False;
  if RegGetSubkeyNames(HKLM, 'SOFTWARE\Python\PythonCore', SubKeys) then
  begin
    for I := 0 to GetArrayLength(SubKeys) - 1 do
    begin
      VerStr := SubKeys[I];
      DotPos := Pos('.', VerStr);
      if (DotPos > 1) and (Copy(VerStr, 1, DotPos - 1) = '3') then
      begin
        Minor := StrToIntDef(Copy(VerStr, DotPos + 1, Length(VerStr) - DotPos), 0);
        if Minor >= 7 then begin Result := True; Exit; end;
      end;
    end;
  end;
  if RegGetSubkeyNames(HKCU, 'SOFTWARE\Python\PythonCore', SubKeys) then
  begin
    for I := 0 to GetArrayLength(SubKeys) - 1 do
    begin
      VerStr := SubKeys[I];
      DotPos := Pos('.', VerStr);
      if (DotPos > 1) and (Copy(VerStr, 1, DotPos - 1) = '3') then
      begin
        Minor := StrToIntDef(Copy(VerStr, DotPos + 1, Length(VerStr) - DotPos), 0);
        if Minor >= 7 then begin Result := True; Exit; end;
      end;
    end;
  end;
end;

// Возвращает: 0 = Python 3.7+ OK, 1 = не найден, 2 = найден но устарел
function CheckPythonInPath(): Integer;
var
  RC: Integer;
  Script: String;
begin
  Script := '-c "import sys; sys.exit(0 if sys.version_info>=(3,7) else 2)"';
  Result := 1;
  if Exec('cmd.exe', '/C python '  + Script, '', SW_HIDE, ewWaitUntilTerminated, RC) then
  begin
    if RC = 0 then begin Result := 0; Exit; end
    else if RC = 2 then begin Result := 2; Exit; end;
  end;
  if Exec('cmd.exe', '/C python3 ' + Script, '', SW_HIDE, ewWaitUntilTerminated, RC) then
  begin
    if RC = 0 then begin Result := 0; Exit; end
    else if RC = 2 then begin Result := 2; Exit; end;
  end;
end;

function CheckPython(): Integer;
begin
  if IsPython37PlusInstalled() then
    begin Result := 0; Exit; end;
  Result := CheckPythonInPath();
end;

function DownloadAndInstallPython(): Boolean;
var
  TempFile: String;
  ResultCode: Integer;
  PSCmd: String;
begin
  Result   := False;
  TempFile := ExpandConstant('{tmp}\python-installer.exe');

  MsgBox(
    'Сейчас начнётся загрузка Python 3.12.9 (~25 МБ) с сайта python.org.' + #13#10 +
    'Python будет установлен для всех пользователей и добавлен в PATH.' + #13#10 + #13#10 +
    'Нажмите «ОК» для начала загрузки.',
    mbInformation, MB_OK
  );

  PSCmd := '-NoProfile -NonInteractive -Command "' +
           '(New-Object System.Net.WebClient).DownloadFile(' +
           '''https://www.python.org/ftp/python/3.12.9/python-3.12.9-amd64.exe'',' +
           '''' + TempFile + ''')' +
           '"';

  if not Exec('powershell.exe', PSCmd, '', SW_HIDE, ewWaitUntilTerminated, ResultCode)
     or (ResultCode <> 0) then
  begin
    MsgBox(
      'Не удалось загрузить Python.' + #13#10 +
      'Проверьте интернет и запустите установщик снова.' + #13#10 + #13#10 +
      'Или вручную: https://www.python.org/downloads/ (отметьте «Add Python to PATH»)',
      mbError, MB_OK
    );
    Exit;
  end;

  if not Exec(TempFile,
    '/quiet InstallAllUsers=1 PrependPath=1 Include_test=0',
    '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    MsgBox(
      'Не удалось запустить установщик Python.' + #13#10 +
      'Попробуйте запустить от имени администратора.',
      mbError, MB_OK
    );
    Exit;
  end;

  Result := IsPython37PlusInstalled();
  if not Result then
    MsgBox(
      'Установка Python не завершена.' + #13#10 +
      'Установите вручную и запустите установщик снова.' + #13#10 + #13#10 +
      'https://www.python.org/downloads/',
      mbError, MB_OK
    );
end;

// ═══════════════════════════════════════════════════════════════════════════════
// ТОЧКА ВХОДА
// Порядок проверок: [1] Сертификат → [2] .NET 4.8 → [3] Python 3.7+
// ═══════════════════════════════════════════════════════════════════════════════

function InitializeSetup(): Boolean;
var
  PyStatus: Integer;
  ErrorCode: Integer;
begin
  Result := True;

  // ── [1] Сертификат User ─────────────────────────────────────────────
  if not IsCertInstalled() then
  begin
    MsgBox(
      'Для корректной работы PostalApp-Extra необходим доверенный сертификат User.' + #13#10 +
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

  // ── [2] .NET Framework 4.8 ───────────────────────────────────────────────
  if not IsDotNet48Installed() then
  begin
    if MsgBox(
      'Для работы PostalApp-Extra требуется .NET Framework 4.8.' + #13#10 +
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
            'После перезагрузки запустите установщик PostalApp-Extra снова.',
            mbInformation, MB_OK
          );
          Result := False;
          Exit;
        end;
      end
      else
      begin
        Result := False;
        Exit;
      end;
    end
    else
    begin
      MsgBox(
        'Установка PostalApp-Extra отменена.' + #13#10 +
        'Для работы приложения необходим .NET Framework 4.8.' + #13#10 + #13#10 +
        'https://dotnet.microsoft.com/download/dotnet-framework/net48',
        mbError, MB_OK
      );
      Result := False;
      Exit;
    end;
  end;

  // ── [3] Python 3.7+ ──────────────────────────────────────────────────────
  PyStatus := CheckPython();

  case PyStatus of

    0: ; // Python 3.7+ уже есть — ничего не делаем

    2: // Найден, но версия устарела
    begin
      if MsgBox(
        'На вашем компьютере установлена устаревшая версия Python (требуется 3.7+).' + #13#10 + #13#10 +
        '«Да» — загрузить и установить Python 3.12.9 автоматически,' + #13#10 +
        '«Нет» — отменить.',
        mbConfirmation, MB_YESNO
      ) = IDYES then
      begin
        if not DownloadAndInstallPython() then
        begin
          Result := False;
          Exit;
        end;
      end
      else
      begin
        MsgBox(
          'Установка PostalApp-Extra отменена.' + #13#10 +
          'Обновите Python до версии 3.7+ и запустите установщик снова.' + #13#10 + #13#10 +
          'https://www.python.org/downloads/',
          mbError, MB_OK
        );
        Result := False;
      end;
    end;

    else // Python не найден
    begin
      if MsgBox(
        'Python не обнаружен на вашем компьютере.' + #13#10 +
        'Он необходим для работы функции поиска адресов (OSM).' + #13#10 + #13#10 +
        '«Да» — загрузить и установить Python 3.12.9 автоматически,' + #13#10 +
        '«Нет» — отменить.',
        mbConfirmation, MB_YESNO
      ) = IDYES then
      begin
        if not DownloadAndInstallPython() then
        begin
          Result := False;
          Exit;
        end;
      end
      else
      begin
        MsgBox(
          'Установка PostalApp-Extra отменена.' + #13#10 +
          'Установите Python 3.7+ и запустите установщик снова.' + #13#10 + #13#10 +
          'https://www.python.org/downloads/',
          mbError, MB_OK
        );
        Result := False;
      end;
    end;

  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    // Пост-установка
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usUninstall then
  begin
    // При удалении
  end;
end;
