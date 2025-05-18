#!/bin/bash

echo "Building CheapAvaloniaBlazor Template Package..."

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Clean any existing build artifacts
echo "Cleaning previous build artifacts..."
rm -f *.nupkg
rm -rf nupkg

# Create directory structure
mkdir -p nupkg/content

# Copy all files except excluded ones using rsync
echo "Copying template files..."
rsync -av --progress . nupkg/content/ \
    --exclude='.git' \
    --exclude='bin' \
    --exclude='obj' \
    --exclude='.vs' \
    --exclude='nupkg' \
    --exclude='temp-package' \
    --exclude='*.nupkg' \
    --exclude='*.user' \
    --exclude='exclude.txt' \
    --exclude='build-template*.bat' \
    --exclude='build-template*.sh' \
    --exclude='*.nuspec' \
    --exclude='README.md' \
    --exclude='nuget.exe'

# Ensure template.config is properly copied
mkdir -p "nupkg/content/.template.config"
cp ".template.config/template.json" "nupkg/content/.template.config/"

# Create the .nuspec file
echo "Creating nuspec file..."
cat > CheapAvaloniaBlazor.Template.nuspec << 'EOF'
<?xml version="1.0" encoding="utf-8"?>
<package xmlns="http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd">
  <metadata>
    <id>CheapAvaloniaBlazor.Template</id>
    <version>1.0.0</version>
    <title>CheapAvaloniaBlazor Project Template</title>
    <authors>CheapLudes</authors>
    <description>A project template for creating cross-platform desktop applications using Blazor Server, MudBlazor, Avalonia, and Photino</description>
    <requireLicenseAcceptance>false</requireLicenseAcceptance>
    <tags>blazor mudblazor avalonia photino desktop template</tags>
    <packageTypes>
      <packageType name="Template" />
    </packageTypes>
  </metadata>
  <files>
    <file src="nupkg/content/**" target="content" />
  </files>
</package>
EOF

# Check if dotnet is available
if ! command -v dotnet &> /dev/null; then
    echo -e "${RED}✗ .NET SDK not found! Please install .NET 8.0 SDK${NC}"
    exit 1
fi

# Build the package using dotnet pack
echo "Building NuGet package..."
dotnet pack . -p:PackageId=CheapAvaloniaBlazor.Template -p:PackageVersion=1.0.0 -p:NuspecFile=CheapAvaloniaBlazor.Template.nuspec -o .

# Alternative method using nuget if dotnet pack doesn't work with custom nuspec
if [ ! -f "CheapAvaloniaBlazor.Template.1.0.0.nupkg" ]; then
    echo "Trying alternative method with nuget..."
    
    # Download nuget.exe equivalent (mono) if needed
    if command -v mono &> /dev/null && command -v wget &> /dev/null; then
        if [ ! -f "nuget.exe" ]; then
            echo "Downloading nuget.exe..."
            wget -q https://dist.nuget.org/win-x86-commandline/latest/nuget.exe
        fi
        mono nuget.exe pack CheapAvaloniaBlazor.Template.nuspec
    else
        echo -e "${YELLOW}⚠ Alternative nuget method not available. Install mono and wget for fallback support.${NC}"
    fi
fi

# Check if package was created
if [ -f "CheapAvaloniaBlazor.Template.1.0.0.nupkg" ]; then
    echo -e "${GREEN}✓ Package created successfully: CheapAvaloniaBlazor.Template.1.0.0.nupkg${NC}"
    echo ""
    echo "To test locally:"
    echo "  dotnet new install CheapAvaloniaBlazor.Template.1.0.0.nupkg"
    echo "  dotnet new cheapavaloniablazor -n MyTestApp"
    echo ""
else
    echo -e "${RED}✗ Package creation failed!${NC}"
    echo ""
fi

# Clean up temporary files
rm -f CheapAvaloniaBlazor.Template.nuspec
rm -rf nupkg

echo "Done!"