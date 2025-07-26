# build.ps1 - Build script for Windows

param(
    [string]$Configuration = "Release",
    [string]$Version = "1.0.0",
    [switch]$Pack,
    [switch]$Push,
    [string]$ApiKey = ""
)

$ErrorActionPreference = "Stop"

Write-Host "Building CheapAvaloniaBlazor v$Version..." -ForegroundColor Cyan

# Clean previous builds
Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
Remove-Item -Path "bin" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path "obj" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path "*.nupkg" -Force -ErrorAction SilentlyContinue

# Restore dependencies
Write-Host "Restoring dependencies..." -ForegroundColor Yellow
dotnet restore

# Build the project
Write-Host "Building project..." -ForegroundColor Yellow
dotnet build --configuration $Configuration --no-restore /p:Version=$Version

# Run tests if they exist
if (Test-Path "tests") {
    Write-Host "Running tests..." -ForegroundColor Yellow
    dotnet test --configuration $Configuration --no-build
}

# Create NuGet package
if ($Pack) {
    Write-Host "Creating NuGet package..." -ForegroundColor Yellow
    dotnet pack --configuration $Configuration --no-build /p:Version=$Version --output .
    
    $packageFile = Get-ChildItem -Path "." -Filter "*.nupkg" | Select-Object -First 1
    
    if ($packageFile) {
        Write-Host "Package created: $($packageFile.Name)" -ForegroundColor Green
        
        # Push to NuGet if requested
        if ($Push -and $ApiKey) {
            Write-Host "Pushing to NuGet.org..." -ForegroundColor Yellow
            dotnet nuget push $packageFile.FullName --api-key $ApiKey --source https://api.nuget.org/v3/index.json
            Write-Host "Package published successfully!" -ForegroundColor Green
        }
        elseif ($Push) {
            Write-Host "API key required to push to NuGet.org" -ForegroundColor Red
        }
    }
}

Write-Host "Build completed!" -ForegroundColor Green