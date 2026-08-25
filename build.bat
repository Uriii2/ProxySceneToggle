@echo off
setlocal

rem Ishem kompilyator csc.exe sredi ustanovlennyh versiy .NET Framework
set "CSC="
for %%D in (
  "%WINDIR%\Microsoft.NET\Framework64\v4.0.30319"
  "%WINDIR%\Microsoft.NET\Framework\v4.0.30319"
  "%WINDIR%\Microsoft.NET\Framework64\v3.5"
  "%WINDIR%\Microsoft.NET\Framework\v3.5"
  "%WINDIR%\Microsoft.NET\Framework64\v2.0.50727"
  "%WINDIR%\Microsoft.NET\Framework\v2.0.50727"
) do if not defined CSC if exist "%%~D\csc.exe" set "CSC=%%~D\csc.exe"

if not defined CSC (
  echo.
  echo ERROR: csc.exe ne nayden. Ustanovite .NET Framework 4.0+:
  echo   https://dotnet.microsoft.com/download/dotnet-framework
  echo.
  pause
  exit /b 1
)

echo Ispolzuyu: %CSC%
echo Sborka ProxySceneToggle.exe...
"%CSC%" /nologo /codepage:65001 /target:winexe /out:ProxySceneToggle.exe /optimize /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll ProxySceneToggle.cs
if exist ProxySceneToggle.exe (echo OK: ProxySceneToggle.exe created) else (echo BUILD FAILED)
pause
