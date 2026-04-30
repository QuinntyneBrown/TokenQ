@echo off
setlocal EnableExtensions

REM Install the latest local build of TokenQ as a global .NET tool.
REM If TokenQ is already installed, uninstall it first.
REM
REM Layout assumed (per docs/detailed-designs/05-distribution):
REM   <repo>\src\TokenQ\TokenQ.csproj
REM   <repo>\src\TokenQ\nupkg\         (PackageOutputPath)
REM
REM This script lives at <repo>\eng\scripts\install-local.bat, so we
REM resolve the repo root relative to the script file and run from there.

set "TOOL_ID=TokenQ"
set "PROJECT=src\TokenQ\TokenQ.csproj"
set "NUPKG_DIR=src\TokenQ\nupkg"

pushd "%~dp0..\.." || goto :fail

echo.
echo === Checking for existing %TOOL_ID% installation ===
dotnet tool list --global | findstr /I /R /C:"^%TOOL_ID% " >nul
if not errorlevel 1 (
    echo Existing %TOOL_ID% found. Uninstalling...
    dotnet tool uninstall --global %TOOL_ID% || goto :fail
) else (
    echo No existing %TOOL_ID% installation.
)

echo.
echo === Packing %PROJECT% (Release) ===
if not exist "%NUPKG_DIR%" mkdir "%NUPKG_DIR%" || goto :fail
dotnet pack "%PROJECT%" -c Release -o "%NUPKG_DIR%" || goto :fail

echo.
echo === Installing %TOOL_ID% from %NUPKG_DIR% ===
dotnet tool install --global --add-source "%NUPKG_DIR%" %TOOL_ID% || goto :fail

echo.
echo === %TOOL_ID% installed. Verifying... ===
dotnet tool list --global | findstr /I /R /C:"^%TOOL_ID% " || goto :fail

popd
endlocal
exit /b 0

:fail
echo.
echo *** install-local.bat failed. ***
popd
endlocal
exit /b 1
