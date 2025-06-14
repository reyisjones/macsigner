#!/bin/bash

# MacSigner Demo Script
# This script demonstrates the CLI functionality of MacSigner

set -e

echo "🚀 MacSigner CLI Demo"
echo "===================="
echo ""

echo "📋 1. Showing version information:"
echo "   ./macsigner.sh version"
echo ""
cd /Users/reyisnieves/Dev/macsigner
./macsigner.sh version
echo ""

echo "📋 2. Showing help for sign command:"
echo "   ./macsigner.sh sign --help"
echo ""
./macsigner.sh sign --help
echo ""

echo "📋 3. Testing sign command with missing configuration:"
echo "   ./macsigner.sh sign --path /tmp/test"
echo ""
mkdir -p /tmp/test
echo "print('Hello World')" > /tmp/test/test.py
echo "echo 'Hello World'" > /tmp/test/test.sh
echo ""

# This will show the configuration error
if ./macsigner.sh sign --path /tmp/test 2>&1; then
    echo "✅ Command executed successfully"
else
    echo "❌ Command failed as expected (missing Azure configuration)"
fi

echo ""
echo "🎉 Demo completed!"
echo ""
echo "To use MacSigner in production:"
echo "1. Configure Azure Trusted Signing credentials"
echo "2. Run: ./macsigner.sh sign --path /path/to/your/files"
echo "3. Or launch GUI: ./macsigner.sh"
