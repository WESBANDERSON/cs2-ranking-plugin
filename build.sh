#!/bin/bash

# Exit on error
set -e

echo "Starting build process..."

# Create necessary directories
echo "Creating directories..."
mkdir -p release/addons/cs2-ranking

# Clean previous build
echo "Cleaning previous build..."
rm -rf release/addons/cs2-ranking/*

# Build the project
echo "Building project..."
dotnet build -c Release

# Copy config file
echo "Setting up configuration..."
if [ ! -f "config.json" ]; then
    echo "No config.json found, using example config..."
    cp config.json.example release/addons/cs2-ranking/config.json
else
    echo "Using existing config.json..."
    cp config.json release/addons/cs2-ranking/
fi

# Create a zip file of the release
echo "Creating release package..."
cd release
zip -r ../CS2RankingPlugin.zip addons/
cd ..

# Verify the build
echo "Verifying build..."
if [ -f "CS2RankingPlugin.zip" ]; then
    echo "Build successful! The plugin is available in CS2RankingPlugin.zip"
    echo "Contents of the release package:"
    unzip -l CS2RankingPlugin.zip
else
    echo "Error: Build failed - CS2RankingPlugin.zip was not created"
    exit 1
fi

# Create build directory
mkdir -p build

# Build the plugin
dotnet build -c Release -o build

# Create deployment package
mkdir -p deploy/cs2-ranking
cp build/*.dll deploy/cs2-ranking/
cp build/*.deps.json deploy/cs2-ranking/
cp build/*.runtimeconfig.json deploy/cs2-ranking/

# Create zip file
cd deploy
zip -r CS2RankingPlugin.zip cs2-ranking
cd ..

echo "Build complete! Plugin package is in deploy/CS2RankingPlugin.zip" 