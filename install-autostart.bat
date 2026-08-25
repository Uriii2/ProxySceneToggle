@echo off
setlocal

rem Sozdaet yarlyk ProxySceneToggle.exe v papke avtozapuska Windows
set "EXE=%~dp0ProxySceneToggle.exe"

if not exist "%EXE%" (
  echo ERROR: ProxySceneToggle.exe ne nayden.
  echo Snachala soberite ego cherez build.bat.
  pause
  exit /b 1
)

set "STARTUP=%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup"
set "LNK=%STARTUP%\ProxySceneToggle.lnk"

powershell -NoProfile -Command ^
  "$s=(New-Object -ComObject WScript.Shell).CreateShortcut('%LNK%');" ^
  "$s.TargetPath='%EXE%';" ^
  "$s.WorkingDirectory=Split-Path '%EXE%';" ^
  "$s.Description='ProxySceneToggle';" ^
  "$s.Save()"

if exist "%LNK%" (
  echo OK: ProxySceneToggle dobavlen v avtozapusk.
  echo Yarlyk: %LNK%
) else (
  echo BUILD FAILED: yarlyk ne sozdan.
)
pause
