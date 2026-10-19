@echo off
setlocal
cd /d "%~dp0"

echo ========================================
echo        GIT - UNDO LOCAL CHANGES
echo ========================================
echo.

echo WARNING!
echo This will discard ALL uncommitted changes
echo to tracked files, including staged changes.
echo.
echo Untracked files will NOT be deleted.
echo.

git status --short

echo.
set /p "CONFIRM=Type UNDO to continue: "

if not "%CONFIRM%"=="UNDO" (
    echo Cancelled.
    goto END
)

git restore --staged --worktree .
if errorlevel 1 (
    echo ERROR: Restore failed.
    goto END
)

echo.
echo SUCCESS: Tracked files restored.
echo.
git status --short

:END
echo.
pause