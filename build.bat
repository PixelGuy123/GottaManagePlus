@echo off
setlocal enabledelayedexpansion

:: ===============
:: Build script for GottaManagePlus
:: ===============

echo ========================================
echo Building GottaManagePlus
echo ========================================

:: Read version
for /f "tokens=3 delims=<>" %%a in ('findstr "<Version>" GottaManagePlus.csproj') do set VERSION=%%a
if "%VERSION%"=="" set VERSION=0.3.0.0
echo Version: %VERSION%

:: Initialize build status flags
set "WINDOWS_BUILT=0"
set "MACOS_BUILT=0"
set "DEB_BUILT=0"
set "RPM_BUILT=0"

:: ============================================
:: 1. Publish for all platforms
:: ============================================
echo.
echo [1/4] Publishing for all platforms...
echo - Windows x64
dotnet publish GottaManagePlus.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:DebugType=none -p:DebugSymbols=false -o build/publish/win-x64
if errorlevel 1 ( echo Publish failed! exit /b 1 )

echo - macOS x64
dotnet publish GottaManagePlus.csproj -c Release -r osx-x64 --self-contained false -p:PublishSingleFile=false -p:DebugType=none -p:DebugSymbols=false -o build/publish/osx-x64
if errorlevel 1 ( echo Publish failed! exit /b 1 )

echo - Linux x64
dotnet publish GottaManagePlus.csproj -c Release -r linux-x64 --self-contained false -p:PublishSingleFile=true -p:DebugType=none -p:DebugSymbols=false -o build/publish/linux-x64
if errorlevel 1 ( echo Publish failed! exit /b 1 )

:: ============================================
:: 2. Windows - NSIS Installer
:: ============================================
echo.
echo [2/4] Creating Windows installer...

:: 传递给 makensis
makensis /DVERSION=%VERSION% "NSI_Setup/GottaManagePlus.nsi"
if errorlevel 1 (
    echo NSIS build failed!
) else (
    echo Windows installer created: build/GottaManagePlus-Setup.exe
    set "WINDOWS_BUILT=1"
)

:: ============================================
:: 3. macOS - .app Bundle
:: ============================================
echo.
echo [3/4] Creating macOS bundle...
call :create_macos_bundle
if errorlevel 1 (
    echo macOS bundle creation failed!
    exit /b 1
) else (
    set "MACOS_BUILT=1"
)

:: Zip the macOS app
echo.
echo Zipping macOS app...
for /f "delims=" %%i in ('wsl wslpath -a "%CD%\build"') do set "WSL_BUILD_DIR=%%i"
wsl tar -czf "%WSL_BUILD_DIR%/GottaManagePlus.app.tar.gz" -C "%WSL_BUILD_DIR%" "GottaManagePlus.app"
if errorlevel 1 (
    echo Failed to zip macOS app.
) else (
    echo macOS app zipped: build/GottaManagePlus.app.tar.gz
)

:: ============================================
:: 4. Linux - .deb and .rpm (via WSL2)
:: ============================================
echo.
echo [4/4] Creating Linux packages (via WSL2)...
wsl ls /proc/version >nul 2>nul
if errorlevel 1 (
    echo WARNING: WSL2 not detected or not accessible. Skipping Linux packages.
    echo Please ensure WSL2 is installed and configured.
) else (
    call :create_linux_packages
)

:: ============================================
:: Build Summary
:: ============================================
echo.
echo ========================================
echo Build Summary:
if %WINDOWS_BUILT%==1 echo   [OK] Windows installer: build/GottaManagePlus-Setup.exe
if %MACOS_BUILT%==1 echo   [OK] macOS app bundle: build/GottaManagePlus.app (and .tar.gz)
if %DEB_BUILT%==1 echo   [OK] Debian package: build/gottamanageplus_%VERSION%_amd64.deb
if %RPM_BUILT%==1 echo   [OK] RPM package: build/gottamanageplus-%VERSION%-1.x86_64.rpm
echo ========================================
exit /b 0

