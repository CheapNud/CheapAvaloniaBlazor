@echo off
echo Building Blazor Desktop App Template Package...

REM Clean any existing build artifacts
if exist "build" rmdir /s /q build
if exist "*.nupkg" del *.nupkg

REM Create directory structure
mkdir build
mkdir build\content
mkdir build\contentFiles\any\any\templates
mkdir build\.template.config

REM Copy template files
xcopy "." "build\content\" /s /e /exclude:exclude.txt

REM Copy template configuration
copy ".template.config\template.json" "build\.template.config\"

REM Copy project template files
copy "MyTemplate.vstemplate" "build\contentFiles\any\any\templates\"

REM Build NuGet package
nuget pack CheapAvaloniaBlazor.nuspec -OutputDirectory .

echo Package created successfully!
echo To install as dotnet template: dotnet new install CheapAvaloniaBlazor.1.0.0.nupkg
echo To use template: dotnet new blazordesktop -n MyNewApp
pause