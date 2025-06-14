#!/bin/bash

# MacSigner Launch Script
# Usage: ./macsigner.sh [gui|sign|version] [options...]

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$SCRIPT_DIR/macsigner.ui"

echo -e "${BLUE}MacSigner - Digital Code Signing Tool${NC}"
echo "=========================================="

# Check if .NET is installed
if ! command -v dotnet &> /dev/null; then
    echo -e "${RED}Error: .NET 6.0 SDK is required but not installed.${NC}"
    echo "Please install .NET 6.0 SDK from: https://dotnet.microsoft.com/download"
    exit 1
fi

# Check if project directory exists
if [ ! -d "$PROJECT_DIR" ]; then
    echo -e "${RED}Error: Project directory not found: $PROJECT_DIR${NC}"
    exit 1
fi

cd "$PROJECT_DIR"

# Build the project if needed
if [ ! -f "bin/Debug/net6.0/macsigner.dll" ]; then
    echo -e "${YELLOW}Building MacSigner...${NC}"
    dotnet build --configuration Debug
    if [ $? -ne 0 ]; then
        echo -e "${RED}Build failed!${NC}"
        exit 1
    fi
    echo -e "${GREEN}Build completed successfully!${NC}"
fi

# Handle different launch modes
if [ $# -eq 0 ] || [ "$1" = "gui" ]; then
    # Launch GUI mode
    echo -e "${GREEN}Launching MacSigner GUI...${NC}"
    dotnet run
elif [ "$1" = "sign" ]; then
    # Launch CLI sign command
    echo -e "${GREEN}Running MacSigner CLI...${NC}"
    shift # Remove 'sign' from arguments
    dotnet run -- sign "$@"
elif [ "$1" = "version" ]; then
    # Show version
    dotnet run -- version
elif [ "$1" = "help" ] || [ "$1" = "--help" ] || [ "$1" = "-h" ]; then
    # Show help
    echo ""
    echo "Usage: $0 [MODE] [OPTIONS]"
    echo ""
    echo "MODES:"
    echo "  gui                 Launch the GUI application (default)"
    echo "  sign [OPTIONS]      Run CLI signing command"
    echo "  version             Show version information"
    echo "  help                Show this help message"
    echo ""
    echo "GUI MODE:"
    echo "  $0"
    echo "  $0 gui"
    echo ""
    echo "CLI MODE:"
    echo "  $0 sign --path /path/to/files"
    echo "  $0 sign --path /path/to/files --verbose"
    echo "  $0 sign --help"
    echo ""
    echo "EXAMPLES:"
    echo "  $0                                    # Launch GUI"
    echo "  $0 sign --path ~/MyApp               # Sign all files in ~/MyApp"
    echo "  $0 sign --path ~/MyApp/app.exe       # Sign specific file"
    echo "  $0 version                           # Show version"
    echo ""
else
    echo -e "${RED}Error: Unknown mode '$1'${NC}"
    echo "Use '$0 help' for usage information"
    exit 1
fi