:: ============================================
:: Subroutine: Create macOS Bundle
:: ============================================
:create_macos_bundle
set BUNDLE_NAME=GottaManagePlus
set BUNDLE_ID=com.PixelGuy.GottaManagePlus
set APP_DIR=build\%BUNDLE_NAME%.app
set CONTENTS_DIR=%APP_DIR%\Contents
set MACOS_DIR=%CONTENTS_DIR%\MacOS
set RESOURCES_DIR=%CONTENTS_DIR%\Resources

rmdir /s /q "%APP_DIR%" 2>nul
mkdir "%MACOS_DIR%" 2>nul
mkdir "%RESOURCES_DIR%" 2>nul

xcopy /e /i /y "build\publish\osx-x64\*" "%MACOS_DIR%\" >nul
copy "GMP_Icon.ico" "%RESOURCES_DIR%\%BUNDLE_NAME%.icns" >nul 2>nul

(
echo ^<?xml version="1.0" encoding="UTF-8"?^>
echo ^<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd"^>
echo ^<plist version="1.0"^>
echo ^<dict^>
echo ^  ^<key^>CFBundleDevelopmentRegion^</key^>
echo ^  ^<string^>en^</string^>
echo ^  ^<key^>CFBundleExecutable^</key^>
echo ^  ^<string^>GottaManagePlus^</string^>
echo ^  ^<key^>CFBundleIdentifier^</key^>
echo ^  ^<string^>%BUNDLE_ID%^</string^>
echo ^  ^<key^>CFBundleInfoDictionaryVersion^</key^>
echo ^  ^<string^>6.0^</string^>
echo ^  ^<key^>CFBundleName^</key^>
echo ^  ^<string^>GottaManagePlus^</string^>
echo ^  ^<key^>CFBundleDisplayName^</key^>
echo ^  ^<string^>Gotta Manage Plus^</string^>
echo ^  ^<key^>CFBundlePackageType^</key^>
echo ^  ^<string^>APPL^</string^>
echo ^  ^<key^>CFBundleShortVersionString^</key^>
echo ^  ^<string^>%VERSION%^</string^>
echo ^  ^<key^>CFBundleVersion^</key^>
echo ^  ^<string^>%VERSION%^</string^>
echo ^  ^<key^>CFBundleIconFile^</key^>
echo ^  ^<string^>GottaManagePlus.icns^</string^>
echo ^  ^<key^>NSHighResolutionCapable^</key^>
echo ^  ^<true/^>
echo ^  ^<key^>NSPrincipalClass^</key^>
echo ^  ^<string^>NSApplication^</string^>
echo ^  ^<key^>CFBundleURLTypes^</key^>
echo ^  ^<array^>
echo ^    ^<dict^>
echo ^      ^<key^>CFBundleURLSchemes^</key^>
echo ^      ^<array^>
echo ^        ^<string^>gottamanageplus^</string^>
echo ^      ^</array^>
echo ^      ^<key^>CFBundleURLName^</key^>
echo ^      ^<string^>%BUNDLE_ID%.urlscheme^</string^>
echo ^    ^</dict^>
echo ^  ^</array^>
echo ^</dict^>
echo ^</plist^>
) > "%CONTENTS_DIR%\Info.plist"

for /f "delims=" %%i in ('wsl wslpath -a "%MACOS_DIR%\GottaManagePlus"') do set "MACOS_EXE=%%i"
wsl chmod +x "%MACOS_EXE%"
echo macOS bundle created: %APP_DIR%
exit /b 0

:: ============================================
:: Subroutine: Create Linux Packages (.deb and .rpm)
:: ============================================
:create_linux_packages
echo Creating Linux packages...

:: ---- Check prerequisites inside WSL ----
wsl dpkg-deb --version >nul 2>&1 || ( echo ERROR: dpkg-deb not found in WSL. Install with: sudo apt install dpkg-dev & exit /b 1 )
wsl rpmbuild --version >nul 2>&1 || ( echo ERROR: rpmbuild not found in WSL. Install with: sudo apt install rpm & exit /b 1 )

