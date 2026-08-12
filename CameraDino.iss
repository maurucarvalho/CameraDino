[Setup]
AppName=Camera Dino
AppVersion=10.1
AppPublisher=Mauro Carvalho
DefaultDirName={autopf}\Camera Dino
DisableProgramGroupPage=yes
LicenseFile=LICENSE
AppMutex=CameraDinoMutex_V2
CloseApplications=force
OutputDir=Release
OutputBaseFilename=CameraDino_Setup
Compression=lzma2/ultra64
SolidCompression=yes
SetupIconFile=dino_valid.ico
UninstallDisplayIcon={app}\CameraDino.exe

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"
Name: "startmenuicon"; Description: "Create a Start Menu shortcut"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "LICENSE"; DestDir: "{app}"; Flags: ignoreversion
Source: "ffmpeg.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "go2rtc.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "go2rtc.yaml"; DestDir: "{app}"; Flags: ignoreversion
Source: "dino.png"; DestDir: "{app}"; Flags: ignoreversion
Source: "dino.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "CameraDino.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "www\*"; DestDir: "{app}\www"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{userprograms}\Camera Dino"; Filename: "{app}\CameraDino.exe"; Tasks: startmenuicon
Name: "{userdesktop}\Camera Dino"; Filename: "{app}\CameraDino.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\CameraDino.exe"; Description: "Run Camera Dino now"; Flags: postinstall skipifsilent

[UninstallRun]
Filename: "taskkill"; Parameters: "/F /IM go2rtc.exe"; Flags: runhidden; RunOnceId: "kill_go2rtc"
Filename: "taskkill"; Parameters: "/F /IM ffmpeg.exe"; Flags: runhidden; RunOnceId: "kill_ffmpeg"
Filename: "taskkill"; Parameters: "/F /IM CameraDino.exe"; Flags: runhidden; RunOnceId: "kill_cameradino"

[UninstallDelete]
Type: filesandordirs; Name: "{userappdata}\CameraDino"
Type: files; Name: "{userstartup}\CameraDino.lnk"
