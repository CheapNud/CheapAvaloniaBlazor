@echo off
echo Building CheapAvaloniaBlazor Template Package...

REM Clean any existing build artifacts
if exist "nupkg" rmdir /s /q nupkg
if exist "*.nupkg" del *.nupkg

REM Create package directory
mkdir nupkg
mkdir nupkg\content

REM Copy all files except excluded ones
echo Copying template files...
robocopy . nupkg\content /E /XD .git bin obj .vs nupkg /XF *.nupkg *.user exclude.txt build-template.bat build-template.ps1 *.nuspec README.md

REM Create the .template.config directory if it doesn't exist
if not exist "nupkg\content\.template.config" mkdir "nupkg\content\.template.config"

REM Ensure template.json is copied
copy ".template.config\template.json" "nupkg\content\.template.config\"

REM Build NuGet package
echo Building NuGet package...
nuget pack CheapAvaloniaBlazor.Template.nuspec -OutputDirectory .

echo.
echo Package created successfully!
echo.
echo To test locally:
echo   dotnet new install CheapAvaloniaBlazor.Template.1.0.0.nupkg
echo   dotnet new cheapavaloniablazor -n MyTestApp
echo.
echo To publish:
echo   nuget push CheapAvaloniaBlazor.Template.1.0.0.nupkg -Source https://api.nuget.org/v3/index.json -ApiKey YOUR_API_KEY
echo.
pause