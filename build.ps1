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
        
        # Validate package contents
        Write-Host "Validating package contents..." -ForegroundColor Yellow
        
        # Extract and verify package structure
        $tempDir = Join-Path $env:TEMP "CheapAvaloniaBlazor_Validation"
        if (Test-Path $tempDir) { Remove-Item -Path $tempDir -Recurse -Force }
        
        # Extract and validate package
        try {
            Add-Type -AssemblyName System.IO.Compression.FileSystem
            [System.IO.Compression.ZipFile]::ExtractToDirectory($packageFile.FullName, $tempDir)
            
            # Check required files
            $requiredFiles = @("lib\net9.0\CheapAvaloniaBlazor.dll", "build\CheapAvaloniaBlazor.props", "build\CheapAvaloniaBlazor.targets", "README.md")
            $validationPassed = $true
            
            foreach ($file in $requiredFiles) {
                if (-not (Test-Path (Join-Path $tempDir $file))) {
                    Write-Host "ERROR: Missing required file: $file" -ForegroundColor Red
                    $validationPassed = $false
                }
            }
            
            # Check assemblies
            $dllFiles = Get-ChildItem -Path (Join-Path $tempDir "lib\net9.0") -Filter "*.dll" -ErrorAction SilentlyContinue
            if ($dllFiles.Count -gt 0) {
                Write-Host "✓ Package contains required assemblies" -ForegroundColor Green
            } else {
                Write-Host "ERROR: No assemblies found" -ForegroundColor Red
                $validationPassed = $false
            }
            
            if ($validationPassed) {
                Write-Host "✓ Package validation PASSED" -ForegroundColor Green
            } else {
                Write-Host "✗ Package validation FAILED" -ForegroundColor Red
                exit 1
            }
        }
        catch {
            Write-Host "ERROR: Package validation failed: $_" -ForegroundColor Red
            exit 1
        }
        finally {
            Remove-Item -Path $tempDir -Recurse -Force -ErrorAction SilentlyContinue
        }
        
        # Push to NuGet if requested and validation passed
        if ($Push -and $ApiKey -and $validationPassed) {
            Write-Host "Pushing to NuGet.org..." -ForegroundColor Yellow
            dotnet nuget push $packageFile.FullName --api-key $ApiKey --source https://api.nuget.org/v3/index.json
            Write-Host "Package published successfully!" -ForegroundColor Green
        }
        elseif ($Push -and -not $validationPassed) {
            Write-Host "ERROR: Cannot push package - validation failed" -ForegroundColor Red
            exit 1
        }
        elseif ($Push) {
            Write-Host "API key required to push to NuGet.org" -ForegroundColor Red
        }
    }
}

Write-Host "Build completed!" -ForegroundColor Green