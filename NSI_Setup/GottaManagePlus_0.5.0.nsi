;=============================================================================
; GottaManagePlus Installer
; Generated with NSI Designer 0.1.3
; (Tested with NSIS 3.11)
; https://github.com/digidigital/nsi-designer
;
; SYNOPSIS
;   GottaManagePlus-0.5.0-x86_64.exe [/S] [/NOICONS] [/LOG[=FILE]] [/D=PATH]
;
; OPTIONS
;   /S
;       Run the installer silently (no UI)
;
;   /NOICONS
;       Suppress creation of desktop and Start Menu shortcuts
;
;   /LOG[=FILE]
;       Enable logging. If FILE (Path with filename) is given, log is written there
;       If FILE is omitted, defaults to %TEMP%\GottaManagePlus_install.log
;
;   /D=PATH
;       Override installation directory
;       (must be the last argument and WITHOUT quotation marks)
;=============================================================================
; ANSI Installer

!define APPNAME "GottaManagePlus"
!define COMPANYNAME "Pixel Guy"
!ifdef VERSION
    !define VERSION "${VERSION}"
!else
    !define VERSION "0.3.0.0"
!endif
!define EXEFILE "GottaManagePlus.exe"
!define ABOUTURL "https://github.com/PixelGuy123/GottaManagePlus"
!define HELPLINK ""
!define UPDATEURL ""
!define SIZE 146832
!define COMMENTS "A Mod Manager for Baldi's Basics Plus. Made by Pixel Guy."
!define CONTACT ""
OutFile "GottaManagePlus-0.5.0-x86_64.exe"

Name "${APPNAME} ${VERSION}"
Caption "Installation Wizard"

SetCompressor /SOLID lzma
RequestExecutionLevel admin

!include "WinMessages.nsh"
!include "MUI2.nsh"
!include "FileFunc.nsh"
!include "StrFunc.nsh"
!insertmacro GetParameters
!insertmacro GetOptions
!insertmacro GetParent

!define MUI_PRODUCT "${APPNAME}"
!define MUI_VERSION "${VERSION}"
!define MUI_ICON "GMP_Icon.ico"
!define MUI_UNICON "generic.ico"
!define MUI_WELCOMEFINISHPAGE_BITMAP "generic.bmp"

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "LICENSE.rtf"
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

!insertmacro MUI_LANGUAGE "English"

BrandingText "A Mod Manager for Baldi's Basics Plus."

InstallDir "$PROGRAMFILES64\${APPNAME}"
InstallDirRegKey HKLM "Software\\${APPNAME}" "Install_Dir"

Var NOICONS
Var LOGFILE
Var LOGHANDLE
;=============================================================================
;--- Helper: write a message to log file if logging is enabled ---
Function WriteLog
  Exch $0
  StrCmp $LOGHANDLE "" 0 +3
    Exch $0
    Return
  FileWrite $LOGHANDLE "$0$\r$\n"
  Exch $0
FunctionEnd

;=============================================================================
;--- Init: parse /NOICONS and /LOG[=FILE] ---
Function .onInit
  ${GetParameters} $R0

  ClearErrors
  ${GetOptions} $R0 "/NOICONS" $R1
  IfErrors +2
    StrCpy $NOICONS "1"

  ClearErrors
  ${GetOptions} $R0 "/LOG=" $R1
  IfErrors tryPlainLog
    StrCpy $LOGFILE $R1
    Goto setupLog

  tryPlainLog:
  ClearErrors
  ${GetOptions} $R0 "/LOG" $R1
  IfErrors endLog
    StrCpy $LOGFILE "$TEMP\${APPNAME}_install.log"
    Goto setupLog

  setupLog:
    ${GetParent} $LOGFILE $R2
    CreateDirectory "$R2"
    ClearErrors
    FileOpen $LOGHANDLE $LOGFILE w
    IfErrors 0 +3
      MessageBox MB_ICONEXCLAMATION "Failed to open log file: $LOGFILE"
      StrCpy $LOGHANDLE ""
    Push "Logging enabled: $LOGFILE"
    Call WriteLog

  endLog:
FunctionEnd

