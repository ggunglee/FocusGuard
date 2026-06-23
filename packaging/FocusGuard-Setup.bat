@echo off
setlocal EnableExtensions
chcp 65001 >nul

call :HasWebView2
if not errorlevel 1 (
    echo Microsoft Edge WebView2 Runtime is already installed.
    echo You can now run FocusGuard.exe.
    pause
    exit /b 0
)

echo Installing Microsoft Edge WebView2 Runtime...
set "INSTALLER=%TEMP%\MicrosoftEdgeWebView2Setup.exe"

powershell.exe -NoProfile -ExecutionPolicy Bypass -Command ^
  "try { Invoke-WebRequest -UseBasicParsing 'https://go.microsoft.com/fwlink/p/?LinkId=2124703' -OutFile '%INSTALLER%' } catch { Write-Error $_; exit 1 }"

if errorlevel 1 (
    echo Failed to download WebView2 Runtime.
    pause
    exit /b 1
)

start "" /wait "%INSTALLER%" /silent /install
set "INSTALL_RESULT=%ERRORLEVEL%"
del /q "%INSTALLER%" >nul 2>&1

if not "%INSTALL_RESULT%"=="0" (
    echo WebView2 Runtime installation failed. Error: %INSTALL_RESULT%
    pause
    exit /b %INSTALL_RESULT%
)

call :HasWebView2
if errorlevel 1 (
    echo WebView2 Runtime could not be detected after installation.
    echo Restart Windows and run this setup file again.
    pause
    exit /b 1
)

echo Setup completed. You can now run FocusGuard.exe.
pause
exit /b 0

:HasWebView2
for /d %%D in ("%ProgramFiles(x86)%\Microsoft\EdgeWebView\Application\*") do (
    if exist "%%~fD\msedgewebview2.exe" exit /b 0
)
for /d %%D in ("%ProgramFiles%\Microsoft\EdgeWebView\Application\*") do (
    if exist "%%~fD\msedgewebview2.exe" exit /b 0
)
for /d %%D in ("%LOCALAPPDATA%\Microsoft\EdgeWebView\Application\*") do (
    if exist "%%~fD\msedgewebview2.exe" exit /b 0
)
exit /b 1
