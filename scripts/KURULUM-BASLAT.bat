@echo off
setlocal
chcp 65001 >nul

fltmc >nul 2>&1
if not "%errorlevel%"=="0" (
    powershell.exe -NoProfile -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
    exit /b
)

if not exist "%~dp0install-player-win7.ps1" (
    echo HATA: install-player-win7.ps1 bulunamadi.
    echo ZIP dosyasini once bir klasore cikarin ve bu BAT dosyasini o klasorden calistirin.
    pause
    exit /b 1
)

set "SERVER_URL="
set /p SERVER_URL=Sunucu adresi [%SERVER_URL%]:
set /p SCREEN_CODE=Ekran kodu ^(ornek SCREEN-001^):
set /p DEVICE_TOKEN=Cihaz tokeni:

if "%SERVER_URL%"=="" (
    echo HATA: Sunucu adresi bos olamaz.
    pause
    exit /b 1
)

if "%SCREEN_CODE%"=="" (
    echo HATA: Ekran kodu bos olamaz.
    pause
    exit /b 1
)

set "SIGNAGE_SERVER_URL=%SERVER_URL%"
set "SIGNAGE_SCREEN_CODE=%SCREEN_CODE%"
set "SIGNAGE_DEVICE_TOKEN=%DEVICE_TOKEN%"

powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "& '%~dp0install-player-win7.ps1' -ServerUrl $env:SIGNAGE_SERVER_URL -ScreenCode $env:SIGNAGE_SCREEN_CODE -DeviceToken $env:SIGNAGE_DEVICE_TOKEN"
set "INSTALL_RESULT=%errorlevel%"

if not "%INSTALL_RESULT%"=="0" (
    echo.
    echo Kurulum basarisiz. Yukaridaki HATA satirini kontrol edin.
    echo Ayrintili kayit: C:\CompanySignagePublic\Player\Logs\install.log
    echo Hata kodu: %INSTALL_RESULT%
    pause
    exit /b %INSTALL_RESULT%
)

echo.
echo Kurulum tamamlandi.
pause