;=============================================================================
Section "Install"
  SetRegView 64
  SetShellVarContext all

  Push "Installation started"
  Call WriteLog

  SetOutPath "$INSTDIR"
  ; Copy application files (recursively) from exported exe directory
  File /r "build\publish\win-x64\*.*"

  ; Write custom registry entries
  Push "Writing custom registry entries"
  Call WriteLog
  WriteRegStr HKLM "Software\\Classes\\gottamanageplus" '' 'URL:Gotta Manage Plus'
  Push 'WriteRegStr HKLM Software\\Classes\\gottamanageplus =URL:Gotta Manage Plus'
  Call WriteLog
  WriteRegStr HKLM "Software\\Classes\\gottamanageplus" 'URL Protocol' '""'
  Push 'WriteRegStr HKLM Software\\Classes\\gottamanageplus URL Protocol=""'
  Call WriteLog
  WriteRegStr HKLM "Software\\Classes\\gottamanageplus\\shell\\open\\command" '' '"${INSTDIR}\${EXEFILE}" "%1"'
  Push 'WriteRegStr HKLM Software\\Classes\\gottamanageplus\\shell\\open\\command ="${INSTDIR}\${EXEFILE}" "%1"'
  Call WriteLog
  ; Notify system about potential shell changes
  Push "Trigger ShellChangeNotify"
  Call WriteLog
  System::Call 'shell32::SHChangeNotify(i 0x08000000, i 0x0000, p 0, p 0)'

  ; Uninstall registration (Add/Remove Programs)
  SetRegView 64
  Push "Registering uninstaller in Add/Remove Programs"
  Call WriteLog
  WriteRegStr HKLM "Software\\${APPNAME}" "Install_Dir" "$INSTDIR"
  WriteRegStr HKLM "Software\\${APPNAME}" "Publisher" "${COMPANYNAME}"
  WriteRegStr HKLM "Software\\${APPNAME}" "Version" "${VERSION}"
  WriteRegStr HKLM "Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\${APPNAME}" "DisplayName" '${APPNAME} ${VERSION}'
  WriteRegStr HKLM "Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\${APPNAME}" "DisplayVersion" '${VERSION}'
  WriteRegStr HKLM "Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\${APPNAME}" "UninstallString" '"$INSTDIR\\Uninstall.exe"'
  WriteRegStr HKLM "Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\${APPNAME}" "QuietUninstallString" '"$INSTDIR\\Uninstall.exe" /S'
  WriteRegStr HKLM "Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\${APPNAME}" "DisplayIcon" '"$INSTDIR\\${EXEFILE}"'
  WriteRegStr HKLM "Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\${APPNAME}" "Publisher" '${COMPANYNAME}'
  WriteRegExpandStr HKLM "Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\${APPNAME}" "InstallLocation" '$INSTDIR'
  ; Skipping HelpLink
  WriteRegStr HKLM "Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\${APPNAME}" "URLInfoAbout" '${ABOUTURL}'
  ; Skipping Contact email
  WriteRegStr HKLM "Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\${APPNAME}" "Comments" '${COMMENTS}'
  ; Skipping UpdateURL
  WriteRegDWORD HKLM "Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\${APPNAME}" "EstimatedSize" ${SIZE}
  WriteRegDWORD HKLM "Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\${APPNAME}" "NoModify" 1
  WriteRegDWORD HKLM "Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\${APPNAME}" "NoRepair" 1
  WriteUninstaller "$INSTDIR\\Uninstall.exe"

  ; Shortcuts
  StrCmp $NOICONS "1" skipShortcuts
    Push "Creating shortcuts"
    Call WriteLog
    CreateDirectory "$SMPROGRAMS\\${APPNAME}"
    CreateShortCut "$SMPROGRAMS\\${APPNAME}\\${APPNAME}.lnk" "$INSTDIR\\${EXEFILE}"
    CreateShortCut "$DESKTOP\\${APPNAME}.lnk" "$INSTDIR\\${EXEFILE}"
  skipShortcuts:

  Push "Installation finished successfully"
  Call WriteLog
SectionEnd
;=============================================================================
Section "Uninstall"
  SetRegView 64
  SetShellVarContext all

  ; Remove shortcuts
  Delete "$SMPROGRAMS\\${APPNAME}\\${APPNAME}.lnk"
  RMDir "$SMPROGRAMS\\${APPNAME}"
  Delete "$DESKTOP\\${APPNAME}.lnk"

  ; Remove custom registry entries (mirrors install writes)
  DeleteRegValue HKLM "Software\\Classes\\gottamanageplus" ""
  DeleteRegKey /ifempty HKLM "Software\\Classes\\gottamanageplus"
  DeleteRegValue HKLM "Software\\Classes\\gottamanageplus" "URL Protocol"
  DeleteRegKey /ifempty HKLM "Software\\Classes\\gottamanageplus"
  DeleteRegValue HKLM "Software\\Classes\\gottamanageplus\\shell\\open\\command" ""
  DeleteRegKey /ifempty HKLM "Software\\Classes\\gottamanageplus\\shell\\open\\command"
  ; Notify system about potential shell changes
  System::Call 'shell32::SHChangeNotify(i 0x08000000, i 0x0000, p 0, p 0)'

  ; Remove uninstall registry keys
  SetRegView 64
  DeleteRegKey HKLM "Software\\${APPNAME}"
  DeleteRegKey HKLM "Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\${APPNAME}"

  ; Remove installed directory recursively
  RMDir /r "$INSTDIR"

SectionEnd