#define MyAppName "Lumos"
#define MyAppVersion "0.3.7"
#define MyAppPublisher "Lumos"
#define MyAppExeName "Lumos.exe"
#define MyAppId "B559AE28-D7A5-4E7F-A409-ED329E0880BB"

[Setup]
AppId={{{#MyAppId}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL=https://github.com/kalaiarasut/Lumos
AppSupportURL=https://github.com/kalaiarasut/Lumos/issues
AppUpdatesURL=https://github.com/kalaiarasut/Lumos/releases
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=..\artifacts\installer
OutputBaseFilename=LumosSetup-{#MyAppVersion}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
SetupIconFile=..\src\Lumos\Resources\logo.ico
InfoBeforeFile=InstallNotes.txt
CloseApplications=no
RestartApplications=no
PrivilegesRequired=lowest
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
UninstallDisplayName={#MyAppName}
UninstallDisplayIcon={app}\{#MyAppExeName}
VersionInfoDescription={#MyAppName} per-application brightness controller
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}

[Messages]
WelcomeLabel1=Welcome to the {#MyAppName} Setup Wizard
WelcomeLabel2={#MyAppName} is a lightweight tray app that remembers and restores brightness per application.%n%nIt runs quietly in the background, learns from your manual brightness changes, and stores its profiles locally on this PC.
SelectDirDesc=Choose where {#MyAppName} should be installed.
SelectTasksDesc=Choose the shortcuts you want Setup to create.
ReadyLabel1=Setup is ready to install {#MyAppName}.
ReadyLabel2a=Click Install to copy the app files and prepare {#MyAppName} for use.
FinishedHeadingLabel=Completing the {#MyAppName} Setup Wizard
FinishedLabelNoIcons=Setup has finished installing {#MyAppName}. You can launch it now, or start it later from the Start Menu.
FinishedLabel={#MyAppName} has been installed successfully. You can launch it now, or start it later from the Start Menu.

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "..\artifacts\publish\Lumos\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName} after setup"; Flags: nowait postinstall skipifsilent

[Code]
function UninstallKey(): String;
begin
  Result := 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{' + '{#MyAppId}' + '}_is1';
end;

function IsLumosInstalled(): Boolean;
var
  InstallLocation: String;
begin
  Result :=
    RegQueryStringValue(HKCU, UninstallKey(), 'InstallLocation', InstallLocation) or
    RegQueryStringValue(HKLM, UninstallKey(), 'InstallLocation', InstallLocation);
end;

function GetLumosUninstaller(var Uninstaller: String): Boolean;
begin
  Result :=
    RegQueryStringValue(HKCU, UninstallKey(), 'UninstallString', Uninstaller) or
    RegQueryStringValue(HKLM, UninstallKey(), 'UninstallString', Uninstaller);
end;

function ShowMaintenanceForm(): Integer;
var
  Form: TSetupForm;
  HeroPanel: TPanel;
  ContentPanel: TPanel;
  HeaderLabel: TNewStaticText;
  SubtitleLabel: TNewStaticText;
  VersionLabel: TNewStaticText;
  BodyLabel: TNewStaticText;
  ReinstallButton: TNewButton;
  UninstallButton: TNewButton;
  CancelButton: TNewButton;
begin
  Form := CreateCustomForm(ScaleX(680), ScaleY(470), False, True);
  try
    Form.Caption := '{#MyAppName} Setup';
    Form.Color := StrToColor('#101217');

    HeroPanel := TPanel.Create(Form);
    HeroPanel.Parent := Form;
    HeroPanel.Left := ScaleX(18);
    HeroPanel.Top := ScaleY(18);
    HeroPanel.Width := Form.ClientWidth - ScaleX(36);
    HeroPanel.Height := ScaleY(118);
    HeroPanel.Color := StrToColor('#171A22');
    HeroPanel.BevelOuter := bvNone;
    HeroPanel.ParentBackground := False;

    ContentPanel := TPanel.Create(Form);
    ContentPanel.Parent := Form;
    ContentPanel.Left := ScaleX(18);
    ContentPanel.Top := ScaleY(154);
    ContentPanel.Width := Form.ClientWidth - ScaleX(36);
    ContentPanel.Height := ScaleY(286);
    ContentPanel.Color := StrToColor('#F6F7F9');
    ContentPanel.BevelOuter := bvNone;
    ContentPanel.ParentBackground := False;

    HeaderLabel := TNewStaticText.Create(HeroPanel);
    HeaderLabel.Parent := HeroPanel;
    HeaderLabel.Left := ScaleX(24);
    HeaderLabel.Top := ScaleY(22);
    HeaderLabel.Width := HeroPanel.Width - ScaleX(48);
    HeaderLabel.Height := ScaleY(30);
    HeaderLabel.Caption := '{#MyAppName} is already installed';
    HeaderLabel.Font.Color := clWhite;
    HeaderLabel.Font.Size := 14;
    HeaderLabel.Font.Style := [fsBold];

    SubtitleLabel := TNewStaticText.Create(HeroPanel);
    SubtitleLabel.Parent := HeroPanel;
    SubtitleLabel.Left := HeaderLabel.Left;
    SubtitleLabel.Top := HeaderLabel.Top + ScaleY(36);
    SubtitleLabel.Width := HeaderLabel.Width;
    SubtitleLabel.Height := ScaleY(38);
    SubtitleLabel.Caption := 'Choose how Setup should handle the existing Lumos installation.';
    SubtitleLabel.Font.Color := StrToColor('#C8CDD8');
    SubtitleLabel.Font.Size := 10;

    VersionLabel := TNewStaticText.Create(HeroPanel);
    VersionLabel.Parent := HeroPanel;
    VersionLabel.Left := HeaderLabel.Left;
    VersionLabel.Top := ScaleY(84);
    VersionLabel.Width := HeaderLabel.Width;
    VersionLabel.Height := ScaleY(18);
    VersionLabel.Caption := 'Installer version {#MyAppVersion}';
    VersionLabel.Font.Color := StrToColor('#8F98AA');

    BodyLabel := TNewStaticText.Create(ContentPanel);
    BodyLabel.Parent := ContentPanel;
    BodyLabel.Left := ScaleX(24);
    BodyLabel.Top := ScaleY(20);
    BodyLabel.Width := ContentPanel.Width - ScaleX(48);
    BodyLabel.Height := ScaleY(42);
    BodyLabel.WordWrap := True;
    BodyLabel.Caption :=
      'Your saved profiles and settings are stored separately from the app files. Reinstalling updates Lumos without removing your brightness memory.';
    BodyLabel.Font.Color := StrToColor('#3B4252');

    ReinstallButton := TNewButton.Create(ContentPanel);
    ReinstallButton.Parent := ContentPanel;
    ReinstallButton.Style := bsCommandLink;
    ReinstallButton.Caption := 'Reinstall Lumos';
    ReinstallButton.CommandLinkHint := 'Update the installed app files to this version. Local settings and brightness profiles are kept.';
    ReinstallButton.Left := ScaleX(24);
    ReinstallButton.Top := ScaleY(78);
    ReinstallButton.Width := ContentPanel.Width - ScaleX(48);
    ReinstallButton.ModalResult := IDYES;
    ReinstallButton.Default := True;
    ReinstallButton.Font.Size := 10;
    ReinstallButton.AdjustHeightIfCommandLink;

    UninstallButton := TNewButton.Create(ContentPanel);
    UninstallButton.Parent := ContentPanel;
    UninstallButton.Style := bsCommandLink;
    UninstallButton.Caption := 'Uninstall Lumos';
    UninstallButton.CommandLinkHint := 'Open the existing Lumos uninstaller and remove the installed app.';
    UninstallButton.Left := ReinstallButton.Left;
    UninstallButton.Top := ReinstallButton.Top + ReinstallButton.Height + ScaleY(12);
    UninstallButton.Width := ReinstallButton.Width;
    UninstallButton.ModalResult := IDNO;
    UninstallButton.Font.Size := 10;
    UninstallButton.AdjustHeightIfCommandLink;

    CancelButton := TNewButton.Create(ContentPanel);
    CancelButton.Parent := ContentPanel;
    CancelButton.Style := bsCommandLink;
    CancelButton.Caption := 'Cancel setup';
    CancelButton.CommandLinkHint := 'Close this installer without changing your current Lumos installation.';
    CancelButton.Left := ReinstallButton.Left;
    CancelButton.Top := UninstallButton.Top + UninstallButton.Height + ScaleY(12);
    CancelButton.Width := ReinstallButton.Width;
    CancelButton.ModalResult := IDCANCEL;
    CancelButton.Cancel := True;
    CancelButton.Font.Size := 10;
    CancelButton.AdjustHeightIfCommandLink;

    Form.ActiveControl := ReinstallButton;
    Form.FlipAndCenterIfNeeded(True, WizardForm, False);

    Result := Form.ShowModal();
  finally
    Form.Free();
  end;
end;

function InitializeSetup(): Boolean;
var
  Choice: Integer;
  Uninstaller: String;
  ResultCode: Integer;
begin
  Result := True;

  if not IsLumosInstalled() then
    Exit;

  if WizardSilent() then
    Exit;

  Choice := ShowMaintenanceForm();

  if Choice = IDYES then
  begin
    Result := True;
  end
  else if Choice = IDNO then
  begin
    if GetLumosUninstaller(Uninstaller) then
    begin
      Exec(RemoveQuotes(Uninstaller), '', '', SW_SHOWNORMAL, ewWaitUntilTerminated, ResultCode);
    end
    else
    begin
      MsgBox('Lumos is installed, but Setup could not find the uninstaller entry.', mbError, MB_OK);
    end;

    Result := False;
  end
  else
  begin
    Result := False;
  end;
end;

procedure StopRunningLumos();
var
  ResultCode: Integer;
begin
  if FileExists(ExpandConstant('{app}\{#MyAppExeName}')) then
  begin
    Exec(ExpandConstant('{app}\{#MyAppExeName}'), '--shutdown', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    Sleep(2500);
  end;

  Exec(
    ExpandConstant('{cmd}'),
    '/C taskkill /IM "{#MyAppExeName}" /T /F',
    '',
    SW_HIDE,
    ewWaitUntilTerminated,
    ResultCode);
  Sleep(1000);
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  StopRunningLumos();
  Result := '';
end;
