@echo off
REM ============================================
REM   Battery Monitor - Build Script
REM   Auto-detects newest BatteryMonitor_XX.Y.cs
REM   Copies -> compiles -> renames exe -> cleans up
REM ============================================

echo ============================================
echo   Battery Monitor - Build Script
echo ============================================

REM ---- STEP 1: Find the newest BatteryMonitor_XX.Y.cs ----
set NEWEST_SRC=
for /f "delims=" %%F in ('dir /b /o:n BatteryMonitor_*.cs 2^>nul') do (
    set NEWEST_SRC=%%F
)

if "%NEWEST_SRC%"=="" (
    echo ERROR: No BatteryMonitor_XX.Y.cs file found in this folder.
    pause
    exit /b 1
)

echo Found newest source: %NEWEST_SRC%

REM ---- Extract version string from filename ----
set VER=%NEWEST_SRC:BatteryMonitor_=%
set VER=%VER:.cs=%
echo Version: %VER%

REM ---- STEP 2: Copy to BatteryMonitor.cs ----
echo Copying %NEWEST_SRC% -> BatteryMonitor.cs ...
copy /y "%NEWEST_SRC%" BatteryMonitor.cs >nul

REM ---- STEP 3: Locate compiler ----
set CSC_2022_64=C:\Program Files\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe
set CSC_2022_32=C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe
set CSC_COMMUNITY_64=C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\Roslyn\csc.exe
set CSC_COMMUNITY_32=C:\Program Files (x86)\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\Roslyn\csc.exe
set CSC_FALLBACK=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe

set CSC=
if exist "%CSC_2022_64%"      set CSC=%CSC_2022_64%
if exist "%CSC_2022_32%"      set CSC=%CSC_2022_32%
if exist "%CSC_COMMUNITY_64%" set CSC=%CSC_COMMUNITY_64%
if exist "%CSC_COMMUNITY_32%" set CSC=%CSC_COMMUNITY_32%
if exist "%CSC_FALLBACK%"     set CSC=%CSC_FALLBACK%

if "%CSC%"=="" (
    echo ERROR: No C# compiler found!
    del BatteryMonitor.cs >nul 2>&1
    pause
    exit /b 1
)

REM ---- STEP 4: Compile ----
echo Compiling BatteryMonitor.cs ...

set ICON_PARAM=
if exist battery.ico (
    set ICON_PARAM=/win32icon:battery.ico
    echo Using icon: battery.ico
) else (
    echo No battery.ico found - building without custom icon
)

set MANIFEST_PARAM=
if exist app.manifest (
    set MANIFEST_PARAM=/win32manifest:app.manifest
    echo Using manifest: app.manifest  [requests Admin elevation + WMI access]
) else (
    echo WARNING: app.manifest not found - battery temperature will not be available
)

"%CSC%" /target:winexe /out:BatteryMonitor.exe %ICON_PARAM% %MANIFEST_PARAM% /reference:System.dll /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Management.dll BatteryMonitor.cs

if %ERRORLEVEL% NEQ 0 (
    echo ============================================
    echo   BUILD FAILED - check errors above
    echo   BatteryMonitor.cs kept for diagnostics
    echo ============================================
    pause
    exit /b 1
)

REM ---- STEP 5: Rename exe ----
set EXE_OUT=BatteryMonitor_%VER%.exe
if exist "%EXE_OUT%" del "%EXE_OUT%" >nul
rename BatteryMonitor.exe "%EXE_OUT%"
echo Renamed to: %EXE_OUT%

REM ---- STEP 6: Cleanup ----
del BatteryMonitor.cs >nul
echo Cleaned up BatteryMonitor.cs

echo ============================================
echo   BUILD SUCCESSFUL
echo   Output: %EXE_OUT%
echo ============================================

pause
