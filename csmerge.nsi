
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
  
SectionEnd ; end the section