:: ---- Prepare paths ----
set "WIN_PUBLISH=build\publish\linux-x64"
set "WIN_BUILD=build"
set "WIN_ICON=GMP_Icon.ico"
for /f "delims=" %%i in ('wsl wslpath -a "%CD%\%WIN_PUBLISH%"') do set "WSL_PUBLISH=%%i"
for /f "delims=" %%i in ('wsl wslpath -a "%CD%\%WIN_BUILD%"') do set "WSL_BUILD=%%i"
for /f "delims=" %%i in ('wsl wslpath -a "%CD%\%WIN_ICON%"') do set "WSL_ICON=%%i"

:: ---- Create temporary working directories inside WSL ----
set "WSL_TMP=/tmp/gmp_build_%RANDOM%"
wsl mkdir -p "%WSL_TMP%/deb_package/usr/local/bin"
wsl mkdir -p "%WSL_TMP%/deb_package/usr/share/applications"
wsl mkdir -p "%WSL_TMP%/deb_package/usr/share/pixmaps"
wsl mkdir -p "%WSL_TMP%/deb_package/DEBIAN"

wsl mkdir -p "%WSL_TMP%/rpmbuild/BUILD"
wsl mkdir -p "%WSL_TMP%/rpmbuild/RPMS"
wsl mkdir -p "%WSL_TMP%/rpmbuild/SOURCES"
wsl mkdir -p "%WSL_TMP%/rpmbuild/SPECS"
wsl mkdir -p "%WSL_TMP%/rpmbuild/SRPMS"

wsl mkdir -p "%WSL_TMP%/rpmbuild/SOURCES/gottamanageplus-%VERSION%/usr/local/bin"
wsl mkdir -p "%WSL_TMP%/rpmbuild/SOURCES/gottamanageplus-%VERSION%/usr/share/applications"
wsl mkdir -p "%WSL_TMP%/rpmbuild/SOURCES/gottamanageplus-%VERSION%/usr/share/pixmaps"

:: ---- Copy binary and icon ----
wsl cp "%WSL_PUBLISH%/GottaManagePlus" "%WSL_TMP%/deb_package/usr/local/bin/"
wsl cp "%WSL_PUBLISH%/GottaManagePlus" "%WSL_TMP%/rpmbuild/SOURCES/gottamanageplus-%VERSION%/usr/local/bin/"
wsl cp "%WSL_ICON%" "%WSL_TMP%/deb_package/usr/share/pixmaps/gottamanageplus.png"
wsl cp "%WSL_ICON%" "%WSL_TMP%/rpmbuild/SOURCES/gottamanageplus-%VERSION%/usr/share/pixmaps/gottamanageplus.png"

:: ---- Generate .desktop file ----
set "DESKTOP_FILE=%TEMP%\gottamanageplus.desktop"
(
echo [Desktop Entry]
echo Version=%VERSION%
echo Type=Application
echo Name=GottaManagePlus
echo Comment=Gotta Manage Plus Application
echo Exec=/usr/local/bin/GottaManagePlus
echo Icon=gottamanageplus
echo Terminal=false
echo Categories=Utility;
echo StartupNotify=true
echo MimeType=x-scheme-handler/gottamanageplus;
) > "%DESKTOP_FILE%"

for /f "delims=" %%i in ('wsl wslpath -a "%DESKTOP_FILE%"') do set "WSL_DESKTOP=%%i"
wsl cp "%WSL_DESKTOP%" "%WSL_TMP%/deb_package/usr/share/applications/gottamanageplus.desktop"
wsl cp "%WSL_DESKTOP%" "%WSL_TMP%/rpmbuild/SOURCES/gottamanageplus-%VERSION%/usr/share/applications/gottamanageplus.desktop"
del "%DESKTOP_FILE%" 2>nul

:: ---- Generate DEBIAN/control ----
set "CONTROL_FILE=%TEMP%\control"
(
echo Package: gottamanageplus
echo Version: %VERSION%
echo Section: utils
echo Priority: optional
echo Architecture: amd64
echo Maintainer: PixelGuy ^<support@pixelguy.com^>
echo Description: GottaManagePlus - A management utility
echo  GottaManagePlus is a cross-platform management application
echo  built with Avalonia UI and .NET 10.
echo Installed-Size: 10240
) > "%CONTROL_FILE%"

