@Echo Off
 ::
 :: Example of what to use via Projects Properties -> Build Events -> Pre Build:
 ::
 ::	"$(ProjectDir)Properties\Versioning.cmd"
 ::

 :: Setting variables and constants that will be used later.
 ::
SET GIT="C:\Program Files\Git\bin\git.exe"
SET SFK="..\..\MPEI\Versioning\sfk.exe"
SET VersionTemplate=VersionInfo.cs.tmpl
SET VersionFile=VersionInfo.cs

 :: Changing path to home of this batch file, so that relative paths work correctly
 ::
cd /d %~dp0%

 :: Verifying that all the files exist
 ::
IF NOT EXIST %GIT% GOTO Abort
IF NOT EXIST %SFK% GOTO Abort
IF NOT EXIST "..\..\.git\config" GOTO Abort
IF NOT EXIST "%VersionTemplate%" GOTO Abort

 :: Obtain GitHub commit revision number
FOR /F "tokens=*" %%G IN ('%GIT% rev-list HEAD --count') DO (
	SET GitRevision=%%G
)
echo GitHub revision = %GitRevision%

 :: Verify GitRevision is a valid number
IF /I %GitRevision% GEQ 1 (
	goto FixRevision
)
echo ERROR: Bad revision number!
goto Abort

:FixRevision
 :: Copy VersionTemplate to VersionFile with revision obtained from GitHub
 ::
%SFK% filter "%VersionTemplate%" -replace "_$WCREV$_%GitRevision%_" > "%VersionFile%"

 :: All done, clearing ERRORLEVEL just to be sure :)
 ::
exit 0

 :: Something went wrong :(
 ::
:Abort
exit 1