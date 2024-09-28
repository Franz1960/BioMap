git-bash rebase-current-branch.sh
IF %ERRORLEVEL% EQU 0 exit 0
"C:\Program Files\Git\git-bash" rebase-current-branch.sh
IF %ERRORLEVEL% EQU 0 exit 0
echo "Command 'git-bash' must be in path. It is usually in 'C:\Program Files\Git'"
pause
