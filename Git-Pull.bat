@echo off
setlocal
cd /d "%~dp0"

echo ========================================
echo         GIT - PULL UPDATES
echo ========================================
echo.

git rev-parse --is-inside-work-tree >nul 2>&1
if errorlevel 1 goto NOT_REPO

for /f "delims=" %%A in ('git status --porcelain') do (
    echo ERROR: You have uncommitted changes.
    echo Commit, stash, or discard them first.
    goto END
)

echo Downloading latest changes...
git pull --ff-only
if errorlevel 1 goto FAILED

echo.
echo SUCCESS: Project is up to date!
goto END

:NOT_REPO
echo ERROR: This folder is not a Git repository.
goto END

:FAILED
echo.
echo Pull failed. Your changes were not overwritten.
echo Check whether your branch has diverged.

:END
echo.
pause