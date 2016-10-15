!include "EnvVarUpdate.nsh"

; The name of the installer
Name "CsMerge"

; The file to write
OutFile "csmerge-install.exe"

; The default installation directory
InstallDir $PROGRAMFILES32\CsMerge

; Request application privileges for Windows Vista
RequestExecutionLevel admin

;--------------------------------

; Pages
Page license
Page directory
Page instfiles
LicenseText "CsMerge MIT License"
LicenseData "./license.txt"

;--------------------------------

; The stuff to install
Section "" ;No components page, name is not important

  ; Set output path to the installation directory.
  SetOutPath $INSTDIR
  
  ; Add files
  File /r ".\binaries\Release\*"

  ReadEnvStr $0 "PATH"

  StrLen $1 $0
  StrLen $2 $INSTDIR

  IntOp $3 $1 - $2

  IntCmp $3 ${NSIS_MAX_STRLEN} toobig ok toobig

  ok:
    ${EnvVarUpdate} $4 "PATH"  "R" "HKLM" $INSTDIR ; Remove
    ${EnvVarUpdate} $5 "PATH"  "A" "HKLM" $INSTDIR ; Append
    Goto done
  toobig:
    DetailPrint "Path too big to modify, please add CsMerge to path manually!"
  done:

  WriteUninstaller "uninstall.exe"
  
SectionEnd ; end the section