for /f "delims=" %%i in ('wsl wslpath -a "%CONTROL_FILE%"') do set "WSL_CONTROL=%%i"
wsl cp "%WSL_CONTROL%" "%WSL_TMP%/deb_package/DEBIAN/control"
del "%CONTROL_FILE%" 2>nul

:: ---- Set correct permissions for deb ----
wsl chmod 755 "%WSL_TMP%/deb_package/usr/local/bin/GottaManagePlus"
wsl chmod -R 755 "%WSL_TMP%/deb_package/DEBIAN"
wsl chmod 644 "%WSL_TMP%/deb_package/usr/share/applications/gottamanageplus.desktop"
wsl chmod 644 "%WSL_TMP%/deb_package/usr/share/pixmaps/gottamanageplus.png"

:: ---- Build .deb ----
wsl dpkg-deb --build "%WSL_TMP%/deb_package" "%WSL_BUILD%/gottamanageplus_%VERSION%_amd64.deb"
if errorlevel 1 (
    echo dpkg-deb failed.
    exit /b 1
) else (
    set "DEB_BUILT=1"
)

:: ---- Build .rpm ----
:: Create source tarball
wsl tar -czf "%WSL_TMP%/rpmbuild/SOURCES/gottamanageplus-%VERSION%.tar.gz" -C "%WSL_TMP%/rpmbuild/SOURCES" "gottamanageplus-%VERSION%"

:: ---- Generate .spec file ----
set "SPEC_FILE=%TEMP%\gottamanageplus.spec"
(
echo Name: gottamanageplus
echo Version: %VERSION%
echo Release: 1%%{?dist}
echo Summary: GottaManagePlus - A management utility
echo License: MIT
echo URL: https://github.com/PixelGuy/GottaManagePlus
echo Vendor: PixelGuy
echo Packager: PixelGuy ^<support@pixelguy.com^>
echo BuildArch: x86_64
echo Source0: gottamanageplus-%%{version}.tar.gz
echo.
echo %%description
echo GottaManagePlus is a cross-platform management application
echo built with Avalonia UI and .NET 10.
echo.
echo %%prep
echo %%setup -n gottamanageplus-%%{version}
echo.
echo %%build
echo # Nothing to build
echo.
echo %%install
echo rm -rf %%{buildroot}
echo mkdir -p %%{buildroot}/usr
echo cp -a usr/* %%{buildroot}/usr/
echo.
echo %%files
echo /usr/local/bin/GottaManagePlus
echo /usr/share/applications/gottamanageplus.desktop
echo /usr/share/pixmaps/gottamanageplus.png
echo.
echo %%post
echo update-desktop-database
echo.
echo %%postun
echo update-desktop-database
) > "%SPEC_FILE%"

for /f "delims=" %%i in ('wsl wslpath -a "%SPEC_FILE%"') do set "WSL_SPEC=%%i"
wsl cp "%WSL_SPEC%" "%WSL_TMP%/rpmbuild/SPECS/gottamanageplus.spec"
del "%SPEC_FILE%" 2>nul

:: ---- Convert spec file to Unix (LF) line endings ----
wsl sed -i 's/\r$//' "%WSL_TMP%/rpmbuild/SPECS/gottamanageplus.spec"

:: ---- Build RPM ----
wsl rpmbuild --define "_topdir %WSL_TMP%/rpmbuild" --define "_dbpath %WSL_TMP%/rpmdb" --nodeps -bb "%WSL_TMP%/rpmbuild/SPECS/gottamanageplus.spec"
if errorlevel 1 (
    echo rpmbuild failed.
    exit /b 1
) else (
    set "RPM_BUILT=1"
)

:: ---- Copy the resulting RPM to Windows build ----
wsl cp "%WSL_TMP%/rpmbuild/RPMS/x86_64/gottamanageplus-%VERSION%-1.*.rpm" "%WSL_BUILD%/"

:: ---- Cleanup ----
wsl rm -rf "%WSL_TMP%"
echo Linux packages created.
exit /b 0