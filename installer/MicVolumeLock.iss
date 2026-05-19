#define AppName "Mic Volume Lock"
#define AppVersion "1.0.3"
#define AppPublisher "Mic Volume Lock"
#define AppExeName "MicVolumeLock.exe"

[Setup]
AppId={{6C23E26A-25BF-4E75-A562-8076967502F5}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\Mic Volume Lock
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
DirExistsWarning=no
OutputDir=..\dist
OutputBaseFilename=MicVolumeLockSetup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
UninstallDisplayIcon={app}\MicVolumeLock.ico
LicenseFile=..\LICENSE
SetupIconFile=..\Assets\MicVolumeLock.ico
ShowLanguageDialog=yes
LanguageDetectionMethod=none

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"
Name: "german"; MessagesFile: "compiler:Languages\German.isl"
Name: "french"; MessagesFile: "compiler:Languages\French.isl"
Name: "italian"; MessagesFile: "compiler:Languages\Italian.isl"
Name: "polish"; MessagesFile: "compiler:Languages\Polish.isl"
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"
Name: "chinesesimplified"; MessagesFile: "compiler:Default.isl,Languages\ChineseSimplified.isl"
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"
Name: "korean"; MessagesFile: "compiler:Languages\Korean.isl"
Name: "dutch"; MessagesFile: "compiler:Languages\Dutch.isl"
Name: "indonesian"; MessagesFile: "compiler:Default.isl,Languages\Indonesian.isl"
Name: "vietnamese"; MessagesFile: "compiler:Default.isl,Languages\Vietnamese.isl"
Name: "czech"; MessagesFile: "compiler:Languages\Czech.isl"
Name: "arabic"; MessagesFile: "compiler:Languages\Arabic.isl"
Name: "hindi"; MessagesFile: "compiler:Default.isl,Languages\Hindi.isl"
Name: "romanian"; MessagesFile: "compiler:Default.isl,Languages\Romanian.isl"

[CustomMessages]
english.RunAtStartup=Run Mic Volume Lock at Windows startup
russian.RunAtStartup=Запускать Mic Volume Lock вместе с Windows
spanish.RunAtStartup=Ejecutar Mic Volume Lock al iniciar Windows
brazilianportuguese.RunAtStartup=Executar Mic Volume Lock ao iniciar o Windows
german.RunAtStartup=Mic Volume Lock mit Windows starten
french.RunAtStartup=Lancer Mic Volume Lock au démarrage de Windows
italian.RunAtStartup=Esegui Mic Volume Lock all'avvio di Windows
polish.RunAtStartup=Uruchamiaj Mic Volume Lock wraz z Windows
turkish.RunAtStartup=Mic Volume Lock'u Windows başlangıcında çalıştır
chinesesimplified.RunAtStartup=Windows 启动时运行 Mic Volume Lock
japanese.RunAtStartup=Windows 起動時に Mic Volume Lock を実行
korean.RunAtStartup=Windows 시작 시 Mic Volume Lock 실행
dutch.RunAtStartup=Mic Volume Lock starten met Windows
indonesian.RunAtStartup=Jalankan Mic Volume Lock saat Windows mulai
vietnamese.RunAtStartup=Chạy Mic Volume Lock khi Windows khởi động
czech.RunAtStartup=Spouštět Mic Volume Lock při startu Windows
arabic.RunAtStartup=تشغيل Mic Volume Lock عند بدء Windows
hindi.RunAtStartup=Windows शुरू होने पर Mic Volume Lock चलाएँ
romanian.RunAtStartup=Rulează Mic Volume Lock la pornirea Windows
english.CreateDesktopIcon=Create a desktop shortcut
russian.CreateDesktopIcon=Создать ярлык на рабочем столе
spanish.CreateDesktopIcon=Crear un acceso directo en el escritorio
brazilianportuguese.CreateDesktopIcon=Criar um atalho na área de trabalho
german.CreateDesktopIcon=Desktopverknüpfung erstellen
french.CreateDesktopIcon=Créer un raccourci sur le bureau
italian.CreateDesktopIcon=Crea un collegamento sul desktop
polish.CreateDesktopIcon=Utwórz skrót na pulpicie
turkish.CreateDesktopIcon=Masaüstü kısayolu oluştur
chinesesimplified.CreateDesktopIcon=创建桌面快捷方式
japanese.CreateDesktopIcon=デスクトップ ショートカットを作成
korean.CreateDesktopIcon=바탕 화면 바로 가기 만들기
dutch.CreateDesktopIcon=Maak een snelkoppeling op het bureaublad
indonesian.CreateDesktopIcon=Buat pintasan desktop
vietnamese.CreateDesktopIcon=Tạo lối tắt trên màn hình
czech.CreateDesktopIcon=Vytvořit zástupce na ploše
arabic.CreateDesktopIcon=إنشاء اختصار على سطح المكتب
hindi.CreateDesktopIcon=डेस्कटॉप शॉर्टकट बनाएँ
romanian.CreateDesktopIcon=Creează o comandă rapidă pe desktop
english.LaunchAfterInstall=Launch Mic Volume Lock
russian.LaunchAfterInstall=Запустить Mic Volume Lock
spanish.LaunchAfterInstall=Iniciar Mic Volume Lock
brazilianportuguese.LaunchAfterInstall=Iniciar Mic Volume Lock
german.LaunchAfterInstall=Mic Volume Lock starten
french.LaunchAfterInstall=Lancer Mic Volume Lock
italian.LaunchAfterInstall=Avvia Mic Volume Lock
polish.LaunchAfterInstall=Uruchom Mic Volume Lock
turkish.LaunchAfterInstall=Mic Volume Lock'u başlat
chinesesimplified.LaunchAfterInstall=启动 Mic Volume Lock
japanese.LaunchAfterInstall=Mic Volume Lock を起動
korean.LaunchAfterInstall=Mic Volume Lock 실행
dutch.LaunchAfterInstall=Mic Volume Lock starten
indonesian.LaunchAfterInstall=Jalankan Mic Volume Lock
vietnamese.LaunchAfterInstall=Khởi chạy Mic Volume Lock
czech.LaunchAfterInstall=Spustit Mic Volume Lock
arabic.LaunchAfterInstall=تشغيل Mic Volume Lock
hindi.LaunchAfterInstall=Mic Volume Lock शुरू करें
romanian.LaunchAfterInstall=Lansează Mic Volume Lock
english.AlreadyInstalledTitle=Mic Volume Lock is already installed
russian.AlreadyInstalledTitle=Mic Volume Lock уже установлен
spanish.AlreadyInstalledTitle=Mic Volume Lock ya está instalado
brazilianportuguese.AlreadyInstalledTitle=Mic Volume Lock já está instalado
german.AlreadyInstalledTitle=Mic Volume Lock ist bereits installiert
french.AlreadyInstalledTitle=Mic Volume Lock est déjà installé
italian.AlreadyInstalledTitle=Mic Volume Lock è già installato
polish.AlreadyInstalledTitle=Mic Volume Lock jest już zainstalowany
turkish.AlreadyInstalledTitle=Mic Volume Lock zaten yüklü
chinesesimplified.AlreadyInstalledTitle=Mic Volume Lock 已安装
japanese.AlreadyInstalledTitle=Mic Volume Lock は既にインストールされています
korean.AlreadyInstalledTitle=Mic Volume Lock이 이미 설치되어 있습니다
dutch.AlreadyInstalledTitle=Mic Volume Lock is al geïnstalleerd
indonesian.AlreadyInstalledTitle=Mic Volume Lock sudah terinstal
vietnamese.AlreadyInstalledTitle=Mic Volume Lock đã được cài đặt
czech.AlreadyInstalledTitle=Mic Volume Lock je již nainstalován
arabic.AlreadyInstalledTitle=Mic Volume Lock مثبت بالفعل
hindi.AlreadyInstalledTitle=Mic Volume Lock पहले से स्थापित है
romanian.AlreadyInstalledTitle=Mic Volume Lock este deja instalat
english.AlreadyInstalledText=Choose what you want to do.
russian.AlreadyInstalledText=Выберите, что нужно сделать.
spanish.AlreadyInstalledText=Elige lo que quieres hacer.
brazilianportuguese.AlreadyInstalledText=Escolha o que deseja fazer.
german.AlreadyInstalledText=Wählen Sie aus, was Sie tun möchten.
french.AlreadyInstalledText=Choisissez ce que vous voulez faire.
italian.AlreadyInstalledText=Scegli cosa vuoi fare.
polish.AlreadyInstalledText=Wybierz, co chcesz zrobić.
turkish.AlreadyInstalledText=Ne yapmak istediğinizi seçin.
chinesesimplified.AlreadyInstalledText=请选择要执行的操作。
japanese.AlreadyInstalledText=実行する操作を選択してください。
korean.AlreadyInstalledText=수행할 작업을 선택하세요.
dutch.AlreadyInstalledText=Kies wat u wilt doen.
indonesian.AlreadyInstalledText=Pilih tindakan yang ingin dilakukan.
vietnamese.AlreadyInstalledText=Chọn điều bạn muốn làm.
czech.AlreadyInstalledText=Vyberte, co chcete udělat.
arabic.AlreadyInstalledText=اختر ما تريد فعله.
hindi.AlreadyInstalledText=चुनें कि आप क्या करना चाहते हैं।
romanian.AlreadyInstalledText=Alege ce vrei să faci.
english.UpdateReinstall=Update / Reinstall
russian.UpdateReinstall=Обновить / Переустановить
spanish.UpdateReinstall=Actualizar / Reinstalar
brazilianportuguese.UpdateReinstall=Atualizar / Reinstalar
german.UpdateReinstall=Aktualisieren / Neu installieren
french.UpdateReinstall=Mettre à jour / Réinstaller
italian.UpdateReinstall=Aggiorna / Reinstalla
polish.UpdateReinstall=Aktualizuj / Zainstaluj ponownie
turkish.UpdateReinstall=Güncelle / Yeniden yükle
chinesesimplified.UpdateReinstall=更新 / 重新安装
japanese.UpdateReinstall=更新 / 再インストール
korean.UpdateReinstall=업데이트 / 다시 설치
dutch.UpdateReinstall=Bijwerken / Opnieuw installeren
indonesian.UpdateReinstall=Perbarui / Instal ulang
vietnamese.UpdateReinstall=Cập nhật / Cài đặt lại
czech.UpdateReinstall=Aktualizovat / Přeinstalovat
arabic.UpdateReinstall=تحديث / إعادة تثبيت
hindi.UpdateReinstall=अपडेट / पुनः स्थापित करें
romanian.UpdateReinstall=Actualizează / Reinstalează
english.UninstallOnly=Uninstall
russian.UninstallOnly=Удалить
spanish.UninstallOnly=Desinstalar
brazilianportuguese.UninstallOnly=Desinstalar
german.UninstallOnly=Deinstallieren
french.UninstallOnly=Désinstaller
italian.UninstallOnly=Disinstalla
polish.UninstallOnly=Odinstaluj
turkish.UninstallOnly=Kaldır
chinesesimplified.UninstallOnly=卸载
japanese.UninstallOnly=アンインストール
korean.UninstallOnly=제거
dutch.UninstallOnly=Verwijderen
indonesian.UninstallOnly=Copot pemasangan
vietnamese.UninstallOnly=Gỡ cài đặt
czech.UninstallOnly=Odinstalovat
arabic.UninstallOnly=إزالة التثبيت
hindi.UninstallOnly=अनइंस्टॉल करें
romanian.UninstallOnly=Dezinstalează
english.CancelSetup=Cancel
russian.CancelSetup=Отмена
spanish.CancelSetup=Cancelar
brazilianportuguese.CancelSetup=Cancelar
german.CancelSetup=Abbrechen
french.CancelSetup=Annuler
italian.CancelSetup=Annulla
polish.CancelSetup=Anuluj
turkish.CancelSetup=İptal
chinesesimplified.CancelSetup=取消
japanese.CancelSetup=キャンセル
korean.CancelSetup=취소
dutch.CancelSetup=Annuleren
indonesian.CancelSetup=Batal
vietnamese.CancelSetup=Hủy
czech.CancelSetup=Zrušit
arabic.CancelSetup=إلغاء
hindi.CancelSetup=रद्द करें
romanian.CancelSetup=Anulează

[Tasks]
Name: "startup"; Description: "{cm:RunAtStartup}"; Flags: checkedonce
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; Flags: unchecked

[Files]
Source: "..\publish\{#AppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\Assets\MicVolumeLock.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\LICENSE"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"; IconFilename: "{app}\MicVolumeLock.ico"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; IconFilename: "{app}\MicVolumeLock.ico"; Tasks: desktopicon

[Registry]
Root: HKLM; Subkey: "Software\MicVolumeLock"; ValueType: string; ValueName: "DefaultLanguage"; ValueData: "{code:GetAppLanguage}"; Flags: uninsdeletekeyifempty
Root: HKLM; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "MicVolumeLock"; ValueData: """{app}\{#AppExeName}"" --minimized"; Flags: uninsdeletevalue; Tasks: startup

[Run]
Filename: "{sys}\ie4uinit.exe"; Parameters: "-show"; Flags: runhidden skipifdoesntexist
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchAfterInstall}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "{cmd}"; Parameters: "/C taskkill /IM MicVolumeLock.exe /F /T >nul 2>nul & exit /B 0"; Flags: runhidden; RunOnceId: "StopMicVolumeLock"

[UninstallDelete]
Type: files; Name: "{app}\LICENSE"
Type: filesandordirs; Name: "{app}"

[Code]
function GetUninstallString(var UninstallString: String): Boolean;
begin
  Result := RegQueryStringValue(
    HKLM,
    'Software\Microsoft\Windows\CurrentVersion\Uninstall\{6C23E26A-25BF-4E75-A562-8076967502F5}_is1',
    'UninstallString',
    UninstallString);
end;

procedure StopRunningApp();
var
  ResultCode: Integer;
begin
  Exec(
    ExpandConstant('{cmd}'),
    '/C taskkill /IM {#AppExeName} /F /T >nul 2>nul & exit /B 0',
    '',
    SW_HIDE,
    ewWaitUntilTerminated,
    ResultCode);
end;

function ShowExistingInstallDialog(): Integer;
var
  Form: TSetupForm;
  TitleLabel: TNewStaticText;
  TextLabel: TNewStaticText;
  UpdateButton: TNewButton;
  UninstallButton: TNewButton;
  CancelButton: TNewButton;
begin
  Form := CreateCustomForm(ScaleX(440), ScaleY(170), False, True);
  try
    Form.Caption := CustomMessage('AlreadyInstalledTitle');
    Form.Position := poScreenCenter;

    TitleLabel := TNewStaticText.Create(Form);
    TitleLabel.Parent := Form;
    TitleLabel.Left := ScaleX(18);
    TitleLabel.Top := ScaleY(18);
    TitleLabel.Width := ScaleX(400);
    TitleLabel.Height := ScaleY(24);
    TitleLabel.Font.Style := [fsBold];
    TitleLabel.Caption := CustomMessage('AlreadyInstalledTitle');

    TextLabel := TNewStaticText.Create(Form);
    TextLabel.Parent := Form;
    TextLabel.Left := ScaleX(18);
    TextLabel.Top := ScaleY(52);
    TextLabel.Width := ScaleX(400);
    TextLabel.Height := ScaleY(40);
    TextLabel.Caption := CustomMessage('AlreadyInstalledText');

    UpdateButton := TNewButton.Create(Form);
    UpdateButton.Parent := Form;
    UpdateButton.Left := ScaleX(18);
    UpdateButton.Top := ScaleY(118);
    UpdateButton.Width := ScaleX(150);
    UpdateButton.Height := ScaleY(30);
    UpdateButton.Caption := CustomMessage('UpdateReinstall');
    UpdateButton.ModalResult := 1001;

    UninstallButton := TNewButton.Create(Form);
    UninstallButton.Parent := Form;
    UninstallButton.Left := ScaleX(178);
    UninstallButton.Top := ScaleY(118);
    UninstallButton.Width := ScaleX(110);
    UninstallButton.Height := ScaleY(30);
    UninstallButton.Caption := CustomMessage('UninstallOnly');
    UninstallButton.ModalResult := 1002;

    CancelButton := TNewButton.Create(Form);
    CancelButton.Parent := Form;
    CancelButton.Left := ScaleX(298);
    CancelButton.Top := ScaleY(118);
    CancelButton.Width := ScaleX(110);
    CancelButton.Height := ScaleY(30);
    CancelButton.Caption := CustomMessage('CancelSetup');
    CancelButton.ModalResult := 1003;

    Result := Form.ShowModal;
  finally
    Form.Free();
  end;
end;

function InitializeSetup(): Boolean;
var
  UninstallString: String;
  Choice: Integer;
  ResultCode: Integer;
begin
  Result := True;
  if GetUninstallString(UninstallString) then
  begin
    Choice := ShowExistingInstallDialog();
    if Choice = 1002 then
    begin
      Exec(RemoveQuotes(UninstallString), '', '', SW_SHOW, ewWaitUntilTerminated, ResultCode);
      Result := False;
    end
    else if Choice <> 1001 then
    begin
      Result := False;
    end;
  end;
end;

function GetAppLanguage(Param: String): String;
begin
  if ActiveLanguage = 'russian' then Result := 'ru-RU'
  else if ActiveLanguage = 'spanish' then Result := 'es-ES'
  else if ActiveLanguage = 'brazilianportuguese' then Result := 'pt-BR'
  else if ActiveLanguage = 'german' then Result := 'de-DE'
  else if ActiveLanguage = 'french' then Result := 'fr-FR'
  else if ActiveLanguage = 'italian' then Result := 'it-IT'
  else if ActiveLanguage = 'polish' then Result := 'pl-PL'
  else if ActiveLanguage = 'turkish' then Result := 'tr-TR'
  else if ActiveLanguage = 'chinesesimplified' then Result := 'zh-CN'
  else if ActiveLanguage = 'japanese' then Result := 'ja-JP'
  else if ActiveLanguage = 'korean' then Result := 'ko-KR'
  else if ActiveLanguage = 'dutch' then Result := 'nl-NL'
  else if ActiveLanguage = 'indonesian' then Result := 'id-ID'
  else if ActiveLanguage = 'vietnamese' then Result := 'vi-VN'
  else if ActiveLanguage = 'czech' then Result := 'cs-CZ'
  else if ActiveLanguage = 'arabic' then Result := 'ar-SA'
  else if ActiveLanguage = 'hindi' then Result := 'hi-IN'
  else if ActiveLanguage = 'romanian' then Result := 'ro-RO'
  else Result := 'en-US';
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssInstall then
  begin
    StopRunningApp();
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
  begin
    DelTree(ExpandConstant('{userappdata}\MicVolumeLock'), True, True, True);
  end;
end;


