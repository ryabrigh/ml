@echo off
setlocal
:PROMPT
SET /P AREYOUSURE=This will sync FROM s3 and destroy any local changes. Are you sure (Y/[N])?
IF /I "%AREYOUSURE%" NEQ "Y" GOTO END

echo .. >> s3sync.log
echo -- >> s3sync.log
echo %DATE% -- %TIME% >> s3sync.log
aws s3 sync s3://rbmldevs3/ml/ . >> s3sync.log

:END
endlocal
