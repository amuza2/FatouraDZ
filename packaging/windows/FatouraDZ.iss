; ---------------------------------------------------------------------------
;  FatouraDZ — installateur Windows (Inno Setup 6)
;
;  Construit par le job Windows de .github/workflows/release.yml :
;
;    ISCC.exe packaging/windows/FatouraDZ.iss ^
;        /DAppVersion=0.2.0 ^
;        /DSourceDir=..\artifacts\FatouraDZ-0.2.0-win-x64 ^
;        /DOutputDir=..\artifacts
;
;  Le publish doit avoir été fait avant (scripts/build-release.sh produit aussi
;  les artefacts Windows depuis Linux : le SDK .NET compile pour win-x64).
; ---------------------------------------------------------------------------

#ifndef AppVersion
  #define AppVersion "0.1.0"
#endif

#ifndef SourceDir
  ; Relatif au dossier de ce script (packaging\windows\), donc la racine du dépôt.
  #define SourceDir "..\..\artifacts\FatouraDZ-" + AppVersion + "-win-x64"
#endif

#ifndef OutputDir
  #define OutputDir "..\..\artifacts"
#endif

#define AppName "FatouraDZ"
#define AppPublisher "amuza2"
#define AppUrl "https://github.com/amuza2/FatouraDZ"
#define AppExeName "FatouraDZ.exe"

[Setup]
; L'AppId ne doit JAMAIS changer : c'est lui qui permet à une nouvelle version de
; remplacer l'installation précédente au lieu de s'installer à côté.
AppId={{9C1B4F52-6E8A-4A3D-9F1E-2B7C5D0A7E31}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppUrl}
AppSupportURL={#AppUrl}/issues
AppUpdatesURL={#AppUrl}/releases
VersionInfoVersion={#AppVersion}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
OutputDir={#OutputDir}
OutputBaseFilename={#AppName}-{#AppVersion}-windows-x64-setup
SetupIconFile=..\..\src\Assets\fatouradz.ico
UninstallDisplayIcon={app}\{#AppExeName}
LicenseFile=..\..\LICENSE
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
; L'application embarque le runtime .NET : aucune dépendance à installer.
; L'installateur ne cible que x64 ; les machines ARM64 utilisent l'archive
; portable (un installateur par architecture serait nécessaire pour inclure les
; deux charges utiles dans un seul .exe).
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog

[Languages]
Name: "french"; MessagesFile: "compiler:Languages\French.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Le dossier publié contient l'exécutable et, selon le mode de publication, des
; sous-dossiers — d'où recursesubdirs.
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{group}\{cm:UninstallProgram,{#AppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent

; Désinstallation : les fichiers installés sont retirés, mais JAMAIS
; %LOCALAPPDATA%\FatouraDZ, qui contient la base de factures, les paramètres et
; les journaux. Effacer les factures d'un utilisateur au motif qu'il désinstalle
; le logiciel serait une perte de données irréversible.
