#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif
#ifndef PublishDir
  #define PublishDir "..\artifacts\publish"
#endif

[Setup]
AppId={{3FFB1718-9BC4-4FC4-9580-55BF58EC7183}
AppName=xShot Mirror
AppVersion={#AppVersion}
AppPublisher=xShot Mirror
DefaultDirName={autopf}\xShot Mirror
DefaultGroupName=xShot Mirror
DisableProgramGroupPage=yes
OutputDir=..\dist
OutputBaseFilename=xShotMirror-{#AppVersion}-win-x64-Setup
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.19045
PrivilegesRequired=admin
WizardStyle=modern
Compression=lzma2
SolidCompression=yes
UninstallDisplayIcon={app}\xShotMirror.exe
LicenseFile=..\LICENSE
InfoBeforeFile=user-guide.txt
CloseApplications=yes
CloseApplicationsFilter=xShotMirror.exe,uxplay.exe
RestartApplications=no
AppMutex=xShotMirror.SingleInstance
#ifdef SignCommand
SignTool=release {#SignCommand}
SignedUninstaller=yes
#endif

[Languages]
Name: "chinesesimp"; MessagesFile: "ChineseSimplified.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "创建桌面快捷方式"
Name: "firewall"; Description: "允许同一局域网的 iPhone 连接（添加本程序的防火墙规则）"

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\xShot Mirror"; Filename: "{app}\xShotMirror.exe"
Name: "{group}\使用说明"; Filename: "{app}\使用说明.txt"
Name: "{autodesktop}\xShot Mirror"; Filename: "{app}\xShotMirror.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\xShotMirror.exe"; Description: "启动 xShot Mirror"; Flags: nowait postinstall skipifsilent runasoriginaluser

[Code]
const
  RulePrefix = 'xShotMirror.3FFB1718.';

procedure OpenBonjour(Sender: TObject);
var ResultCode: Integer;
begin
  ShellExec('open', 'https://support.apple.com/zh-cn/106380', '', '', SW_SHOWNORMAL, ewNoWait, ResultCode);
end;

procedure InitializeWizard;
var Page: TWizardPage; Text: TNewStaticText; Button: TNewButton;
begin
  Page := CreateCustomPage(wpInfoBefore, 'iPhone 发现服务', '使用屏幕镜像前需要 Apple Bonjour');
  Text := TNewStaticText.Create(Page);
  Text.Parent := Page.Surface;
  Text.Left := 0;
  Text.Top := 8;
  Text.Width := Page.SurfaceWidth;
  Text.Height := ScaleY(120);
  Text.AutoSize := False;
  Text.WordWrap := True;
  if FileExists(ExpandConstant('{sys}\dnssd.dll')) and
     RegKeyExists(HKLM, 'SYSTEM\CurrentControlSet\Services\Bonjour Service') then
    Text.Caption := '已检测到 Bonjour。请确保 Windows 服务中的 Bonjour Service 正在运行。'#13#10#13#10 +
      'xShot Mirror 已包含 .NET 和视频运行库，无需另行安装。电脑与 iPhone 需要连接同一局域网。'
  else
    Text.Caption := '尚未检测到完整的 Bonjour 安装。您可以继续安装 xShot Mirror，但在使用投屏前，需要先安装 Apple Bonjour，并启动 Bonjour Service。'#13#10#13#10 +
      '本安装包不包含 Apple 软件。下面的按钮打开 Apple 官方下载说明；请按 Apple 安装器完成安装。';
  Button := TNewButton.Create(Page);
  Button.Parent := Page.Surface;
  Button.Left := 0;
  Button.Top := ScaleY(140);
  Button.Width := ScaleX(230);
  Button.Height := ScaleY(28);
  Button.Caption := '打开 Apple Bonjour 官方页面';
  Button.OnClick := @OpenBonjour;
end;

procedure RemoveRules;
var Policy: Variant;
begin
  Policy := CreateOleObject('HNetCfg.FwPolicy2');
  try Policy.Rules.Remove(RulePrefix + 'TCP'); except end;
  try Policy.Rules.Remove(RulePrefix + 'UDP'); except end;
end;

procedure AddRule(Protocol: Integer; Suffix: String);
var Policy, Rule: Variant;
begin
  Policy := CreateOleObject('HNetCfg.FwPolicy2');
  Rule := CreateOleObject('HNetCfg.FWRule');
  Rule.Name := RulePrefix + Suffix;
  Rule.Description := 'xShot Mirror - incoming connections from the local subnet only';
  Rule.ApplicationName := ExpandConstant('{app}\receiver\uxplay.exe');
  Rule.Protocol := Protocol;
  Rule.Direction := 1;
  Rule.Action := 1;
  Rule.Profiles := 7;
  Rule.RemoteAddresses := 'LocalSubnet';
  Rule.Enabled := True;
  Policy.Rules.Add(Rule);
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    try
      RemoveRules;
      if WizardIsTaskSelected('firewall') then
      begin
        AddRule(6, 'TCP');
        AddRule(17, 'UDP');
      end;
    except
      Log('Firewall configuration failed: ' + GetExceptionMessage);
      if not WizardSilent then
        MsgBox('程序已安装，但未能配置防火墙。请在 Windows 防火墙中允许安装目录 receiver 下的 uxplay.exe 接收本地子网连接。', mbInformation, MB_OK);
    end;
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usUninstall then
  begin
    try RemoveRules; except Log('Could not remove firewall rules: ' + GetExceptionMessage); end;
  end;
end;
