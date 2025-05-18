# Build Blazor Desktop App Template Package
Write-Host "Building Blazor Desktop App Template Package..." -ForegroundColor Green

# Clean any existing build artifacts
if (Test-Path "build") { Remove-Item -Recurse -Force "build" }
Get-ChildItem -Filter "*.nupkg" | Remove-Item -Force

# Create directory structure
New-Item -Type Directory -Path "build" -Force
New-Item -Type Directory -Path "build\content" -Force
New-Item -Type Directory -Path "build\contentFiles\any\any\templates" -Force
New-Item -Type Directory -Path "build\.template.config" -Force

# Copy template files (excluding build artifacts)
$exclude = Get-Content "exclude.txt"
Get-ChildItem -Recurse | Where-Object { 
    $item = $_.FullName
    $shouldExclude = $false
    foreach ($pattern in $exclude) {
        if ($item -like "*$pattern*") {
            $shouldExclude = $true
            break
        }
    }
    !$shouldExclude -and !$_.PSIsContainer
} | ForEach-Object {
    $relativePath = $_.FullName.Substring((Get-Location).Path.Length + 1)
    $destinationPath = Join-Path "build\content" $relativePath
    $destinationDir = Split-Path $destinationPath -Parent
    
    if (!(Test-Path $destinationDir)) {
        New-Item -Type Directory -Path $destinationDir -Force
    }
    
    Copy-Item $_.FullName $destinationPath
}

# Copy template configuration
Copy-Item ".template.config\template.json" "build\.template.config\"

# Copy project template files  
Copy-Item "MyTemplate.vstemplate" "build\contentFiles\any\any\templates\"

# Build NuGet package
& nuget pack BlazorDesktopApp.Template.nuspec -OutputDirectory .

Write-Host "Package created successfully!" -ForegroundColor Green
Write-Host "To install as dotnet template: dotnet new install BlazorDesktopApp.Template.1.0.0.nupkg" -ForegroundColor Yellow
Write-Host "To use template: dotnet new blazordesktop -n MyNewApp" -ForegroundColor Yellow

Read-Host "Press Enter to continue"