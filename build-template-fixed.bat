@echo off
echo Building CheapAvaloniaBlazor Template Package...

REM Clean any existing build artifacts
if exist "*.nupkg" del *.nupkg
if exist "nupkg" rmdir /s /q nupkg

REM Create directory structure
mkdir nupkg
mkdir nupkg\content

REM Copy all files except excluded ones using robocopy
echo Copying template files...
robocopy . nupkg\content /E /XD .git bin obj .vs nupkg temp-package /XF *.nupkg *.user exclude.txt build-template*.bat build-template*.ps1 *.nuspec README.md nuget.exe temp.nuspec

REM Ensure template.config is properly copied
if not exist "nupkg\content\.template.config" mkdir "nupkg\content\.template.config"
copy ".template.config\template.json" "nupkg\content\.template.config\" /Y

REM Create the .nuspec file for this specific package
echo ^<?xml version="1.0" encoding="utf-8"?^> > CheapAvaloniaBlazor.Template.nuspec
echo ^<package xmlns="http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd"^> >> CheapAvaloniaBlazor.Template.nuspec
echo   ^<metadata^> >> CheapAvaloniaBlazor.Template.nuspec
echo     ^<id^>CheapAvaloniaBlazor.Template^</id^> >> CheapAvaloniaBlazor.Template.nuspec
echo     ^<version^>1.0.0^</version^> >> CheapAvaloniaBlazor.Template.nuspec
echo     ^<title^>CheapAvaloniaBlazor Project Template^</title^> >> CheapAvaloniaBlazor.Template.nuspec
echo     ^<authors^>YourName^</authors^> >> CheapAvaloniaBlazor.Template.nuspec
echo     ^<description^>A project template for creating cross-platform desktop applications using Blazor Server, MudBlazor, Avalonia, and Photino^</description^> >> CheapAvaloniaBlazor.Template.nuspec
echo     ^<requireLicenseAcceptance^>false^</requireLicenseAcceptance^> >> CheapAvaloniaBlazor.Template.nuspec
echo     ^<tags^>blazor mudblazor avalonia photino desktop template^</tags^> >> CheapAvaloniaBlazor.Template.nuspec
echo     ^<packageTypes^> >> CheapAvaloniaBlazor.Template.nuspec
echo       ^<packageType name="Template" /^> >> CheapAvaloniaBlazor.Template.nuspec
echo     ^</packageTypes^> >> CheapAvaloniaBlazor.Template.nuspec
echo   ^</metadata^> >> CheapAvaloniaBlazor.Template.nuspec
echo   ^<files^> >> CheapAvaloniaBlazor.Template.nuspec
echo     ^<file src="nupkg\content\**" target="content" /^> >> CheapAvaloniaBlazor.Template.nuspec
echo   ^</files^> >> CheapAvaloniaBlazor.Template.nuspec
echo ^</package^> >> CheapAvaloniaBlazor.Template.nuspec

REM Download nuget.exe if it doesn't exist
if not exist "nuget.exe" (
    echo Downloading nuget.exe...
    powershell -Command "Invoke-WebRequest -Uri 'https://dist.nuget.org/win-x86-commandline/latest/nuget.exe' -OutFile 'nuget.exe'"
)

REM Build the package
echo Building NuGet package...
nuget.exe pack CheapAvaloniaBlazor.Template.nuspec

REM Check if package was created
if exist "CheapAvaloniaBlazor.Template.1.0.0.nupkg" (
    echo.
    echo ✓ Package created successfully: CheapAvaloniaBlazor.Template.1.0.0.nupkg
    echo.
    echo To test locally:
    echo   dotnet new install CheapAvaloniaBlazor.Template.1.0.0.nupkg
    echo   dotnet new cheapavaloniablazor -n MyTestApp
    echo.
) else (
    echo.
    echo ✗ Package creation failed!
    echo.
)

REM Clean up temporary files
del CheapAvaloniaBlazor.Template.nuspec
rmdir /s /q nupkg

pause