@Echo Off
 :: Verify this batchfile was given the three parameters it needs.
 ::
 ::	1 = Filename of XMP2 project file
 ::	2 = Full Path+File of the VersionInfo.cs file
 ::	3 = Full link to future location of MPE1 package without version number + extention
 ::
 :: Example of what to use via Projects Properties -> Build Events -> Post Build:
 ::
 ::	"$(ProjectDir)..\MPEI\Versioning\MPEI.Versioning.cmd" "$(ProjectName).xmp2" "http://imdbplus.googlecode.com/files/imdbplus.plugin.v" "$(ProjectDir)Properties\VersionInfo.cs"
 ::
IF "%~1%"=="" GOTO Abort
IF "%~2%"=="" GOTO Abort
IF "%~3%"=="" GOTO Abort

 :: Setting variables and constants that will be used later.
 ::
SET MPEFile=%~1%
SET MPELink=%~2%
SET VersionFile=%~3%

 :: Changing path to home of this batch file, so that relative paths work correctly
 ::
cd /d %~dp0%

 :: Verifying that all the files exist
 ::
IF NOT EXIST "..\%MPEFile%" GOTO Abort
IF NOT EXIST "%VersionFile%" GOTO Abort


 :: Scanning VersionFile for Major.Minor.Build.Revision info
 ::
FOR /F "usebackq eol=] tokens=2 delims=()" %%x IN ("%VersionFile%") DO (
	SET version=%%x
)
FOR /F "tokens=1,2,1-4 delims=." %%a IN (%version%) DO (
	SET major=%%a
	SET minor=%%b
	SET build=%%c
	SET revision=%%d
)

 :: Section of the XMP2 file that is going to be modified:
 ::
 ::  <GeneralInfo>
 ::  (snip)
 ::    <Version>
 ::      <Major>...</Major>
 ::      <Minor>...</Minor>
 ::      <Build>...</Build>
 ::      <Revision>...</Revision>
 ::    </Version>
 ::  (snip)
 ::  <OnlineLocation>http://(...).mpe1</OnlineLocation>
 ::  (snip)
 ::  </GeneralInfo>


 :: Making Backup first
 ::
IF NOT EXIST Previous mkdir Previous
move /Y "..\%MPEFile%" "Previous\%MPEFile%" > nul

 :: Building XMP2 file with new versioning
 ::
sfk filter "Previous\%MPEFile%" -inc- "*" to "<GeneralInfo>" > "..\%MPEFile%"
sfk filter "Previous\%MPEFile%" -inc "<GeneralInfo>" to "</GeneralInfo>" -replace "_<Major>*</Major>_<Major>%major%</Major>_" -replace "_<Minor>*</Minor>_<Minor>%minor%</Minor>_" -replace "_<Build>*</Build>_<Build>%build%</Build>_" -replace "_<Revision>*</Revision>_<Revision>%revision%</Revision>_" -replace "_<OnlineLocation>%MPELink%*.mpe1</OnlineLocation>_<OnlineLocation>%MPELink%%major%.%minor%.%build%.%revision%.mpe1</OnlineLocation>_" >> "..\%MPEFile%"
sfk filter "Previous\%MPEFile%" -inc- "</GeneralInfo>" to "*" >> "..\%MPEFile%"

 :: All done, clearing ERRORLEVEL set by sfk :o)
 ::
exit 0

 :: Something went wrong :(
 ::
:Abort
exit 1