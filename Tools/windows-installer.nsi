Unicode true
!include "MUI2.nsh"
Name "NIGHTRUN"
OutFile "../delivery/NIGHTRUN-Setup.exe"
InstallDir "$LOCALAPPDATA\Programs\NIGHTRUN"
RequestExecutionLevel user
SetCompressor /SOLID lzma
!define MUI_FINISHPAGE_RUN "$INSTDIR\NIGHTRUN.exe"
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_LANGUAGE "English"
Section "NIGHTRUN"
  SetOutPath "$INSTDIR"
  File /r "../Builds/Windows/*"
  WriteUninstaller "$INSTDIR\Uninstall.exe"
  CreateDirectory "$SMPROGRAMS\NIGHTRUN"
  CreateShortcut "$SMPROGRAMS\NIGHTRUN\NIGHTRUN.lnk" "$INSTDIR\NIGHTRUN.exe"
  CreateShortcut "$DESKTOP\NIGHTRUN.lnk" "$INSTDIR\NIGHTRUN.exe"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\NIGHTRUN" "DisplayName" "NIGHTRUN"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\NIGHTRUN" "UninstallString" '$\"$INSTDIR\Uninstall.exe$\"'
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\NIGHTRUN" "DisplayIcon" "$INSTDIR\NIGHTRUN.exe"
SectionEnd
Section "Uninstall"
  Delete "$DESKTOP\NIGHTRUN.lnk"
  Delete "$SMPROGRAMS\NIGHTRUN\NIGHTRUN.lnk"
  RMDir "$SMPROGRAMS\NIGHTRUN"
  DeleteRegKey HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\NIGHTRUN"
  RMDir /r "$INSTDIR\NIGHTRUN_Data"
  RMDir /r "$INSTDIR\MonoBleedingEdge"
  Delete "$INSTDIR\NIGHTRUN.exe"
  Delete "$INSTDIR\UnityPlayer.dll"
  Delete "$INSTDIR\UnityCrashHandler64.exe"
  Delete "$INSTDIR\WinPixEventRuntime.dll"
  Delete "$INSTDIR\README-Windows.txt"
  Delete "$INSTDIR\THIRD_PARTY.txt"
  Delete "$INSTDIR\Uninstall.exe"
  RMDir "$INSTDIR"
SectionEnd
