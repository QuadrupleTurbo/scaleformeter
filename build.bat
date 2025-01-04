set targetfile=%~1
set folderToUse=%~2
echo %targetfile%
xcopy /Y /S %targetfile% "F:\QDX-Test-Environment\resources\[Other Scripts]\scaleformeter\%folderToUse%"
xcopy /Y /S %targetfile% "F:\Clarity\Clarity-Servers\resources\[Scripts]\scaleformeter\%folderToUse%"