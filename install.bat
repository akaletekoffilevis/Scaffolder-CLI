@echo off
setlocal enabledelayedexpansion
echo Installation de Scaffolder pour Windows...
set "URL=https://github.com/akaletekoffilevis/Scaffolder-CLI/releases/latest/download/scaffold-win-x64.tar.gz"
set "TMP=%TEMP%\scaffold-install"
if exist "%TMP%" rmdir /s /q "%TMP%"
mkdir "%TMP%"
curl -L -o "%TMP%\scaffold.tar.gz" "%URL%"
tar -xzf "%TMP%\scaffold.tar.gz" -C "%TMP%"
if not exist "%TMP%\scaffold.exe" (
  echo Binaire introuvable.
  exit /b 1
)
set "DEST=%LOCALAPPDATA%\Scaffolder"
if not exist "%DEST%" mkdir "%DEST%"
copy /y "%TMP%\scaffold.exe" "%DEST%\scaffold.exe" >nul
setx PATH "%PATH%;%DEST%" >nul
echo Scaffolder installe dans %DEST%.
echo Ouvrez un nouveau terminal et lancez : scaffold --help
rmdir /s /q "%TMP%" 2>nul
