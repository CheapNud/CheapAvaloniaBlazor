# Build Blazor Desktop App Template Package
Write-Host "Building Blazor Desktop App Template Package..." -ForegroundColor Green

# Clean any existing build artifacts
if (Test-Path "nupkg") { Remove-Item -Recurse -Force "nupkg" }
Get-ChildItem -Filter "*.nupkg" | Remove-Item -Force

# Create package directory
New-Item -Type Directory -Path "nupkg\content" -Force

# Define exclusions
$excludeDirs = @('.git', 'bin', 'obj', '.vs', 'nupkg')
$excludeFiles = @('*.nupkg', '*.user', 'exclude.txt', 'build-template.bat', 'build-template.ps1', '*.nuspec', 'README.md')

Write-Host "Copying template files..." -ForegroundColor Yellow

# Copy all files except excluded ones
Get-ChildItem -Recurse | Where-Object { 
    $item = $_
    $shouldExclude = $false
    
    # Check if it's in an excluded directory
    foreach ($excludeDir in $excludeDirs) {
        if ($item.FullName -like "*\$excludeDir\*" -or $item.Name -eq $excludeDir) {
            $shouldExclude = $true
            break
        }
    }
    
    # Check if it matches excluded file patterns
    if (!$shouldExclude -and !$item.PSIsContainer) {
        foreach ($excludeFile in $excludeFiles) {
            if ($item.Name -like $excludeFile) {
                $shouldExclude = $true
                break
            }
        }
    }
    
    return !$shouldExclude
} | ForEach-Object {
    if (!$_.PSIsContainer) {
        $relativePath = $_.FullName.Substring((Get-Location).Path.Length + 1)
        $destinationPath = Join-Path "nupkg\content" $relativePath
        $destinationDir = Split-Path $destinationPath -Parent
        
        if (!(Test-Path $destinationDir)) {
            New-Item -Type Directory -Path $destinationDir -Force | Out-Null
        }
        
        Copy-Item $_.FullName $destinationPath
    }
}

# Ensure template configuration is copied
if (!(Test-Path "nupkg\content\.template.config")) {
    New-Item -Type Directory -Path "nupkg\content\.template.config" -Force | Out-Null
}
Copy-Item ".template.config\template.json" "nupkg\content\.template.config\"

# Build NuGet package
Write-Host "Building NuGet package..." -ForegroundColor Yellow
& nuget pack CheapAvaloniaBlazor.nuspec -OutputDirectory .

Write-Host "`nPackage created successfully!" -ForegroundColor Green
Write-Host "`nTo test locally:" -ForegroundColor Cyan
Write-Host "  dotnet new install CheapAvaloniaBlazor.1.0.0.nupkg" -ForegroundColor Gray
Write-Host "  dotnet new cheapavaloniablazor -n MyTestApp" -ForegroundColor Gray
Write-Host "`nTo publish:" -ForegroundColor Cyan
Write-Host "  nuget push CheapAvaloniaBlazor.1.0.0.nupkg -Source https://api.nuget.org/v3/index.json -ApiKey YOUR_API_KEY" -ForegroundColor Gray
Write-Host ""

Read-Host "Press Enter to continue"