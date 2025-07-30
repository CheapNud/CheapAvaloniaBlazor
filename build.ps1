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
        
        try {
            # Extract package to temp directory
            Add-Type -AssemblyName System.IO.Compression.FileSystem
            [System.IO.Compression.ZipFile]::ExtractToDirectory($packageFile.FullName, $tempDir)
            
            # Check required files exist
            $requiredFiles = @(
                "lib\net9.0\CheapAvaloniaBlazor.dll",
                "build\CheapAvaloniaBlazor.props",
                "build\CheapAvaloniaBlazor.targets",
                "README.md"
            )
            
            $validationPassed = $true
            foreach ($file in $requiredFiles) {
                $fullPath = Join-Path $tempDir $file
                if (-not (Test-Path $fullPath)) {
                    Write-Host "ERROR: Missing required file: $file" -ForegroundColor Red
                    $validationPassed = $false
                }
            }
            
            # Check for embedded resources
            $resourcesPath = Join-Path $tempDir "lib\net9.0"
            if (Test-Path $resourcesPath) {
                $dllFiles = Get-ChildItem -Path $resourcesPath -Filter "*.dll" -Recurse
                if ($dllFiles.Count -gt 0) {
                    Write-Host "✓ Package contains required assemblies" -ForegroundColor Green
                } else {
                    Write-Host "ERROR: No assemblies found in package" -ForegroundColor Red
                    $validationPassed = $false
                }
            }
            
            # Validate package metadata
            $nuspecPath = Get-ChildItem -Path $tempDir -Filter "*.nuspec" -Recurse | Select-Object -First 1
            if ($nuspecPath) {
                $nuspecContent = Get-Content $nuspecPath.FullName -Raw
                $requiredMetadata = @("id", "version", "authors", "description", "license")
                foreach ($metadata in $requiredMetadata) {
                    if ($nuspecContent -notmatch "<$metadata>") {
                        Write-Host "WARNING: Missing metadata: $metadata" -ForegroundColor Yellow
                    }
                }
                Write-Host "✓ Package metadata validation completed" -ForegroundColor Green
            }
            
            if ($validationPassed) {
                Write-Host "✓ Package validation PASSED" -ForegroundColor Green
                
                # Test package installation (dry run)
                Write-Host "Testing package installation..." -ForegroundColor Yellow
                $testProject = Join-Path $env:TEMP "TestCheapAvaloniaBlazor"
                if (Test-Path $testProject) { Remove-Item -Path $testProject -Recurse -Force }
                
                dotnet new console -n "TestCheapAvaloniaBlazor" -o $testProject --force | Out-Null
                Push-Location $testProject
                
                try {
                    # Add local package source and install
                    dotnet nuget add source (Split-Path $packageFile.FullName -Parent) --name "LocalTest" | Out-Null
                    dotnet add package CheapAvaloniaBlazor --version $Version --source "LocalTest" --prerelease | Out-Null
                    
                    # Verify installation
                    if (dotnet list package | Select-String "CheapAvaloniaBlazor") {
                        Write-Host "✓ Package installation test PASSED" -ForegroundColor Green
                    } else {
                        Write-Host "ERROR: Package installation test FAILED" -ForegroundColor Red
                        $validationPassed = $false
                    }
                }
                catch {
                    Write-Host "ERROR: Package installation test failed: $_" -ForegroundColor Red
                    $validationPassed = $false
                }
                finally {
                    Pop-Location
                    Remove-Item -Path $testProject -Recurse -Force -ErrorAction SilentlyContinue
                }
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
            # Cleanup
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