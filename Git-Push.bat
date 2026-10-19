@echo off
setlocal
cd /d "%~dp0"

echo ========================================
echo         GIT - PUSH CHANGES
echo ========================================
echo.

git rev-parse --is-inside-work-tree >nul 2>&1
if errorlevel 1 goto NOT_REPO

git branch --show-current
echo.

set /p "MESSAGE=Enter commit message: "

if not defined MESSAGE (
    echo ERROR: Commit message cannot be empty.
    goto END
)

echo.
echo Staging changes...
git add -A
if errorlevel 1 goto FAILED

git diff --cached --quiet
if not errorlevel 1 (
    echo No new changes to commit.
    goto CHECK_REMOTE
)

echo.
echo Creating commit...
git commit -m "%MESSAGE%"
if errorlevel 1 goto FAILED

:CHECK_REMOTE
echo.
echo Fetching remote updates...
git fetch
if errorlevel 1 goto FAILED

git rev-parse --abbrev-ref --symbolic-full-name "@{upstream}" >nul 2>&1
if errorlevel 1 (
    echo ERROR: No upstream branch configured.
    echo Set one with git push -u origin BRANCH_NAME
    goto END
)

for /f "tokens=1,2" %%A in ('git rev-list --left-right --count "HEAD...@{upstream}"') do (
    set "AHEAD=%%A"
    set "BEHIND=%%B"
)

if not "%BEHIND%"=="0" (
    echo.
    echo ERROR: Remote branch has new commits.
    echo Run Git-Pull.bat before pushing.
    goto END
)

echo.
echo Uploading changes...
git push
if errorlevel 1 goto FAILED

echo.
echo SUCCESS: Changes pushed to GitHub!
goto END

:NOT_REPO
echo ERROR: This folder is not a Git repository.
goto END

:FAILED
echo ERROR: Git operation failed.

:END
echo.
pause