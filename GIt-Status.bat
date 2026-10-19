@echo off
setlocal
cd /d "%~dp0"

echo ========================================
echo          GIT - PROJECT STATUS
echo ========================================
echo.

echo Current branch:
git branch --show-current

echo.
echo Local changes:
git status --short

echo.
echo Checking GitHub for updates...
git fetch
if errorlevel 1 (
    echo WARNING: Could not contact remote.
    goto HISTORY
)

echo.
echo Branch status:
git status -sb

:HISTORY
echo.
echo Recent commits:
git log -5 --oneline

echo.
pause