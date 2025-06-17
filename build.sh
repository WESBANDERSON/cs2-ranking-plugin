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

# Copy build output to release directory
echo "Copying build output..."
cp -r bin/Release/net7.0/* release/addons/cs2-ranking/

# Create build directory
mkdir -p build

dotnet build -c Release -o build

# Create deployment package directory
mkdir -p deploy/cs2-ranking
cp build/*.dll deploy/cs2-ranking/
cp build/*.deps.json deploy/cs2-ranking/
cp build/*.runtimeconfig.json deploy/cs2-ranking/

# No zip creation here
echo "Build complete! Plugin package is in release/addons/cs2-ranking and deploy/cs2-ranking" 